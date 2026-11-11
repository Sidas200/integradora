using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Caja de dialogo con efecto de maquina de escribir. Vive en GameSystems.
// Avanza con E, Espacio, Enter, clic o A / × de un mando. X del teclado o Start / Options omiten todo.
// Mientras esta abierta, el jugador no se mueve ni ataca.
// El texto de continuar y los {botones} de las lineas cambian segun el ultimo dispositivo utilizado.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Image portrait;
    [SerializeField] private GameObject portraitFrame;
    [SerializeField] private GameObject nameBox;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private GameObject continueHint;
    [Tooltip("Texto opcional dentro de Continue Hint para mostrar el boton del dispositivo activo.")]
    [SerializeField] private TMP_Text continueHintText;
    [SerializeField] private float charsPerSecond = 45f;

    [Header("Bustos")]
    [Tooltip("Bustos que acompañan la caja: a color cuando ese personaje habla, en grises cuando habla otro. " +
             "Tsuki va a la derecha; Varn y Kaede a la izquierda (Varn volteado para que la mire).")]
    [SerializeField] private BustSlot[] busts =
    {
        new BustSlot { speaker = "Tsuki", resource = "Portraits/tsuki_bust", grayResource = "Portraits/tsuki_bust_gray" },
        new BustSlot { speaker = "Varn", resource = "Portraits/varn_bust", grayResource = "Portraits/varn_bust_gray", leftSide = true, flip = true },
        new BustSlot { speaker = "Kaede", resource = "Portraits/kaede_bust", grayResource = "Portraits/kaede_bust_gray", leftSide = true, aliases = new[] { "???" } },
    };
    [SerializeField] private Vector2 bustSize = new Vector2(380f, 522f);
    [SerializeField] private float bustFadeSpeed = 8f;

    [Serializable]
    public class BustSlot
    {
        public string speaker;
        public string resource;
        public string grayResource;
        [Tooltip("Del lado izquierdo de la caja (si no, del derecho).")]
        public bool leftSide;
        [Tooltip("Voltea la imagen horizontalmente.")]
        public bool flip;
        [Tooltip("Otros nombres con los que habla antes de presentarse (p. ej. \"???\"). Solo cuentan en " +
                 "diálogos donde el personaje también habla con su nombre.")]
        public string[] aliases;

        [NonSerialized] public Image image;
        [NonSerialized] public Sprite color, gray;
        [NonSerialized] public bool talking;
        [NonSerialized] public float lit;
        [NonSerialized] public bool aliasesActive;

        public bool Says(string name) =>
            name == speaker || (aliasesActive && aliases != null && Array.IndexOf(aliases, name) >= 0);
    }

    // Lo que ocupan el texto, el nombre y el retrato chico sin bustos (se recorren para no taparse)
    private RectTransform bodyRect, hintRect, nameRect;
    private Vector2 bodySize, bodyPos, hintPos, namePos;
    private bool layoutCaptured;

    private DialogueData current;
    private int lineIndex;
    private bool typing;
    private Action onFinished;
    private Coroutine typeRoutine;
    private float inputReadyAt;
    private float previousTimeScale = 1f;
    private ControlHints.Scheme shownScheme;

    public bool IsOpen => panel != null && panel.activeSelf;

    // Momento en que se cerro el ultimo dialogo (evita reabrirlo con la misma tecla).
    public static float LastClosedTime { get; private set; } = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Al entrar en Play Mode sin recarga de dominio, Time.unscaledTime vuelve a cero.
        LastClosedTime = -10f;
        GameInput.ClearAll();
    }

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);

        // Si el indicador ya contiene un texto TMP, usarlo automaticamente.
        // Si solo es una imagen, asignar un TMP_Text en el Inspector.
        if (continueHintText == null && continueHint != null)
        {
            continueHintText = continueHint.GetComponent<TMP_Text>();
            if (continueHintText == null)
                continueHintText = continueHint.GetComponentInChildren<TMP_Text>(true);
        }

        CreateBust();
    }

    // Los bustos se arman en código para no depender de reconstruir el prefab GameSystems.
    // Van a un lado de la caja, apoyados en su borde inferior y sobresaliendo por arriba.
    private void CreateBust()
    {
        if (panel == null) return;
        foreach (var slot in busts)
        {
            slot.color = Resources.Load<Sprite>(slot.resource);
            slot.gray = Resources.Load<Sprite>(slot.grayResource);
            if (slot.color == null) continue;

            var go = new GameObject("SpeakerBust_" + slot.speaker, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(panel.transform, false);
            float side = slot.leftSide ? 0f : 1f;
            rt.anchorMin = rt.anchorMax = new Vector2(side, 0f);
            // Pivote al centro para poder voltearlo sin que cambie de lugar
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(slot.leftSide ? 12f + bustSize.x / 2f : -12f - bustSize.x / 2f, 0f);
            rt.sizeDelta = bustSize;
            slot.image = go.AddComponent<Image>();
            slot.image.sprite = slot.color;
            slot.image.preserveAspect = true;
            slot.image.raycastTarget = false;
            go.SetActive(false);
        }
    }

    // Se toma al abrir el primer diálogo: UiSkin reacomoda la caja al iniciar, después de Awake
    private void CaptureLayout()
    {
        if (layoutCaptured) return;
        layoutCaptured = true;
        if (bodyText != null) { bodyRect = bodyText.rectTransform; bodySize = bodyRect.sizeDelta; bodyPos = bodyRect.anchoredPosition; }
        if (continueHint != null) { hintRect = continueHint.transform as RectTransform; hintPos = hintRect.anchoredPosition; }
        if (nameBox != null) { nameRect = nameBox.transform as RectTransform; namePos = nameRect.anchoredPosition; }
    }

    private BustSlot BustFor(string speaker)
    {
        foreach (var slot in busts)
            if (slot.image != null && slot.Says(speaker)) return slot;
        return null;
    }

    // Cada busto aparece solo en conversaciones donde su personaje dice algo; el texto (y el nombre,
    // si hay busto a la izquierda) se recorren para no taparse.
    private void SetupBust()
    {
        CaptureLayout();
        float left = 0f, right = 0f;
        foreach (var slot in busts)
        {
            if (slot.image == null) continue;
            bool show = current.lines.Exists(l => l.speaker == slot.speaker);
            slot.aliasesActive = show;
            slot.image.gameObject.SetActive(show);
            slot.talking = false;
            slot.lit = 0f;
            ApplyBust(slot);
            if (!show) continue;
            if (slot.leftSide) left = Mathf.Max(left, 12f + bustSize.x + 20f - bodyPos.x);
            else right = Mathf.Max(right, bustSize.x - 20f);
        }
        left = Mathf.Max(0f, left);
        if (bodyRect != null)
        {
            bodyRect.anchoredPosition = bodyPos + new Vector2(left, 0f);
            bodyRect.sizeDelta = new Vector2(bodySize.x - left - right, bodySize.y);
        }
        if (hintRect != null) hintRect.anchoredPosition = hintPos - new Vector2(right, 0f);
        if (nameRect != null) nameRect.anchoredPosition = namePos + new Vector2(left, 0f);
        leftBustShown = left > 0f;
    }

    private bool leftBustShown;

    private void UpdateBust()
    {
        foreach (var slot in busts)
        {
            if (slot.image == null || !slot.image.gameObject.activeSelf) continue;
            slot.lit = Mathf.MoveTowards(slot.lit, slot.talking ? 1f : 0f, bustFadeSpeed * Time.unscaledDeltaTime);
            ApplyBust(slot);
        }
    }

    // Encendido: sprite a color, tamaño completo. Apagado: sprite en grises, más oscuro y un poco más chico.
    private void ApplyBust(BustSlot slot)
    {
        slot.image.sprite = slot.lit > 0.5f || slot.gray == null ? slot.color : slot.gray;
        float v = Mathf.Lerp(0.7f, 1f, slot.lit);
        slot.image.color = new Color(v, v, v, 1f);
        float scale = Mathf.Lerp(0.95f, 1f, slot.lit);
        slot.image.rectTransform.localScale = new Vector3(slot.flip ? -scale : scale, scale, 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Play(DialogueData dialogue, Action finished = null)
    {
        if (Instance == null || dialogue == null || dialogue.lines.Count == 0)
        {
            finished?.Invoke();
            return;
        }
        Instance.Open(dialogue, finished);
    }

    private void Open(DialogueData dialogue, Action finished)
    {
        // Si ya habia uno abierto, terminarlo para no perder su callback.
        if (IsOpen) Close();

        current = dialogue;
        onFinished = finished;
        lineIndex = 0;
        panel.SetActive(true);
        GameInput.Lock(this);

        // Detener el mundo durante el dialogo.
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        inputReadyAt = Time.unscaledTime + 0.15f;
        SetupBust();
        UpdateControlHint();
        ShowLine();
    }

    private void Update()
    {
        if (!IsOpen) return;
        UpdateBust();

        // Actualizar el indicador aunque el juego este pausado o el texto se este escribiendo.
        UpdateControlHint();
        RefreshLineForDevice();

        if (Time.unscaledTime < inputReadyAt) return;

        // X del teclado o Start / Options del mando saltan toda la conversacion; el callback corre
        // igual, asi la historia avanza como si se leyera. La pausa no se abre en ese frame
        // (GameInput.GameplayBlocked).
        if (SkipPressed())
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

        lineIndex++;
        if (lineIndex >= current.lines.Count) Close();
        else ShowLine();
    }

    private void UpdateControlHint()
    {
        if (continueHintText == null) return;

        string message = ControlHints.Cap("continue") + " Continuar      " + ControlHints.Cap("skip") + " Saltar";

        if (continueHintText.text != message)
            continueHintText.text = message;
    }

    private static bool AdvancePressed()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        bool keyboardAdvance =
            kb != null &&
            (kb.eKey.wasPressedThisFrame ||
             kb.spaceKey.wasPressedThisFrame ||
             kb.enterKey.wasPressedThisFrame);

        bool mouseAdvance =
            mouse != null && mouse.leftButton.wasPressedThisFrame;

        // AYN Thor / Xbox: A; PlayStation: Cruz.
        bool gamepadAdvance = GamepadInput.SouthPressed;

        return keyboardAdvance || mouseAdvance || gamepadAdvance;
    }

    private static bool SkipPressed() =>
        (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) ||
        GamepadInput.StartPressed;

    // Si cambia el dispositivo mientras se lee una linea con {botones}, se reescribe con los nuevos.
    private void RefreshLineForDevice()
    {
        var scheme = ControlHints.Current;
        if (scheme == shownScheme || current == null) return;
        shownScheme = scheme;
        string raw = current.lines[lineIndex].text;
        if (string.IsNullOrEmpty(raw) || raw.IndexOf('{') < 0) return;
        bodyText.text = ControlHints.Format(raw);
        if (!typing) bodyText.maxVisibleCharacters = int.MaxValue;
    }

    private void ShowLine()
    {
        var line = current.lines[lineIndex];
        bool narration = string.IsNullOrEmpty(line.speaker);

        nameBox.SetActive(!narration);
        speakerText.text = line.speaker;
        foreach (var slot in busts) slot.talking = slot.image != null && slot.Says(line.speaker);
        // Si el que habla tiene busto, ya lo representa: no se repite el retrato pequeño.
        // Con un busto a la izquierda el retrato quedaría debajo, así que tampoco se muestra.
        var speakerBust = BustFor(line.speaker);
        bool bustShown = speakerBust != null && speakerBust.image.gameObject.activeSelf;
        portraitFrame.SetActive(line.portrait != null && !bustShown && !leftBustShown);
        portrait.sprite = line.portrait;
        bodyText.fontStyle = narration ? FontStyles.Italic : FontStyles.Normal;
        shownScheme = ControlHints.Current;
        bodyText.text = ControlHints.Format(line.text);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        typing = true;
        continueHint.SetActive(false);
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
        continueHint.SetActive(true);
        UpdateControlHint();
    }

    // Cierra sin ejecutar el callback (cambio de escena con dialogo abierto).
    public void Cancel()
    {
        if (!IsOpen) return;
        onFinished = null;
        Close();
    }

    private void Close()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        panel.SetActive(false);
        current = null;
        GameInput.Unlock(this);
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        LastClosedTime = Time.unscaledTime;

        var callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }
}
