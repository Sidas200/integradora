using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Decisión al final de un diálogo: en medio de la pantalla aparece una franja inclinada (SlantedShape)
// con solo los ojos del personaje y debajo las opciones (2 o 3). Al elegir, la franja se desliza
// hacia la derecha y desaparece. La arma DialogueUI en código; todo corre con tiempo sin escala
// porque el mundo está detenido durante el diálogo.
// Controles: W/S o flechas, cruceta / stick; E, Espacio, Enter, A / × o clic para elegir; 1-3 elige directo.
public class DialogueChoiceUI : MonoBehaviour
{
    private static readonly Color Gold = new Color32(222, 168, 92, 255);
    private static readonly Color ShadowColor = new Color32(120, 18, 50, 235);
    private static readonly Color OptionBack = new Color32(28, 10, 28, 235);
    private static readonly Color OptionBackSelected = new Color32(116, 22, 58, 245);
    private static readonly Color TextIdle = new Color32(214, 198, 210, 255);
    private static readonly Color TextSelected = new Color32(255, 238, 205, 255);

    // Medidas en unidades de 1920x1080 (desde el centro de la pantalla)
    private const float StripWidth = 820f, StripHeight = 210f, StripY = 230f;
    private const float OptionWidth = 720f, OptionHeight = 58f, OptionGap = 72f;
    private const float OptionsTop = 40f, OptionsTopNoEyes = 140f;
    private const float InTime = 0.34f, OutTime = 0.4f;

    private class Option
    {
        public RectTransform rt;
        public CanvasGroup group;
        public SlantedShape back, edge;
        public TMP_Text text;
        public float lit;
    }

    private RectTransform root, strip, nameRt;
    private CanvasGroup stripGroup;
    private Image dim;
    private SlantedShape eyes, eyesBack, glint;
    private TMP_Text nameText;
    private readonly List<Option> options = new List<Option>();
    private TMP_Text fontSource;
    private Canvas canvas;

    private Action<int> onChosen;
    private int selected, count;
    private bool choosing, animatingIn, hasEyes;
    private float readyAt, previousNavY;
    private Rect eyesUv;
    private Coroutine routine;

    public bool IsActive => root != null && root.gameObject.activeSelf;

    public static DialogueChoiceUI Create(Canvas canvas, TMP_Text fontSource)
    {
        var go = new GameObject("DialogueChoice", typeof(RectTransform));
        var ui = go.AddComponent<DialogueChoiceUI>();
        ui.canvas = canvas;
        ui.fontSource = fontSource;
        ui.Build(canvas.transform);
        return ui;
    }

    // ------------------------------------------------------------------ armado

    private void Build(Transform parent)
    {
        root = (RectTransform)transform;
        root.SetParent(parent, false);
        Stretch(root, 0f);
        root.SetAsLastSibling();

        // Oscurece el fondo y bloquea clics al juego
        dim = NewChild(root, "Dim").gameObject.AddComponent<Image>();
        Stretch(dim.rectTransform, 0f);
        dim.color = new Color(0f, 0f, 0f, 0f);

        strip = NewChild(root, "Eyes");
        Center(strip, new Vector2(0f, StripY), new Vector2(StripWidth, StripHeight));
        stripGroup = strip.gameObject.AddComponent<CanvasGroup>();

        var shadow = Shape(strip, "Shadow", ShadowColor);
        shadow.rectTransform.anchoredPosition = new Vector2(24f, -18f);
        var border = Shape(strip, "Border", Gold);
        border.rectTransform.sizeDelta = new Vector2(16f, 14f);
        // Fondo oscuro: los bustos con transparencia (Varn) no dejan ver el borde dorado a través
        eyesBack = Shape(strip, "Back", new Color32(24, 10, 26, 255));
        eyes = Shape(strip, "Portrait", Color.white);
        // Brillo que cruza la franja al aparecer
        glint = Shape(strip, "Glint", new Color(1f, 1f, 1f, 0f));
        glint.rectTransform.anchorMin = glint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        glint.rectTransform.sizeDelta = new Vector2(70f, StripHeight);
        glint.Slant = 0.3f;

        // Nombre en un listón carmesí debajo de la franja, a la derecha
        nameRt = NewChild(strip, "Name");
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(1f, 0f);
        nameRt.pivot = new Vector2(1f, 1f);
        nameRt.anchoredPosition = new Vector2(-30f, 4f);
        nameRt.sizeDelta = new Vector2(240f, 46f);
        var plateEdge = Shape(nameRt, "Edge", Gold);
        plateEdge.rectTransform.sizeDelta = new Vector2(10f, 8f);
        plateEdge.Slant = 0.12f;
        var plate = Shape(nameRt, "Plate", ShadowColor);
        plate.Slant = 0.12f;
        nameText = Text(nameRt, 28f);
        nameText.color = Gold;
        nameText.fontStyle = FontStyles.Bold;

        root.gameObject.SetActive(false);
    }

    private Option NewOption(int i)
    {
        var o = new Option();
        o.rt = NewChild(root, "Option" + i);
        Center(o.rt, Vector2.zero, new Vector2(OptionWidth, OptionHeight));
        o.group = o.rt.gameObject.AddComponent<CanvasGroup>();
        o.edge = Shape(o.rt, "Edge", Gold);
        o.edge.rectTransform.sizeDelta = new Vector2(12f, 10f);
        o.edge.Slant = 0.04f;
        o.back = Shape(o.rt, "Back", OptionBack);
        o.back.Slant = 0.04f;
        o.text = Text(o.rt, 30f);
        o.text.rectTransform.offsetMin = new Vector2(48f, 4f);
        o.text.rectTransform.offsetMax = new Vector2(-48f, -4f);
        return o;
    }

    private static RectTransform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static SlantedShape Shape(RectTransform parent, string name, Color color)
    {
        var rt = NewChild(parent, name);
        Stretch(rt, 0f);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var s = rt.gameObject.AddComponent<SlantedShape>();
        s.color = color;
        s.raycastTarget = false;
        return s;
    }

    private TMP_Text Text(RectTransform parent, float size)
    {
        var rt = NewChild(parent, "Text");
        Stretch(rt, 0f);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fontSource != null)
        {
            t.font = fontSource.font;
            t.fontSharedMaterial = fontSource.fontSharedMaterial;
        }
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = size * 0.65f;
        t.raycastTarget = false;
        return t;
    }

    // ------------------------------------------------------------------ mostrar / ocultar

    // portrait: busto o retrato del personaje; eyesRect: zona de los ojos en él (0-1, desde abajo a la izquierda).
    public void Show(Sprite portrait, Rect eyesRect, bool flip, string speaker, IList<string> texts, Action<int> chosen)
    {
        onChosen = chosen;
        count = texts.Count;
        selected = 0;

        hasEyes = portrait != null;
        strip.gameObject.SetActive(hasEyes);
        if (hasEyes)
        {
            var tex = portrait.texture;
            Rect tr = portrait.textureRect;
            eyesUv = new Rect(
                (tr.x + eyesRect.x * tr.width) / tex.width,
                (tr.y + eyesRect.y * tr.height) / tex.height,
                eyesRect.width * tr.width / tex.width,
                eyesRect.height * tr.height / tex.height);
            eyes.SetImage(tex, eyesUv, flip);
        }
        nameRt.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        nameText.text = speaker;

        while (options.Count < count) options.Add(NewOption(options.Count));
        float top = hasEyes ? OptionsTop : OptionsTopNoEyes;
        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            bool used = i < count;
            o.rt.gameObject.SetActive(used);
            if (!used) continue;
            o.text.text = texts[i];
            o.rt.anchoredPosition = new Vector2(0f, top - i * OptionGap);
            o.lit = i == selected ? 1f : 0f;
        }
        RefreshOptions(true);

        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
        choosing = true;
        previousNavY = 0f;
        readyAt = Time.unscaledTime + InTime * 0.8f;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AnimateIn());
    }

    // Cierre inmediato (cuando se cancela el diálogo)
    public void Hide()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        choosing = false;
        animatingIn = false;
        onChosen = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    private IEnumerator AnimateIn()
    {
        animatingIn = true;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / InTime);
            float e = EaseOutCubic(t);
            dim.color = new Color(0f, 0f, 0f, 0.42f * e);

            // La franja entra desde la izquierda y "abre los ojos" (crece de alto)
            strip.anchoredPosition = new Vector2(Mathf.Lerp(-760f, 0f, e), StripY);
            strip.localScale = new Vector3(1f, Mathf.Lerp(0.12f, 1f, EaseOutBack(t)), 1f);
            stripGroup.alpha = Mathf.Clamp01(t * 2.5f);
            SetEyesSlant(Mathf.Lerp(0.2f, 0.08f, e));

            // Un brillo cruza la franja
            float g = Mathf.Clamp01((t - 0.35f) / 0.65f);
            glint.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-StripWidth * 0.45f, StripWidth * 0.45f, g), 0f);
            glint.color = new Color(1f, 1f, 1f, 0.35f * Mathf.Sin(g * Mathf.PI));

            // Las opciones llegan una tras otra
            for (int i = 0; i < count; i++)
            {
                float k = Mathf.Clamp01((t - 0.25f - 0.15f * i) / 0.5f);
                options[i].group.alpha = k;
                options[i].rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, EaseOutCubic(k));
            }
            yield return null;
        }
        for (int i = 0; i < count; i++) { options[i].group.alpha = 1f; options[i].rt.localScale = Vector3.one; }
        glint.color = new Color(1f, 1f, 1f, 0f);
        animatingIn = false;
        routine = null;
    }

    private IEnumerator AnimateOut(int chosen)
    {
        float t = 0f;
        Vector2 stripFrom = strip.anchoredPosition;
        Vector2 optionFrom = options[chosen].rt.anchoredPosition;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / OutTime);
            float e = EaseInCubic(t);

            // La franja se desliza a la derecha y se inclina más con la velocidad
            strip.anchoredPosition = new Vector2(Mathf.Lerp(stripFrom.x, 1400f, e), stripFrom.y);
            SetEyesSlant(Mathf.Lerp(0.08f, 0.22f, e));
            stripGroup.alpha = 1f - Mathf.Clamp01((t - 0.4f) / 0.6f);
            dim.color = new Color(0f, 0f, 0f, 0.42f * (1f - t));

            // La elegida destella y la sigue; las demás se apagan
            for (int i = 0; i < count; i++)
            {
                var o = options[i];
                if (i == chosen)
                {
                    float d = Mathf.Clamp01((t - 0.25f) / 0.75f);
                    o.rt.anchoredPosition = optionFrom + new Vector2(EaseInCubic(d) * 1400f, 0f);
                    o.group.alpha = 1f - d;
                    o.back.color = Color.Lerp(Color.white, OptionBackSelected, Mathf.Clamp01(t * 4f));
                }
                else
                {
                    o.group.alpha = Mathf.Max(0f, 1f - t * 3f);
                }
            }
            yield return null;
        }
        options[chosen].rt.anchoredPosition = optionFrom;
        root.gameObject.SetActive(false);
        routine = null;

        var callback = onChosen;
        onChosen = null;
        callback?.Invoke(chosen);
    }

    // ------------------------------------------------------------------ entrada

    private void Update()
    {
        if (!IsActive) return;

        // Mientras se decide: la franja flota y los ojos se mueven apenas
        if (choosing && !animatingIn && hasEyes)
        {
            float time = Time.unscaledTime;
            strip.anchoredPosition = new Vector2(0f, StripY + Mathf.Sin(time * 1.6f) * 4f);
            eyes.UvRect = new Rect(eyesUv.x + Mathf.Sin(time * 0.7f) * eyesUv.width * 0.012f, eyesUv.y, eyesUv.width, eyesUv.height);
        }

        RefreshOptions(false);

        if (!choosing || Time.unscaledTime < readyAt) return;
        HandleInput();
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // Teclado
        if (kb != null)
        {
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Move(-1);
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Move(1);

            var digits = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key };
            for (int i = 0; i < count && i < digits.Length; i++)
                if (digits[i].wasPressedThisFrame) { selected = i; Choose(); return; }

            if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                Choose();
                return;
            }
        }

        // Mando: cruceta o stick (un paso por inclinación)
        float navY = GamepadInput.Move.y;
        if (navY > 0.5f && previousNavY <= 0.5f) Move(-1);
        if (navY < -0.5f && previousNavY >= -0.5f) Move(1);
        previousNavY = navY;
        if (GamepadInput.SouthPressed) { Choose(); return; }

        // Mouse: pasar encima selecciona, clic elige
        if (mouse != null)
        {
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 pos = mouse.position.ReadValue();
            int hover = -1;
            for (int i = 0; i < count; i++)
                if (options[i].back.ContainsScreenPoint(pos, cam)) hover = i;
            if (hover >= 0 && mouse.delta.ReadValue().sqrMagnitude > 0f) selected = hover;
            if (hover >= 0 && mouse.leftButton.wasPressedThisFrame)
            {
                selected = hover;
                Choose();
            }
        }
    }

    private void Move(int step)
    {
        if (count == 0) return;
        selected = (selected + step + count) % count;
    }

    private void Choose()
    {
        if (!choosing || count == 0) return;
        choosing = false;
        if (routine != null) StopCoroutine(routine);
        animatingIn = false;
        for (int i = 0; i < count; i++) options[i].rt.localScale = Vector3.one;
        routine = StartCoroutine(AnimateOut(selected));
    }

    // La opción elegida se enciende (carmesí, borde dorado) y se corre un poco a la derecha
    private void RefreshOptions(bool instant)
    {
        if (!choosing) return;
        float top = hasEyes ? OptionsTop : OptionsTopNoEyes;
        for (int i = 0; i < count; i++)
        {
            var o = options[i];
            float target = i == selected ? 1f : 0f;
            o.lit = instant ? target : Mathf.MoveTowards(o.lit, target, Time.unscaledDeltaTime * 8f);
            float e = EaseOutCubic(o.lit);
            o.back.color = Color.Lerp(OptionBack, OptionBackSelected, e);
            o.edge.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Lerp(0.35f, 1f, e));
            o.text.color = Color.Lerp(TextIdle, TextSelected, e);
            o.rt.anchoredPosition = new Vector2(18f * e, top - i * OptionGap);
        }
    }

    private void SetEyesSlant(float s)
    {
        eyes.Slant = s;
        eyesBack.Slant = s;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t) => t * t * t;

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
