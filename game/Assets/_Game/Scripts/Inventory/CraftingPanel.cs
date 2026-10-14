using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel de crafteo: muestra las recetas, lo que tienes/necesitas y permite craftear.
// Pausa el juego mientras está abierto. Las filas se construyen por código.
public class CraftingPanel : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("UI")]
    [SerializeField] private RectTransform content;   // contenedor de las filas
    [SerializeField] private Button closeButton;
    [SerializeField] private float rowHeight = 80f;

    private Inventory inventory;
    private readonly List<Row> rows = new List<Row>();
    private bool built;
    private bool paused;
    private float previousTimeScale = 1f;

    private struct Row
    {
        public CraftingRecipe recipe;
        public Button button;
        public TMP_Text label;
    }

    public bool IsOpen => gameObject.activeSelf;

    public void Open()
    {
        if (IsOpen) return;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        paused = true;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        gameObject.SetActive(false); // OnDisable restaura el tiempo
    }

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("CraftingPanel: no hay Inventory en la escena.");
            return;
        }

        BuildRows();
        inventory.OnAmountChanged += HandleAmountChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnAmountChanged -= HandleAmountChanged;

        if (paused)
        {
            Time.timeScale = previousTimeScale;
            paused = false;
        }
    }

    private void HandleAmountChanged(string id, int amount)
    {
        Refresh();
    }

    private void BuildRows()
    {
        if (built || content == null) return;
        built = true;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            var rowGO = new GameObject("Row_" + recipe.name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGO.transform.SetParent(content, false);
            rowGO.GetComponent<LayoutElement>().preferredHeight = rowHeight;

            var bg = rowGO.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.12f, 0.10f, 0.95f);

            var button = rowGO.GetComponent<Button>();
            button.targetGraphic = bg;
            var captured = recipe;
            button.onClick.AddListener(() => Craft(captured));

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(rowGO.transform, false);
            var rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f, 6f);
            rt.offsetMax = new Vector2(-16f, -6f);

            var label = textGO.GetComponent<TextMeshProUGUI>();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            rows.Add(new Row { recipe = recipe, button = button, label = label });
        }
    }

    private void Refresh()
    {
        if (inventory == null) return;

        foreach (var row in rows)
        {
            row.button.interactable = inventory.CanCraft(row.recipe);
            row.label.text = BuildLabel(row.recipe);
        }
    }

    private string BuildLabel(CraftingRecipe recipe)
    {
        var sb = new StringBuilder();
        sb.Append(recipe.displayName);
        if (recipe.resultAmount > 1) sb.Append(" x").Append(recipe.resultAmount);
        sb.Append('\n');

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            var ing = recipe.ingredients[i];
            if (ing.material == null) continue;
            int have = inventory.GetAmount(ing.material.id);
            if (i > 0) sb.Append("  |  ");
            sb.Append(ing.material.displayName).Append(' ').Append(have).Append('/').Append(ing.amount);
        }
        return sb.ToString();
    }

    private void Craft(CraftingRecipe recipe)
    {
        if (inventory == null) return;
        if (inventory.TryCraft(recipe))
            Debug.Log("Crafteado: " + recipe.displayName);
        Refresh();
    }
}