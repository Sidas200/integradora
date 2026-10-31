
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Forja de Kaede (HEX-05).
// Permite fabricar objetos y mejorar la espada.
// Compatible con teclado, mouse, Xbox, PS5 y AYN Thor.
// Incluye navegación continua y textos dinámicos.

public class ForgeScreen : MonoBehaviour
{
    public static ForgeScreen Instance { get; private set; }

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

    [Header("Navegacion con control")]
    [SerializeField] private float navigationInitialDelay = 0.30f;
    [SerializeField] private float navigationRepeatInterval = 0.12f;

    private NavRepeat navigation;

    private TMP_Text closeLabel;
    // Ayuda de controles que crea el builder (etiqueta "Hint" del panel)
    private TMP_Text hintLabel;

    private static readonly Color Ready =
        new Color(0.55f, 0.95f, 0.6f);

    private static readonly Color Missing =
        new Color(1f, 0.5f, 0.5f);

    private static readonly Color Done =
        new Color(0.72f, 0.70f, 0.78f);

    private Inventory inventory;
    private PlayerStats stats;
    private PlayerEquipment equipment;

    private int selected;
    private float openedAt;

    public bool IsOpen =>
        panel != null && panel.activeSelf;

    // ========================================
    // INICIALIZACION
    // ========================================

    private void Awake()
    {
        Instance = this;

        navigation = new NavRepeat(
            navigationInitialDelay,
            navigationRepeatInterval
        );

        if (panel != null)
        {
            panel.SetActive(false);
            hintLabel = UiNav.FindLabel(panel.transform, "Hint");
        }

        for (int i = 0; i < rows.Count; i++)
        {
            int index = i;

            rows[i].Button.onClick.AddListener(
                () => Select(index)
            );
        }

        if (craftButton != null)
            craftButton.onClick.AddListener(CraftSelected);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);

            closeLabel =
                closeButton.GetComponentInChildren<TMP_Text>(true);
        }

        // El mando se maneja en Update; el EventSystem no debe navegar estos botones.
        var selectables = new List<Selectable> { craftButton, closeButton };
        foreach (var row in rows)
            if (row != null) selectables.Add(row.Button);
        UiNav.DisableNavigation(selectables);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ========================================
    // ABRIR FORJA
    // ========================================

    public void Open()
    {
        if (IsOpen)
            return;

        var player = SceneContext.FindPlayer();

        if (player == null)
            return;

        inventory = player.GetComponent<Inventory>();
        stats = player.GetComponent<PlayerStats>();
        equipment = player.GetComponent<PlayerEquipment>();

        panel.SetActive(true);

        // Pausar mundo
        Time.timeScale = 0f;
        GameInput.Lock(this);

        openedAt = Time.unscaledTime;

        // Reiniciar navegacion
        navigation.Reset();

        // Actualizar recetas
        Refresh();

        // Seleccionar primera receta disponible
        Select(FirstCraftable());

        // Limpiar seleccion de EventSystem
        UiNav.ClearSelectionIn(panel.transform);

        // Mostrar botones correspondientes
        UpdateControlHints();
    }

    // ========================================
    // CERRAR FORJA
    // ========================================

    public void Close()
    {
        if (!IsOpen)
            return;

        panel.SetActive(false);

        Time.timeScale = 1f;
        GameInput.Unlock(this);

        navigation.Reset();
    }

    // ========================================
    // CONTROLES
    // ========================================

    private void Update()
    {
        if (!IsOpen)
            return;

        // Actualizar instrucciones en pantalla
        UpdateControlHints();

        // El panel maneja el mando por codigo: si el EventSystem tuviera un boton
        // seleccionado, A confirmaria dos veces (boton + Forjar).
        UiNav.ClearSelectionIn(panel.transform);

        // Evitar pulsaciones accidentales
        // inmediatamente al abrir la forja
        if (Time.unscaledTime - openedAt < 0.2f)
            return;

        var kb = Keyboard.current;

        // ====================================
        // CERRAR FORJA
        // ====================================

        // Teclado: Escape / E
        bool escapePressed =
            kb != null &&
            kb.escapeKey.wasPressedThisFrame;

        bool keyboardClose =
            escapePressed ||
            (kb != null && kb.eKey.wasPressedThisFrame);

        // Xbox / Thor: B
        // PlayStation: Circulo
        bool gamepadClose = GamepadInput.EastPressed;

        if (keyboardClose || gamepadClose)
        {
            if (escapePressed)
                GameInput.ConsumeEscape();

            Close();
            return;
        }

        // ====================================
        // NAVEGACION (stick, cruceta, W/S, flechas)
        // ====================================

        // Se leen todos los mandos conectados y solo el eje vertical: con el stick
        // en diagonal sigue bajando. Mantener inclinado repite el paso.
        float vertical = GamepadInput.Move.y;

        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                vertical = 1f;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                vertical = -1f;
        }

        int direction = navigation.Step(new Vector2(0f, vertical)).y;

        if (direction != 0)
        {
            if (recipes.Count > 0)
            {
                Select(
                    Mathf.Clamp(
                        selected + direction,
                        0,
                        recipes.Count - 1
                    )
                );
            }

            return;
        }

        // ====================================
        // FABRICAR OBJETO
        // ====================================

        // Teclado: F / Enter
        bool keyboardCraft =
            kb != null &&
            (
                kb.enterKey.wasPressedThisFrame ||
                kb.numpadEnterKey.wasPressedThisFrame ||
                kb.fKey.wasPressedThisFrame
            );

        // Xbox / Thor: A
        // PlayStation: Cruz
        bool gamepadCraft = GamepadInput.SouthPressed;

        if (keyboardCraft || gamepadCraft)
        {
            CraftSelected();
        }
    }

    // ========================================
    // TEXTOS DINAMICOS SEGUN DISPOSITIVO
    // ========================================

    private void UpdateControlHints()
    {
        if (!IsOpen)
            return;

        // Ayuda general del panel
        if (hintLabel != null)
        {
            string hint = ControlHints.For(
                "W/S: elegir   ·   F: forjar   ·   E / Esc: cerrar",
                "Stick / Cruceta: elegir   ·   A: forjar   ·   B: cerrar",
                "Stick / Cruceta: elegir   ·   " + ControlHints.Cross + ": forjar   ·   " + ControlHints.Circle + ": cerrar"
            );

            if (hintLabel.text != hint)
                hintLabel.text = hint;
        }

        // Texto para cerrar
        string closeText = ControlHints.For(
            "Esc / E: Cerrar",
            "B: Cerrar",
            ControlHints.Circle + ": Cerrar"
        );

        if (closeLabel != null &&
            closeLabel.text != closeText)
        {
            closeLabel.text = closeText;
        }

        // Validar receta
        if (recipes.Count == 0 ||
            selected < 0 ||
            selected >= recipes.Count ||
            recipes[selected] == null)
        {
            return;
        }

        State state = StateOf(recipes[selected]);

        // Texto para fabricar
        string craftText = ControlHints.For(
            "F / Enter: Forjar",
            "A: Forjar",
            ControlHints.Cross + ": Forjar"
        );

        // Mostrar estados existentes
        if (state == State.Done)
        {
            craftText = "Ya forjado";
        }
        else if (state == State.Owned)
        {
            craftText = "Ya la tienes";
        }

        if (craftLabel != null &&
            craftLabel.text != craftText)
        {
            craftLabel.text = craftText;
        }
    }

    // ========================================
    // ESTADO DE RECETAS
    // ========================================

    private enum State
    {
        Ready,
        Missing,
        Owned,
        Done
    }

    private State StateOf(CraftingRecipe r)
    {
        if (r.upgradesSword &&
            stats != null &&
            !stats.CanUpgradeSword)
        {
            return State.Done;
        }

        // Armaduras que ya posee el jugador
        if (r.result != null &&
            ItemUsage.IsEquipment(r.result.id) &&
            inventory.GetAmount(r.result.id) > 0)
        {
            return State.Owned;
        }

        return inventory.CanCraft(r)
            ? State.Ready
            : State.Missing;
    }

    private static string StateText(State s)
    {
        return s == State.Ready
            ? "Listo para forjar"
            : s == State.Missing
                ? "Faltan materiales"
                : s == State.Owned
                    ? "Ya la tienes"
                    : "Ya forjado";
    }

    private static Color StateColor(State s)
    {
        return s == State.Ready
            ? Ready
            : s == State.Missing
                ? Missing
                : Done;
    }

    private Sprite IconOf(CraftingRecipe r)
    {
        return r.upgradesSword ||
               r.result == null ||
               r.result.icon == null
            ? swordUpgradeIcon
            : r.result.icon;
    }

    // ========================================
    // PRIMERA RECETA DISPONIBLE
    // ========================================

    private int FirstCraftable()
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null &&
                StateOf(recipes[i]) == State.Ready)
            {
                return i;
            }
        }

        return 0;
    }

    // ========================================
    // ACTUALIZAR LISTA DE RECETAS
    // ========================================

    private void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            bool has =
                i < recipes.Count &&
                recipes[i] != null;

            rows[i].gameObject.SetActive(has);

            if (!has)
                continue;

            var state = StateOf(recipes[i]);

            rows[i].Show(
                IconOf(recipes[i]),
                recipes[i].displayName,
                StateText(state),
                StateColor(state)
            );
        }
    }

    // ========================================
    // SELECCIONAR RECETA
    // ========================================

    private void Select(int index)
    {
        if (recipes.Count == 0)
            return;

        selected = Mathf.Clamp(
            index,
            0,
            recipes.Count - 1
        );

        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].SetSelected(i == selected);
        }

        var r = recipes[selected];

        if (r == null)
            return;

        var state = StateOf(r);

        detailIcon.sprite = IconOf(r);
        detailName.text = r.displayName;
        detailEffect.text = EffectOf(r);

        detailText.text =
            r.result != null
                ? r.result.description
                : "";

        // Mostrar ingredientes
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            bool has =
                i < r.ingredients.Count &&
                r.ingredients[i].material != null;

            ingredientSlots[i].gameObject.SetActive(has);
            ingredientTexts[i].gameObject.SetActive(has);

            if (!has)
                continue;

            var ing = r.ingredients[i];

            int have =
                inventory.GetAmount(ing.material.id);

            ingredientSlots[i].Show(
                ing.material.icon,
                ""
            );

            ingredientTexts[i].text =
                $"{ing.material.displayName}\n" +
                $"<size=80%>{have} / {ing.amount}</size>";

            ingredientTexts[i].color =
                have >= ing.amount
                    ? Ready
                    : Missing;
        }

        // Habilitar fabricacion solo si
        // existen los materiales necesarios
        craftButton.interactable =
            state == State.Ready;

        // Mostrar boton correspondiente
        UpdateControlHints();
    }

    // ========================================
    // EFECTOS DE RECETAS
    // ========================================

    private string EffectOf(CraftingRecipe r)
    {
        if (r.upgradesSword && stats != null)
        {
            return stats.CanUpgradeSword
                ? $"Despierta la espada carmesí · " +
                  $"Daño {stats.Attack} → " +
                  $"{stats.Attack + SwordUpgradeBonus}"
                : "La espada ya despertó";
        }

        if (r.result == null)
            return "";

        var piece = equipment != null
            ? equipment.ForItem(r.result.id)
            : null;

        if (piece != null)
        {
            return InventoryScreen.BonusText(piece);
        }

        switch (r.result.id)
        {
            case "pocion":
                return "Recupera 30 de vida";

            case "gota_amrita":
                return "Despierta a un guardián dormido";

            default:
                return "";
        }
    }

    // ========================================
    // FABRICAR
    // ========================================

    private void CraftSelected()
    {
        if (recipes.Count == 0)
            return;

        var r = recipes[selected];

        if (r == null ||
            StateOf(r) != State.Ready)
        {
            return;
        }

        bool ok;

        if (r.upgradesSword)
        {
            ok =
                stats != null &&
                inventory.TryConsumeIngredients(r) &&
                stats.UpgradeSword();
        }
        else
        {
            ok = inventory.TryCraft(r);
        }

        if (!ok)
            return;

        string message =
            $"¡Forjaste: {r.displayName}!";

        if (r.result != null &&
            ItemUsage.IsEquipment(r.result.id))
        {
            message += ControlHints.Format(
                " Equípala en el inventario " +
                "({inventory} › Equipo).");
        }

        if (GameHud.Instance != null)
        {
            GameHud.Instance.ShowToast(
                message,
                3f
            );
        }

        // Guardar progreso
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.SaveProgress();
        }

        // Actualizar recetas y seleccion
        Refresh();
        Select(selected);
    }
}
