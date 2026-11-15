using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// Sincroniza el juego con la API de integradora-web (cuenta, guardado, sesiones, eventos y logros).
// El juego sigue funcionando sin red ni cuenta: el guardado local es el principal y esto solo
// lo copia a la nube. Lo pendiente se guarda en CloudOutbox y se manda al volver la conexión.
public class CloudSync : MonoBehaviour
{
    public const int Slot = 1;
    private const int SessionBatch = 50;
    private const int EventBatch = 100;
    private const int EventsBeforeFlush = 25;
    private const float RetrySeconds = 60f;
    // Una sesión más corta (entrar y salir) no aporta al historial.
    private const int MinSessionSeconds = 5;

    // Logros que el juego desbloquea en el capítulo 1 (catálogo achievements de la API).
    public const string FirstMaterial = "first_material";
    public const string FirstCraft = "first_craft";
    public const string SkyHunter = "sky_hunter_10";
    public const string FirstPermit = "first_permit";
    private const int SkyHunterKills = 10;

    public static CloudSync Instance { get; private set; }

    public CloudSession Session { get; private set; }
    public string ApiRoot { get; private set; }

    // Mensaje corto para la UI de cuenta del menú.
    public string Status { get; private set; } = "";
    public event Action StatusChanged;

    private CloudOutbox outbox;
    private GameFlow boundFlow;
    private bool flushing;
    private bool flushAgain;
    private bool restoring;
    private float nextRetryAt;

    private bool playing;
    private string playId;
    private DateTime playStartedAt;
    private float playActiveSeconds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("CloudSync");
        DontDestroyOnLoad(go);
        go.AddComponent<CloudSync>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var config = CloudConfig.Load();
        ApiRoot = config.ApiRoot;
        Session = new CloudSession(new CloudApiClient(ApiRoot), new FileCloudTokenStore());
        Session.Changed += UpdateStatus;
        outbox = new CloudOutbox();
        outbox.Load();

        PlayStats.Recorded += OnPlayEvent;
        Application.quitting += OnQuitting;
        Debug.Log($"[Cloud] API: {ApiRoot}");
    }

    private void Start()
    {
        BindFlow();
        // La primera escena pudo avisar SceneReady antes de que este objeto existiera.
        if (SceneContext.Current != null) OnSceneReady(SceneContext.Current);
        UpdateStatus();
        Run(RestoreAndSync());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        PlayStats.Recorded -= OnPlayEvent;
        Application.quitting -= OnQuitting;
        if (boundFlow != null)
        {
            boundFlow.SaveWritten -= OnSaveWritten;
            boundFlow.SceneReady -= OnSceneReady;
        }
    }

    private void Update()
    {
        if (boundFlow != GameFlow.Instance) BindFlow();

        var context = SceneContext.Current;
        if (playing && Time.timeScale > 0f && context != null && context.IsGameplayZone)
            playActiveSeconds += Time.unscaledDeltaTime;

        if (Time.unscaledTime < nextRetryAt) return;
        if (Session.IsSignedIn && outbox.State.HasPending && !flushing)
        {
            nextRetryAt = Time.unscaledTime + RetrySeconds;
            Run(Flush());
        }
        else if (!Session.IsSignedIn && Session.HasStoredSession && !restoring)
        {
            // Había sesión pero no hubo red al abrir: se reintenta sin pedir la contraseña.
            nextRetryAt = Time.unscaledTime + RetrySeconds;
            Run(RestoreAndSync());
        }
    }

    // ---------------------------------------------------------------- cuenta

    // null si entró; si no, el mensaje para el jugador.
    public async Awaitable<string> Login(string email, string password)
    {
        string error = await Session.Login(email, password);
        if (error == null) await AfterSignIn();
        UpdateStatus();
        return error;
    }

    public async Awaitable Logout()
    {
        // Lo pendiente se intenta mandar antes de cerrar la sesión.
        if (Session.IsSignedIn && outbox.State.HasPending) await Flush();
        await Session.Logout();
        UpdateStatus();
    }

    private async Awaitable RestoreAndSync()
    {
        if (restoring || !Session.HasStoredSession) return;
        restoring = true;
        try
        {
            if (await Session.Restore()) await AfterSignIn();
            else nextRetryAt = Time.unscaledTime + RetrySeconds;
        }
        finally
        {
            restoring = false;
            UpdateStatus();
        }
    }

    private async Awaitable AfterSignIn()
    {
        outbox.BindPlayer(Session.Player.Id);
        outbox.Save();
        Debug.Log($"[Cloud] Sesión iniciada: {Session.Player.Username}");
        await SyncDown();
        await Flush();
    }

    // Al entrar: si la nube tiene una partida más reciente (otro equipo) y estamos en el menú, se baja.
    // Si la local es más reciente, se sube. Así "Continuar" funciona en cualquier PC con la cuenta.
    private async Awaitable SyncDown()
    {
        var flow = GameFlow.Instance;
        var local = flow != null ? flow.LoadLocalSave() : null;
        var response = await Session.Send("GET", $"/account/saves/{Slot}/data");
        var state = outbox.State;

        if (response.Ok)
        {
            var cloud = response.As<CloudSaveData>();
            state.SaveVersion = cloud.Version;
            var menu = FindFirstObjectByType<MainMenuController>();
            bool cloudIsNewer = local == null || CloudSaveMapper.IsNewer(cloud.ExtraData?.SavedAtUtc, local.savedAtUtc);

            if (cloudIsNewer && menu != null && flow != null)
            {
                var downloaded = CloudSaveMapper.ToLocal(cloud, Slot);
                if (flow.WriteLocalSave(downloaded))
                {
                    local = downloaded;
                    state.SavePending = false;
                    menu.RefreshSaveState();
                    Debug.Log($"[Cloud] Partida descargada de la nube (versión {cloud.Version}).");
                }
            }
            else if (local != null && CloudSaveMapper.IsNewer(local.savedAtUtc, cloud.ExtraData?.SavedAtUtc))
            {
                state.SavePending = true;
            }
        }
        else if (response.Status == 404)
        {
            state.SaveVersion = 0;
            state.SavePending = local != null;
        }
        else
        {
            Debug.LogWarning($"[Cloud] No se pudo leer la partida de la nube ({(response.NetworkError ? "sin red" : response.Status.ToString())}).");
        }

        if (local != null) QueueAchievementsFrom(local);
        outbox.Save();
    }

    // ---------------------------------------------------------------- juego

    private void BindFlow()
    {
        if (boundFlow != null)
        {
            boundFlow.SaveWritten -= OnSaveWritten;
            boundFlow.SceneReady -= OnSceneReady;
        }
        boundFlow = GameFlow.Instance;
        if (boundFlow != null)
        {
            boundFlow.SaveWritten += OnSaveWritten;
            boundFlow.SceneReady += OnSceneReady;
        }
    }

    private void OnSceneReady(SceneContext context)
    {
        if (context.IsGameplayZone) StartPlay();
        else EndPlay();

        var menu = FindFirstObjectByType<MainMenuController>();
        if (menu != null && FindFirstObjectByType<CloudAccountPanel>() == null)
            CloudAccountPanel.Create(menu.transform.root);
    }

    private void OnSaveWritten(SaveData data)
    {
        outbox.State.SavePending = true;
        outbox.Save();
        if (Session.IsSignedIn) Run(Flush());
        UpdateStatus();
    }

    private void OnPlayEvent(PlayEvent e)
    {
        outbox.AddEvent(new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Type = e.Type,
            Data = e.Data,
            OccurredAt = e.OccurredAtUtc
        });
        QueueAchievementsFrom(e);
        outbox.Save();
        if (Session.IsSignedIn && outbox.State.Events.Count >= EventsBeforeFlush) Run(Flush());
    }

    private void StartPlay()
    {
        if (playing) return;
        playing = true;
        playId = Guid.NewGuid().ToString();
        playStartedAt = DateTime.UtcNow;
        playActiveSeconds = 0f;
    }

    private void EndPlay()
    {
        if (!playing) return;
        playing = false;
        int duration = Mathf.FloorToInt(playActiveSeconds);
        if (duration < MinSessionSeconds) return;

        outbox.AddSession(new CloudPlaySession
        {
            Id = playId,
            StartedAt = playStartedAt,
            EndedAt = DateTime.UtcNow,
            DurationSec = duration,
            Platform = PlatformName(),
            GameVersion = GameVersion()
        });
        outbox.Save();
        if (Session.IsSignedIn) Run(Flush());
        UpdateStatus();
    }

    // Al cerrar no hay tiempo para la red: la sesión queda en la cola y se manda al volver a abrir.
    private void OnQuitting() => EndPlay();

    // ---------------------------------------------------------------- logros

    private void QueueAchievementsFrom(PlayEvent e)
    {
        var s = SessionState.Instance;
        switch (e.Type)
        {
            case PlayStats.PickupType: Unlock(FirstMaterial, e.OccurredAtUtc); break;
            case PlayStats.CraftType: Unlock(FirstCraft, e.OccurredAtUtc); break;
            case PlayStats.EnemyDefeatedType:
                if (s != null && s.enemiesDefeated >= SkyHunterKills) Unlock(SkyHunter, e.OccurredAtUtc);
                break;
            case PlayStats.MilestoneType:
                if (e.Data.TryGetValue("milestoneId", out var id) && (string)id == "permit_obtained") Unlock(FirstPermit, e.OccurredAtUtc);
                break;
        }
    }

    // Partidas jugadas antes de iniciar sesión también cuentan para los logros.
    private void QueueAchievementsFrom(SaveData save)
    {
        var at = CloudSaveMapper.ParseUtc(save.savedAtUtc) ?? DateTime.UtcNow;
        var stats = save.stats ?? new StatsData();
        if (stats.materialsCollected > 0) Unlock(FirstMaterial, at);
        if (stats.itemsCrafted > 0) Unlock(FirstCraft, at);
        if (stats.enemiesDefeated >= SkyHunterKills) Unlock(SkyHunter, at);
        if (save.progress.flags != null && save.progress.flags.Contains("permit_obtained")) Unlock(FirstPermit, at);
    }

    private void Unlock(string id, DateTime at)
    {
        if (outbox.AddAchievement(id, at)) Debug.Log($"[Cloud] Logro desbloqueado: {id}");
    }

    // ---------------------------------------------------------------- envío

    private async Awaitable Flush()
    {
        if (flushing)
        {
            flushAgain = true;
            return;
        }
        if (!Session.IsSignedIn) return;

        flushing = true;
        UpdateStatus();
        try
        {
            do
            {
                flushAgain = false;
                if (!await FlushOnce())
                {
                    nextRetryAt = Time.unscaledTime + RetrySeconds;
                    break;
                }
            } while (flushAgain && Session.IsSignedIn);
        }
        finally
        {
            flushing = false;
            outbox.Save();
            UpdateStatus();
        }
    }

    // false = hay que reintentar más tarde (sin red, servidor caído o sesión vencida).
    private async Awaitable<bool> FlushOnce()
    {
        var state = outbox.State;

        // 1. El guardado va primero: sesiones y eventos necesitan que la ranura exista en el servidor.
        if (state.SavePending)
        {
            var local = GameFlow.Instance != null ? GameFlow.Instance.LoadLocalSave() : null;
            if (local == null) state.SavePending = false;
            else if (!await UploadSave(local)) return false;
        }

        // 2. Sesiones de juego
        while (state.Sessions.Count > 0)
        {
            var batch = state.Sessions.Take(SessionBatch).ToList();
            var response = await Session.Send("POST", $"/account/saves/{Slot}/sessions", batch);
            if (response.Ok) state.Sessions.RemoveRange(0, batch.Count);
            else if (response.Status == 404) break; // todavía no hay guardado en la nube
            else if (response.Status == 400) Drop(state.Sessions, batch.Count, "sesiones", response);
            else return false;
            outbox.Save();
        }

        // 3. Eventos de telemetría
        while (state.Events.Count > 0 && state.SaveVersion > 0)
        {
            var batch = state.Events.Take(EventBatch).ToList();
            var response = await Session.Send("POST", $"/account/saves/{Slot}/events", batch);
            if (response.Ok) state.Events.RemoveRange(0, batch.Count);
            else if (response.Status == 404) break;
            else if (response.Status == 400) Drop(state.Events, batch.Count, "eventos", response);
            else return false;
            outbox.Save();
        }

        // 4. Logros (por cuenta, no necesitan guardado)
        while (state.AchievementsPending.Count > 0)
        {
            var a = state.AchievementsPending[0];
            var response = await Session.Send("PUT", $"/account/achievements/{a.Id}", new CloudUnlockRequest { UnlockedAt = a.UnlockedAt, Slot = Slot });
            if (response.Ok) state.AchievementsSent.Add(a.Id);
            else if (response.Status != 404 && response.Status != 400) return false;
            state.AchievementsPending.RemoveAt(0);
            outbox.Save();
        }

        state.LastSyncUtc = DateTime.UtcNow;
        return true;
    }

    private async Awaitable<bool> UploadSave(SaveData local)
    {
        var state = outbox.State;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var body = CloudSaveMapper.ToCloud(local, state.SaveVersion);
            var response = await Session.Send("PUT", $"/account/saves/{Slot}/data", body);
            if (response.Ok)
            {
                state.SaveVersion = response.As<CloudSaveAccepted>().Version;
                state.SavePending = false;
                Debug.Log($"[Cloud] Partida guardada en la nube (versión {state.SaveVersion}).");
                return true;
            }

            var problem = response.Problem;
            if (response.Status == 409 && problem?.ServerVersion != null)
            {
                // Otro equipo guardó mientras se jugaba aquí: gana lo que se acaba de jugar en este equipo.
                Debug.Log($"[Cloud] Conflicto de versión (servidor {problem.ServerVersion}); se reemplaza con la partida local.");
                state.SaveVersion = problem.ServerVersion.Value;
                continue;
            }
            if (response.Status == 400)
            {
                // Datos que el servidor nunca aceptará: reintentar no sirve.
                Debug.LogWarning($"[Cloud] El servidor rechazó el guardado: {response.Body}");
                state.SavePending = false;
                return true;
            }
            return false;
        }
        return false;
    }

    private static void Drop<T>(List<T> list, int count, string what, CloudResponse response)
    {
        Debug.LogWarning($"[Cloud] El servidor rechazó {count} {what}; se descartan: {response.Body}");
        list.RemoveRange(0, count);
    }

    // ---------------------------------------------------------------- estado

    private void UpdateStatus()
    {
        string status;
        if (Session.State == CloudSessionState.Restoring || restoring) status = "Conectando con la cuenta…";
        else if (!Session.IsSignedIn)
            status = Session.HasStoredSession
                ? "Sin conexión con el servidor. Se reintenta solo."
                : "Sin sesión: el progreso se guarda solo en este equipo.";
        else if (flushing) status = "Sincronizando…";
        else if (outbox.State.HasPending) status = "Hay progreso pendiente de subir.";
        else status = "Progreso guardado en la nube.";

        if (status == Status) return;
        Status = status;
        StatusChanged?.Invoke();
    }

    private static void Run(Awaitable task) => _ = Observe(task);

    // Los errores de una tarea en segundo plano se registran en vez de perderse.
    private static async Awaitable Observe(Awaitable task)
    {
        try { await task; }
        catch (Exception e) { Debug.LogException(e); }
    }

    private static string PlatformName()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor: return "WINDOWS";
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor: return "LINUX";
            default: return "MACOS";
        }
    }

    private static string GameVersion()
    {
        string v = Application.version;
        return !string.IsNullOrEmpty(v) && Regex.IsMatch(v, "^[0-9A-Za-z.+-]{1,30}$") ? v : null;
    }
}
