using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// HEX-04: inventario emergente (Tab). Pestaña "Objetos": materiales, consumibles y objetos clave
// con su descripción. Pestaña "Estadísticas": vida, ataque, defensa, velocidad, espada y progreso.
// Pausa el juego mientras está abierto. Tab o Esc cierran; 1/2 o A/D cambian de pestaña.
public class InventoryScreen : MonoBehaviour
{
    public static InventoryScreen Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button itemsTab;
    [SerializeField] private Button statsTab;
    [SerializeField] private GameObject itemsPage;
    [SerializeField] private GameObject statsPage;

    [Header("Objetos")]
    [SerializeField] private RectTransform itemList;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private MaterialData[] catalog;
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite permitIcon;

    [Header("Estadísticas")]
    [SerializeField] private TMP_Text statsText;

    private struct Entry
    {
        public Sprite icon;
        public string name;
        public string amount;
        public string description;
    }

    private readonly List<GameObject> rows = new List<GameObject>();
    private bool showingStats;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (itemsTab != null) itemsTab.onClick.AddListener(() => ShowTab(false));
        if (statsTab != null) statsTab.onClick.AddListener(() => ShowTab(true));
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
            else if (kb.digit1Key.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) ShowTab(false);
            else if (kb.digit2Key.wasPressedThisFrame || kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) ShowTab(true);
            return;
        }

        if (!kb.tabKey.wasPressedThisFrame) return;
        var context = SceneContext.Current;
        var health = SceneContext.FindPlayer() != null ? SceneContext.FindPlayer().GetComponent<Health>() : null;
        bool canOpen = context != null && context.AllowPause && !GameInput.Locked
                       && (health == null || health.CurrentHealth > 0)
                       && (SceneLoader.Instance == null || !SceneLoader.Instance.IsBusy);
        if (canOpen) Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
        GameInput.Lock(this);
        BuildItems();
        BuildStats();
        ShowTab(showingStats);
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        Time.timeScale = 1f;
        GameInput.Unlock(this);
    }

    private void ShowTab(bool stats)
    {
        showingStats = stats;
        itemsPage.SetActive(!stats);
        statsPage.SetActive(stats);
        Highlight(itemsTab, !stats);
        Highlight(statsTab, stats);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // La pestaña activa en carmesí, la otra apagada
    private static void Highlight(Button tab, bool active)
    {
        var colors = tab.colors;
        colors.normalColor = colors.selectedColor = active ? new Color(0.72f, 0.14f, 0.24f, 1f) : new Color(0.16f, 0.13f, 0.22f, 1f);
        colors.highlightedColor = active ? colors.normalColor : new Color(0.3f, 0.2f, 0.32f, 1f);
        tab.colors = colors;
    }

    // ---------------------------------------------------------------- objetos

    private List<Entry> CollectEntries()
    {
        var list = new List<Entry>();
        var player = SceneContext.FindPlayer();
        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        var stats = player != null ? player.GetComponent<PlayerStats>() : null;

        // Objetos clave
        if (StoryFlags.Has(SwordPickup.Flag))
        {
            bool upgraded = stats != null && stats.SwordLevel >= 1;
            list.Add(new Entry
            {
                icon = swordIcon,
                name = "Espada carmesí",
                amount = upgraded ? "Despierta" : "Dormida",
                description = upgraded
                    ? "La espada despertó en la forja. Su filo carmesí puede herir a un guardián."
                    : "Una espada carmesí sin filo, encontrada en la cabaña. Kaede cree que puede despertar a las doncellas."
            });
        }
        if (StoryFlags.Has(ChapterObjectives.PermitObtained))
            list.Add(new Entry
            {
                icon = permitIcon,
                name = "Permiso del Guardián",
                amount = "1 de 4",
                description = "El permiso de Varn, guardián del umbral. Se necesitan los cuatro para entrar al Gran Juzgado."
            });

        // Materiales y consumibles
        if (inventory != null && catalog != null)
            foreach (var m in catalog)
            {
                if (m == null) continue;
                int amount = inventory.GetAmount(m.id);
                if (amount <= 0) continue;
                list.Add(new Entry { icon = m.icon, name = m.displayName, amount = "x" + amount, description = m.description });
            }
        return list;
    }

    private void BuildItems()
    {
        foreach (var r in rows) Destroy(r);
        rows.Clear();

        var entries = CollectEntries();
        if (entries.Count == 0)
        {
            ShowDetail(new Entry { name = "Aún no tienes objetos", description = "Recoge materiales en la isla y vence enemigos para conseguirlos." });
            return;
        }

        foreach (var e in entries)
        {
            var captured = e;
            var row = new GameObject("Row_" + e.name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(itemList, false);
            row.GetComponent<LayoutElement>().preferredHeight = 64f;
            var bg = row.GetComponent<Image>();
            bg.color = Color.white;
            var button = row.GetComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.13f, 0.22f, 1f);
            colors.highlightedColor = colors.selectedColor = new Color(0.72f, 0.14f, 0.24f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => ShowDetail(captured));

            AddIcon(row.transform, e.icon, new Vector2(12f, 0f), 44f);
            AddText(row.transform, e.name, 28f, TextAlignmentOptions.MidlineLeft, new Vector2(68f, 0f), new Vector2(-150f, 0f));
            AddText(row.transform, e.amount, 26f, TextAlignmentOptions.MidlineRight, new Vector2(0f, 0f), new Vector2(-16f, 0f));
            rows.Add(row);
        }
        ShowDetail(entries[0]);
    }

    private void ShowDetail(Entry e)
    {
        detailName.text = e.name;
        detailText.text = e.description;
        detailIcon.sprite = e.icon;
        detailIcon.enabled = e.icon != null;
    }

    private static void AddIcon(Transform parent, Sprite sprite, Vector2 left, float size)
    {
        if (sprite == null) return;
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = left;
        rt.sizeDelta = new Vector2(size, size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    private static void AddText(Transform parent, string text, float size, TextAlignmentOptions align, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = new Color(0.95f, 0.93f, 0.9f);
        t.raycastTarget = false;
    }

    // ---------------------------------------------------------------- estadísticas

    private void BuildStats()
    {
        var player = SceneContext.FindPlayer();
        var stats = player != null ? player.GetComponent<PlayerStats>() : null;
        var health = player != null ? player.GetComponent<Health>() : null;
        var sb = new StringBuilder();

        sb.AppendLine("<b>Tsuki</b>");
        sb.AppendLine();
        if (health != null) sb.AppendLine($"Vida:  {health.CurrentHealth} / {health.MaxHealth}");
        if (stats != null)
        {
            sb.AppendLine($"Ataque:  {stats.Attack}");
            sb.AppendLine($"Defensa:  {stats.Defense}");
            sb.AppendLine($"Velocidad:  {stats.MoveSpeed:0.0}");
            sb.AppendLine();
            string sword = !StoryFlags.Has(SwordPickup.Flag) ? "sin espada"
                : stats.SwordLevel >= 1 ? "despierta (Nv. 1)" : "dormida (Nv. 0)";
            sb.AppendLine($"Espada carmesí:  {sword}");
        }
        sb.AppendLine($"Permisos de guardián:  {(StoryFlags.Has(ChapterObjectives.PermitObtained) ? 1 : 0)} / 4");
        sb.AppendLine($"Doncellas despiertas:  {(StoryFlags.Has(MaidenStatue.Flag) ? 1 : 0)} / 4");
        statsText.text = sb.ToString();
    }
}
