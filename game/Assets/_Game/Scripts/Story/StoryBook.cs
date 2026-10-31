using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cuento de la intro: el diálogo se lee en un libro que se abre y pasa de página (estilo "Érase una vez").
// Cada línea del DialogueData es una doble página: la ilustración de esa línea a la izquierda y el texto
// a la derecha con máquina de escribir (con el retrato de quien habla junto a su nombre).
// Mismos controles que DialogueUI: E, Espacio, Enter, clic o A / × avanzan (la primera pulsación completa la línea);
// X del teclado o Start / Options saltan todo.
// Todo se construye por código, así que no depende de sprites de libro.
public class StoryBook : MonoBehaviour
{
    [Serializable]
    public struct SpeakerPortrait
    {
        public string speaker;
        public Sprite portrait;
    }

    [Tooltip("Ilustración de cada doble página, en el mismo orden que las líneas del diálogo.")]
    [SerializeField] private Sprite[] pageArt;
    [Tooltip("Si una línea no tiene ilustración propia, se usa esta.")]
    [SerializeField] private Sprite narrationArt;
    [Tooltip("Retratos del libro por personaje (si no hay, se usa el retrato de la línea).")]
    [SerializeField] private SpeakerPortrait[] portraits;
    [SerializeField] private float charsPerSecond = 40f;
    [SerializeField] private float flipDuration = 0.6f;
    [Tooltip("Estrellas que parpadean en el fondo negro (0 = solo negro).")]
    [SerializeField] private int starCount = 90;

    private const float PageW = 820f, PageH = 600f;

    private static readonly Color Leather = new Color(0.33f, 0.17f, 0.11f);
    private static readonly Color LeatherDark = new Color(0.22f, 0.11f, 0.07f);
    private static readonly Color Gold = new Color(0.85f, 0.68f, 0.33f);
    private static readonly Color Paper = new Color(0.93f, 0.87f, 0.73f);
    private static readonly Color PaperEdge = new Color(0.80f, 0.72f, 0.56f);
    private static readonly Color Ink = new Color(0.29f, 0.18f, 0.12f);
    private static readonly Color InkSoft = new Color(0.29f, 0.18f, 0.12f, 0.55f);

    private CanvasGroup group;
    private RectTransform book, spreadRoot, cover;
    private RectTransform leftPage, rightPage;
    private TMP_Text bodyText;
    private GameObject hint;
    private TMP_Text hintText;
    private Image[] stars;
    private float[] starPhase;

    private DialogueData current;
    private int lineIndex;
    private bool typing, busy, open;
    private Action onFinished;
    private Coroutine typeRoutine;
    private float inputReadyAt;

    public void Setup(Sprite[] pages, Sprite fallback, SpeakerPortrait[] speakerPortraits)
    {
        pageArt = pages;
        narrationArt = fallback;
        portraits = speakerPortraits;
    }

    public void Play(DialogueData dialogue, Action finished = null)
    {
        if (dialogue == null || dialogue.lines.Count == 0)
        {
            finished?.Invoke();
            return;
        }
        current = dialogue;
        onFinished = finished;
        lineIndex = 0;
        open = true;
        GameInput.Lock(this);
        Build();
        StartCoroutine(OpenBook());
    }

    private void OnDestroy() => GameInput.Unlock(this);

    // ---------------------------------------------------------- INPUT
    private void Update()
    {
        TwinkleStars();

        // Actualizar las instrucciones segun el ultimo dispositivo utilizado.
        // Solo cambia el texto; no afecta las animaciones ni los controles reales.
        if (open && hintText != null)
        {
            string message = ControlHints.Format("{continue}: continuar     {skip}: saltar");

            if (hintText.text != message)
                hintText.text = message;
        }

        if (!open || busy || Time.unscaledTime < inputReadyAt) return;
        // X del teclado o Start / Options del mando
        if ((Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) ||
            GamepadInput.StartPressed)
        {
            Close();
            return;
        }
        if (!AdvancePressed()) return;

        if (typing)
        {
            FinishTyping();
            return;
        }

        if (lineIndex + 1 >= current.lines.Count) StartCoroutine(CloseBook());
        else StartCoroutine(Flip(lineIndex + 1));
    }

    
    private static bool AdvancePressed()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // Teclado: E, Espacio o Enter
        bool keyboardAdvance =
            kb != null &&
            (
                kb.eKey.wasPressedThisFrame ||
                kb.spaceKey.wasPressedThisFrame ||
                kb.enterKey.wasPressedThisFrame
            );

        // Mouse: clic izquierdo
        bool mouseAdvance =
            mouse != null &&
            mouse.leftButton.wasPressedThisFrame;

        // AYN Thor / Xbox: A
        // PlayStation 5: Cruz
        bool gamepadAdvance = GamepadInput.SouthPressed;

        return keyboardAdvance ||
            mouseAdvance ||
            gamepadAdvance;
    }


    // ---------------------------------------------------------- CONSTRUCCIÓN
    private void Build()
    {
        var cgo = new GameObject("StoryBookCanvas", typeof(RectTransform));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        group = cgo.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var backdrop = Rect(cgo.transform, "Backdrop", Vector2.zero, Vector2.zero, Color.black);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        BuildStars(backdrop);

        book = Rect(cgo.transform, "Book", new Vector2(PageW * 2f, PageH), new Vector2(0f, 30f), null);

        // tapa de cuero que asoma por detrás de las hojas
        Rect(book, "Shadow", new Vector2(PageW * 2f + 90f, PageH + 80f), new Vector2(10f, -18f), new Color(0f, 0f, 0f, 0.45f));
        Rect(book, "Cover", new Vector2(PageW * 2f + 70f, PageH + 60f), Vector2.zero, Leather);
        Rect(book, "Spine", new Vector2(46f, PageH + 60f), Vector2.zero, LeatherDark);

        // grosor de las hojas (bordes apilados)
        for (int i = 3; i >= 1; i--)
        {
            float off = i * 5f;
            Rect(book, "EdgeL", new Vector2(PageW, PageH), new Vector2(-PageW / 2f - off, -off), PaperEdge);
            Rect(book, "EdgeR", new Vector2(PageW, PageH), new Vector2(PageW / 2f + off, -off), PaperEdge);
        }

        spreadRoot = Rect(book, "Spread", new Vector2(PageW * 2f, PageH), Vector2.zero, null);

        // tapa que se abre al empezar (cubre la hoja derecha y gira sobre el lomo)
        cover = Rect(book, "FrontCover", new Vector2(PageW + 35f, PageH + 60f), Vector2.zero, Leather);
        cover.pivot = new Vector2(0f, 0.5f);
        cover.anchoredPosition = Vector2.zero;
        var frame = Rect(cover, "Frame", new Vector2(PageW - 60f, PageH - 20f), new Vector2((PageW + 35f) / 2f, 0f), Gold);
        Rect(frame, "Inner", new Vector2(PageW - 72f, PageH - 32f), Vector2.zero, Leather);
        var title = Text(cover, "Érase una vez...", 64, Gold, new Vector2(PageW - 120f, 300f), new Vector2((PageW + 35f) / 2f, 40f));
        title.fontStyle = FontStyles.Italic | FontStyles.Bold;

        // La instruccion se determina dinamicamente mediante ControlHints.
        var h = Text(cgo.transform, "", 26, new Color(1f, 1f, 1f, 0.8f),
             new Vector2(1200f, 40f), new Vector2(0f, -495f));
        hintText = h;
        hint = h.gameObject;
        hint.SetActive(false);

        leftPage = BuildPage(current.lines[0], true, 0);
        rightPage = BuildPage(current.lines[0], false, 0);
    }

    private void BuildStars(RectTransform parent)
    {
        stars = new Image[starCount];
        starPhase = new float[starCount];
        for (int i = 0; i < starCount; i++)
        {
            float size = UnityEngine.Random.value < 0.85f ? UnityEngine.Random.Range(2f, 4f) : UnityEngine.Random.Range(5f, 7f);
            var star = Rect(parent, "Star", new Vector2(size, size), Vector2.zero, Color.white);
            star.anchorMin = star.anchorMax = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
            if (size > 4f) star.localRotation = Quaternion.Euler(0f, 0f, 45f);
            stars[i] = star.GetComponent<Image>();
            starPhase[i] = UnityEngine.Random.Range(0f, 10f);
        }
    }

    private void TwinkleStars()
    {
        if (stars == null) return;
        float t = Time.unscaledTime;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            float a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * (0.8f + i % 5 * 0.35f) + starPhase[i]));
            stars[i].color = new Color(0.95f, 0.92f, 1f, a);
        }
    }

    // Una hoja con su contenido. El pivote queda en el lomo para poder "girarla".
    private RectTransform BuildPage(DialogueData.Line line, bool left, int index)
    {
        var page = Rect(spreadRoot, left ? "PageL" : "PageR", new Vector2(PageW, PageH), Vector2.zero, Paper);
        page.pivot = new Vector2(left ? 1f : 0f, 0.5f);
        page.anchoredPosition = Vector2.zero;

        // sombra suave junto al lomo
        var fold = Rect(page, "Fold", new Vector2(40f, PageH), new Vector2(left ? PageW / 2f - 20f : -PageW / 2f + 20f, 0f),
                        new Color(0.45f, 0.33f, 0.2f, 0.18f));
        fold.SetAsFirstSibling();

        // marco fino
        var border = Rect(page, "Border", new Vector2(PageW - 70f, PageH - 70f), Vector2.zero, InkSoft);
        Rect(border, "Fill", new Vector2(PageW - 74f, PageH - 74f), Vector2.zero, Paper);

        int pageNumber = index * 2 + (left ? 1 : 2);
        Text(page, pageNumber.ToString(), 26, InkSoft, new Vector2(100f, 40f), new Vector2(left ? -PageW / 2f + 80f : PageW / 2f - 80f, -PageH / 2f + 55f));

        if (left) BuildIllustration(page, line, index);
        else BuildText(page, line, index);
        return page;
    }

    private void BuildIllustration(RectTransform page, DialogueData.Line line, int index)
    {
        var art = pageArt != null && index < pageArt.Length && pageArt[index] != null ? pageArt[index] : narrationArt;
        if (art == null) art = line.portrait;
        if (art == null)
        {
            var orn = Text(page, "~ * ~", 90, InkSoft, new Vector2(480f, 200f), Vector2.zero);
            orn.fontStyle = FontStyles.Bold;
            return;
        }

        // el marco se ajusta a la proporción de la ilustración
        Vector2 max = new Vector2(PageW - 150f, PageH - 150f);
        float aspect = art.rect.width / art.rect.height;
        Vector2 size = max.x / aspect <= max.y ? new Vector2(max.x, max.x / aspect) : new Vector2(max.y * aspect, max.y);
        var frame = Rect(page, "ArtFrame", size + new Vector2(12f, 12f), new Vector2(0f, 10f), Ink);
        var img = Rect(frame, "Art", size, Vector2.zero, Color.white);
        img.GetComponent<Image>().sprite = art;
    }

    private Sprite PortraitFor(DialogueData.Line line)
    {
        if (portraits != null)
            foreach (var p in portraits)
                if (p.portrait != null && p.speaker == line.speaker) return p.portrait;
        return line.portrait;
    }

    private void BuildText(RectTransform page, DialogueData.Line line, int index)
    {
        bool narration = string.IsNullOrEmpty(line.speaker);
        if (!narration)
        {
            var portrait = PortraitFor(line);
            if (portrait != null)
            {
                var frame = Rect(page, "PortraitFrame", new Vector2(162f, 162f), new Vector2(0f, 170f), Ink);
                var img = Rect(frame, "Portrait", new Vector2(150f, 150f), Vector2.zero, Color.white);
                var image = img.GetComponent<Image>();
                image.sprite = portrait;
                image.preserveAspect = true;
            }
            var name = Text(page, line.speaker, 46, Ink, new Vector2(560f, 60f), new Vector2(0f, portrait != null ? 55f : 170f));
            name.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
            Rect(page, "Rule", new Vector2(240f, 3f), new Vector2(0f, portrait != null ? 20f : 135f), InkSoft);
        }

        var body = narration
            ? Text(page, Decorate(ControlHints.Format(line.text), true), 42, Ink, new Vector2(620f, 420f), Vector2.zero)
            : Text(page, ControlHints.Format(line.text), 38, Ink, new Vector2(620f, 220f), new Vector2(0f, -140f));
        body.alignment = narration ? TextAlignmentOptions.Center : TextAlignmentOptions.Top;
        body.fontStyle = narration ? FontStyles.Italic : FontStyles.Normal;
        body.lineSpacing = 10f;
        body.maxVisibleCharacters = 0;
        bodyText = body;
    }

    // Letra capital grande al inicio de la narración, como en los cuentos.
    private static string Decorate(string text, bool narration)
    {
        if (!narration || string.IsNullOrEmpty(text) || !char.IsLetter(text[0])) return text;
        return "<size=180%><b>" + text[0] + "</b></size>" + text.Substring(1);
    }

    // ---------------------------------------------------------- ANIMACIONES
    private IEnumerator OpenBook()
    {
        busy = true;
        book.localScale = Vector3.one * 0.85f;
        yield return Tween(0.4f, p =>
        {
            group.alpha = p;
            book.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, p);
        });
        yield return new WaitForSecondsRealtime(0.5f);

        // la tapa gira sobre el lomo y deja ver la primera página
        var coverImg = cover.GetComponent<Image>();
        yield return Tween(flipDuration * 1.2f, p =>
        {
            cover.localScale = new Vector3(Mathf.Max(0.001f, 1f - p), 1f, 1f);
            coverImg.color = Color.Lerp(Leather, LeatherDark, p);
        });
        Destroy(cover.gameObject);

        hint.SetActive(true);
        busy = false;
        StartTyping();
    }

    private IEnumerator Flip(int next)
    {
        busy = true;
        var oldLeft = leftPage;
        var oldRight = rightPage;
        var line = current.lines[next];

        // la página derecha nueva queda debajo; la vieja se levanta hacia el lomo
        var newRight = BuildPage(line, false, next);
        newRight.SetSiblingIndex(oldRight.GetSiblingIndex());
        var newLeft = BuildPage(line, true, next);
        newLeft.localScale = new Vector3(0.001f, 1f, 1f);

        var shade = Rect(oldRight, "Shade", new Vector2(PageW, PageH), Vector2.zero, new Color(0f, 0f, 0f, 0f));
        var shadeImg = shade.GetComponent<Image>();
        yield return Tween(flipDuration * 0.5f, p =>
        {
            float q = Ease(p);
            oldRight.localScale = new Vector3(Mathf.Max(0.001f, 1f - q), 1f, 1f);
            shadeImg.color = new Color(0f, 0f, 0f, 0.35f * q);
        });
        Destroy(oldRight.gameObject);

        // y cae del otro lado ya con la ilustración nueva
        newLeft.SetAsLastSibling();
        var shadeL = Rect(newLeft, "Shade", new Vector2(PageW, PageH), Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
        var shadeLImg = shadeL.GetComponent<Image>();
        yield return Tween(flipDuration * 0.5f, p =>
        {
            float q = Ease(p);
            newLeft.localScale = new Vector3(Mathf.Max(0.001f, q), 1f, 1f);
            shadeLImg.color = new Color(0f, 0f, 0f, 0.35f * (1f - q));
        });
        newLeft.localScale = Vector3.one;
        Destroy(shadeL.gameObject);
        Destroy(oldLeft.gameObject);

        leftPage = newLeft;
        rightPage = newRight;
        lineIndex = next;
        busy = false;
        StartTyping();
    }

    private IEnumerator CloseBook()
    {
        busy = true;
        hint.SetActive(false);
        yield return Tween(0.5f, p =>
        {
            group.alpha = 1f - p;
            book.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, p);
        });
        Close();
    }

    private void StartTyping()
    {
        inputReadyAt = Time.unscaledTime + 0.1f;
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        typing = true;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;
        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        FinishTyping();
    }

    private void FinishTyping()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;
        typing = false;
        bodyText.maxVisibleCharacters = int.MaxValue;
    }

    private void Close()
    {
        if (!open) return;
        open = false;
        StopAllCoroutines();
        GameInput.Unlock(this);
        if (group != null) Destroy(group.gameObject);

        var callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private static IEnumerator Tween(float duration, Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }

    private static float Ease(float x) => x * x * (3f - 2f * x);

    // ---------------------------------------------------------- AYUDAS UI
    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 pos, Color? color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        if (color.HasValue)
        {
            var img = go.AddComponent<Image>();
            img.color = color.Value;
            img.raycastTarget = false;
        }
        return rt;
    }

    private static TMP_Text Text(Transform parent, string text, float size, Color color, Vector2 box, Vector2 pos)
    {
        var rt = Rect(parent, "Text", box, pos, null);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
