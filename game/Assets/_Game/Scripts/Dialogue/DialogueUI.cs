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

        UpdateControlHint();
        ShowLine();
    }

    private void Update()
    {
        if (!IsOpen) return;

        // Actualizar el indicador aunque el juego este pausado o el texto se este escribiendo.
        UpdateControlHint();
        RefreshLineForDevice();

        if (Time.unscaledTime < inputReadyAt) return;

        // X del teclado o Start / Options del mando saltan toda la conversacion y ejecutan el callback.
        // La pausa no se abre en ese frame (GameInput.GameplayBlocked).
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
        portraitFrame.SetActive(line.portrait != null);
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
