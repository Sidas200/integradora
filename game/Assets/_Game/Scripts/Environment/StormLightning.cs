using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tormenta de la arena del guardián (perfil con storm = true en OutdoorAmbience):
// - Rayos al fondo, detrás de la isla, de vez en cuando; el cielo destella con cada uno.
// - Durante la pelea con el guardián caen rayos al azar sobre la arena (algunos cerca de Tsuki).
//   Antes de caer marcan el suelo con un círculo que parpadea; si Tsuki sigue dentro al caer, le
//   quita vida.
// El sprite "Environment/lightning" es una tira de 8 cuadros (crece, destella y se apaga).
[DefaultExecutionOrder(110)]
public class StormLightning : MonoBehaviour
{
    private const string SheetResource = "Environment/lightning";
    private const string MaterialResource = "Environment/SkyUnlit";
    private const float SheetWidth = 2172f;
    private const float SheetHeight = 724f;

    // Columnas de cada cuadro en la tira (en píxeles de la imagen original) y franja vertical común
    private static readonly Vector2Int[] FrameColumns =
    {
        new Vector2Int(57, 203), new Vector2Int(294, 496), new Vector2Int(531, 799), new Vector2Int(801, 1093),
        new Vector2Int(1097, 1372), new Vector2Int(1402, 1613), new Vector2Int(1686, 1870), new Vector2Int(1980, 2110),
    };
    private const float FrameTop = 110f;    // desde arriba
    private const float FrameBottom = 630f; // desde arriba

    [Header("Fondo (detrás de la isla)")]
    [SerializeField] private Vector2 backgroundInterval = new Vector2(1.2f, 3.5f);
    [SerializeField] private Vector2 backgroundHeight = new Vector2(1.3f, 2.1f);
    [SerializeField] private float backgroundAlpha = 0.8f;

    [Header("Arena (durante la pelea)")]
    [SerializeField] private Vector2 strikeInterval = new Vector2(2.6f, 4f);
    [Tooltip("Intervalo cuando el guardián tiene la mitad de vida o menos.")]
    [SerializeField] private Vector2 enragedInterval = new Vector2(1.8f, 3f);
    [SerializeField] private float warningTime = 1f;
    [SerializeField] private float strikeRadius = 0.22f;
    [SerializeField, Min(1)] private int strikeDamage = 14;
    [Tooltip("Probabilidad de que el rayo apunte cerca de Tsuki en vez de a un punto cualquiera.")]
    [SerializeField, Range(0f, 1f)] private float aimAtPlayerChance = 0.55f;
    [SerializeField] private float aimSpread = 0.4f;
    [SerializeField] private float strikeHeight = 1.45f;
    [SerializeField] private float framesPerSecond = 22f;

    private Sprite[] frames;
    private Material material;
    private Camera cam;
    private SkyParallax sky;
    private GuardianBoss boss;
    private Transform player;
    private Health playerHealth;
    private Sprite circleSprite;
    private float nextBackground;
    private float nextStrike;
    private readonly List<Telegraph> warningPool = new List<Telegraph>();
    private readonly List<Telegraph> warningsCreated = new List<Telegraph>();

    private class BackgroundBolt
    {
        public SpriteRenderer sr;
        public Vector2 offset; // respecto a la cámara
    }
    private readonly List<BackgroundBolt> backgroundBolts = new List<BackgroundBolt>();

    private void Start()
    {
        cam = Camera.main;
        sky = GetComponent<SkyParallax>();
        material = Resources.Load<Material>(MaterialResource);
        frames = LoadFrames();
        if (frames == null || cam == null) { enabled = false; return; }

        boss = FindAnyObjectByType<GuardianBoss>(FindObjectsInactive.Exclude);
        if (boss != null) circleSprite = boss.CircleSprite;
        if (circleSprite == null) circleSprite = MakeCircleSprite();

        nextBackground = Time.time + Random.Range(0.4f, 1.2f);
        nextStrike = Time.time + strikeInterval.y;
    }

    private void OnDestroy()
    {
        foreach (var t in warningsCreated)
            if (t != null) Destroy(t.gameObject);
    }

    private static Sprite[] LoadFrames()
    {
        var tex = Resources.Load<Texture2D>(SheetResource);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        // Por si la textura se importa a otro tamaño
        float sx = tex.width / SheetWidth;
        float sy = tex.height / SheetHeight;
        var result = new Sprite[FrameColumns.Length];
        for (int i = 0; i < FrameColumns.Length; i++)
        {
            var c = FrameColumns[i];
            var rect = new Rect(c.x * sx, (SheetHeight - FrameBottom) * sy, (c.y - c.x) * sx, (FrameBottom - FrameTop) * sy);
            // Pivote abajo al centro: la punta del rayo toca el punto de impacto
            result[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.02f), 100f * sy);
            result[i].name = "lightning_" + i;
        }
        return result;
    }

    private void Update()
    {
        if (Time.time >= nextBackground)
        {
            nextBackground = Time.time + Random.Range(backgroundInterval.x, backgroundInterval.y);
            StartCoroutine(BackgroundStrike());
        }

        if (!FightActive())
        {
            nextStrike = Mathf.Max(nextStrike, Time.time + 1.5f); // respiro al empezar la pelea
            return;
        }
        if (Time.time >= nextStrike)
        {
            bool enraged = boss.Health != null && boss.Health.CurrentHealth * 2 <= boss.Health.MaxHealth;
            var range = enraged ? enragedInterval : strikeInterval;
            nextStrike = Time.time + Random.Range(range.x, range.y);
            StartCoroutine(ArenaStrike(PickTarget()));
            // Con el guardián débil, a veces cae un segundo rayo
            if (enraged && Random.value < 0.4f)
                StartCoroutine(ArenaStrike(PickTarget(), 0.35f));
        }
    }

    private void LateUpdate()
    {
        // Los rayos del fondo siguen a la cámara, como el cielo
        Vector2 camPos = cam.transform.position;
        foreach (var b in backgroundBolts)
            if (b.sr != null) b.sr.transform.position = new Vector3(camPos.x + b.offset.x, camPos.y + b.offset.y, 0f);
    }

    private bool FightActive()
    {
        if (boss == null || !boss.isActiveAndEnabled) return false;
        if (boss.Current == GuardianBoss.State.Dormant || boss.Current == GuardianBoss.State.Dead) return false;
        if (player == null)
        {
            var go = SceneContext.FindPlayer();
            if (go == null) return false;
            player = go.transform;
            playerHealth = go.GetComponentInParent<Health>();
        }
        return playerHealth == null || !playerHealth.IsDead;
    }

    private Vector2 PickTarget()
    {
        Vector2 center = boss.ArenaCenter;
        Vector2 radii = boss.ArenaRadii * 0.85f;
        Vector2 p;
        if (Random.value < aimAtPlayerChance)
        {
            var r = Random.insideUnitCircle * aimSpread;
            p = (Vector2)player.position + new Vector2(r.x, r.y * 0.5f);
        }
        else
        {
            var r = Random.insideUnitCircle;
            p = center + new Vector2(r.x * radii.x, r.y * radii.y);
        }
        // Siempre dentro de la elipse de la arena
        Vector2 d = p - center;
        float k = (d.x * d.x) / (radii.x * radii.x) + (d.y * d.y) / (radii.y * radii.y);
        if (k > 1f) p = center + d / Mathf.Sqrt(k);
        return p;
    }

    private IEnumerator ArenaStrike(Vector2 point, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var warning = GetWarning();
        warning.ShowCircle(point, strikeRadius, warningTime, Telegraph.Danger);
        yield return new WaitForSeconds(warningTime);

        var sr = NewBolt("Lightning_Strike", "UI", -20);
        sr.transform.position = new Vector3(point.x, point.y, 0f);
        float scale = strikeHeight / Mathf.Max(0.01f, frames[3].bounds.size.y);
        sr.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);

        bool damaged = false;
        float frameTime = 1f / Mathf.Max(1f, framesPerSecond);
        for (int i = 0; i < frames.Length; i++)
        {
            sr.sprite = frames[i];
            if (i == 2 && !damaged)
            {
                damaged = true;
                warning.Impact(0.2f);
                if (sky != null) sky.Flash = Mathf.Max(sky.Flash, 0.8f);
                CameraShake.Shake(0.035f, 0.15f);
                if (player != null && FightActive() && CombatUtil.InsideIsoCircle(player.position, point, strikeRadius))
                    CombatUtil.DamagePlayer(player.gameObject, strikeDamage, point);
            }
            yield return new WaitForSeconds(frameTime);
        }
        Destroy(sr.gameObject);
        ReleaseWarning(warning);
    }

    private IEnumerator BackgroundStrike()
    {
        Vector2 view = new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
        float height = Random.Range(backgroundHeight.x, backgroundHeight.y);
        // Cae desde arriba de la vista hasta más o menos la altura del horizonte
        var bolt = new BackgroundBolt
        {
            sr = NewBolt("Lightning_Back", "Background", -290),
            offset = new Vector2(Random.Range(-view.x * 0.9f, view.x * 0.9f), view.y + 0.1f - height),
        };
        float scale = height / Mathf.Max(0.01f, frames[3].bounds.size.y);
        bolt.sr.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);
        bolt.sr.color = new Color(1f, 0.85f, 0.95f, backgroundAlpha);
        backgroundBolts.Add(bolt);
        LateUpdate();

        float frameTime = 1f / Mathf.Max(1f, framesPerSecond);
        for (int i = 0; i < frames.Length; i++)
        {
            bolt.sr.sprite = frames[i];
            if (i == 3 && sky != null) sky.Flash = Mathf.Max(sky.Flash, Random.Range(0.35f, 0.6f));
            yield return new WaitForSeconds(frameTime);
        }
        backgroundBolts.Remove(bolt);
        Destroy(bolt.sr.gameObject);
    }

    private SpriteRenderer NewBolt(string name, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        if (material != null) sr.sharedMaterial = material; // sin luz: el rayo brilla aunque la arena esté oscura
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return sr;
    }

    private Telegraph GetWarning()
    {
        Telegraph t;
        if (warningPool.Count > 0)
        {
            t = warningPool[warningPool.Count - 1];
            warningPool.RemoveAt(warningPool.Count - 1);
        }
        else
        {
            t = Telegraph.Create(circleSprite, "LightningWarning");
            warningsCreated.Add(t);
        }
        return t;
    }

    private void ReleaseWarning(Telegraph t)
    {
        if (t == null) return;
        warningPool.Add(t);
    }

    // Respaldo si el guardián no trae su sprite de círculo: elipse isométrica 0.64 x 0.32 unidades
    private static Sprite MakeCircleSprite()
    {
        const int w = 64, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "LightningCircle" };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f);
                float dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                float r = dx * dx + dy * dy;
                byte a = r > 1f ? (byte)0 : r > 0.8f ? (byte)255 : (byte)150;
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
