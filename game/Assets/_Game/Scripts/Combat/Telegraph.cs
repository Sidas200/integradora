using UnityEngine;

// Aviso de ataque en el suelo (círculo o franja). Parpadea más rápido cuanto más cerca está el golpe.
// Se dibuja en el layer Background por encima del suelo y debajo de los personajes.
[RequireComponent(typeof(SpriteRenderer))]
public class Telegraph : MonoBehaviour
{
    public static readonly Color Danger = new Color(1f, 0.15f, 0.25f, 1f);

    [Header("Uso pasivo (lo activa otro script, p. ej. Enemy_Melee)")]
    [SerializeField] private float defaultDuration = 0.6f;
    [SerializeField] private Color defaultColor = new Color(1f, 0.15f, 0.25f, 1f);

    // Intensidad del aviso (1 = normal). Los enemigos comunes usan uno más tenue que el jefe.
    public float Opacity { get; set; } = 1f;

    private SpriteRenderer sr;
    private float duration;
    private float elapsed;
    private Color color;

    // Franja que crece hasta su largo final durante el aviso (desde el centro o hacia delante)
    private bool growing;
    private bool growForward;
    private Vector2 growCenter, growAxis;
    private float growLength, growWidth;
    // Destello del golpe: un instante casi opaco y se oculta solo
    private float impactTime = -1f;

    // Crea un aviso suelto en la escena (no hijo de un SortingGroup).
    // Intensidad de las franjas de los enemigos comunes: se ven, pero sin tapar el escenario
    public const float SubtleOpacity = 0.45f;

    public static Telegraph Create(Sprite sprite, string name = "Telegraph")
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 20;
        var t = go.AddComponent<Telegraph>();
        go.SetActive(false);
        return t;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (duration <= 0f)
        {
            duration = defaultDuration;
            color = defaultColor;
        }
    }

    public void SetDefaults(float time, Color tint)
    {
        defaultDuration = time;
        defaultColor = tint;
    }

    // Círculo isométrico de radio dado (el sprite mide 0.64 x 0.32 unidades).
    public void ShowCircle(Vector2 center, float radius, float time, Color tint)
    {
        growing = false;
        transform.position = center;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(radius * 2f / 0.64f, radius * 2f / 0.64f, 1f);
        Begin(time, tint);
    }

    // Franja desde el origen en una dirección (el sprite mide 0.16 x 0.16 con pivote a la izquierda).
    public void ShowLine(Vector2 origin, Vector2 direction, float length, float width, float time, Color tint)
    {
        growing = false;
        transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(length / 0.16f, width / 0.16f, 1f);
        Begin(time, tint);
    }

    // Franja centrada en un punto, horizontal o vertical (patrones de ataque de los enemigos).
    public void ShowBand(Vector2 center, bool horizontal, float length, float width, float time, Color tint)
    {
        Vector2 axis = horizontal ? Vector2.right : Vector2.up;
        ShowLine(center - axis * (length * 0.5f), axis, length, width, time, tint);
    }

    // Igual que ShowBand, pero la franja nace en el centro y se estira hacia ambos lados
    // mientras dura el aviso: se ve cuánto falta para el golpe.
    public void ShowBandGrowing(Vector2 center, bool horizontal, float length, float width, float time, Color tint)
    {
        ShowBand(center, horizontal, length, width, time, tint);
        growing = true;
        growForward = false;
        growCenter = center;
        growAxis = horizontal ? Vector2.right : Vector2.up;
        growLength = length;
        growWidth = width;
        ApplyGrow(0f);
    }

    // Franja que nace en el origen y se estira solo hacia delante (golpe frontal)
    public void ShowLineGrowing(Vector2 origin, Vector2 direction, float length, float width, float time, Color tint)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        ShowLine(origin, dir, length, width, time, tint);
        growing = true;
        growForward = true;
        growCenter = origin;
        growAxis = dir;
        growLength = length;
        growWidth = width;
        ApplyGrow(0f);
    }

    // ¿El punto queda dentro de la franja que sale del origen hacia delante?
    public static bool InsideLine(Vector2 point, Vector2 origin, Vector2 direction, float length, float width)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 d = point - origin;
        float along = Vector2.Dot(d, dir);
        float across = Mathf.Abs(d.x * dir.y - d.y * dir.x);
        return along >= 0f && along <= length && across <= width * 0.5f;
    }

    // Destello del golpe sobre la franja ya mostrada; se oculta solo
    public void Impact(float time = 0.12f)
    {
        growing = false;
        gameObject.SetActive(true);
        impactTime = Mathf.Max(0.01f, time);
        elapsed = 0f;
    }

    private void ApplyGrow(float k)
    {
        // Sale rápido y frena al llegar al borde (ease-out)
        float eased = 1f - (1f - k) * (1f - k);
        float len = Mathf.Lerp(0.15f, 1f, eased) * growLength;
        // Hacia delante el origen queda fijo; centrada, crece hacia ambos lados
        transform.position = growForward ? growCenter : growCenter - growAxis * (len * 0.5f);
        transform.localScale = new Vector3(len / 0.16f, growWidth / 0.16f, 1f);
    }

    // ¿El punto queda dentro de la franja centrada?
    public static bool InsideBand(Vector2 point, Vector2 center, bool horizontal, float length, float width)
    {
        Vector2 d = point - center;
        float along = horizontal ? d.x : d.y;
        float across = horizontal ? d.y : d.x;
        return Mathf.Abs(along) <= length * 0.5f && Mathf.Abs(across) <= width * 0.5f;
    }

    // Sprite de franja hecho por código (0.16 x 0.16 unidades, pivote a la izquierda), para los
    // enemigos que no traen el sprite de franja del jefe.
    private static Sprite bandSprite;
    public static Sprite BandSprite
    {
        get
        {
            if (bandSprite != null) return bandSprite;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "TelegraphBand" };
            var pixels = new Color32[16 * 16];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    // Borde un poco más opaco para que se lea el límite de la franja
                    bool edge = y == 0 || y == 15;
                    pixels[y * 16 + x] = new Color32(255, 255, 255, (byte)(edge ? 255 : 170));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            bandSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0f, 0.5f), 100f);
            return bandSprite;
        }
    }

    private void Begin(float time, Color tint)
    {
        impactTime = -1f;
        duration = Mathf.Max(0.01f, time);
        color = tint;
        gameObject.SetActive(false);
        gameObject.SetActive(true); // reinicia el parpadeo
        elapsed = 0f;
        Update();
    }

    public void Hide() => gameObject.SetActive(false);

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (impactTime > 0f)
        {
            float f = Mathf.Clamp01(elapsed / impactTime);
            var ic = color;
            ic.a = Mathf.Lerp(0.85f, 0f, f);
            sr.color = ic;
            if (f >= 1f) { impactTime = -1f; gameObject.SetActive(false); }
            return;
        }

        float k = Mathf.Clamp01(elapsed / duration);
        if (growing) ApplyGrow(k);
        // Parpadeo que acelera; al final queda casi sólido (el golpe es inminente)
        float speed = Mathf.Lerp(6f, 22f, k);
        float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(elapsed * speed));
        var c = color;
        c.a = Mathf.Lerp(0.35f, 0.9f, k) * blink * Opacity;
        sr.color = c;
    }
}
