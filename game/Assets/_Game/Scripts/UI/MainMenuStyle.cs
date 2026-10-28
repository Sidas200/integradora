using UnityEngine;
using UnityEngine.UI;

// Estilo del menú principal: pétalos cayendo sobre el fondo y botones con brillo.
// Se aplica al cargar la escena para no rehacer los botones que genera el builder del capítulo.
public class MainMenuStyle : MonoBehaviour
{
    [SerializeField] private RectTransform background;
    [SerializeField] private Sprite[] petals;
    [SerializeField] private Sprite buttonIcon;
    [SerializeField] private Button[] buttons;

    public void Setup(RectTransform bg, Sprite[] petalSprites, Sprite icon, Button[] menuButtons)
    {
        background = bg;
        petals = petalSprites;
        buttonIcon = icon;
        buttons = menuButtons;
    }

    private void Awake()
    {
        if (petals != null && petals.Length > 0)
        {
            // capa de pétalos justo encima del fondo (detrás de los botones)
            var layer = new GameObject("Petals", typeof(RectTransform));
            var rt = (RectTransform)layer.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (background != null) rt.SetSiblingIndex(background.GetSiblingIndex() + 1);
            layer.AddComponent<PetalFall>().Setup(petals);
        }

        if (buttons == null) return;
        foreach (var b in buttons)
            if (b != null) b.gameObject.AddComponent<MenuButtonGlow>().Build(buttonIcon);
    }
}
