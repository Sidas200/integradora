using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Fila de receta de la forja: icono del resultado, nombre y estado (listo / faltan / ya forjado).
public class RecipeRowView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text status;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Button button;

    public Button Button => button;

    public void Setup(Image bg, Image iconImage, TMP_Text titleText, TMP_Text statusText, Sprite normal, Sprite selected, Button rowButton)
    {
        background = bg;
        icon = iconImage;
        title = titleText;
        status = statusText;
        normalSprite = normal;
        selectedSprite = selected;
        button = rowButton;
    }

    public void Show(Sprite sprite, string name, string state, Color stateColor)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        title.text = name;
        status.text = state;
        status.color = stateColor;
    }

    public void SetSelected(bool selected) => background.sprite = selected ? selectedSprite : normalSprite;
}
