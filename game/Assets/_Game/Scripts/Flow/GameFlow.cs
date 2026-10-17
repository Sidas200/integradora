using System;
using System.Collections;
using UnityEngine;

// Flujo del capítulo: nueva partida, continuar, guardar en punto seguro, derrota y reintento.
public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    [Header("Escenas (rutas, las asigna el builder)")]
    [SerializeField] private string mainMenuScene;
    [SerializeField] private string introScene;
    [SerializeField] private string firstZoneScene;
    [SerializeField] private string firstZoneSpawn;
    [SerializeField] private string demoEndScene;

    [Header("Derrota")]
    [SerializeField] private float defeatDelay = 1.2f;
    [SerializeField] private DefeatScreen defeatScreen;

    private ISaveRepository repository;
    private Health playerHealth;

    // Foto de la sesión al entrar a una zona mientras todavía no hay punto seguro.
    private SaveData entrySnapshot;
    private string entryScene;
    private string entrySpawn;

    public event Action<string> Saved; // id del punto seguro

    public string DemoEndScene => demoEndScene;

    private void Awake()
    {
        Instance = this;
        repository = new LocalSaveRepository();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- menú

    public bool HasValidSave() => LoadUsableSave() != null;

    // Un guardado que apunta a una escena que ya no está en el build (p. ej. la antigua 03_Camp)
    // se trata como inválido: nunca debe romper "Continuar" ni bloquear la nueva partida (SRS).
    private SaveData LoadUsableSave()
    {
        var data = repository.Load();
        if (data == null) return null;
        string scene = data.checkpoint.scene;
        if (!string.IsNullOrEmpty(scene) && UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(scene) < 0)
        {
            Debug.LogWarning($"El guardado apunta a una escena que ya no existe ({scene}); se ignora.");
            return null;
        }
        return data;
    }

    public void NewGame()
    {
        Time.timeScale = 1f;
        SessionState.Instance.ResetState();
        entrySnapshot = null;
        // Un guardado dañado o de otra versión nunca bloquea la nueva partida (SRS)
        try { repository.Delete(); }
        catch (Exception e) { Debug.LogWarning($"No se pudo borrar el guardado anterior: {e.Message}"); }
        SceneLoader.Instance.LoadSceneFresh(introScene, null);
    }

    public void StartChapter()
    {
        SceneLoader.Instance.LoadSceneFresh(firstZoneScene, firstZoneSpawn);
    }

    public bool Continue()
    {
        var data = LoadUsableSave();
        if (data == null) return false;
        entrySnapshot = null;
        SceneLoader.Instance.LoadFromSave(data);
        return true;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneLoader.Instance.LoadSceneFresh(mainMenuScene, null);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GoToDemoEnd()
    {
        SceneLoader.Instance.LoadScene(demoEndScene, null);
    }

    // ---------------------------------------------------------------- guardado

    // HEX-08: guarda en disco el estado actual y marca el punto seguro.
    public bool SaveAtSafePoint(string safePointId, Vector3 position)
    {
        var session = SessionState.Instance;
        session.SyncFromPlayer();
        session.SaveSafePoint(safePointId, position);
        try
        {
            repository.Save(SaveData.FromSession(session));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"No se pudo guardar: {e.Message}");
            return false;
        }
        entrySnapshot = null;
        Saved?.Invoke(safePointId);
        return true;
    }

    // ---------------------------------------------------------------- derrota

    // HEX-07: reintento desde el último punto seguro con el estado que tenía al guardar.
    public void Retry()
    {
        Time.timeScale = 1f;
        var data = LoadUsableSave();
        if (data != null && !string.IsNullOrEmpty(data.checkpoint.scene))
        {
            data.match.health = -1; // reaparece con vida completa
            SceneLoader.Instance.LoadFromSave(data);
            return;
        }

        if (entrySnapshot != null)
        {
            var session = SessionState.Instance;
            entrySnapshot.ApplyTo(session);
            session.currentHealth = -1;
            session.respawnAtSafePoint = false;
            SceneLoader.Instance.LoadSceneFresh(entryScene, entrySpawn);
            return;
        }

        SessionState.Instance.ResetState();
        StartChapter();
    }

    public void OnSceneReady(SceneContext context, string scenePath, string spawnId)
    {
        if (defeatScreen != null) defeatScreen.Hide();
        BindPlayer();

        if (!context.IsGameplayZone) return;
        var session = SessionState.Instance;
        if (session != null && !session.HasSafePoint)
        {
            session.SyncFromPlayer();
            entrySnapshot = SaveData.FromSession(session);
            entryScene = scenePath;
            entrySpawn = spawnId;
        }
    }

    private void BindPlayer()
    {
        if (playerHealth != null) playerHealth.Died -= HandlePlayerDied;
        var player = SceneContext.FindPlayer();
        playerHealth = player != null ? player.GetComponent<Health>() : null;
        if (playerHealth != null) playerHealth.Died += HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (InventoryScreen.Instance != null) InventoryScreen.Instance.Close();
        GameInput.Lock(this);
        StartCoroutine(ShowDefeat());
    }

    private IEnumerator ShowDefeat()
    {
        yield return new WaitForSecondsRealtime(defeatDelay);
        GameInput.Unlock(this);
        if (defeatScreen != null) defeatScreen.Show();
    }
}
