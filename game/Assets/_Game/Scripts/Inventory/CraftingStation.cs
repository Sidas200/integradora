using UnityEngine;
using UnityEngine.InputSystem;

// Mesa de crafteo: con el jugador dentro de la zona, la tecla E abre/cierra el panel.
[RequireComponent(typeof(Collider2D))]
public class CraftingStation : MonoBehaviour
{
    [SerializeField] private CraftingPanel panel;

    private bool playerInside;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (!playerInside || panel == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (panel.IsOpen) panel.Close();
            else panel.Open();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;

        playerInside = false;
        if (panel != null && panel.IsOpen) panel.Close();
    }
}