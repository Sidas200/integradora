using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Casilla de objeto (inventario, equipo, forja): marco, icono, cantidad y marca de equipado.
public class ItemSlotView : MonoBehaviour
{
    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text count;
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Button button;

    public Button Button => button;

    public void Setup(Image frameImage, Image iconImage, TMP_Text countText, GameObject badge, Sprite normal, Sprite selected, Button slotButton)
    {
        frame = frameImage;
        icon = iconImage;
        count = countText;
        equippedBadge = badge;
        normalSprite = normal;
        selectedSprite = selected;
        button = slotButton;
    }

    public void Show(Sprite sprite, string amount, bool equipped = false, float alpha = 1f)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.color = new Color(1f, 1f, 1f, alpha);
        if (count != null) count.text = amount ?? "";
        if (equippedBadge != null) equippedBadge.SetActive(equipped);
    }

    public void Clear() => Show(null, "");

    public void SetSelected(bool selected)
    {
        if (frame != null) frame.sprite = selected ? selectedSprite : normalSprite;
    }
}
