using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Refuerzos de Varn: al bajar su vida a ciertos porcentajes invoca enemigos con el círculo de
// invocación del Santuario, lejos de Tsuki y dentro de la arena. Va junto a GuardianArena, que
// lo arranca al empezar la pelea y lo limpia al vencer al guardián. Los refuerzos no sueltan botín.
public class GuardianSummons : MonoBehaviour
{
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private Sprite summonCircle;
    [Tooltip("Porcentajes de vida (0-1) en los que Varn invoca; cada uno una sola vez por pelea.")]
    [SerializeField] private float[] thresholds = { 0.75f, 0.5f, 0.25f };
    [SerializeField] private int perWave = 2;
    [Tooltip("Enemigos extra por oleada cuando Varn está enfurecido (bajo 50% de vida).")]
    [SerializeField] private int enragedExtra = 1;
    [SerializeField] private int maxAlive = 4;
    [SerializeField] private float summonTime = 1f;
    [SerializeField] private float minDistanceToPlayer = 0.9f;

    private readonly List<GameObject> alive = new List<GameObject>();
    private GuardianBoss boss;
    private int nextThreshold;

    public void Setup(GameObject[] prefabs, Sprite circle)
    {
        minionPrefabs = prefabs;
        summonCircle = circle;
    }

    public void Begin(GuardianBoss guardian)
    {
        boss = guardian;
        nextThreshold = 0;
        boss.Health.HealthChanged += OnBossHealthChanged;
    }

    // Al vencer a Varn sus refuerzos se desvanecen con él.
    public void Clear()
    {
        if (boss != null) boss.Health.HealthChanged -= OnBossHealthChanged;
        StopAllCoroutines();
        foreach (var e in alive)
            if (e != null) Destroy(e);
        alive.Clear();
    }

    private void OnDestroy()
    {
        if (boss != null) boss.Health.HealthChanged -= OnBossHealthChanged;
    }

    private void OnBossHealthChanged(int current, int max)
    {
        if (max <= 0 || current <= 0) return;
        float ratio = (float)current / max;
        int count = 0;
        while (nextThreshold < thresholds.Length && ratio <= thresholds[nextThreshold])
        {
            count += perWave + (thresholds[nextThreshold] <= 0.5f ? enragedExtra : 0);
            nextThreshold++;
        }
        if (count == 0) return;

        alive.RemoveAll(e => e == null || !e.activeInHierarchy || e.GetComponent<EnemyHealth>().IsDead);
        count = Mathf.Min(count, maxAlive - alive.Count);
        if (count <= 0) return;
        if (GameHud.Instance != null) GameHud.Instance.ShowToast("¡Varn invoca refuerzos!");
        StartCoroutine(Summon(count));
    }

    private IEnumerator Summon(int count)
    {
        var spots = new List<Vector2>();
        var circles = new List<SpriteRenderer>();
        for (int i = 0; i < count; i++)
        {
            var spot = FreeSpot();
            spots.Add(spot);
            var go = new GameObject("SummonCircle");
            go.transform.position = spot;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = summonCircle;
            sr.sortingLayerName = "Sprites";
            sr.sortingOrder = -50;
            circles.Add(sr);
        }

        // El mismo círculo que crece y parpadea de la prueba del Santuario
        for (float t = 0f; t < summonTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / summonTime);
            foreach (var sr in circles)
            {
                sr.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k);
                sr.color = new Color(1f, 1f, 1f, Mathf.PingPong(t * 6f, 0.5f) + 0.5f);
            }
            yield return null;
        }

        for (int i = 0; i < spots.Count; i++)
        {
            var prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            if (prefab != null) alive.Add(Instantiate(prefab, spots[i], Quaternion.identity));
        }
        CameraShake.Shake(0.02f, 0.2f);

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            foreach (var sr in circles) sr.color = new Color(1f, 1f, 1f, 1f - t / 0.4f);
            yield return null;
        }
        foreach (var sr in circles) Destroy(sr.gameObject);
    }

    // Un punto al azar dentro de la elipse de la arena que no caiga encima de Tsuki.
    private Vector2 FreeSpot()
    {
        var player = SceneContext.FindPlayer();
        Vector2 center = boss.ArenaCenter, radii = boss.ArenaRadii * 0.8f;
        Vector2 spot = center;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var p = Random.insideUnitCircle;
            spot = center + new Vector2(p.x * radii.x, p.y * radii.y);
            if (player == null || Vector2.Distance(spot, player.transform.position) >= minDistanceToPlayer) break;
        }
        return spot;
    }
}
