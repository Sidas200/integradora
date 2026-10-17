using UnityEngine;
using UnityEngine.InputSystem;

// Forja del campamento: con E abre el CraftingPanel existente (recetas de mejora).
// Se cierra con E, Esc o el botón del panel. El panel ya pausa el juego y no consume
// nada si faltan materiales (Inventory.TryConsumeIngredients es todo o nada).
public class ForgeStation : Interactable
{
    [SerializeField] private CraftingPanel panel;

    private float openedAt;

    public void Setup(CraftingPanel craftingPanel) => panel = craftingPanel;

    private void Start()
    {
        // El panel vive en GameSystems (persistente); se busca si no se asignó
        if (panel == null) panel = FindAnyObjectByType<CraftingPanel>(FindObjectsInactive.Include);
    }

    public override bool CanInteract => panel != null && !panel.IsOpen;

    public override void Interact()
    {
        if (panel == null) return;
        panel.Open();
        openedAt = Time.unscaledTime;
    }

    private void Update()
    {
        if (panel == null || !panel.IsOpen || Time.unscaledTime - openedAt < 0.2f) return;
        var kb = Keyboard.current;
        if (kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            panel.Close();
    }
}
