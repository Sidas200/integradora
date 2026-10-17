using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Forja de Kaede (HEX-05): recetas del equipo (Receta_*) con su resultado, efecto y materiales.
// Pausa el juego. Las reglas son las de Inventory: todo o nada, nunca consume si falta algo.
// El Módulo de espada (upgradesSword) sube la espada; las piezas de cuero se equipan desde
// el inventario (Tab › Equipo). Cada forja guarda el progreso (GameFlow.SaveProgress).
public class ForgeScreen : MonoBehaviour
{
    public static ForgeScreen Instance { get; private set; }

    // Igual que PlayerStats.attackPerSwordLevel (no es público)
    private const int SwordUpgradeBonus = 10;

    [SerializeField] private GameObject panel;
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();
    [SerializeField] private List<RecipeRowView> rows = new List<RecipeRowView>();
    [SerializeField] private Sprite swordUpgradeIcon;

    [Header("Detalle")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailEffect;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private List<ItemSlotView> ingredientSlots = new List<ItemSlotView>();
    [SerializeField] private List<TMP_Text> ingredientTexts = new List<TMP_Text>();
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text craftLabel;
    [SerializeField] private Button closeButton;

    private static readonly Color Ready = new Color(0.55f, 0.95f, 0.6f);
    private static readonly Color Missing = new Color(1f, 0.5f, 0.5f);
    private static readonly Color Done = new Color(0.72f, 0.70f, 0.78f);

    private Inventory inventory;
    private PlayerStats stats;
    private PlayerEquipment equipment;
    private int selected;
    private float openedAt;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        for (int i = 0; i < rows.Count; i++)
        {
            int index = i;
            rows[i].Button.onClick.AddListener(() => Select(index));
        }
        if (craftButton != null) craftButton.onClick.AddListener(CraftSelected);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open()
    {
        if (IsOpen) return;
        var player = SceneContext.FindPlayer();
        if (player == null) return;
        inventory = player.GetComponent<Inventory>();
        stats = player.GetComponent<PlayerStats>();
        equipment = player.GetComponent<PlayerEquipment>();
        panel.SetActive(true);
        Time.timeScale = 0f;
        GameInput.Lock(this);
        openedAt = Time.unscaledTime;
        Refresh();
        Select(FirstCraftable());
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        Time.timeScale = 1f;
        GameInput.Unlock(this);
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < 0.2f) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
        {
            if (kb.escapeKey.wasPressedThisFrame) GameInput.ConsumeEscape();
            Close();
        }
        else if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Select(Mathf.Max(0, selected - 1));
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Select(Mathf.Min(recipes.Count - 1, selected + 1));
        else if (kb.enterKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) CraftSelected();
    }

    // ---------------------------------------------------------------- estado de cada receta

    private enum State { Ready, Missing, Owned, Done }

    private State StateOf(CraftingRecipe r)
    {
        if (r.upgradesSword && stats != null && !stats.CanUpgradeSword) return State.Done;
        // Cada pieza de armadura se forja una sola vez
        if (r.result != null && ItemUsage.IsEquipment(r.result.id) && inventory.GetAmount(r.result.id) > 0) return State.Owned;
        return inventory.CanCraft(r) ? State.Ready : State.Missing;
    }

    private static string StateText(State s) =>
        s == State.Ready ? "Listo para forjar" : s == State.Missing ? "Faltan materiales" : s == State.Owned ? "Ya la tienes" : "Ya forjado";

    private static Color StateColor(State s) => s == State.Ready ? Ready : s == State.Missing ? Missing : Done;

    private Sprite IconOf(CraftingRecipe r) =>
        r.upgradesSword || r.result == null || r.result.icon == null ? swordUpgradeIcon : r.result.icon;

    private int FirstCraftable()
    {
        for (int i = 0; i < recipes.Count; i++)
            if (recipes[i] != null && StateOf(recipes[i]) == State.Ready) return i;
        return 0;
    }

    private void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            bool has = i < recipes.Count && recipes[i] != null;
            rows[i].gameObject.SetActive(has);
            if (!has) continue;
            var s = StateOf(recipes[i]);
            rows[i].Show(IconOf(recipes[i]), recipes[i].displayName, StateText(s), StateColor(s));
        }
    }

    private void Select(int index)
    {
        if (recipes.Count == 0) return;
        selected = Mathf.Clamp(index, 0, recipes.Count - 1);
        for (int i = 0; i < rows.Count; i++) rows[i].SetSelected(i == selected);

        var r = recipes[selected];
        var state = StateOf(r);
        detailIcon.sprite = IconOf(r);
        detailName.text = r.displayName;
        detailEffect.text = EffectOf(r);
        detailText.text = r.result != null ? r.result.description : "";

        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            bool has = i < r.ingredients.Count && r.ingredients[i].material != null;
            ingredientSlots[i].gameObject.SetActive(has);
            ingredientTexts[i].gameObject.SetActive(has);
            if (!has) continue;
            var ing = r.ingredients[i];
            int have = inventory.GetAmount(ing.material.id);
            ingredientSlots[i].Show(ing.material.icon, "");
            ingredientTexts[i].text = $"{ing.material.displayName}\n<size=80%>{have} / {ing.amount}</size>";
            ingredientTexts[i].color = have >= ing.amount ? Ready : Missing;
        }

        craftButton.interactable = state == State.Ready;
        craftLabel.text = state == State.Done ? "Ya forjado" : state == State.Owned ? "Ya la tienes" : "Forjar";
    }

    private string EffectOf(CraftingRecipe r)
    {
        if (r.upgradesSword && stats != null)
            return stats.CanUpgradeSword ? $"Despierta la espada carmesí · Daño {stats.Attack} → {stats.Attack + SwordUpgradeBonus}" : "La espada ya despertó";
        if (r.result == null) return "";
        var piece = equipment != null ? equipment.ForItem(r.result.id) : null;
        if (piece != null) return InventoryScreen.BonusText(piece);
        switch (r.result.id)
        {
            case "pocion": return "Recupera 30 de vida";
            case "gota_amrita": return "Despierta a un guardián dormido";
            default: return "";
        }
    }

    // ---------------------------------------------------------------- forjar

    private void CraftSelected()
    {
        if (recipes.Count == 0) return;
        var r = recipes[selected];
        if (StateOf(r) != State.Ready) return;

        bool ok;
        if (r.upgradesSword) ok = stats != null && inventory.TryConsumeIngredients(r) && stats.UpgradeSword();
        else ok = inventory.TryCraft(r);
        if (!ok) return;

        string message = $"¡Forjaste: {r.displayName}!";
        if (r.result != null && ItemUsage.IsEquipment(r.result.id)) message += " Equípala en el inventario (Tab › Equipo).";
        if (GameHud.Instance != null) GameHud.Instance.ShowToast(message, 3f);
        if (GameFlow.Instance != null) GameFlow.Instance.SaveProgress();
        Refresh();
        Select(selected);
    }
}
