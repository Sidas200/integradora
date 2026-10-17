using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// HEX-01: pausa con Esc. Detiene el tiempo y bloquea el input del jugador.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
        if (retryButton != null) retryButton.onClick.AddListener(() => { Close(); GameFlow.Instance.Retry(); });
        if (menuButton != null) menuButton.onClick.AddListener(() => { Close(); GameFlow.Instance.ReturnToMenu(); });
        if (quitButton != null) quitButton.onClick.AddListener(() => GameFlow.Instance.QuitGame());
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (GameInput.EscapeConsumedFrame == Time.frameCount) return; // otro panel ya cerró con Esc

        if (IsOpen)
        {
            Close();
            return;
        }

        var context = SceneContext.Current;
        bool canPause = context != null && context.AllowPause
                        && (SceneLoader.Instance == null || !SceneLoader.Instance.IsBusy)
                        && !GameInput.Locked;
        if (canPause) Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
        GameInput.Lock(this);
        if (resumeButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        Time.timeScale = 1f;
        GameInput.Unlock(this);
    }
}
