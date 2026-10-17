using UnityEngine;

// Forja del campamento: con E abre la ForgeScreen (recetas del equipo). La pantalla
// pausa el juego, se cierra con E o Esc y no consume nada si faltan materiales.
public class ForgeStation : Interactable
{
    public override bool CanInteract => ForgeScreen.Instance != null && !ForgeScreen.Instance.IsOpen;

    public override void Interact()
    {
        if (ForgeScreen.Instance != null) ForgeScreen.Instance.Open();
    }
}
