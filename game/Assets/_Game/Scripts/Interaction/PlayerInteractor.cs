using UnityEngine;
using UnityEngine.InputSystem;

// Va en el jugador: busca el interactivo más cercano, muestra "E: ..." en el HUD y lo usa con E.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float reopenCooldown = 0.3f;

    private Interactable focused;

    private void Update()
    {
        var best = GameInput.Locked ? null : FindBest();
        if (best != focused)
        {
            if (focused != null) focused.SetHighlighted(false);
            focused = best;
            if (focused != null) focused.SetHighlighted(true);
        }

        if (GameHud.Instance != null)
            GameHud.Instance.SetPrompt(focused != null ? "E: " + focused.Prompt : null);

        if (focused == null) return;
        if (Time.unscaledTime - DialogueUI.LastClosedTime < reopenCooldown) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            focused.Interact();
    }

    private void OnDisable()
    {
        if (focused != null) focused.SetHighlighted(false);
        focused = null;
        if (GameHud.Instance != null) GameHud.Instance.SetPrompt(null);
    }

    private Interactable FindBest()
    {
        Interactable best = null;
        float bestDist = float.MaxValue;
        Vector2 me = transform.position;
        foreach (var it in Interactable.All)
        {
            if (it == null || !it.CanInteract) continue;
            float d = Vector2.Distance(me, it.transform.position);
            if (d <= it.Radius && d < bestDist)
            {
                best = it;
                bestDist = d;
            }
        }
        return best;
    }
}
