using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// HEX-07: pantalla de derrota con reintento desde el último punto seguro.
public class DefeatScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (retryButton != null) retryButton.onClick.AddListener(() => { Hide(); GameFlow.Instance.Retry(); });
        if (menuButton != null) menuButton.onClick.AddListener(() => { Hide(); GameFlow.Instance.ReturnToMenu(); });
    }

    public void Show()
    {
        panel.SetActive(true);
        GameInput.Lock(this);
        if (retryButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        GameInput.Unlock(this);
    }
}
