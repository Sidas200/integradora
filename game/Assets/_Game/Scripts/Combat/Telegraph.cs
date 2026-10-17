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

    private SpriteRenderer sr;
    private float duration;
    private float elapsed;
    private Color color;

    // Crea un aviso suelto en la escena (no hijo de un SortingGroup).
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
        transform.position = center;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(radius * 2f / 0.64f, radius * 2f / 0.64f, 1f);
        Begin(time, tint);
    }

    // Franja desde el origen en una dirección (el sprite mide 0.16 x 0.16 con pivote a la izquierda).
    public void ShowLine(Vector2 origin, Vector2 direction, float length, float width, float time, Color tint)
    {
        transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(length / 0.16f, width / 0.16f, 1f);
        Begin(time, tint);
    }

    private void Begin(float time, Color tint)
    {
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
        float k = Mathf.Clamp01(elapsed / duration);
        // Parpadeo que acelera; al final queda casi sólido (el golpe es inminente)
        float speed = Mathf.Lerp(6f, 22f, k);
        float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(elapsed * speed));
        var c = color;
        c.a = Mathf.Lerp(0.35f, 0.9f, k) * blink;
        sr.color = c;
    }
}
