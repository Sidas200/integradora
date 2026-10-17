using UnityEngine;
using UnityEngine.InputSystem;

// Pantallas simples (intro provisional, cierre): cualquier tecla o clic continúa.
public class ClickToContinue : MonoBehaviour
{
    public enum Action { StartChapter, ReturnToMenu }

    [SerializeField] private Action onContinue;
    [SerializeField] private float minDelay = 1f;

    private float readyAt;
    private bool done;

    private void OnEnable() => readyAt = Time.unscaledTime + minDelay;

    private void Update()
    {
        if (done || Time.unscaledTime < readyAt || GameFlow.Instance == null) return;
        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                       || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!pressed) return;

        done = true;
        if (onContinue == Action.StartChapter) GameFlow.Instance.StartChapter();
        else GameFlow.Instance.ReturnToMenu();
    }
}
