using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Muestra en la página Inventory del menú (Pocket) los materiales que tiene el jugador.
// Va como componente en el objeto InventoryPage. Solo toca UI, no el juego.
public class InventoryMenuPanel : MonoBehaviour
{
    [Tooltip("Materiales que se pueden mostrar. Arrastra aquí MaterialA, MaterialB, etc.")]
    [SerializeField] private List<MaterialData> materials = new List<MaterialData>();

    private const int Columns = 5;
    private const int Rows = 4;

    private Inventory inventory;
    private readonly List<Image> icons = new List<Image>();
    private readonly List<TextMeshProUGUI> amountTexts = new List<TextMeshProUGUI>();
    private bool built;

    private void OnEnable()
    {
        BuildSlots();
        HideDecorativeItems();

        inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null)
            inventory.OnAmountChanged += HandleAmountChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnAmountChanged -= HandleAmountChanged;
        inventory = null;
    }

    private void HandleAmountChanged(string id, int amount)
    {
        Refresh();
    }

    // Crea un ícono y un texto de cantidad dentro de cada casilla (solo una vez).
    private void BuildSlots()
    {
        if (built) return;
        built = true;

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Transform slot = transform.Find($"InventorySlot_{x}_{y}");
                if (slot == null)
                {
                    icons.Add(null);
                    amountTexts.Add(null);
                    continue;
                }

                // Ícono
                var iconGO = new GameObject("MaterialIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(slot, false);
                var iconRect = (RectTransform)iconGO.transform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(42, 42);
                var img = iconGO.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                icons.Add(img);

                // Cantidad
                var textGO = new GameObject("MaterialAmount", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(slot, false);
                var textRect = (RectTransform)textGO.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(2, 2);
                textRect.offsetMax = new Vector2(-4, -2);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.BottomRight;
                tmp.raycastTarget = false;
                amountTexts.Add(tmp);
            }
        }
    }

    // Esconde los objetos de adorno (Item_0, Item_1...) que puso el builder de Santiago.
    private void HideDecorativeItems()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Item_"))
                child.gameObject.SetActive(false);
        }
    }

    // Pinta en orden los materiales con cantidad > 0 y vacía el resto de casillas.
    private void Refresh()
    {
        int slotIndex = 0;

        if (inventory != null)
        {
            foreach (MaterialData material in materials)
            {
                if (material == null) continue;

                int amount = inventory.GetAmount(material.id);
                if (amount <= 0) continue;
                if (slotIndex >= icons.Count) break;

                SetSlot(slotIndex, material.icon, "x" + amount);
                slotIndex++;
            }
        }

        for (int i = slotIndex; i < icons.Count; i++)
            ClearSlot(i);
    }

    private void SetSlot(int index, Sprite icon, string amountText)
    {
        if (icons[index] == null) return;
        icons[index].sprite = icon;
        icons[index].gameObject.SetActive(true);
        amountTexts[index].text = amountText;
        amountTexts[index].gameObject.SetActive(true);
    }

    private void ClearSlot(int index)
    {
        if (icons[index] == null) return;
        icons[index].gameObject.SetActive(false);
        amountTexts[index].gameObject.SetActive(false);
    }
}