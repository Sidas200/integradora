using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Botón con el estilo de la UI (UiSkin): placa ciruela con borde rosa y, al elegirlo con el mando,
// el teclado o el mouse, listón carmesí con puntas y un rombo dorado en la punta izquierda.
[DisallowMultipleComponent]
public class UiThemeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private Image background;
    private Image ornament;
    private Image icon;
    private TMP_Text label;
    private Sprite normalSprite, activeSprite;
    private bool hovered;
    private int shown = -1;

    public void Setup(Image bg, Sprite normal, Sprite active, Image iconImage, Image ornamentImage, TMP_Text text)
    {
        button = GetComponent<Button>();
        background = bg;
        normalSprite = normal;
        activeSprite = active;
        icon = iconImage;
        ornament = ornamentImage;
        label = text;
        if (button != null) button.transition = Selectable.Transition.None;
        shown = -1;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void OnDisable()
    {
        hovered = false;
        shown = -1;
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        bool interactable = button == null || button.IsInteractable();
        var es = EventSystem.current;
        bool selected = es != null && es.currentSelectedGameObject == gameObject;
        bool active = interactable && (selected || hovered);
        int state = (active ? 1 : 0) | (interactable ? 2 : 0);
        if (state == shown) return;
        shown = state;

        if (background != null)
        {
            background.sprite = active ? activeSprite : normalSprite;
            background.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }
        if (ornament != null) ornament.enabled = active;
        var textColor = active ? UiArt.TextCream : new Color(0.88f, 0.80f, 0.80f, 1f);
        if (!interactable) textColor.a = 0.55f;
        if (label != null) label.color = textColor;
        if (icon != null) icon.color = active ? Color.white : new Color(0.86f, 0.74f, 0.66f, interactable ? 1f : 0.55f);
    }
}
