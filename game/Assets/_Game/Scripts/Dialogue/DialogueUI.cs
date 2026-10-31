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

    [Header("Busto de Tsuki")]
    [Tooltip("Busto que acompaña la caja: a color cuando habla, en grises cuando habla alguien más.")]
    [SerializeField] private string bustSpeaker = "Tsuki";
    [SerializeField] private string bustResource = "Portraits/tsuki_bust";
    [SerializeField] private string bustGrayResource = "Portraits/tsuki_bust_gray";
    [SerializeField] private Vector2 bustSize = new Vector2(380f, 522f);
    [SerializeField] private float bustFadeSpeed = 8f;

    private Image bust;
    private Sprite bustColor, bustGray;
    private bool bustTalking;
    private float bustLit;
    private RectTransform bodyRect, hintRect;
    private Vector2 bodySize, hintPos;

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

    // El busto se arma en código para no depender de reconstruir el prefab GameSystems.
    // Va a la derecha de la caja, apoyado en su borde inferior y sobresaliendo por arriba.
    private void CreateBust()
    {
        bustColor = Resources.Load<Sprite>(bustResource);
        bustGray = Resources.Load<Sprite>(bustGrayResource);
        if (panel == null || bustColor == null) return;

        var go = new GameObject("SpeakerBust", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(panel.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-12f, 0f);
        rt.sizeDelta = bustSize;
        bust = go.AddComponent<Image>();
        bust.sprite = bustColor;
        bust.preserveAspect = true;
        bust.raycastTarget = false;
        go.SetActive(false);

        if (bodyText != null) { bodyRect = bodyText.rectTransform; bodySize = bodyRect.sizeDelta; }
        if (continueHint != null) { hintRect = continueHint.transform as RectTransform; hintPos = hintRect.anchoredPosition; }
    }

    private bool IsBustSpeaker(DialogueData.Line line) => line.speaker == bustSpeaker;

    // El busto solo aparece en conversaciones donde Tsuki dice algo; el texto se acorta para no taparse.
    private void SetupBust()
    {
        if (bust == null) return;
        bool show = current.lines.Exists(IsBustSpeaker);
        bust.gameObject.SetActive(show);
        float room = show ? bustSize.x - 20f : 0f;
        if (bodyRect != null) bodyRect.sizeDelta = new Vector2(bodySize.x - room, bodySize.y);
        if (hintRect != null) hintRect.anchoredPosition = hintPos - new Vector2(room, 0f);
        bustTalking = false;
        bustLit = 0f;
        ApplyBust();
    }

    private void UpdateBust()
    {
        if (bust == null || !bust.gameObject.activeSelf) return;
        bustLit = Mathf.MoveTowards(bustLit, bustTalking ? 1f : 0f, bustFadeSpeed * Time.unscaledDeltaTime);
        ApplyBust();
    }

    // Encendido: sprite a color, tamaño completo. Apagado: sprite en grises, más oscuro y un poco más chico.
    private void ApplyBust()
    {
        bust.sprite = bustLit > 0.5f || bustGray == null ? bustColor : bustGray;
        float v = Mathf.Lerp(0.7f, 1f, bustLit);
        bust.color = new Color(v, v, v, 1f);
        bust.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, bustLit);
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

        string message = ControlHints.Format("{continue}: continuar   ·   {skip}: saltar");

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
        bustTalking = IsBustSpeaker(line);
        // Cuando Tsuki habla, su busto ya lo representa: no se repite el retrato pequeño
        portraitFrame.SetActive(line.portrait != null && !(bustTalking && bust != null && bust.gameObject.activeSelf));
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
