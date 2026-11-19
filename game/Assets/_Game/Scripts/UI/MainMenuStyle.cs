using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Estilo del menú principal: pétalos cayendo sobre el fondo, botones con brillo y la tarjeta de cuenta
// con marco ornamentado. Se aplica al cargar la escena para no rehacer lo que generan el builder del
// capítulo y CloudSync (que crea la tarjeta de cuenta por código).
public class MainMenuStyle : MonoBehaviour
{
    private static readonly Color FrameColor = new Color(0.64f, 0.5f, 0.9f, 1f);
    private static readonly Color CardFill = new Color(0.05f, 0.03f, 0.09f, 0.86f);
    private static readonly Color Glow = new Color(1f, 0.45f, 0.95f, 1f);

    [SerializeField] private RectTransform background;
    [SerializeField] private Sprite[] petals;
    [Tooltip("Ícono de cada botón, en el mismo orden que Buttons.")]
    [SerializeField] private Sprite[] buttonIcons;
    [SerializeField] private Button[] buttons;

    [Header("Tarjeta de cuenta")]
    [SerializeField] private Sprite flower;
    [SerializeField] private Sprite cloudIcon;

    public void Setup(RectTransform bg, Sprite[] petalSprites, Sprite[] icons, Button[] menuButtons, Sprite flowerSprite, Sprite cloud)
    {
        background = bg;
        petals = petalSprites;
        buttonIcons = icons;
        buttons = menuButtons;
        flower = flowerSprite;
        cloudIcon = cloud;
    }

    private void Awake()
    {
        if (petals != null && petals.Length > 0)
        {
            // capa de pétalos justo encima del fondo (detrás de los botones)
            var layer = new GameObject("Petals", typeof(RectTransform));
            var rt = (RectTransform)layer.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (background != null) rt.SetSiblingIndex(background.GetSiblingIndex() + 1);
            layer.AddComponent<PetalFall>().Setup(petals);
        }

        // Confirmación de nueva partida con el estilo del resto de la UI
        UiSkin.StyleConfirm(transform.Find("ConfirmNewGame"));

        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            var icon = buttonIcons != null && i < buttonIcons.Length ? buttonIcons[i] : null;
            if (buttons[i] != null) buttons[i].gameObject.AddComponent<MenuButtonGlow>().Build(icon);
        }
    }

    // CloudSync crea la tarjeta cuando la escena termina de cargar; se espera un momento a que exista.
    private IEnumerator Start()
    {
        float until = Time.unscaledTime + 5f;
        CloudAccountPanel panel;
        while ((panel = FindAnyObjectByType<CloudAccountPanel>()) == null || panel.Card == null)
        {
            if (Time.unscaledTime > until) yield break;
            yield return null;
        }
        StyleAccountCard(panel);
    }

    private void StyleAccountCard(CloudAccountPanel panel)
    {
        var card = panel.Card;
        var size = new Vector2(600f, 240f);
        card.sizeDelta = size;
        card.GetComponent<Image>().color = CardFill;

        Place(panel.AccountText.rectTransform, new Vector2(44f, -34f), new Vector2(512f, 44f));
        panel.AccountText.fontSize = 32;
        panel.AccountText.color = new Color(0.86f, 0.79f, 0.97f);
        Place(panel.StatusText.rectTransform, new Vector2(44f, -84f), new Vector2(512f, 36f));
        panel.StatusText.fontSize = 22;
        panel.StatusText.color = new Color(0.78f, 0.72f, 0.88f);

        Ornament(card, size, 1f);

        // botón con el mismo estilo que los del menú
        var button = panel.AccountButton;
        var brt = (RectTransform)button.transform;
        brt.anchoredPosition = new Vector2(44f, 34f);
        brt.sizeDelta = new Vector2(320f, 66f);
        var label = button.GetComponentInChildren<TMPro.TMP_Text>();
        if (label != null) label.fontSize = 28;
        button.gameObject.AddComponent<MenuButtonGlow>().Build(cloudIcon);

        // textos y botón encima de la decoración
        panel.AccountText.transform.SetAsLastSibling();
        panel.StatusText.transform.SetAsLastSibling();
        brt.SetAsLastSibling();

        StyleLoginBox(panel);
    }

    // Ventana de inicio de sesión con el mismo marco que la tarjeta y botones como los del menú.
    private void StyleLoginBox(CloudAccountPanel panel)
    {
        var box = panel.LoginBox;
        if (box == null) return;
        var size = box.sizeDelta;
        box.GetComponent<Image>().color = new Color(CardFill.r, CardFill.g, CardFill.b, 0.96f);

        // lo que ya tenía la ventana va encima de la decoración
        var content = new Transform[box.childCount];
        for (int i = 0; i < content.Length; i++) content[i] = box.GetChild(i);
        Ornament(box, size, 1.4f);
        foreach (var t in content) t.SetAsLastSibling();

        panel.LoginTitle.color = new Color(0.94f, 0.88f, 1f);
        panel.LoginTitle.fontStyle = TMPro.FontStyles.Bold;
        panel.LoginSubtitle.color = new Color(0.78f, 0.72f, 0.88f);
        foreach (var l in panel.LoginFieldLabels) l.color = new Color(0.86f, 0.79f, 0.97f);

        // separador bajo el título, como los de las listas del menú
        var sep = Deco(box, "TitleRule", new Vector2(0f, size.y / 2f - 158f), new Vector2(420f, 1f), new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.6f));
        sep.transform.SetSiblingIndex(content.Length > 0 ? content[0].GetSiblingIndex() : 0);
        Deco(box, "TitleDiamond", new Vector2(0f, size.y / 2f - 158f), new Vector2(9f, 9f), FrameColor)
            .rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        StyleField(panel.EmailField);
        StyleField(panel.PasswordField);
        StyleDialogButton(panel.SubmitButton);
        StyleDialogButton(panel.CancelButton);
    }

    private static void StyleField(TMPro.TMP_InputField field)
    {
        if (field == null) return;
        var rt = (RectTransform)field.transform;
        var size = rt.sizeDelta;
        var bg = field.GetComponent<Image>();
        bg.sprite = null;
        bg.color = Color.white;
        var colors = field.colors;
        colors.normalColor = new Color(0.09f, 0.04f, 0.14f, 0.85f);
        colors.highlightedColor = new Color(0.18f, 0.07f, 0.24f, 0.9f);
        colors.selectedColor = new Color(0.26f, 0.09f, 0.30f, 0.92f);
        colors.pressedColor = colors.selectedColor;
        colors.colorMultiplier = 1f;
        field.colors = colors;

        // marco fino rosa como el de los botones
        Deco(rt, "Top", new Vector2(0f, size.y / 2f - 1f), new Vector2(size.x, 2f), new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.75f));
        Deco(rt, "Bottom", new Vector2(0f, -size.y / 2f + 1f), new Vector2(size.x, 2f), new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.75f));
        Deco(rt, "Left", new Vector2(-size.x / 2f + 1f, 0f), new Vector2(2f, size.y), new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.75f));
        Deco(rt, "Right", new Vector2(size.x / 2f - 1f, 0f), new Vector2(2f, size.y), new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.75f));
        Deco(rt, "Diamond", new Vector2(-size.x / 2f + 1f, 0f), new Vector2(9f, 9f), FrameColor)
            .rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        if (field.textViewport != null)
        {
            field.textViewport.offsetMin = new Vector2(22f, field.textViewport.offsetMin.y);
            field.textViewport.offsetMax = new Vector2(-22f, field.textViewport.offsetMax.y);
        }
        field.caretColor = Color.white;
        field.selectionColor = new Color(Glow.r, Glow.g, Glow.b, 0.35f);
    }

    private static void StyleDialogButton(Button button)
    {
        if (button == null) return;
        button.gameObject.AddComponent<MenuButtonGlow>().Build(null);
        var label = button.GetComponentInChildren<TMPro.TMP_Text>();
        if (label == null) return;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.margin = new Vector4(24f, 0f, 24f, 0f);
        label.fontSize = 30;
    }

    // Marco doble con muescas, destello a la izquierda y flores en la derecha. scale agranda la decoración.
    private void Ornament(RectTransform card, Vector2 size, float scale)
    {
        Line(card, new Vector2(0f, size.y / 2f - 1f), new Vector2(size.x, 2f), 0.9f);
        Line(card, new Vector2(0f, -size.y / 2f + 1f), new Vector2(size.x, 2f), 0.9f);
        Line(card, new Vector2(-size.x / 2f + 1f, 0f), new Vector2(2f, size.y), 0.9f);
        Line(card, new Vector2(size.x / 2f - 1f, 0f), new Vector2(2f, size.y), 0.9f);
        Line(card, new Vector2(0f, size.y / 2f - 10f), new Vector2(size.x - 20f, 1f), 0.45f);
        Line(card, new Vector2(0f, -size.y / 2f + 10f), new Vector2(size.x - 20f, 1f), 0.45f);
        Line(card, new Vector2(-size.x / 2f + 10f, 0f), new Vector2(1f, size.y - 20f), 0.45f);
        Line(card, new Vector2(size.x / 2f - 10f, 0f), new Vector2(1f, size.y - 20f), 0.45f);
        foreach (var c in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
            Line(card, new Vector2(c.x * (size.x / 2f - 10f), c.y * (size.y / 2f - 10f)), new Vector2(8f, 8f), 0.9f);

        // destello en el borde izquierdo
        var sparkPos = new Vector2(-size.x / 2f, 10f);
        var glow = Deco(card, "SparkGlow", sparkPos, new Vector2(90f, 90f) * scale, new Color(Glow.r, Glow.g, Glow.b, 0.55f));
        glow.sprite = RadialSprite();
        Deco(card, "SparkH", sparkPos, new Vector2(64f * scale, 2f), Color.white);
        Deco(card, "SparkV", sparkPos, new Vector2(2f, 40f * scale), Color.white);
        Deco(card, "Spark", sparkPos, new Vector2(12f, 12f), Color.white).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        // flores en las esquinas de la derecha
        if (flower != null)
        {
            Flower(card, new Vector2(size.x / 2f - 18f, size.y / 2f - 6f), 78f * scale, -12f);
            Flower(card, new Vector2(size.x / 2f - 20f, size.y / 2f - 58f * scale), 30f * scale, 25f);
            Flower(card, new Vector2(size.x / 2f - 14f, -size.y / 2f + 14f), 44f * scale, 18f);
        }
        if (petals != null && petals.Length > 0)
        {
            var petal = Deco(card, "Petal", new Vector2(size.x / 2f - 60f * scale, -size.y / 2f + 30f * scale), new Vector2(34f, 34f) * scale, Color.white);
            petal.sprite = petals[Mathf.Min(6, petals.Length - 1)];
            petal.preserveAspect = true;
            petal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        }
    }

    private static Sprite radial;

    // Brillo redondo y suave para el destello, generado una sola vez.
    private static Sprite RadialSprite()
    {
        if (radial != null) return radial;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - 0.5f, n / 2f - 0.5f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * a));
            }
        tex.Apply();
        radial = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return radial;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Line(RectTransform parent, Vector2 pos, Vector2 size, float alpha) =>
        Deco(parent, "Frame", pos, size, new Color(FrameColor.r, FrameColor.g, FrameColor.b, alpha));

    private void Flower(RectTransform parent, Vector2 pos, float size, float angle)
    {
        var img = Deco(parent, "Flower", pos, new Vector2(size, size), Color.white);
        img.sprite = flower;
        img.preserveAspect = true;
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Imagen decorativa posicionada desde el centro de la tarjeta.
    private static Image Deco(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
