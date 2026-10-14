using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PocketMapToggle : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuRoot;

    [Header("Controller")]
    [SerializeField] private PocketMapController controller;

    [Header("Tabs")]
    [SerializeField] private PocketTabsAnimator tabsAnimator;

    [Header("Gameplay")]
    [SerializeField] private bool pauseGame = true;

    [Header("Timing")]
    [SerializeField] private float tabsCloseDelay = 0.32f;
    [SerializeField] private float tabsOpenDelay = 0.08f;

    private bool isOpen = false;
    private bool isAnimating = false;

    private int lastPage = 0;

    public bool IsOpen => isOpen;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (menuRoot == null)
            return;

        FindReferences();

        if (tabsAnimator != null)
        {
            tabsAnimator.HideInstantly();
        }

        menuRoot.SetActive(false);

        isOpen = false;
        isAnimating = false;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        bool tabPressed = false;
        bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current != null)
        {
            tabPressed =
                Keyboard.current.tabKey.wasPressedThisFrame;

            escapePressed =
                Keyboard.current.escapeKey.wasPressedThisFrame;
        }

#endif

#if ENABLE_LEGACY_INPUT_MANAGER

        tabPressed |=
            Input.GetKeyDown(KeyCode.Tab);

        escapePressed |=
            Input.GetKeyDown(KeyCode.Escape);

#endif

        if (isAnimating)
            return;

        if (tabPressed)
        {
            ToggleMenu();
        }

        if (
            escapePressed &&
            isOpen
        )
        {
            CloseMenu();
        }
    }

    // =========================================================
    // SETUP
    // =========================================================

    public void SetMenu(GameObject menu)
    {
        menuRoot = menu;

        FindReferences();
    }

    private void FindReferences()
    {
        if (menuRoot == null)
            return;

        controller =
            menuRoot.GetComponent<PocketMapController>();

        Transform tabs =
            menuRoot.transform.Find(
                "PixelMapRoot/SideTabs"
            );

        if (tabs != null)
        {
            tabsAnimator =
                tabs.GetComponent<PocketTabsAnimator>();
        }
    }

    // =========================================================
    // TOGGLE
    // =========================================================

    public void ToggleMenu()
    {
        if (isAnimating)
            return;

        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    // =========================================================
    // OPEN
    // =========================================================

    public void OpenMenu()
    {
        if (
            menuRoot == null ||
            isAnimating
        )
            return;

        StartCoroutine(
            OpenRoutine()
        );
    }

    private IEnumerator OpenRoutine()
    {
        isAnimating = true;

        menuRoot.SetActive(true);

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        FindReferences();

        // Tabs quedan ocultos durante la apertura.
        if (tabsAnimator != null)
        {
            tabsAnimator.HideInstantly();
        }

        // Recuperamos la última página usada.
        if (controller != null)
        {
            controller.SetCurrentPageInstant(lastPage);
        }

        // Abrir la libreta usando los frames originales
        // pero en orden inverso.
        if (controller != null)
        {
            yield return
                controller.PlayNotebookOpenAnimation();
        }

        yield return new WaitForSecondsRealtime(
            tabsOpenDelay
        );

        // Después salen los botones laterales.
        if (tabsAnimator != null)
        {
            tabsAnimator.ShowTabs();
        }

        isOpen = true;
        isAnimating = false;
    }

    // =========================================================
    // CLOSE
    // =========================================================

    public void CloseMenu()
    {
        if (
            menuRoot == null ||
            isAnimating
        )
            return;

        StartCoroutine(
            CloseRoutine()
        );
    }

    private IEnumerator CloseRoutine()
    {
        isAnimating = true;

        if (controller != null)
        {
            lastPage =
                controller.CurrentPage;
        }

        // Primero esconder los círculos.
        if (tabsAnimator != null)
        {
            tabsAnimator.HideTabs();
        }

        yield return new WaitForSecondsRealtime(
            tabsCloseDelay
        );

        // Después guardar/cerrar la libreta completa.
        if (controller != null)
        {
            yield return
                controller.PlayNotebookCloseAnimation();
        }

        menuRoot.SetActive(false);

        if (pauseGame)
        {
            Time.timeScale = 1f;
        }

        isOpen = false;
        isAnimating = false;
    }
}