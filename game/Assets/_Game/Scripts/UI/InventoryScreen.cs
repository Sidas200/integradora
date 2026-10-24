using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// HEX-04: inventario emergente (Tab), pausa el juego. Tres pestañas:
//  - Objetos: cuadrícula con objetos clave, materiales y consumibles; detalle y botón Usar/Equipar.
//  - Equipo: muñeco que se viste con el casco, la pechera y las botas equipadas (+defensa).
//  - Estadísticas: vida, ataque, defensa, velocidad y progreso del capítulo.
// Tab o Esc cierran; 1/2/3 o A/D cambian de pestaña. Si cambió el equipo, guarda el progreso al cerrar.
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

    private Inventory inventory;
    private PlayerStats stats;
    private Health health;
    private PlayerEquipment equipment;
    private ItemUser itemUser;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (IsOpen)
        {
            if (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
            {
                if (kb.escapeKey.wasPressedThisFrame) GameInput.ConsumeEscape();
                Close();
            }
            else if (kb.digit1Key.wasPressedThisFrame) ShowTab(Tab.Items);
            else if (kb.digit2Key.wasPressedThisFrame) ShowTab(Tab.Equipment);
            else if (kb.digit3Key.wasPressedThisFrame) ShowTab(Tab.Stats);
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) ShowTab((Tab)(((int)tab + 2) % 3));
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) ShowTab((Tab)(((int)tab + 1) % 3));
            return;
        }

        if (!kb.tabKey.wasPressedThisFrame) return;
        var context = SceneContext.Current;
        var player = SceneContext.FindPlayer();
        var h = player != null ? player.GetComponent<Health>() : null;
        bool canOpen = context != null && context.AllowPause && !GameInput.Locked
                       && (h == null || !h.IsDead)
                       && (SceneLoader.Instance == null || !SceneLoader.Instance.IsBusy);
        if (canOpen) Open();
    }

    public void Open()
    {
        var player = SceneContext.FindPlayer();
        if (player == null) return;
        inventory = player.GetComponent<Inventory>();
        stats = player.GetComponent<PlayerStats>();
        health = player.GetComponent<Health>();
        equipment = player.GetComponent<PlayerEquipment>();
        itemUser = player.GetComponent<ItemUser>();

        panel.SetActive(true);
        Time.timeScale = 0f;
        GameInput.Lock(this);
        equipmentChanged = false;
        selectedItem = 0;
        ShowTab(tab);
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
