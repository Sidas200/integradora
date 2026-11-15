using UnityEngine;
using UnityEngine.UI;

// Adornos de la tarjeta "Fin de la demo" con el arte de UiArt (el mismo de inventario y menús):
// marco dorado en la orilla, divisores con rombo, ramos de sakura junto al título, placa del aviso
// y pétalos cayendo. Los textos viven en la escena (los arma ChapterUi / DemoEndCard);
// aquí solo se decora alrededor de ellos, al mostrarse la tarjeta.
public class EndCardDecor : MonoBehaviour
{
    private const float Px = UiSkin.Px;
    private const float DecoPx = UiSkin.DecoPx;

    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform[] dividersAt = new RectTransform[0];
    [SerializeField] private RectTransform hintPlate;
    [SerializeField] private int petals = 14;

    private bool built;

    private void Awake() => Build();

    private void Build()
    {
        if (built) return;
        built = true;
        var root = (RectTransform)transform;

        // Marco dorado a todo lo ancho (por detrás de los textos)
        var frame = Child(root, "Frame", 0);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = new Vector2(18f, 18f);
        frame.offsetMax = new Vector2(-18f, -18f);
        var frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.sprite = UiSkin.FrameSprite;
        frameImg.type = Image.Type.Sliced;
        frameImg.fillCenter = false;
        frameImg.pixelsPerUnitMultiplier = 1f / Px;
        frameImg.raycastTarget = false;

        // Flores en las esquinas y al centro de arriba / abajo del marco
        var corner = UiArt.Get("sakura13", () => UiArt.Sakura(13));
        Deco(root, "TopFlower", corner, new Vector2(0.5f, 1f), new Vector2(0f, -18f), DecoPx);
        Deco(root, "BottomFlower", corner, new Vector2(0.5f, 0f), new Vector2(0f, 18f), DecoPx);

        if (title != null)
        {
            // Divisores largos arriba y abajo del título
            var longDivider = UiArt.Get("divider220", () => UiArt.Divider(220));
            float h = title.rect.height;
            Deco(title, "LineTop", longDivider, new Vector2(0.5f, 1f), new Vector2(0f, 16f), Px);
            Deco(title, "LineBottom", longDivider, new Vector2(0.5f, 0f), new Vector2(0f, -14f), Px);

            // Ramos de sakura a los lados del título (el derecho en espejo)
            var cluster = UiArt.Get("sakuraCluster7", () => UiArt.SakuraCluster(7));
            var big = UiArt.Get("sakura15", () => UiArt.Sakura(15));
            foreach (int side in new[] { -1, 1 })
            {
                var a = new Vector2(side < 0 ? 0f : 1f, 0.5f);
                var c = Deco(title, side < 0 ? "SakuraL" : "SakuraR", cluster, a, new Vector2(side * 44f, 0f), DecoPx * 1.3f);
                if (side > 0) c.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                Deco(title, side < 0 ? "SakuraL2" : "SakuraR2", big, a, new Vector2(side * 96f, -26f), DecoPx * 1.2f);
            }
        }

        // Divisores cortos entre párrafos
        var shortDivider = UiArt.Get("divider140", () => UiArt.Divider(140));
        foreach (var at in dividersAt)
            if (at != null) Deco(at, "Divider", shortDivider, new Vector2(0.5f, 0.5f), Vector2.zero, Px);

        // Placa del aviso, como los botones del juego
        if (hintPlate != null)
        {
            var plate = hintPlate.GetComponent<Image>();
            if (plate == null) plate = hintPlate.gameObject.AddComponent<Image>();
            plate.sprite = UiSkin.PlateSprite;
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = 1f / Px;
            plate.color = Color.white;
            plate.raycastTarget = false;
            var diamond = UiArt.Get("diamond", UiArt.Diamond);
            Deco(hintPlate, "DiamondL", diamond, new Vector2(0f, 0.5f), new Vector2(-2f, 0f), DecoPx);
            Deco(hintPlate, "DiamondR", diamond, new Vector2(1f, 0.5f), new Vector2(2f, 0f), DecoPx);
        }

        // Pétalos cayendo sobre todo
        if (petals > 0)
        {
            var layer = Child(root, "Petals", root.childCount);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            var sprites = new[]
            {
                UiArt.Get("petal0", () => UiArt.PetalSprite(0)),
                UiArt.Get("petal1", () => UiArt.PetalSprite(1)),
            };
            layer.gameObject.AddComponent<UiPetals>().Setup(sprites, petals, DecoPx);
        }
    }

    private static Image Deco(RectTransform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, float scale)
    {
        var rt = Child(parent, name, parent.childCount);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sprite.rect.size * scale;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform Child(Transform parent, string name, int sibling)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.SetSiblingIndex(Mathf.Clamp(sibling, 0, parent.childCount - 1));
        return rt;
    }
}
