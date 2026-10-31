using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Pantallas simples (intro provisional, cierre): cualquier tecla, clic o A / × / Start del mando continúa.
// El aviso ("Hint") cambia según el último dispositivo utilizado.
public class ClickToContinue : MonoBehaviour
{
    public enum Action { StartChapter, ReturnToMenu }

    [SerializeField] private Action onContinue;
    [SerializeField] private float minDelay = 1f;
    [Tooltip("Aviso opcional. Si está vacío se busca un texto hijo llamado \"Hint\".")]
    [SerializeField] private TMP_Text hintText;

    private float readyAt;
    private bool done;

    private void Awake()
    {
        if (hintText == null) hintText = UiNav.FindLabel(transform, "Hint");
    }

    private void OnEnable() => readyAt = Time.unscaledTime + minDelay;

    private void Update()
    {
        UpdateHint();

        if (done || Time.unscaledTime < readyAt || GameFlow.Instance == null) return;
        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                       || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                       || GamepadInput.SouthPressed
                       || GamepadInput.StartPressed;
        if (!pressed) return;

        done = true;
        if (onContinue == Action.StartChapter) GameFlow.Instance.StartChapter();
        else GameFlow.Instance.ReturnToMenu();
    }

    private void UpdateHint()
    {
        if (hintText == null) return;
        string where = onContinue == Action.StartChapter ? "continuar" : "volver al menú";
        string message = ControlHints.For(
            "Pulsa cualquier tecla para " + where,
            "Pulsa A para " + where,
            "Pulsa " + ControlHints.Cross + " para " + where);
        if (hintText.text != message) hintText.text = message;
    }
}
