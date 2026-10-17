using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Va una por escena del capítulo. Asegura que existan los sistemas persistentes,
// coloca al jugador en el punto de aparición pedido y avisa al HUD y al flujo del juego.
[DefaultExecutionOrder(-200)]
public class SceneContext : MonoBehaviour
{
    public const string SystemsResource = "Chapter1/GameSystems";

    public static SceneContext Current { get; private set; }

    [Tooltip("Nombre de la zona que se muestra al entrar (vacío = no se muestra).")]
    [SerializeField] private string zoneName;
    [SerializeField] private string defaultSpawnId;
    [SerializeField] private bool showHud = true;
    [SerializeField] private bool allowPause = true;
    [Tooltip("Las escenas jugables guardan una foto de entrada para reintentar si aún no hay punto seguro.")]
    [SerializeField] private bool isGameplayZone = true;

    public string ZoneName => zoneName;
    public bool ShowHud => showHud;
    public bool AllowPause => allowPause;
    public bool IsGameplayZone => isGameplayZone;

    public void Configure(string zone, string spawn, bool hud, bool pause, bool gameplay)
    {
        zoneName = zone;
        defaultSpawnId = spawn;
        showHud = hud;
        allowPause = pause;
        isGameplayZone = gameplay;
    }

    private void Awake()
    {
        Current = this;
        EnsureSystems();

        var session = SessionState.Instance;
        if (session != null && !session.respawnAtSafePoint)
        {
            string spawnId = string.IsNullOrEmpty(session.pendingSpawnId) ? defaultSpawnId : session.pendingSpawnId;
            MovePlayerTo(SpawnPoint.Find(spawnId));
        }
    }

    private IEnumerator Start()
    {
        // Espera a que SessionSync.Start restaure el estado (y el punto seguro si aplica)
        yield return null;
        var session = SessionState.Instance;
        string arrivedAt = session != null ? session.pendingSpawnId : null;
        if (session != null) session.pendingSpawnId = null;

        SnapCamera();
        if (GameHud.Instance != null) GameHud.Instance.OnSceneReady(this);
        if (GameFlow.Instance != null) GameFlow.Instance.OnSceneReady(this, SceneManager.GetActiveScene().path, arrivedAt ?? defaultSpawnId);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    public static void EnsureSystems()
    {
        if (GameSystems.Instance != null) return;
        var prefab = Resources.Load<GameObject>(SystemsResource);
        if (prefab == null)
        {
            Debug.LogError($"No se encontró Resources/{SystemsResource}. Ejecuta Tools/Claude/Build Chapter 1.");
            return;
        }
        Instantiate(prefab).name = "GameSystems";
    }

    public static GameObject FindPlayer() => GameObject.FindGameObjectWithTag("Player");

    public static void MovePlayerTo(SpawnPoint spawn)
    {
        if (spawn == null) return;
        var player = FindPlayer();
        if (player == null) return;

        player.transform.position = spawn.transform.position;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = spawn.transform.position;
            rb.linearVelocity = Vector2.zero;
        }
        SnapCamera();
    }

    private static void SnapCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.SnapToTarget();
    }
}
