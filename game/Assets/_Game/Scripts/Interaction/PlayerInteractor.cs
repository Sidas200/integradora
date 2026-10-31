using UnityEngine;
using UnityEngine.InputSystem;

// Va en el jugador: busca el interactivo más cercano, muestra "E: ..." (Y / ▲ con mando) en el HUD y lo usa.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float reopenCooldown = 0.3f;

    private Interactable focused;

    
    private void Update()
    {
        // Buscar el objeto interactivo mas cercano
        var best = GameInput.Locked ? null : FindBest();
        // Mismo frame en que se cerró un panel (p. ej. la forja con E): no reabrirlo
        bool justUnlocked = !GameInput.Locked && GameInput.GameplayBlocked;

        if (best != focused)
        {
            if (focused != null)
                focused.SetHighlighted(false);

            focused = best;

            if (focused != null)
                focused.SetHighlighted(true);
        }

        // Detectar teclado
        var kb = Keyboard.current;

        // Mostrar indicacion de interaccion en el HUD
        if (GameHud.Instance != null)
        {
            string prompt = null;

            if (focused != null)
            {
                // Mostrar el boton segun el ultimo dispositivo utilizado
                // E / Y (Xbox, AYN Thor) / ▲ (PlayStation)
                string button = ControlHints.Key("interact");
                prompt = button + ": " + focused.Prompt;
            }

            GameHud.Instance.SetPrompt(prompt);
        }

        // No hay objeto para interactuar
        if (focused == null || justUnlocked)
            return;

        // Evitar abrir nuevamente un dialogo
        // inmediatamente despues de cerrarlo
        if (Time.unscaledTime - DialogueUI.LastClosedTime
            < reopenCooldown)
            return;

        // Teclado: E
        bool keyboardInteract =
            kb != null &&
            kb.eKey.wasPressedThisFrame;

        // Xbox / AYN Thor: Y
        // PlayStation 5: Triangulo
        bool gamepadInteract = GamepadInput.NorthPressed;

        // Ejecutar interaccion
        if (keyboardInteract || gamepadInteract)
        {
            focused.Interact();
        }
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
