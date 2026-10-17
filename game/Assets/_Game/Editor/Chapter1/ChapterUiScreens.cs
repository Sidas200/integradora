using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.EditorTools.Chapter1.UiFactory;

namespace Game.EditorTools.Chapter1
{
    // Pantallas emergentes con el kit de UI pixel art (marcos 9-slice a 4x):
    // forja de Kaede (ForgeScreen) e inventario con pestañas (InventoryScreen).
    public static class ChapterUiScreens
    {
        private const float Px = 4f; // 1 pixel del arte = 4 unidades de UI (referencia 1920x1080)

        public static readonly Color GoldText = new Color(0.94f, 0.82f, 0.48f);
        private static readonly Color HintText = new Color(0.56f, 0.85f, 1f);

        private static readonly string[] Catalog =
        {
            "AlmaPequena", "GotaAmrita", "Pocion", "ModuloEspada",
            "CascoCuero", "PecheraCuero", "BotasCuero",
            "AlmaAntigua", "CircuitoAntiguo", "CueroAntiguo",
            "FlorDestello", "ConcentradoSlime", "RestoSlime",
        };

        // ---------------------------------------------------------------- forja

        public static void BuildForge(Transform canvas)
        {
            var rootRt = Stretch(canvas, "Forge");
            var screen = rootRt.gameObject.AddComponent<ForgeScreen>();
            var panel = Stretch(rootRt, "Panel");
            Image(panel, DimColor).raycastTarget = true;
            var box = Window(panel, "Forja de Kaede", "Elige una receta. Si te falta algo, no se gasta nada.");
            Label(box, "Hint", "W/S: elegir   ·   F: forjar   ·   E / Esc: cerrar", 24, TextAlignmentOptions.Right,
                new Vector2(1, 1), new Vector2(-64, -56), new Vector2(700, 34), MutedText);

            // Lista de recetas
            var list = Rect(box, "Recipes", new Vector2(0, 0), new Vector2(0, 1), new Vector2(60, 60), new Vector2(700, -170));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12; layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var rows = new List<RecipeRowView>();
            for (int i = 0; i < 6; i++) rows.Add(RecipeRow(list, i));

            // Detalle
            var detail = Rect(box, "Detail", new Vector2(0, 0), new Vector2(1, 1), new Vector2(740, 60), new Vector2(-60, -170));
            Kit(detail, "ui_panel");
            var iconSlot = Slot(detail, "Icon", new Vector2(0, 1), new Vector2(36, -36), 168);
            iconSlot.Button.interactable = false;
            var name = Label(detail, "Name", "", 42, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(228, -40), new Vector2(370, 110), GoldText);
            name.fontStyle = FontStyles.Bold;
            var effect = Label(detail, "Effect", "", 28, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(228, -150), new Vector2(370, 60), new Color(1f, 0.85f, 0.55f));
            var text = Label(detail, "Description", "", 26, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(36, -224), new Vector2(560, 70), MutedText);
            Divider(detail, -298);
            Label(detail, "MaterialsTitle", "Materiales", 28, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(36, -310), new Vector2(400, 40), GoldText);
            var ingSlots = new List<ItemSlotView>();
            var ingTexts = new List<TMP_Text>();
            for (int i = 0; i < 3; i++)
            {
                var s = Slot(detail, "Ingredient" + i, new Vector2(0, 1), new Vector2(76 + i * 200, -356), 88);
                s.Button.interactable = false;
                ingSlots.Add(s);
                ingTexts.Add(Label(detail, "IngredientText" + i, "", 22, TextAlignmentOptions.Top, new Vector2(0, 1), new Vector2(30 + i * 200, -450), new Vector2(180, 56)));
            }
            var craft = KitButton(detail, "Craft", "Forjar", new Vector2(320, 80));
            Place(craft, new Vector2(1, 0), new Vector2(-28, 28));
            var close = KitButton(detail, "Close", "Cerrar", new Vector2(220, 80));
            Place(close, new Vector2(1, 0), new Vector2(-364, 28));

            var so = new SerializedObject(screen);
            so.FindProperty("panel").objectReferenceValue = panel.gameObject;
            SetList(so.FindProperty("recipes"), ChapterData.ForgeRecipes());
            SetList(so.FindProperty("rows"), rows);
            so.FindProperty("swordUpgradeIcon").objectReferenceValue = PlaceholderArtGenerator.Load("placeholder_icon_sword_module");
            so.FindProperty("detailIcon").objectReferenceValue = iconSlot.transform.Find("Icon").GetComponent<Image>();
            so.FindProperty("detailName").objectReferenceValue = name;
            so.FindProperty("detailEffect").objectReferenceValue = effect;
            so.FindProperty("detailText").objectReferenceValue = text;
            SetList(so.FindProperty("ingredientSlots"), ingSlots);
            SetList(so.FindProperty("ingredientTexts"), ingTexts);
            so.FindProperty("craftButton").objectReferenceValue = craft;
            so.FindProperty("craftLabel").objectReferenceValue = craft.GetComponentInChildren<TMP_Text>();
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RecipeRowView RecipeRow(Transform parent, int index)
        {
            var rt = Rect(parent, "Recipe" + index, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.sizeDelta = new Vector2(0, 92);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 92;
            var bg = Kit(rt, "ui_row");
            bg.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var icon = Box(rt, "Icon", new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(68, 68));
            var iconImg = Image(icon, Color.white, PlaceholderArtGenerator.Load("placeholder_icon_sword_module"));
            iconImg.preserveAspect = true;
            var title = Label(rt, "Title", "", 30, TextAlignmentOptions.BottomLeft, new Vector2(0, 0.5f), new Vector2(104, 2), new Vector2(500, 40));
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.pivot = new Vector2(0, 0);
            var status = Label(rt, "Status", "", 22, TextAlignmentOptions.TopLeft, new Vector2(0, 0.5f), new Vector2(104, -2), new Vector2(500, 32), MutedText);
            status.rectTransform.pivot = new Vector2(0, 1);
            var view = rt.gameObject.AddComponent<RecipeRowView>();
            view.Setup(bg, iconImg, title, status, PlaceholderArtGenerator.Load("ui_row"), PlaceholderArtGenerator.Load("ui_row_selected"), button);
            return view;
        }

        // ---------------------------------------------------------------- inventario

        public static void BuildInventory(Transform canvas)
        {
            var rootRt = Stretch(canvas, "Inventory");
            var screen = rootRt.gameObject.AddComponent<InventoryScreen>();
            var panel = Stretch(rootRt, "Panel");
            Image(panel, DimColor).raycastTarget = true;
            var box = Window(panel, "Inventario", null);
            Label(box, "Hint", "Tab / Esc: cerrar   ·   1 2 3: pestañas", 24, TextAlignmentOptions.Right,
                new Vector2(1, 1), new Vector2(-64, -56), new Vector2(700, 34), MutedText);

            var tabs = new List<Button>();
            string[] names = { "Objetos", "Equipo", "Estadísticas" };
            for (int i = 0; i < names.Length; i++)
            {
                var tab = TabButton(box, names[i]);
                Place(tab, new Vector2(0, 1), new Vector2(60 + i * 308, -112));
                tabs.Add(tab);
            }
            var pageArea = Rect(box, "Pages", Vector2.zero, Vector2.one, new Vector2(60, 60), new Vector2(-60, -192));
            Kit(pageArea, "ui_panel");

            var so = new SerializedObject(screen);
            var pages = new List<GameObject>
            {
                BuildItemsPage(pageArea, so),
                BuildEquipmentPage(pageArea, so),
                BuildStatsPage(pageArea, so),
            };

            so.FindProperty("panel").objectReferenceValue = panel.gameObject;
            SetList(so.FindProperty("tabButtons"), tabs);
            SetList(so.FindProperty("pages"), pages);
            so.FindProperty("tabSprite").objectReferenceValue = PlaceholderArtGenerator.Load("ui_tab");
            so.FindProperty("tabActiveSprite").objectReferenceValue = PlaceholderArtGenerator.Load("ui_tab_active");
            so.FindProperty("swordIcon").objectReferenceValue = PlaceholderArtGenerator.Load("placeholder_icon_sword");
            so.FindProperty("permitIcon").objectReferenceValue = PlaceholderArtGenerator.Load("placeholder_icon_permit");
            var catalog = so.FindProperty("catalog");
            catalog.arraySize = Catalog.Length;
            for (int i = 0; i < Catalog.Length; i++) catalog.GetArrayElementAtIndex(i).objectReferenceValue = ChapterPrefabs.LoadMaterial(Catalog[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildItemsPage(RectTransform area, SerializedObject so)
        {
            var page = Stretch(area, "ItemsPage");
            var slots = new List<ItemSlotView>();
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 6; x++)
                    slots.Add(Slot(page, $"Slot{y * 6 + x}", new Vector2(0, 1), new Vector2(32 + x * 134, -32 - y * 134), 120));

            var detail = Rect(page, "Detail", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-480, 24), new Vector2(-24, -24));
            Kit(detail, "ui_row");
            var big = Slot(detail, "Big", new Vector2(0.5f, 1), new Vector2(0, -28), 152);
            big.Button.interactable = false;
            var name = Label(detail, "Name", "", 36, TextAlignmentOptions.Top, new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(420, 48), GoldText);
            name.fontStyle = FontStyles.Bold;
            var type = Label(detail, "Type", "", 22, TextAlignmentOptions.Top, new Vector2(0.5f, 1), new Vector2(0, -246), new Vector2(420, 32), MutedText);
            Divider(detail, -286);
            var text = Label(detail, "Description", "", 25, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1), new Vector2(0, -302), new Vector2(400, 120));
            var hint = Label(detail, "Hint", "", 22, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1), new Vector2(0, -428), new Vector2(400, 64), HintText);
            var action = KitButton(detail, "Action", "Usar", new Vector2(280, 76));
            Place(action, new Vector2(0.5f, 0), new Vector2(0, 22));

            SetList(so.FindProperty("itemSlots"), slots);
            so.FindProperty("detailSlot").objectReferenceValue = big;
            so.FindProperty("detailName").objectReferenceValue = name;
            so.FindProperty("detailType").objectReferenceValue = type;
            so.FindProperty("detailText").objectReferenceValue = text;
            so.FindProperty("detailHint").objectReferenceValue = hint;
            so.FindProperty("actionButton").objectReferenceValue = action;
            so.FindProperty("actionLabel").objectReferenceValue = action.GetComponentInChildren<TMP_Text>();
            return page.gameObject;
        }

        private static GameObject BuildEquipmentPage(RectTransform area, SerializedObject so)
        {
            var page = Stretch(area, "EquipmentPage");

            // Ranuras del cuerpo a la izquierda del muñeco
            ItemSlotView BodySlot(string label, int row)
            {
                var s = Slot(page, label, new Vector2(0, 1), new Vector2(40, -40 - row * 136), 120);
                Label(page, label + "Label", label, 26, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(176, -82 - row * 136), new Vector2(150, 36), MutedText);
                return s;
            }
            var head = BodySlot("Cabeza", 0);
            var chest = BodySlot("Pecho", 1);
            var feet = BodySlot("Pies", 2);
            var weapon = BodySlot("Arma", 3);
            weapon.Button.interactable = false;

            // Muñeco: base + piezas encima (mismo tamaño, 6x)
            var dollArea = Box(page, "Doll", new Vector2(0, 1), new Vector2(330, -24), new Vector2(380, 552));
            Kit(dollArea, "ui_row");
            Image Part(string sprite, string partName)
            {
                var rt = Box(dollArea, partName, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48 * 6, 80 * 6));
                return Image(rt, Color.white, PlaceholderArtGenerator.Load(sprite));
            }
            Part("ui_doll", "Base");
            var dollChest = Part("ui_doll_chest", "Chest");
            var dollFeet = Part("ui_doll_boots", "Boots");
            var dollHead = Part("ui_doll_helmet", "Helmet");

            // Piezas forjadas, detalle y defensa a la derecha
            Label(page, "PiecesTitle", "Armadura de cuero", 30, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(750, -40), new Vector2(500, 40), GoldText);
            var pieces = new List<ItemSlotView>();
            for (int i = 0; i < 3; i++) pieces.Add(Slot(page, "Piece" + i, new Vector2(0, 1), new Vector2(750 + i * 134, -92), 120));
            var pieceName = Label(page, "PieceName", "", 34, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(750, -232), new Vector2(520, 44), GoldText);
            pieceName.fontStyle = FontStyles.Bold;
            var pieceText = Label(page, "PieceText", "", 25, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(750, -282), new Vector2(520, 130));
            var equip = KitButton(page, "Equip", "Equipar", new Vector2(280, 76));
            Place(equip, new Vector2(0, 1), new Vector2(750, -420));
            var defense = Label(page, "Defense", "", 40, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(750, 36), new Vector2(520, 56));
            defense.fontStyle = FontStyles.Bold;

            so.FindProperty("headSlot").objectReferenceValue = head;
            so.FindProperty("chestSlot").objectReferenceValue = chest;
            so.FindProperty("feetSlot").objectReferenceValue = feet;
            so.FindProperty("weaponSlot").objectReferenceValue = weapon;
            so.FindProperty("dollHead").objectReferenceValue = dollHead;
            so.FindProperty("dollChest").objectReferenceValue = dollChest;
            so.FindProperty("dollFeet").objectReferenceValue = dollFeet;
            SetList(so.FindProperty("pieceSlots"), pieces);
            so.FindProperty("pieceName").objectReferenceValue = pieceName;
            so.FindProperty("pieceText").objectReferenceValue = pieceText;
            so.FindProperty("equipButton").objectReferenceValue = equip;
            so.FindProperty("equipLabel").objectReferenceValue = equip.GetComponentInChildren<TMP_Text>();
            so.FindProperty("defenseText").objectReferenceValue = defense;
            so.FindProperty("emptyHead").objectReferenceValue = PlaceholderArtGenerator.Load("ui_empty_head");
            so.FindProperty("emptyChest").objectReferenceValue = PlaceholderArtGenerator.Load("ui_empty_chest");
            so.FindProperty("emptyFeet").objectReferenceValue = PlaceholderArtGenerator.Load("ui_empty_feet");
            return page.gameObject;
        }

        private static GameObject BuildStatsPage(RectTransform area, SerializedObject so)
        {
            var page = Stretch(area, "StatsPage");

            var left = Rect(page, "Character", new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(24, 24), new Vector2(-12, -24));
            Kit(left, "ui_row");
            var portrait = Box(left, "Portrait", new Vector2(0, 1), new Vector2(32, -32), new Vector2(160, 160));
            Kit(portrait, "ui_slot");
            Image(Box(portrait, "Face", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(136, 136)), Color.white,
                PlaceholderArtGenerator.Load("placeholder_portrait_tsuki")).preserveAspect = true;
            var tsuki = Label(left, "Name", "Tsuki", 44, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(220, -48), new Vector2(360, 56), GoldText);
            tsuki.fontStyle = FontStyles.Bold;
            Label(left, "Role", "Portador de la espada carmesí", 22, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(220, -108), new Vector2(380, 32), MutedText);
            var barBg = Box(left, "HealthBar", new Vector2(0, 1), new Vector2(220, -150), new Vector2(380, 36));
            Kit(barBg, "ui_slot");
            var fill = Rect(barBg, "Fill", Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            var fillImg = Image(fill, Crimson);
            fillImg.type = UnityEngine.UI.Image.Type.Filled;
            fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var statsLeft = Label(left, "Stats", "", 34, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(40, -230), new Vector2(560, 340));
            statsLeft.lineSpacing = 10;

            var right = Rect(page, "Progress", new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(12, 24), new Vector2(-24, -24));
            Kit(right, "ui_row");
            Label(right, "Title", "Progreso del capítulo", 32, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(40, -40), new Vector2(560, 44), GoldText);
            Divider(right, -96);
            var statsRight = Label(right, "Stats", "", 32, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(40, -120), new Vector2(560, 400));
            statsRight.lineSpacing = 14;

            so.FindProperty("statsLeft").objectReferenceValue = statsLeft;
            so.FindProperty("statsRight").objectReferenceValue = statsRight;
            so.FindProperty("healthFill").objectReferenceValue = fillImg;
            return page.gameObject;
        }

        // ---------------------------------------------------------------- piezas del kit

        // Ventana con marco dorado, título y subtítulo opcional.
        private static RectTransform Window(Transform parent, string title, string subtitle)
        {
            var box = Box(parent, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1440, 860));
            Kit(box, "ui_frame");
            var t = Label(box, "Title", title, 58, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(64, -36), new Vector2(700, 70), GoldText);
            t.fontStyle = FontStyles.Bold;
            if (subtitle != null)
                Label(box, "Subtitle", subtitle, 24, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(66, -106), new Vector2(900, 40), MutedText);
            return box;
        }

        public static Image Kit(RectTransform rt, string sprite)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PlaceholderArtGenerator.Load(sprite);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Px;
            img.raycastTarget = false;
            return img;
        }

        private static void Divider(RectTransform parent, float y)
        {
            var rt = Rect(parent, "Divider", new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, y - 3), new Vector2(-32, y));
            Image(rt, new Color(0.54f, 0.40f, 0.15f, 1f));
        }

        public static ItemSlotView Slot(Transform parent, string name, Vector2 anchor, Vector2 pos, float size)
        {
            var rt = Box(parent, name, anchor, pos, new Vector2(size, size));
            var frame = Kit(rt, "ui_slot");
            frame.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.88f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = Color.white;
            button.colors = colors;

            var icon = Image(Box(rt, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.66f, size * 0.66f)), Color.white, PlaceholderArtGenerator.Load("placeholder_icon_sword"));
            icon.preserveAspect = true;
            var count = Label(rt, "Count", "", Mathf.Max(20, size * 0.2f), TextAlignmentOptions.BottomRight, new Vector2(1, 0), new Vector2(-10, 6), new Vector2(size - 16, 34));
            count.fontStyle = FontStyles.Bold;
            var badge = Box(rt, "Equipped", new Vector2(0, 1), new Vector2(6, -6), new Vector2(27, 30));
            Image(badge, Color.white, PlaceholderArtGenerator.Load("ui_badge_equipped"));
            badge.gameObject.SetActive(false);

            var view = rt.gameObject.AddComponent<ItemSlotView>();
            view.Setup(frame, icon, count, badge.gameObject, PlaceholderArtGenerator.Load("ui_slot"), PlaceholderArtGenerator.Load("ui_slot_selected"), button);
            return view;
        }

        public static Button KitButton(Transform parent, string name, string label, Vector2 size)
        {
            var rt = Box(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = Kit(rt, "ui_button");
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = new Color(0.88f, 0.88f, 0.88f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.88f, 0.88f, 0.88f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.disabledColor = new Color(0.4f, 0.38f, 0.45f, 0.85f);
            button.colors = colors;
            var t = Text(Stretch(rt, "Label"), label, 32, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            return button;
        }

        private static Button TabButton(Transform parent, string label)
        {
            var rt = Box(parent, "Tab_" + label, new Vector2(0, 1), Vector2.zero, new Vector2(296, 80));
            var img = Kit(rt, "ui_tab");
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.9f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var t = Text(Stretch(rt, "Label"), label, 32, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void Place(Component c, Vector2 anchor, Vector2 pos)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
        }

        private static void SetList<T>(SerializedProperty prop, IList<T> items) where T : Object
        {
            prop.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
