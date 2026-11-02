using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enemigos de campo (capítulo 1). Se crea solo en cada escena; no hay que ponerlo a mano.
// - Cada zona de juego con enemigos que reaparecen suma uno extra, copia de uno de ellos
//   (nunca de uno que suelte un objeto clave, para no dar el objeto dos veces).
// - Un enemigo vencido vuelve a su lugar cuando el jugador se aleja: pasó un rato y su punto
//   quedó fuera de la cámara. Así, al regresar, la zona vuelve a estar poblada.
// No toca a los de la prueba del Santuario (KeepDefeated), al guardián ni a sus invocaciones.
public class EnemyRespawner : MonoBehaviour
{
    [Tooltip("Enemigos de más por zona.")]
    [SerializeField, Min(0)] private int extraEnemies = 1;
    [Tooltip("Segundos mínimos entre que muere y reaparece.")]
    [SerializeField] private float respawnDelay = 20f;
    [Tooltip("Distancia mínima entre el jugador y el punto donde reaparece.")]
    [SerializeField] private float minPlayerDistance = 3f;
    [Tooltip("Margen fuera de la pantalla (fracción de la vista) para que no aparezca a la vista.")]
    [SerializeField] private float offscreenMargin = 0.15f;

    private static readonly Vector2[] SpotOffsets =
    {
        new Vector2(0.45f, 0f), new Vector2(-0.45f, 0f), new Vector2(0f, 0.35f), new Vector2(0f, -0.35f),
        new Vector2(0.35f, 0.3f), new Vector2(-0.35f, 0.3f), new Vector2(0.35f, -0.3f), new Vector2(-0.35f, -0.3f),
    };

    private class Entry
    {
        public EnemyHealth health;
        public EnemyLoot loot;
        public Vector3 home;
        public float diedAt = -1f;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private Transform player;
    private float nextCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Al dar Play directamente en una zona, esa primera escena ya cargó
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        // Uno por escena (la primera escena puede llegar por Bootstrap y por sceneLoaded)
        foreach (var existing in FindObjectsByType<EnemyRespawner>(FindObjectsInactive.Include))
            if (existing.gameObject.scene == scene) return;

        var go = new GameObject("EnemyRespawner");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<EnemyRespawner>();
    }

    private void Start()
    {
        var context = SceneContext.Current;
        if (context == null || !context.IsGameplayZone || context.gameObject.scene != gameObject.scene)
        {
            Destroy(gameObject);
            return;
        }

        foreach (var loot in FindObjectsByType<EnemyLoot>(FindObjectsInactive.Exclude))
        {
            if (loot.gameObject.scene != gameObject.scene || !loot.Respawns) continue;
            if (loot.GetComponent<GuardianBoss>() != null) continue;
            var health = loot.GetComponent<EnemyHealth>();
            if (health == null || health.IsDead) continue;
            entries.Add(new Entry { health = health, loot = loot, home = loot.transform.position });
        }

        if (entries.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        AddExtraEnemies();
    }

    private void AddExtraEnemies()
    {
        var templates = new List<Entry>();
        foreach (var e in entries)
            if (!e.loot.HasKeyItemDrop) templates.Add(e);
        if (templates.Count == 0) return;

        // Orden fijo para que siempre se copie el mismo (y su id no cambie entre partidas)
        templates.Sort((a, b) => string.CompareOrdinal(IdOf(a.loot), IdOf(b.loot)));

        for (int i = 0; i < extraEnemies; i++)
        {
            var template = templates[i % templates.Count];
            Vector3 spot = FreeSpotNear(template.home);
            var source = template.health.gameObject;
            var clone = Instantiate(source, spot, source.transform.rotation, source.transform.parent);
            clone.name = source.name + "_Extra" + (i + 1);

            // Id propio antes de su Start, para que guarde su propio estado de vencido y botín
            if (clone.TryGetComponent<PersistentId>(out var pid))
                pid.SetId(IdOf(template.loot) + "_extra" + (i + 1));

            entries.Add(new Entry
            {
                health = clone.GetComponent<EnemyHealth>(),
                loot = clone.GetComponent<EnemyLoot>(),
                home = spot
            });
        }
    }

    private static string IdOf(EnemyLoot loot) =>
        loot.TryGetComponent<PersistentId>(out var pid) ? pid.Id : loot.name;

    // Un punto libre junto al enemigo copiado (sin paredes ni otros cuerpos sólidos)
    private static Vector3 FreeSpotNear(Vector3 home)
    {
        foreach (var offset in SpotOffsets)
        {
            Vector3 p = home + (Vector3)offset;
            bool blocked = false;
            foreach (var c in Physics2D.OverlapCircleAll(p, 0.12f))
            {
                if (!c.isTrigger) { blocked = true; break; }
            }
            if (!blocked) return p;
        }
        return home + (Vector3)SpotOffsets[0];
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.25f;

        if (player == null)
        {
            var go = SceneContext.FindPlayer();
            if (go == null) return;
            player = go.transform;
        }

        foreach (var e in entries)
        {
            if (e.health == null) continue;
            if (!e.health.IsDead)
            {
                e.diedAt = -1f;
                continue;
            }

            // Time.time: la pausa, los diálogos y el inventario no cuentan
            if (e.diedAt < 0f) e.diedAt = Time.time;
            if (Time.time - e.diedAt < respawnDelay) continue;
            if (e.loot != null && !e.loot.Respawns) continue;
            if (!FarFromPlayer(e.home)) continue;

            Respawn(e);
        }
    }

    private bool FarFromPlayer(Vector3 home)
    {
        if (Vector2.Distance(player.position, home) < minPlayerDistance) return false;

        var cam = Camera.main;
        if (cam == null) return true;
        Vector3 v = cam.WorldToViewportPoint(home);
        bool onScreen = v.x > -offscreenMargin && v.x < 1f + offscreenMargin &&
                        v.y > -offscreenMargin && v.y < 1f + offscreenMargin;
        return !onScreen;
    }

    private static void Respawn(Entry e)
    {
        var go = e.health.gameObject;
        go.transform.position = e.home;
        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.position = e.home;
            rb.linearVelocity = Vector2.zero;
        }
        e.health.Revive();
        go.SetActive(true);
        e.diedAt = -1f;
    }
}
