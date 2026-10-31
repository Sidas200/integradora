
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// HEX-01: menu de pausa.
// Compatible con teclado, mouse, Xbox, PS5 y AYN Thor.
// Navegacion mediante EventSystem y textos dinamicos.

public class PauseMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Botones")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;

    [Header("Instrucciones de controles")]
    [SerializeField] private TMP_Text controlsHintText;

    // Lista de controles del juego que crea el builder (etiqueta "Controls" del panel).
    private TMP_Text controlsList;
    private readonly List<Selectable> buttons = new List<Selectable>();

    public bool IsOpen =>
        panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            controlsList = UiNav.FindLabel(panel.transform, "Controls");
            if (controlsList == controlsHintText)
                controlsList = null;
            if (controlsList != null)
            {
                controlsList.enableAutoSizing = true;
                controlsList.fontSizeMin = 16f;
                controlsList.fontSizeMax = controlsList.fontSize;
            }
        }

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Close);

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                Close();
                GameFlow.Instance.Retry();
            });
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(() =>
            {
                Close();
                GameFlow.Instance.ReturnToMenu();
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() =>
                GameFlow.Instance.QuitGame());
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;

        // Teclado: Escape
        bool escapePressed =
            kb != null &&
            kb.escapeKey.wasPressedThisFrame;

        // PS5: Options
        // Xbox / AYN Thor: Start
        bool optionsPressed = GamepadInput.StartPressed;

        // PS5: Circulo
        // Xbox / AYN Thor: B
        bool backPressed = GamepadInput.EastPressed;

        // ==========================
        // MENU ABIERTO
        // ==========================

        if (IsOpen)
        {
            UpdateControlHints();

            // Tras un clic en el fondo el mando se quedaba sin boton seleccionado
            UiNav.KeepSelection(resumeButton, buttons);

            if (escapePressed ||
                optionsPressed ||
                backPressed)
            {
                if (escapePressed)
                    GameInput.ConsumeEscape();

                Close();
            }

            return;
        }

        // ==========================
        // MENU CERRADO
        // ==========================

        // Evitar conflicto con otro menu
        if (GameInput.EscapeConsumedFrame ==
            Time.frameCount)
        {
            return;
        }

        if (!escapePressed && !optionsPressed)
            return;

        var context = SceneContext.Current;

        // GameplayBlocked: tampoco en el frame en que se cerro otro panel
        // (p. ej. Start/Options al omitir un dialogo)
        bool canPause =
            context != null &&
            context.AllowPause &&
            (SceneLoader.Instance == null ||
             !SceneLoader.Instance.IsBusy) &&
            !GameInput.GameplayBlocked;

        if (canPause)
            Open();
    }

    // ==========================
    // ABRIR MENU
    // ==========================

    public void Open()
    {
        if (IsOpen || panel == null)
            return;

        panel.SetActive(true);

        // Pausar juego
        Time.timeScale = 0f;
        GameInput.Lock(this);

        // Configurar recorrido de botones
        ConfigureNavigation();

        // Seleccionar Reanudar
        if (resumeButton != null &&
            resumeButton.IsActive() &&
            resumeButton.interactable &&
            EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                resumeButton.gameObject
            );
        }

        UpdateControlHints();
    }

    // ==========================
    // CERRAR MENU
    // ==========================

    public void Close()
    {
        if (!IsOpen)
            return;

        // Limpiar seleccion del menu
        if (EventSystem.current != null)
        {
            var selected =
                EventSystem.current.currentSelectedGameObject;

            if (selected != null &&
                selected.transform.IsChildOf(panel.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        panel.SetActive(false);

        Time.timeScale = 1f;
        GameInput.Unlock(this);
    }

    // ==========================
    // NAVEGACION DE BOTONES
    // ==========================

    private void ConfigureNavigation()
    {
        buttons.Clear();

        AddNavigationButton(buttons, resumeButton);
        AddNavigationButton(buttons, retryButton);
        AddNavigationButton(buttons, menuButton);
        AddNavigationButton(buttons, quitButton);

        int count = buttons.Count;

        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            Selectable button = buttons[i];

            Navigation nav = button.navigation;

            nav.mode = Navigation.Mode.Explicit;

            // Boton anterior
            nav.selectOnUp =
                buttons[(i - 1 + count) % count];

            // Siguiente boton
            nav.selectOnDown =
                buttons[(i + 1) % count];

            nav.selectOnLeft = null;
            nav.selectOnRight = null;

            button.navigation = nav;
        }
    }

    private static void AddNavigationButton(
        List<Selectable> buttons,
        Button button)
    {
        if (button != null &&
            button.IsActive() &&
            button.interactable)
        {
            buttons.Add(button);
        }
    }

    // ==========================
    // TEXTOS DINAMICOS
    // ==========================

    private void UpdateControlHints()
    {
        string message = ControlHints.For(
            "W/S o Flechas: Elegir   Enter: Confirmar   Esc: Volver",
            "Stick / Cruceta: Elegir   A: Confirmar   B: Volver",
            "Stick / Cruceta: Elegir   " + ControlHints.Cross + ": Confirmar   " + ControlHints.Circle + ": Volver"
        );

        if (controlsHintText != null &&
            controlsHintText.text != message)
        {
            controlsHintText.text = message;
        }

        if (controlsList == null)
            return;

        string list = ControlHints.Format(
            "{move}: mover\n" +
            "{attack}: atacar   ·   {dash}: esquivar\n" +
            "{interact}: interactuar\n" +
            "{potion}: poción   ·   {amrita}: Gota de Amrita\n" +
            "{inventory}: inventario   ·   {pause}: pausa\n\n") + message;

        if (controlsList.text != list)
            controlsList.text = list;
    }
}
