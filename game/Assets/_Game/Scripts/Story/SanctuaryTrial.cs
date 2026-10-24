using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Prueba de la espada del primer Santuario: al tocar la estatua se sella la entrada y
// aparecen oleadas de enemigos desde círculos de invocación. Al vencerlas despierta Aoi.
// Si Tsuki cae, el reintento vuelve al último guardado y la prueba empieza de nuevo.
public class SanctuaryTrial : MonoBehaviour
{
    public const string DoneFlag = "sword_trial_done";

    [Serializable]
    public class Wave
    {
        public List<GameObject> enemies = new List<GameObject>();
    }

    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField] private Collider2D exitBlocker;
    [SerializeField] private SpriteRenderer exitBarrier;
    [SerializeField] private Sprite summonCircle;
    [SerializeField] private float summonTime = 1f;

    public bool Running { get; private set; }
    public bool Done => StoryFlags.Has(DoneFlag);

    public void Setup(List<Wave> waveList, Collider2D blocker, SpriteRenderer barrier, Sprite circle)
    {
        waves = waveList;
        exitBlocker = blocker;
        exitBarrier = barrier;
        summonCircle = circle;
    }

    private void Awake()
    {
        foreach (var wave in waves)
            foreach (var e in wave.enemies)
                if (e != null && e.TryGetComponent<EnemyLoot>(out var loot)) loot.KeepDefeated();
    }

    private void Start()
    {
        SetExitOpen(true);
        foreach (var wave in waves)
            foreach (var e in wave.enemies)
                // Ya superada: se activan para que EnemyLoot devuelva los drops que quedaron
                // en el suelo y luego se desactiven solos (están marcados como vencidos)
                if (e != null) e.SetActive(Done);
    }

    public void Begin(Action onComplete)
    {
        if (Running || Done) return;
        StartCoroutine(Run(onComplete));
    }

    private IEnumerator Run(Action onComplete)
    {
        Running = true;
        SetExitOpen(false);
        for (int i = 0; i < waves.Count; i++)
        {
            if (GameHud.Instance != null) GameHud.Instance.ShowToast($"Prueba de la espada: oleada {i + 1} de {waves.Count}");
            yield return Summon(waves[i]);
            yield return new WaitUntil(() => waves[i].enemies.TrueForAll(Defeated));
            yield return new WaitForSeconds(0.6f);
        }
        SetExitOpen(true);
        Running = false;
        StoryFlags.Set(DoneFlag);
        if (GameHud.Instance != null) GameHud.Instance.ShowToast("¡Prueba superada!");
        onComplete?.Invoke();
    }

    private static bool Defeated(GameObject e)
    {
        if (e == null || !e.activeInHierarchy) return true;
        var health = e.GetComponent<EnemyHealth>();
        return health == null || health.IsDead;
    }

    // Círculo de invocación que crece y gira; al cerrarse aparece el enemigo.
    private IEnumerator Summon(Wave wave)
    {
        var circles = new List<SpriteRenderer>();
        foreach (var e in wave.enemies)
        {
            if (e == null) continue;
            var go = new GameObject("SummonCircle");
            go.transform.position = e.transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = summonCircle;
            sr.sortingLayerName = "Sprites";
            sr.sortingOrder = -50;
            circles.Add(sr);
        }
        for (float t = 0f; t < summonTime; t += Time.deltaTime)
        {
            float k = t / summonTime;
            foreach (var sr in circles)
            {
                sr.transform.localScale = new Vector3(1f, 1f, 1f) * Mathf.Lerp(0.2f, 1f, Mathf.SmoothStep(0f, 1f, k));
                sr.color = new Color(1f, 1f, 1f, Mathf.PingPong(t * 6f, 0.5f) + 0.5f);
            }
            yield return null;
        }
        foreach (var e in wave.enemies) if (e != null) e.SetActive(true);
        CameraShake.Shake(0.02f, 0.2f);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            foreach (var sr in circles) sr.color = new Color(1f, 1f, 1f, 1f - t / 0.4f);
            yield return null;
        }
        foreach (var sr in circles) Destroy(sr.gameObject);
    }

    private void SetExitOpen(bool open)
    {
        if (exitBlocker != null) exitBlocker.enabled = !open;
        if (exitBarrier != null) exitBarrier.gameObject.SetActive(!open);
    }
}
