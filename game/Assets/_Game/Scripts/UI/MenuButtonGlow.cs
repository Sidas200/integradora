using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Botón del menú principal con marco fino rosa y brillo cuando está seleccionado o con el mouse encima.
// Se dibuja por código sobre un Button existente (fondo, marco doble, rombos en las puntas, ícono y flecha).
// Al pasar el mouse lo selecciona, así teclado y mouse nunca iluminan dos botones a la vez.
[RequireComponent(typeof(Button))]
public class MenuButtonGlow : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color FillNormal = new Color(0.09f, 0.04f, 0.14f, 0.78f);
    private static readonly Color FillLit = new Color(0.42f, 0.13f, 0.45f, 0.88f);
    private static readonly Color Line = new Color(0.86f, 0.56f, 0.96f, 1f);
    private static readonly Color GlowColor = new Color(1f, 0.35f, 0.9f, 1f);
    private static readonly Color TextNormal = new Color(0.86f, 0.82f, 0.92f, 1f);
    private static readonly Color TextLit = Color.white;

    private static Sprite softSprite;

    private Button button;
    private CanvasGroup group;
    private Image glow, fill, icon;
    private Image[] lines;
    private TMP_Text label, chevron;
    private bool selected, hovered;
    private float lit;

    public void Build(Sprite iconSprite)
    {
        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        var baseImg = button.targetGraphic as Image;
        if (baseImg != null) baseImg.color = new Color(0f, 0f, 0f, 0f); // solo para recibir clics
        group = gameObject.AddComponent<CanvasGroup>();

        var size = ((RectTransform)transform).rect.size;
        label = GetComponentInChildren<TMP_Text>();

        glow = Child("Glow", Vector2.zero, size + new Vector2(70f, 60f), GlowColor);
        glow.sprite = SoftSprite();
        glow.type = Image.Type.Sliced;
        fill = Child("Fill", Vector2.zero, size - new Vector2(8f, 8f), FillNormal);

        lines = new[]
        {
            // marco exterior
            Child("Top", new Vector2(0f, size.y / 2f - 2f), new Vector2(size.x - 24f, 2f), Line),
            Child("Bottom", new Vector2(0f, -size.y / 2f + 2f), new Vector2(size.x - 24f, 2f), Line),
            Child("Left", new Vector2(-size.x / 2f + 12f, 0f), new Vector2(2f, size.y - 4f), Line),
            Child("Right", new Vector2(size.x / 2f - 12f, 0f), new Vector2(2f, size.y - 4f), Line),
            // marco interior más tenue
            Child("InTop", new Vector2(0f, size.y / 2f - 9f), new Vector2(size.x - 44f, 1f), Line),
            Child("InBottom", new Vector2(0f, -size.y / 2f + 9f), new Vector2(size.x - 44f, 1f), Line),
            // rombos en las puntas
            Child("DiamondL", new Vector2(-size.x / 2f + 12f, 0f), new Vector2(14f, 14f), Line),
            Child("DiamondR", new Vector2(size.x / 2f - 12f, 0f), new Vector2(14f, 14f), Line),
        };
        lines[6].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        lines[7].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        if (iconSprite != null)
        {
            icon = Child("Icon", new Vector2(-size.x / 2f + 80f, 0f), new Vector2(52f, 52f), Color.white);
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
        }

        var chev = new GameObject("Chevron", typeof(RectTransform));
        var crt = (RectTransform)chev.transform;
        crt.SetParent(transform, false);
        crt.sizeDelta = new Vector2(40f, size.y);
        crt.anchoredPosition = new Vector2(size.x / 2f - 50f, 0f);
        chevron = chev.AddComponent<TextMeshProUGUI>();
        chevron.text = ">";
        chevron.fontSize = 40;
        chevron.alignment = TextAlignmentOptions.Center;
        chevron.raycastTarget = false;

        if (label != null)
        {
            label.transform.SetAsLastSibling();
            label.alignment = TextAlignmentOptions.Left;
            label.margin = new Vector4(iconSprite != null ? 150f : 60f, 0f, 60f, 0f);
        }
        Apply();
    }

    private Image Child(string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Textura suave (bordes con degradado) para el brillo, generada una sola vez.
    private static Sprite SoftSprite()
    {
        if (softSprite != null) return softSprite;
        const int n = 64, edge = 28;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)) / (float)edge;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        softSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                   new Vector4(edge, edge, edge, edge));
        return softSprite;
    }

    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;

    public void OnPointerEnter(PointerEventData e)
    {
        hovered = true;
        if (button != null && button.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData e) => hovered = false;

    private void Update()
    {
        if (button == null) return;
        bool on = button.interactable && (selected || hovered);
        lit = Mathf.MoveTowards(lit, on ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        Apply();
    }

    private void Apply()
    {
        group.alpha = button.interactable ? 1f : 0.45f;
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
        glow.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, 0.55f * lit * pulse);
        fill.color = Color.Lerp(FillNormal, FillLit, lit);
        for (int i = 0; i < lines.Length; i++)
        {
            float a = i is 4 or 5 ? Mathf.Lerp(0.25f, 0.6f, lit) : Mathf.Lerp(0.55f, 1f, lit);
            lines[i].color = new Color(Line.r, Line.g, Line.b, a);
        }
        if (icon != null) icon.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 1f, lit));
        chevron.color = new Color(1f, 1f, 1f, lit);
        if (label != null) label.color = Color.Lerp(TextNormal, TextLit, lit);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.03f, lit);
    }
}
