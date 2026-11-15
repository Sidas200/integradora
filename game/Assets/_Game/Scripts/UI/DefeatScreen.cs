using UnityEngine;
using UnityEngine.UI;

// HEX-07: pantalla de derrota con reintento desde el último punto seguro.
// Con mando: stick / cruceta eligen y A / × confirma (EventSystem).
public class DefeatScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    // Pausa breve antes de aceptar A: si el jugador venía pulsando A (dash) al morir,
    // no debe reintentar sin querer en cuanto aparece la pantalla.
    private const float InputDelay = 0.4f;

    // Con la animación de muerte detrás, el fondo es más transparente para que se vea a Tsuki bajo el reflector
    private const float BackdropAlphaOverStage = 0.55f;

    private Selectable[] buttons;
    private float readyAt;
    private Image backdrop;
    private float backdropAlpha = -1f;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (retryButton != null) retryButton.onClick.AddListener(() => { Hide(); GameFlow.Instance.Retry(); });
        if (menuButton != null) menuButton.onClick.AddListener(() => { Hide(); GameFlow.Instance.ReturnToMenu(); });
        buttons = new Selectable[] { retryButton, menuButton };
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (readyAt > 0f && Time.unscaledTime >= readyAt)
        {
            readyAt = 0f;
            SetInteractable(true);
            UiNav.ChainNavigation(buttons, true);
            UiNav.Select(retryButton);
        }

        // Tras un clic en el fondo el mando se quedaba sin botón seleccionado
        if (readyAt == 0f) UiNav.KeepSelection(retryButton, buttons);
    }

    private void SetInteractable(bool value)
    {
        if (retryButton != null) retryButton.interactable = value;
        if (menuButton != null) menuButton.interactable = value;
    }

    public void Show(bool overDeathStage = false)
    {
        if (backdrop == null) backdrop = panel.GetComponent<Image>();
        if (backdrop != null)
        {
            if (backdropAlpha < 0f) backdropAlpha = backdrop.color.a;
            var c = backdrop.color;
            c.a = overDeathStage ? Mathf.Min(backdropAlpha, BackdropAlphaOverStage) : backdropAlpha;
            backdrop.color = c;
        }

        panel.SetActive(true);
        GameInput.Lock(this);
        SetInteractable(false);
        readyAt = Time.unscaledTime + InputDelay;
    }

    public void Hide()
    {
        if (panel != null)
        {
            UiNav.ClearSelectionIn(panel.transform);
            panel.SetActive(false);
        }
        readyAt = 0f;
        SetInteractable(true);
        GameInput.Unlock(this);
    }
}
