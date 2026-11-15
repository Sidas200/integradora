
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menu principal: Nueva partida / Continuar / Salir.
// Compatible con teclado, mouse, Xbox, PS5 y AYN Thor.
// Navegacion mediante EventSystem y textos dinamicos.

public class MainMenuController : MonoBehaviour
{
    [Header("Botones principales")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private TMP_Text continueHint;

    [Header("Confirmar nueva partida")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button confirmYes;
    [SerializeField] private Button confirmNo;

    [Header("Instrucciones de controles")]
    [SerializeField] private TMP_Text controlsHintText;

    private bool hasSave;

    // Tarjeta de cuenta (la crea CloudSync al cargar el menu, a veces despues de Start)
    private CloudAccountPanel account;
    private float nextAccountLookup;
    private readonly List<Selectable> mainButtons = new List<Selectable>();
    private Selectable[] confirmButtons;

    private bool IsConfirmOpen =>
        confirmPanel != null && confirmPanel.activeSelf;

    // ========================================
    // INICIALIZACION
    // ========================================

    private void Start()
    {
        Time.timeScale = 1f;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        // Boton Nueva partida
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGame);

        // Boton Continuar
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() =>
            {
                if (GameFlow.Instance != null)
                    GameFlow.Instance.Continue();
            });
        }

        // Boton Salir
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() =>
            {
                if (GameFlow.Instance != null)
                    GameFlow.Instance.QuitGame();
            });
        }

        // Confirmar nueva partida
        if (confirmYes != null)
        {
            confirmYes.onClick.AddListener(() =>
            {
                if (GameFlow.Instance != null)
                    GameFlow.Instance.NewGame();
            });
        }

        // Cancelar nueva partida
        if (confirmNo != null)
            confirmNo.onClick.AddListener(CancelConfirmation);

        EnsureControlsHintLabel();
        confirmButtons = new Selectable[] { confirmYes, confirmNo };

        RefreshSaveState();

        // Seleccionar opcion inicial
        Select(hasSave ? continueButton : newGameButton);

        UpdateControlHints();
    }

    // ========================================
    // ACTUALIZACION
    // ========================================

    private void Update()
    {
        // Cambiar textos segun el dispositivo
        UpdateControlHints();

        FindAccountPanel();

        // La ventana de inicio de sesion maneja su propia navegacion
        if (account != null && account.LoginOpen)
            return;

        if (!IsConfirmOpen)
        {
            // Tras un clic en el fondo el mando se quedaba sin boton seleccionado
            UiNav.KeepSelection(hasSave ? continueButton : newGameButton, mainButtons);
            return;
        }

        UiNav.KeepSelection(confirmNo, confirmButtons);

        var kb = Keyboard.current;

        // Teclado: Escape
        bool escapePressed =
            kb != null &&
            kb.escapeKey.wasPressedThisFrame;

        // Xbox / AYN Thor: B
        // PlayStation: Circulo
        bool backPressed = GamepadInput.EastPressed;

        if (escapePressed || backPressed)
            CancelConfirmation();
    }

    // El boton de la cuenta entra al recorrido del mando (antes solo se alcanzaba con el mouse).
    private void FindAccountPanel()
    {
        if (account != null || Time.unscaledTime < nextAccountLookup)
            return;

        nextAccountLookup = Time.unscaledTime + 0.5f;
        account = FindAnyObjectByType<CloudAccountPanel>();
        if (account != null)
            ConfigureMainNavigation();
    }

    // ========================================
    // ESTADO DEL GUARDADO
    // ========================================

    // CloudSync puede llamar este metodo
    // al descargar una partida guardada.
    public void RefreshSaveState()
    {
        hasSave =
            GameFlow.Instance != null &&
            GameFlow.Instance.HasValidSave();

        if (continueButton != null)
            continueButton.interactable = hasSave;

        if (continueHint != null)
        {
            continueHint.text = hasSave
                ? ""
                : "No hay partida guardada";
        }

        // Actualizar navegacion para saltar
        // el boton Continuar si esta deshabilitado
        ConfigureMainNavigation();

        // Si Continuar dejo de estar disponible,
        // seleccionar Nueva partida.
        if (!hasSave &&
            continueButton != null &&
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject ==
            continueButton.gameObject)
        {
            Select(newGameButton);
        }
    }

    // ========================================
    // NUEVA PARTIDA
    // ========================================

    private void OnNewGame()
    {
        if (hasSave && confirmPanel != null)
        {
            confirmPanel.SetActive(true);

            // Configurar navegacion entre
            // Si y No
            ConfigureConfirmationNavigation();

            // Seleccionar No por seguridad
            if (confirmNo != null &&
                confirmNo.interactable)
            {
                Select(confirmNo);
            }
            else
            {
                Select(confirmYes);
            }

            UpdateControlHints();
            return;
        }

        if (GameFlow.Instance != null)
            GameFlow.Instance.NewGame();
    }

    // ========================================
    // CANCELAR CONFIRMACION
    // ========================================

    private void CancelConfirmation()
    {
        if (!IsConfirmOpen)
            return;

        confirmPanel.SetActive(false);

        // Regresar a Nueva partida
        Select(newGameButton);

        UpdateControlHints();
    }

    // ========================================
    // NAVEGACION DEL MENU PRINCIPAL
    // ========================================

    private void ConfigureMainNavigation()
    {
        var accountButton = account != null ? account.AccountButton : null;

        ConfigureNavigation(
            newGameButton,
            continueButton,
            quitButton,
            accountButton
        );

        mainButtons.Clear();
        mainButtons.Add(newGameButton);
        mainButtons.Add(continueButton);
        mainButtons.Add(quitButton);
        if (accountButton != null)
            mainButtons.Add(accountButton);
    }

    // ========================================
    // NAVEGACION DE CONFIRMACION
    // ========================================

    private void ConfigureConfirmationNavigation()
    {
        ConfigureNavigation(
            confirmYes,
            confirmNo
        );
    }

    // ========================================
    // CONFIGURAR BOTONES
    // ========================================

    private static void ConfigureNavigation(
        params Button[] candidates)
    {
        var buttons = new List<Button>();

        foreach (var button in candidates)
        {
            if (button != null &&
                button.IsActive() &&
                button.interactable)
            {
                buttons.Add(button);
            }
        }

        int count = buttons.Count;

        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            Button current = buttons[i];

            Button previous =
                buttons[(i - 1 + count) % count];

            Button next =
                buttons[(i + 1) % count];

            Navigation nav = current.navigation;

            nav.mode = Navigation.Mode.Explicit;

            // Permitir navegacion tanto
            // vertical como horizontal
            nav.selectOnUp = previous;
            nav.selectOnDown = next;
            nav.selectOnLeft = previous;
            nav.selectOnRight = next;

            current.navigation = nav;
        }
    }

    // ========================================
    // TEXTOS DINAMICOS
    // ========================================

    private void UpdateControlHints()
    {
        if (controlsHintText == null)
            return;

        string message;

        if (IsConfirmOpen)
        {
            message = ControlHints.For(
                "Flechas: Elegir   Enter: Confirmar   Esc: Volver",
                "Stick / Cruceta: Elegir   A: Confirmar   B: Volver",
                "Stick / Cruceta: Elegir   " + ControlHints.Cross + ": Confirmar   " + ControlHints.Circle + ": Volver"
            );
        }
        else
        {
            message = ControlHints.For(
                "Flechas / Clic: Elegir   Enter / Clic: Confirmar",
                "Stick / Cruceta: Elegir   A: Confirmar",
                "Stick / Cruceta: Elegir   " + ControlHints.Cross + ": Confirmar"
            );
        }

        if (controlsHintText.text != message)
            controlsHintText.text = message;
    }

    // La escena no trae etiqueta de controles asignada: se crea una abajo a la derecha,
    // con la misma fuente que los botones del menu.
    private void EnsureControlsHintLabel()
    {
        if (controlsHintText != null || newGameButton == null)
            return;

        var canvas = newGameButton.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var go = new GameObject("ControlsHint", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.rootCanvas.transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 30f);
        rt.sizeDelta = new Vector2(900f, 40f);

        var text = go.AddComponent<TextMeshProUGUI>();
        var source = newGameButton.GetComponentInChildren<TMP_Text>(true);
        if (source != null)
        {
            text.font = source.font;
            text.fontSharedMaterial = source.fontSharedMaterial;
        }
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Right;
        text.color = new Color(0.72f, 0.70f, 0.78f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        controlsHintText = text;
    }

    // ========================================
    // SELECCIONAR BOTON
    // ========================================

    private static void Select(Button button)
    {
        if (button == null ||
            !button.IsActive() ||
            !button.interactable ||
            EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(
            button.gameObject
        );
    }
}
