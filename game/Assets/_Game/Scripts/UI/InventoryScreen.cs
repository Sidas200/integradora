using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// HEX-04: inventario emergente (Tab), pausa el juego. Tres pestañas:
//  - Objetos: cuadrícula con objetos clave, materiales y consumibles; detalle y botón Usar/Equipar.
//  - Equipo: muñeco que se viste con el casco, la pechera y las botas equipadas (+defensa).
//  - Estadísticas: vida, ataque, defensa, velocidad y progreso del capítulo.
// Tab o Esc cierran; 1/2/3 o A/D cambian de pestaña. Si cambió el equipo, guarda el progreso al cerrar.
// Mando: View / touchpad o B / ○ cierran, LB/RB (L1/R1) cambian de pestaña, stick o cruceta eligen y A / × usa.
public class InventoryScreen : MonoBehaviour
{
    public static InventoryScreen Instance { get; private set; }

    public enum Tab { Items, Equipment, Stats }

    [SerializeField] private GameObject panel;
    [SerializeField] private List<Button> tabButtons = new List<Button>();
    [SerializeField] private List<GameObject> pages = new List<GameObject>();
    [SerializeField] private Sprite tabSprite;
    [SerializeField] private Sprite tabActiveSprite;

    [Header("Objetos")]
    [SerializeField] private List<ItemSlotView> itemSlots = new List<ItemSlotView>();
    [SerializeField] private ItemSlotView detailSlot;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailType;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text detailHint;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private MaterialData[] catalog;
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite permitIcon;

    [Header("Equipo")]
    [SerializeField] private ItemSlotView headSlot;
    [SerializeField] private ItemSlotView chestSlot;
    [SerializeField] private ItemSlotView feetSlot;
    [SerializeField] private ItemSlotView weaponSlot;
    [SerializeField] private Image dollHead;
    [SerializeField] private Image dollChest;
    [SerializeField] private Image dollFeet;
    [SerializeField] private List<ItemSlotView> pieceSlots = new List<ItemSlotView>();
    [SerializeField] private TMP_Text pieceName;
    [SerializeField] private TMP_Text pieceText;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipLabel;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private Sprite emptyHead;
    [SerializeField] private Sprite emptyChest;
    [SerializeField] private Sprite emptyFeet;

    [Header("Estadísticas")]
    [SerializeField] private TMP_Text statsLeft;
    [SerializeField] private TMP_Text statsRight;
    [SerializeField] private Image healthFill;

    [Header("Ayuda de controles (opcional)")]
    [SerializeField] private TMP_Text controlsHint;

    private struct Entry
    {
        public string id; // vacío para objetos clave sin item (espada, permiso)
        public Sprite icon;
        public string name;
        public string amount;
        public string type;
        public string description;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private Tab tab;
    private int selectedItem;
    private int selectedPiece;
    private bool equipmentChanged;
    [Header("Navegación con control")]
    [SerializeField] private float navigationInitialDelay = 0.30f;
    [SerializeField] private float navigationRepeatInterval = 0.12f;

    // Columnas de la cuadrícula de objetos (la arma ChapterUiScreens.BuildInventory)
    private const int ItemColumns = 6;

    private NavRepeat navigation;
    private ControlHints.Scheme hintScheme;

    private Inventory inventory;
    private PlayerStats stats;
    private Health health;
    private PlayerEquipment equipment;
    private ItemUser itemUser;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        navigation = new NavRepeat(navigationInitialDelay, navigationRepeatInterval);
        if (panel != null) panel.SetActive(false);
        if (controlsHint == null && panel != null) controlsHint = UiNav.FindLabel(panel.transform, "Hint");
        for (int i = 0; i < tabButtons.Count; i++)
        {
            var t = (Tab)i;
            tabButtons[i].onClick.AddListener(() => ShowTab(t));
        }
        for (int i = 0; i < itemSlots.Count; i++)
        {
            int index = i;
            itemSlots[i].Button.onClick.AddListener(() => SelectItem(index));
        }
        for (int i = 0; i < pieceSlots.Count; i++)
        {
            int index = i;
            pieceSlots[i].Button.onClick.AddListener(() => SelectPiece(index));
        }
        headSlot.Button.onClick.AddListener(() => SelectEquippedSlot(EquipmentSlot.Head));
        chestSlot.Button.onClick.AddListener(() => SelectEquippedSlot(EquipmentSlot.Chest));
        feetSlot.Button.onClick.AddListener(() => SelectEquippedSlot(EquipmentSlot.Feet));
        actionButton.onClick.AddListener(UseSelectedItem);
        equipButton.onClick.AddListener(ToggleSelectedPiece);

        // El mando se maneja en Update; el EventSystem no debe navegar estos botones.
        var selectables = new List<Selectable>(tabButtons) { actionButton, equipButton, headSlot.Button, chestSlot.Button, feetSlot.Button };
        foreach (var slot in itemSlots) if (slot != null) selectables.Add(slot.Button);
        foreach (var slot in pieceSlots) if (slot != null) selectables.Add(slot.Button);
        UiNav.DisableNavigation(selectables);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    
    private void Update()
    {
        var kb = Keyboard.current;

        // Abrir/cerrar inventario: Tab, View (Xbox / Thor) o touchpad (PlayStation)
        bool tabPressed = kb != null &&
                        kb.tabKey.wasPressedThisFrame;

        bool inventoryPressed = tabPressed || GamepadInput.InventoryPressed;

        bool escapePressed = kb != null &&
                            kb.escapeKey.wasPressedThisFrame;

        // B / Circulo
        bool backPressed = GamepadInput.EastPressed;

        if (IsOpen)
        {
            // Actualizar instrucciones si cambia el dispositivo usado.
            UpdateControlHints();

            // El panel maneja el mando por codigo: si el EventSystem tuviera un boton
            // seleccionado, A confirmaria dos veces (boton + Usar/Equipar).
            UiNav.ClearSelectionIn(panel.transform);

            // Cerrar inventario
            if (inventoryPressed || escapePressed || backPressed)
            {
                if (escapePressed)
                    GameInput.ConsumeEscape();

                Close();
                return;
            }

            // Cambiar pestañas con teclado
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame)
                    ShowTab(Tab.Items);
                else if (kb.digit2Key.wasPressedThisFrame)
                    ShowTab(Tab.Equipment);
                else if (kb.digit3Key.wasPressedThisFrame)
                    ShowTab(Tab.Stats);
                else if (kb.aKey.wasPressedThisFrame ||
                        kb.leftArrowKey.wasPressedThisFrame)
                    ShowTab((Tab)(((int)tab + 2) % 3));
                else if (kb.dKey.wasPressedThisFrame ||
                        kb.rightArrowKey.wasPressedThisFrame)
                    ShowTab((Tab)(((int)tab + 1) % 3));
            }

            if (!GamepadInput.Connected)
            {
                navigation.Reset();
                return;
            }

            // LB / L1: pestaña anterior
            if (GamepadInput.LeftShoulderPressed)
            {
                ShowTab((Tab)(((int)tab + 2) % 3));
                navigation.Reset();
                return;
            }

            // RB / R1: siguiente pestaña
            if (GamepadInput.RightShoulderPressed)
            {
                ShowTab((Tab)(((int)tab + 1) % 3));
                navigation.Reset();
                return;
            }

            // ============================
            // NAVEGACIÓN CONTINUA (stick o cruceta de cualquier mando)
            // ============================
            Vector2Int step = navigation.Step(GamepadInput.Move);

            // ============================
            // NAVEGAR OBJETOS (cuadrícula)
            // ============================
            if (tab == Tab.Items)
            {
                int count = Mathf.Min(
                    entries.Count,
                    itemSlots.Count
                );

                // Izquierda/derecha: de uno en uno; arriba/abajo: una fila
                int delta = step.x + step.y * ItemColumns;

                if (delta != 0 && count > 0)
                {
                    int target = selectedItem + delta;

                    // Arriba desde la primera fila: se queda donde está
                    if (step.y < 0 && target < 0)
                        target = selectedItem;

                    selectedItem = Mathf.Clamp(
                        target,
                        0,
                        count - 1
                    );

                    SelectItem(selectedItem);
                }
            }

            // ============================
            // NAVEGAR EQUIPAMIENTO
            // ============================
            else if (tab == Tab.Equipment)
            {
                int count = 0;

                if (equipment != null &&
                    equipment.Catalog != null)
                {
                    count = Mathf.Min(
                        pieceSlots.Count,
                        equipment.Catalog.Count
                    );
                }

                int delta = step.x != 0 ? step.x : step.y;

                if (delta != 0 && count > 0)
                {
                    selectedPiece = Mathf.Clamp(
                        selectedPiece + delta,
                        0,
                        count - 1
                    );

                    SelectPiece(selectedPiece);
                }
            }

            // ============================
            // CONFIRMAR CON A / CRUZ
            // ============================
            if (GamepadInput.SouthPressed)
            {
                if (tab == Tab.Items &&
                    selectedItem >= 0 &&
                    selectedItem < entries.Count &&
                    actionButton != null &&
                    actionButton.gameObject.activeInHierarchy)
                {
                    UseSelectedItem();
                }
                else if (tab == Tab.Equipment &&
                        equipButton != null &&
                        equipButton.gameObject.activeInHierarchy)
                {
                    ToggleSelectedPiece();
                }
            }

            return;
        }

        // ============================
        // INVENTARIO CERRADO
        // ============================
        if (!inventoryPressed)
            return;

        var context = SceneContext.Current;
        var player = SceneContext.FindPlayer();

        var h = player != null
            ? player.GetComponent<Health>()
            : null;

        // GameplayBlocked: tampoco en el frame en que se cerró otro panel
        bool canOpen =
            context != null &&
            context.AllowPause &&
            !GameInput.GameplayBlocked &&
            (h == null || !h.IsDead) &&
            (SceneLoader.Instance == null ||
            !SceneLoader.Instance.IsBusy);

        if (canOpen)
            Open();
    }



    // Actualiza los textos segun el ultimo dispositivo utilizado.
    // No cambia acciones ni la logica de navegacion.
    private void UpdateControlHints()
    {
        if (!IsOpen)
            return;

        string actionPrefix = ControlHints.For("Clic: ", "A: ", ControlHints.Cross + ": ");

        // Las pistas de los objetos nombran botones ({potion}, {interact}...): rehacerlas si cambia el dispositivo
        var scheme = ControlHints.Current;
        if (scheme != hintScheme)
        {
            hintScheme = scheme;
            if (tab == Tab.Items && detailHint != null && selectedItem >= 0 && selectedItem < entries.Count)
                detailHint.text = string.IsNullOrEmpty(entries[selectedItem].id) ? "" : ItemUsage.Hint(entries[selectedItem].id);
        }

        if (tab == Tab.Items &&
            actionLabel != null &&
            actionButton != null &&
            actionButton.gameObject.activeInHierarchy &&
            selectedItem >= 0 &&
            selectedItem < entries.Count)
        {
            string id = entries[selectedItem].id;
            string action = ItemUsage.IsEquipment(id)
                ? (equipment != null && equipment.IsEquipped(id) ? "Quitar" : "Equipar")
                : "Usar";

            string label = actionPrefix + action;
            if (actionLabel.text != label)
                actionLabel.text = label;
        }

        if (tab == Tab.Equipment &&
            equipLabel != null &&
            equipButton != null &&
            equipButton.gameObject.activeInHierarchy &&
            equipment != null)
        {
            var pieces = equipment.Catalog;
            if (pieces != null &&
                selectedPiece >= 0 &&
                selectedPiece < pieces.Count &&
                pieces[selectedPiece] != null &&
                pieces[selectedPiece].item != null)
            {
                string id = pieces[selectedPiece].item.id;
                string action = equipment.IsEquipped(id) ? "Quitar" : "Equipar";
                string label = actionPrefix + action;
                if (equipLabel.text != label)
                    equipLabel.text = label;
            }
        }

        // Requiere asignar un TMP_Text para instrucciones en el Inspector.
        // Sin asignarlo, los botones de Usar/Equipar siguen funcionando.
        if (controlsHint != null)
        {
            string message = ControlHints.For(
                "Tab / Esc: Cerrar   ·   1/2/3: Pestañas",
                "View / B: Cerrar   ·   LB/RB: Pestañas   ·   A: Usar",
                "Touchpad / " + ControlHints.Circle + ": Cerrar   ·   L1/R1: Pestañas   ·   " + ControlHints.Cross + ": Usar"
            );

            if (controlsHint.text != message)
                controlsHint.text = message;
        }
    }

    public void Open()
    {
        var player = SceneContext.FindPlayer();

        if (player == null)
            return;

        inventory = player.GetComponent<Inventory>();
        stats = player.GetComponent<PlayerStats>();
        health = player.GetComponent<Health>();
        equipment = player.GetComponent<PlayerEquipment>();
        itemUser = player.GetComponent<ItemUser>();

        panel.SetActive(true);

        // Pausar el juego
        Time.timeScale = 0f;
        GameInput.Lock(this);

        equipmentChanged = false;
        selectedItem = 0;
        selectedPiece = 0;

        navigation.Reset();

        ShowTab(tab);

        // La navegacion del mando se controla
        // directamente desde Update()
        UiNav.ClearSelectionIn(panel.transform);
    }


    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        Time.timeScale = 1f;
        GameInput.Unlock(this);
        if (equipmentChanged && GameFlow.Instance != null) GameFlow.Instance.SaveProgress();
    }

    private void ShowTab(Tab t)
    {
        tab = t;
        for (int i = 0; i < pages.Count; i++) pages[i].SetActive(i == (int)t);
        for (int i = 0; i < tabButtons.Count; i++)
            ((Image)tabButtons[i].targetGraphic).sprite = i == (int)t ? tabActiveSprite : tabSprite;
        if (t == Tab.Items) RefreshItems();
        else if (t == Tab.Equipment) RefreshEquipment();
        else RefreshStats();
    }

    // ---------------------------------------------------------------- objetos

    private void CollectEntries()
    {
        entries.Clear();
        if (StoryFlags.Has(SwordPickup.Flag))
        {
            bool awake = stats != null && stats.SwordLevel >= 1;
            entries.Add(new Entry
            {
                icon = swordIcon,
                name = "Espada carmesí",
                amount = awake ? "Nv.1" : "",
                type = "Arma · objeto clave",
                description = awake
                    ? "La espada despertó con el Módulo de espada. Su filo carmesí puede herir a un guardián."
                    : "Una espada carmesí sin filo, encontrada en la cabaña. Kaede cree que puede despertar a las doncellas."
            });
        }
        if (StoryFlags.Has(ChapterObjectives.PermitObtained))
            entries.Add(new Entry
            {
                icon = permitIcon,
                name = "Permiso del Guardián",
                amount = "1/4",
                type = "Objeto clave",
                description = "El permiso de Varn, guardián del umbral. Se necesitan los cuatro para entrar al Gran Juzgado."
            });

        if (inventory == null || catalog == null) return;
        foreach (var m in catalog)
        {
            if (m == null) continue;
            int amount = inventory.GetAmount(m.id);
            if (amount <= 0) continue;
            entries.Add(new Entry
            {
                id = m.id,
                icon = m.icon,
                name = m.displayName,
                amount = amount > 1 ? amount.ToString() : "",
                type = TypeOf(m.id),
                description = m.description
            });
        }
    }

    private static string TypeOf(string id)
    {
        if (ItemUsage.IsEquipment(id)) return "Equipo · armadura de cuero";
        if (id == "pocion" || id == "gota_amrita") return "Consumible";
        if (ItemUsage.IsKeyItem(id)) return "Objeto clave";
        return "Material";
    }

    private void RefreshItems()
    {
        CollectEntries();
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < entries.Count)
                itemSlots[i].Show(entries[i].icon, entries[i].amount, equipment != null && equipment.IsEquipped(entries[i].id));
            else itemSlots[i].Clear();
            itemSlots[i].Button.interactable = i < entries.Count;
        }
        SelectItem(Mathf.Clamp(selectedItem, 0, Mathf.Max(0, entries.Count - 1)));
    }

    private void SelectItem(int index)
    {
        selectedItem = index;
        for (int i = 0; i < itemSlots.Count; i++) itemSlots[i].SetSelected(i == index && index < entries.Count);

        if (entries.Count == 0)
        {
            detailSlot.Clear();
            detailName.text = "Aún no tienes objetos";
            detailType.text = "";
            detailText.text = "Recoge materiales del suelo y vence enemigos para conseguirlos.";
            detailHint.text = "";
            actionButton.gameObject.SetActive(false);
            return;
        }

        var e = entries[index];
        detailSlot.Show(e.icon, "");
        detailName.text = e.name;
        detailType.text = e.type;
        detailText.text = e.description;
        detailHint.text = string.IsNullOrEmpty(e.id) ? "" : ItemUsage.Hint(e.id);

        bool usable = !string.IsNullOrEmpty(e.id) && (ItemUsage.IsUsable(e.id) || ItemUsage.IsEquipment(e.id));
        actionButton.gameObject.SetActive(usable);
        if (usable)
            actionLabel.text = ItemUsage.IsEquipment(e.id)
                ? (equipment != null && equipment.IsEquipped(e.id) ? "Quitar" : "Equipar")
                : "Usar";

        UpdateControlHints();
    }

    private void UseSelectedItem()
    {
        if (selectedItem >= entries.Count) return;
        string id = entries[selectedItem].id;
        if (ItemUsage.IsEquipment(id))
        {
            TogglePiece(equipment != null ? equipment.ForItem(id) : null);
            RefreshItems();
            return;
        }
        if (id == "pocion")
        {
            if (health != null && health.CurrentHealth >= health.MaxHealth) Toast("Tu vida ya está completa.");
            else if (itemUser != null && itemUser.UsePotion()) Toast("Recuperaste vida.");
            RefreshItems();
        }
        else if (id == "gota_amrita")
        {
            // Se cierra primero: el despertar del guardián necesita que el juego corra
            Close();
            if (itemUser != null) itemUser.UseAmrita();
        }
    }

    // ---------------------------------------------------------------- equipo

    private void RefreshEquipment()
    {
        ShowBodySlot(headSlot, dollHead, EquipmentSlot.Head, emptyHead);
        ShowBodySlot(chestSlot, dollChest, EquipmentSlot.Chest, emptyChest);
        ShowBodySlot(feetSlot, dollFeet, EquipmentSlot.Feet, emptyFeet);
        bool awake = stats != null && stats.SwordLevel >= 1;
        weaponSlot.Show(StoryFlags.Has(SwordPickup.Flag) ? swordIcon : null, awake ? "Nv.1" : "");

        var pieces = equipment != null ? equipment.Catalog : null;
        for (int i = 0; i < pieceSlots.Count; i++)
        {
            var piece = pieces != null && i < pieces.Count ? pieces[i] : null;
            bool owned = piece != null && inventory.GetAmount(piece.item.id) > 0;
            if (piece == null) pieceSlots[i].Clear();
            // Sin forjar se ve como silueta tenue
            else pieceSlots[i].Show(piece.item.icon, "", owned && equipment.IsEquipped(piece.item.id), owned ? 1f : 0.25f);
        }
        if (defenseText != null && stats != null) defenseText.text = $"Defensa  <color=#f0d27a>{stats.Defense}</color>";
        SelectPiece(selectedPiece);
    }

    private void ShowBodySlot(ItemSlotView slot, Image dollPart, EquipmentSlot which, Sprite empty)
    {
        var piece = equipment != null ? equipment.Equipped(which) : null;
        if (piece != null) slot.Show(piece.item != null ? piece.item.icon : piece.icon, "");
        else slot.Show(empty, "", false, 0.35f);
        if (dollPart != null) dollPart.enabled = piece != null;
    }

    private void SelectEquippedSlot(EquipmentSlot which)
    {
        if (equipment == null) return;
        var pieces = equipment.Catalog;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] != null && pieces[i].slot == which) { SelectPiece(i); return; }
    }

    private void SelectPiece(int index)
    {
        selectedPiece = index;
        for (int i = 0; i < pieceSlots.Count; i++) pieceSlots[i].SetSelected(i == index);
        var pieces = equipment != null ? equipment.Catalog : null;
        var piece = pieces != null && index < pieces.Count ? pieces[index] : null;
        if (piece == null || piece.item == null)
        {
            pieceName.text = "";
            pieceText.text = "";
            equipButton.gameObject.SetActive(false);
            return;
        }

        bool owned = inventory.GetAmount(piece.item.id) > 0;
        bool equipped = owned && equipment.IsEquipped(piece.item.id);
        pieceName.text = piece.item.displayName;
        pieceText.text = owned
            ? $"<color=#f0d27a>{BonusText(piece)}</color>\n{piece.item.description}"
            : $"<color=#f0d27a>{BonusText(piece)}</color>\nAún no la tienes. Fórjala en la forja del campamento.";
        equipButton.gameObject.SetActive(owned);
        equipLabel.text = equipped ? "Quitar" : "Equipar";

        UpdateControlHints();
    }

    private void ToggleSelectedPiece()
    {
        var pieces = equipment != null ? equipment.Catalog : null;
        if (pieces == null || selectedPiece >= pieces.Count) return;
        TogglePiece(pieces[selectedPiece]);
        RefreshEquipment();
    }

    private void TogglePiece(EquipmentItem piece)
    {
        if (piece == null || piece.item == null || equipment == null) return;
        if (equipment.IsEquipped(piece.item.id))
        {
            equipment.Unequip(piece.slot);
            Toast($"Te quitaste: {piece.item.displayName}");
        }
        else if (equipment.Equip(piece.item.id))
            Toast($"Equipaste: {piece.item.displayName} ({BonusText(piece)})");
        equipmentChanged = true;
    }

    public static string BonusText(EquipmentItem piece)
    {
        var parts = new List<string>();
        if (piece.bonusDefense != 0) parts.Add($"Defensa +{piece.bonusDefense}");
        if (piece.bonusMaxHealth != 0) parts.Add($"Vida máx. +{piece.bonusMaxHealth}");
        if (piece.bonusAttack != 0) parts.Add($"Ataque +{piece.bonusAttack}");
        if (piece.bonusMoveSpeed != 0f) parts.Add($"Velocidad +{piece.bonusMoveSpeed:0.0#}");
        return string.Join(" · ", parts);
    }

    // ---------------------------------------------------------------- estadísticas

    private void RefreshStats()
    {
        var left = new StringBuilder();
        if (health != null)
        {
            left.AppendLine($"Vida  <color=#f0d27a>{health.CurrentHealth} / {health.MaxHealth}</color>");
            if (healthFill != null) healthFill.fillAmount = health.Normalized;
        }
        left.AppendLine();
        if (stats != null)
        {
            left.AppendLine($"Ataque  <color=#f0d27a>{stats.Attack}</color>");
            left.AppendLine($"Defensa  <color=#f0d27a>{stats.Defense}</color>");
            left.AppendLine($"Velocidad  <color=#f0d27a>{stats.MoveSpeed:0.0#}</color>");
        }
        statsLeft.text = left.ToString();

        int pieces = 0;
        if (equipment != null)
            foreach (var p in equipment.Catalog)
                if (p != null && p.item != null && equipment.IsEquipped(p.item.id)) pieces++;
        string sword = !StoryFlags.Has(SwordPickup.Flag) ? "sin espada"
            : stats != null && stats.SwordLevel >= 1 ? "despierta (Nv. 1)" : "dormida (Nv. 0)";

        var right = new StringBuilder();
        right.AppendLine($"Espada carmesí  <color=#f0d27a>{sword}</color>");
        right.AppendLine($"Armadura de cuero  <color=#f0d27a>{pieces} / 3</color>");
        right.AppendLine($"Doncellas despiertas  <color=#f0d27a>{(StoryFlags.Has(MaidenStatue.Flag) ? 1 : 0)} / 4</color>");
        right.AppendLine($"Permisos de guardián  <color=#f0d27a>{(StoryFlags.Has(ChapterObjectives.PermitObtained) ? 1 : 0)} / 4</color>");
        statsRight.text = right.ToString();
    }

    private static void Toast(string text)
    {
        if (GameHud.Instance != null) GameHud.Instance.ShowToast(text, 2f);
    }
}
