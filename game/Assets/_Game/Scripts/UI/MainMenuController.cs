using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú principal: Nueva partida / Continuar / Salir. Funciona sin red.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text continueHint;

    [Header("Confirmar nueva partida")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button confirmYes;
    [SerializeField] private Button confirmNo;

    private bool hasSave;

    private void Start()
    {
        Time.timeScale = 1f;
        RefreshSaveState();
        if (confirmPanel != null) confirmPanel.SetActive(false);

        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(() => GameFlow.Instance.Continue());
        quitButton.onClick.AddListener(() => GameFlow.Instance.QuitGame());
        if (confirmYes != null) confirmYes.onClick.AddListener(() => GameFlow.Instance.NewGame());
        if (confirmNo != null) confirmNo.onClick.AddListener(() => { confirmPanel.SetActive(false); Select(newGameButton); });

        Select(hasSave ? continueButton : newGameButton);
    }

    // CloudSync la llama al bajar un guardado de la nube con el menú abierto.
    public void RefreshSaveState()
    {
        hasSave = GameFlow.Instance != null && GameFlow.Instance.HasValidSave();
        continueButton.interactable = hasSave;
        if (continueHint != null) continueHint.text = hasSave ? "" : "No hay partida guardada";
    }

    private void OnNewGame()
    {
        if (hasSave && confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            Select(confirmNo);
            return;
        }
        GameFlow.Instance.NewGame();
    }

    private static void Select(Button b)
    {
        if (b != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
    }
}
