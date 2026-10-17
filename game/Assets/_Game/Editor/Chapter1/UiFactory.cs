using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools.Chapter1
{
    // Helpers para construir UI (uGUI + TextMeshPro) por código con el estilo del capítulo.
    public static class UiFactory
    {
        // Opacos: el sprite de UI por defecto ya trae borde suave y con alfa < 1 el fondo distrae
        public static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.10f, 1f);
        public static readonly Color DimColor = new Color(0.02f, 0.02f, 0.05f, 0.8f);
        public static readonly Color Crimson = new Color(0.72f, 0.14f, 0.24f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.93f, 0.90f, 1f);
        public static readonly Color MutedText = new Color(0.72f, 0.70f, 0.78f, 1f);

        private static Sprite whiteSprite;

        public static Sprite White
        {
            get
            {
                if (whiteSprite == null)
                    whiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                return whiteSprite;
            }
        }

        public static Canvas Canvas(GameObject go, int sortingOrder)
        {
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name) =>
            Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Rectángulo de tamaño fijo anclado en un punto (pivot = anchor).
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return rt;
        }

        public static Image Image(RectTransform rt, Color color, Sprite sprite = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : White;
            img.type = sprite != null ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static TMP_Text Text(RectTransform rt, string text, float size, TextAlignmentOptions align, Color? color = null)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static TMP_Text Label(Transform parent, string name, string text, float size, TextAlignmentOptions align, Vector2 anchor, Vector2 pos, Vector2 boxSize, Color? color = null)
        {
            return Text(Box(parent, name, anchor, pos, boxSize), text, size, align, color);
        }

        public static Button Button(Transform parent, string name, string label, Vector2 size)
        {
            var rt = Box(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = Image(rt, Color.white);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.13f, 0.22f, 0.96f);
            colors.highlightedColor = Crimson;
            colors.selectedColor = Crimson;
            colors.pressedColor = new Color(0.45f, 0.08f, 0.15f, 1f);
            colors.disabledColor = new Color(0.12f, 0.12f, 0.14f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = size.y;
            le.preferredWidth = size.x;
            Text(Stretch(rt, "Label"), label, 34, TextAlignmentOptions.Center);
            return button;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Chapter1] {target.GetType().Name} no tiene el campo {field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Chapter1] {target.GetType().Name} no tiene el campo {field}"); return; }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
