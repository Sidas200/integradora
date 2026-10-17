using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Caja de diálogo con efecto de máquina de escribir. Vive en GameSystems.
// Avanza con E, Espacio, Enter o clic; la primera pulsación completa la línea.
// Mientras está abierta, el jugador no se mueve ni ataca.
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
    [SerializeField] private float charsPerSecond = 45f;

    private DialogueData current;
    private int lineIndex;
    private bool typing;
    private Action onFinished;
    private Coroutine typeRoutine;
    private float inputReadyAt;
    private float previousTimeScale = 1f;

    public bool IsOpen => panel != null && panel.activeSelf;

    // Momento en que se cerró el último diálogo (evita reabrirlo con la misma tecla).
    public static float LastClosedTime { get; private set; } = -10f;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
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
        // Si ya había uno abierto, se termina para no perder su callback
        if (IsOpen) Close();

        current = dialogue;
        onFinished = finished;
        lineIndex = 0;
        panel.SetActive(true);
        GameInput.Lock(this);
        // El mundo se detiene durante la conversación (ningún enemigo ataca mientras se lee)
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        inputReadyAt = Time.unscaledTime + 0.15f;
        ShowLine();
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime < inputReadyAt) return;
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

    private static bool AdvancePressed()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        return (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
               || (mouse != null && mouse.leftButton.wasPressedThisFrame);
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
        bodyText.text = line.text;

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
    }

    // Cierra sin ejecutar el callback (cambio de escena con un diálogo abierto).
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
