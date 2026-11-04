using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aplica el estilo de la UI (arte de UiArt) a lo que arma el builder del capítulo dentro de GameSystems:
// vida de Tsuki, objetivo, aviso de interacción, avisos, título de zona, barra del guardián, diálogo,
// pausa y derrota. Se hace al iniciar para no tener que reconstruir el prefab; solo reacomoda y decora,
// las referencias de los scripts (GameHud, DialogueUI, PauseMenu, DefeatScreen) siguen igual.
public static class UiSkin
{
    // Tamaño en pantalla (unidades de 1920x1080) de cada pixel del arte
    public const float Px = 2f;        // marcos, listones, líneas
    public const float DecoPx = 3f;    // flores, íconos, torii, nubes
    public const float IconScale = DecoPx;

    private static readonly Color32 PanelTop = new Color32(24, 10, 26, 246);
    private static readonly Color32 PanelBottom = new Color32(46, 14, 36, 246);
    private static readonly Color32 DefeatTop = new Color32(30, 8, 22, 250);
    private static readonly Color32 DefeatBottom = new Color32(66, 12, 30, 250);
    private static readonly Color WaveColor = new Color(0.78f, 0.36f, 0.58f, 0.16f);

    // ------------------------------------------------------------------ sprites

    public static Sprite FrameSprite => UiArt.Get("frame", UiArt.Frame, Vector4.one * UiArt.FrameBorder);
    public static Sprite PlateSprite => UiArt.Get("plate", UiArt.ButtonPlate, Vector4.one * UiArt.PlateBorder);
    public static Sprite RibbonSprite(int h) => UiArt.Get("ribbon" + h, () => UiArt.Ribbon(h), UiArt.RibbonBorder(h));
    public static Sprite IconSprite(string name) => UiArt.Get("icon_" + name, () => UiArt.Icon(name));
    private static Sprite Diamond => UiArt.Get("diamond", UiArt.Diamond);
    private static Sprite[] petals;

    private static Sprite[] PetalSprites => petals != null && petals[0] != null ? petals : petals = new[]
    {
        UiArt.Get("petal0", () => UiArt.PetalSprite(0)),
        UiArt.Get("petal1", () => UiArt.PetalSprite(1)),
    };

    // ------------------------------------------------------------------ entrada

    public static void Apply(Transform systemsRoot)
    {
        var ui = systemsRoot != null ? systemsRoot.Find("UI") : null;
        if (ui == null) return;
        Safe(() => StyleHud(ui.Find("HUD")), "HUD");
        Safe(() => StyleDialogue(ui.Find("Dialogue")), "diálogo");
        Safe(() => StylePause(ui.Find("Pause")), "pausa");
        Safe(() => StyleDefeat(ui.Find("Defeat")), "derrota");
        Safe(() => StyleWindow(ui.Find("Forge")), "forja");
        Safe(() => StyleWindow(ui.Find("Inventory")), "inventario");
    }

    // Si algo del prefab cambió de nombre, esa parte se queda con el estilo viejo pero el resto sigue
    private static void Safe(System.Action action, string part)
    {
        try { action(); }
        catch (System.Exception e) { Debug.LogWarning("[UiSkin] No se pudo aplicar el estilo a " + part + ": " + e.Message); }
    }

    // ------------------------------------------------------------------ HUD

    private static void StyleHud(Transform hud)
    {
        if (hud == null) return;
        StyleHealth(R(hud, "HealthFrame"));
        StyleObjective(R(hud, "Objective"));
        StylePrompt(R(hud, "Prompt"));

        var hint = R(hud, "InventoryHint");
        if (hint != null)
        {
            hint.anchoredPosition = new Vector2(44f, -136f);
            Tint(hint, UiArt.TextMuted);
        }

        var toast = R(hud, "Toast");
        if (toast != null)
        {
            Panel(toast, PanelTop, PanelBottom, false);
            Tint(toast.Find("Text"), UiArt.TextCream);
            Deco(toast, "Flower", UiArt.Get("sakura11", () => UiArt.Sakura(11)), new Vector2(0f, 0.5f), new Vector2(-4f, 4f), DecoPx);
            Deco(toast, "FlowerR", UiArt.Get("sakura9", () => UiArt.Sakura(9, false, 1f)), new Vector2(1f, 0.5f), new Vector2(4f, -4f), DecoPx);
        }

        var zone = R(hud, "ZoneTitle");
        if (zone != null)
        {
            Panel(zone, PanelTop, PanelBottom, false);
            var text = Text(zone.Find("Text"));
            if (text != null) GoldTitle(text);
            Flourishes(zone, new Vector2(0f, 0f), 640f, true);
        }

        var boss = R(hud, "BossBar");
        if (boss != null)
        {
            ClearImage(boss);
            Panel(boss, PanelTop, PanelBottom, false);
            Tint(boss.Find("Name"), UiArt.TextGold);
            StyleBar(R(boss, "Bar"), false);
        }
    }

    // Vida: rombo con la cara de Tsuki a la izquierda, nombre y números, barra carmesí fina y sakura.
    private static void StyleHealth(RectTransform frame)
    {
        if (frame == null) return;
        ClearImage(frame);
        frame.anchoredPosition = new Vector2(96f, -30f);
        frame.sizeDelta = new Vector2(420f, 88f);
        Panel(frame, PanelTop, PanelBottom, false);

        var name = R(frame, "Name");
        if (name != null)
        {
            name.anchoredPosition = new Vector2(64f, -12f);
            name.sizeDelta = new Vector2(180f, 34f);
            var t = Text(name);
            if (t != null) { GoldTitle(t, 0.12f); t.fontSize = 28; }
        }
        var hp = R(frame, "HealthText");
        if (hp != null)
        {
            hp.anchoredPosition = new Vector2(-26f, -16f);
            Tint(hp, UiArt.TextCream);
        }

        var bar = R(frame, "Bar");
        if (bar != null)
        {
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 0f);
            bar.anchoredPosition = new Vector2(62f, 20f);
            bar.sizeDelta = new Vector2(330f, 18f);
            StyleBar(bar, true);
        }

        // línea fina con rombo bajo la barra
        var line = Deco(frame, "Underline", UiArt.Get("divider160", () => UiArt.Divider(160)), new Vector2(0f, 0f), new Vector2(70f, 6f), Px);
        line.rectTransform.pivot = new Vector2(0f, 0f);
        line.color = new Color(1f, 1f, 1f, 0.7f);

        Portrait(frame);
        var cluster = Deco(frame, "Sakura", UiArt.Get("cluster1", () => UiArt.SakuraCluster(1)), new Vector2(1f, 0f), new Vector2(18f, -14f), Px);
        cluster.rectTransform.pivot = new Vector2(0.7f, 0.4f);
    }

    // Rombo dorado con la cara de Tsuki (recorte del busto del diálogo con una máscara girada 45°)
    private static void Portrait(RectTransform frame)
    {
        var bust = Resources.Load<Sprite>("Portraits/tsuki_bust");
        var holder = Child(frame, "PortraitDiamond");
        holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0f, 0.5f);
        holder.pivot = new Vector2(0.5f, 0.5f);
        holder.anchoredPosition = new Vector2(-10f, 0f);
        holder.sizeDelta = new Vector2(80f, 80f);
        holder.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var back = Stretch(holder, "Mask", 4f);
        var backImg = back.gameObject.AddComponent<Image>();
        backImg.color = new Color32(54, 22, 48, 255);
        backImg.raycastTarget = false;
        back.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        if (bust != null)
        {
            var upright = Child(back, "Upright");
            upright.anchorMin = upright.anchorMax = upright.pivot = new Vector2(0.5f, 0.5f);
            upright.sizeDelta = Vector2.zero;
            upright.localRotation = Quaternion.Euler(0f, 0f, -45f);
            var face = Child(upright, "Face");
            face.anchorMin = face.anchorMax = face.pivot = new Vector2(0.5f, 0.5f);
            // el busto mide 466x640; la cara está arriba a la izquierda del centro
            float k = 0.5f;
            face.sizeDelta = new Vector2(466f, 640f) * k;
            face.anchoredPosition = new Vector2(28f, -118f) * k;
            var img = face.gameObject.AddComponent<Image>();
            img.sprite = bust;
            img.raycastTarget = false;
        }

        var border = Stretch(holder, "Border", 0f);
        var bImg = border.gameObject.AddComponent<Image>();
        bImg.sprite = UiArt.Get("square24", () => UiArt.SquareFrame(24), Vector4.one * 7f);
        bImg.type = Image.Type.Sliced;
        bImg.pixelsPerUnitMultiplier = 1f / Px;
        bImg.raycastTarget = false;
    }

    private static void StyleBar(RectTransform bar, bool crimson)
    {
        if (bar == null) return;
        var outline = bar.GetComponent<Image>();
        if (outline != null) outline.color = new Color32(222, 168, 92, 255);
        var back = bar.Find("Back");
        if (back == null) return;
        var backImg = back.GetComponent<Image>();
        if (backImg != null) backImg.color = new Color32(34, 10, 22, 255);
        var fill = back.Find("Fill")?.GetComponent<Image>();
        if (fill != null && crimson)
        {
            fill.sprite = UiArt.Get("barfill", () => UiArt.Gradient(UiArt.CrimsonLight, UiArt.CrimsonDark, 8));
            fill.color = Color.white;
        }
        var shine = back.Find("Shine")?.GetComponent<Image>();
        if (shine != null) shine.color = new Color(1f, 0.85f, 0.9f, 0.12f);
    }

    // Objetivo: listón "Objetivo" con rombo, texto y un torii rojo entre nubes a la derecha.
    private static void StyleObjective(RectTransform box)
    {
        if (box == null) return;
        ClearImage(box);
        box.sizeDelta = new Vector2(600f, 172f);
        Panel(box, PanelTop, PanelBottom, false);

        var cloud = Deco(box, "Cloud", UiArt.Get("cloud60", () => UiArt.Cloud(60, 16, 5)), new Vector2(1f, 0f), new Vector2(-12f, 10f), Px);
        cloud.rectTransform.pivot = new Vector2(1f, 0f);
        var torii = Deco(box, "Torii", UiArt.Get("torii", UiArt.Torii), new Vector2(1f, 0f), new Vector2(-34f, 18f), DecoPx);
        torii.rectTransform.pivot = new Vector2(1f, 0f);
        var cloudFront = Deco(box, "CloudFront", UiArt.Get("cloud40", () => UiArt.Cloud(40, 12, 8)), new Vector2(1f, 0f), new Vector2(-70f, 8f), Px);
        cloudFront.rectTransform.pivot = new Vector2(1f, 0f);
        Petal(box, new Vector2(0.62f, 0.25f), 20f);
        Petal(box, new Vector2(0.92f, 0.82f), -35f);

        var header = R(box, "Header");
        if (header != null)
        {
            var tag = Ribbon(box, "Tag", 24, new Vector2(0f, 1f), new Vector2(10f, -10f), 250f);
            tag.SetSiblingIndex(header.GetSiblingIndex());
            Deco(tag, "Diamond", Diamond, new Vector2(0f, 0.5f), new Vector2(40f, 0f), Px);
            header.anchoredPosition = new Vector2(66f, -10f);
            header.sizeDelta = new Vector2(200f, 48f);
            var t = Text(header);
            if (t != null)
            {
                t.text = "Objetivo";
                t.fontSize = 28;
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.color = UiArt.TextCream;
            }
        }
        var text = R(box, "Text");
        if (text != null)
        {
            text.anchoredPosition = new Vector2(30f, -66f);
            text.sizeDelta = new Vector2(440f, 94f);
            Tint(text, UiArt.TextCream);
        }
    }

    // Aviso "E: Guardar partida": marco con ícono a la izquierda y sakura en los dos extremos.
    private static void StylePrompt(RectTransform box)
    {
        if (box == null) return;
        ClearImage(box);
        box.sizeDelta = new Vector2(600f, 76f);
        Panel(box, PanelTop, PanelBottom, false);
        var left = Deco(box, "SakuraL", UiArt.Get("cluster2", () => UiArt.SakuraCluster(2)), new Vector2(0f, 0.5f), new Vector2(-6f, -4f), Px);
        left.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        Deco(box, "SakuraR", UiArt.Get("cluster3", () => UiArt.SakuraCluster(3)), new Vector2(1f, 0.5f), new Vector2(6f, 4f), Px);

        var textRt = R(box, "Text");
        var text = Text(textRt);
        var icon = Deco(box, "Icon", IconSprite("save"), new Vector2(0f, 0.5f), new Vector2(70f, 0f), IconScale);
        if (text != null)
        {
            text.color = UiArt.TextCream;
            text.margin = new Vector4(90f, 0f, 30f, 0f);
            box.gameObject.AddComponent<UiPromptIcon>().Setup(text, icon);
        }
    }

    // ------------------------------------------------------------------ diálogo

    private static void StyleDialogue(Transform root)
    {
        var panel = R(root, "Panel");
        if (panel == null) return;
        ClearImage(panel); // sigue recibiendo clics (alfa 0)
        Panel(panel, PanelTop, PanelBottom, false);

        // nubes y pétalos al fondo, rama de sakura en la esquina de arriba a la derecha
        var cloud = Deco(panel, "Cloud", UiArt.Get("cloud80", () => UiArt.Cloud(80, 20, 9)), new Vector2(1f, 0f), new Vector2(-150f, 14f), Px);
        cloud.rectTransform.pivot = new Vector2(1f, 0f);
        cloud.color = new Color(1f, 1f, 1f, 0.8f);
        var cloud2 = Deco(panel, "Cloud2", UiArt.Get("cloud60", () => UiArt.Cloud(60, 16, 5)), new Vector2(1f, 0f), new Vector2(-330f, 10f), Px);
        cloud2.rectTransform.pivot = new Vector2(1f, 0f);
        cloud2.color = new Color(1f, 1f, 1f, 0.6f);
        Petal(panel, new Vector2(0.20f, 0.12f), 30f);
        Petal(panel, new Vector2(0.48f, 0.80f), -20f);
        Petal(panel, new Vector2(0.74f, 0.30f), 60f);
        var branch = Deco(panel, "Sakura", UiArt.Get("branch70", () => UiArt.SakuraBranch(70, 40, 3)), new Vector2(1f, 1f), new Vector2(18f, 26f), DecoPx);
        branch.rectTransform.pivot = new Vector2(1f, 1f);
        branch.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        branch.rectTransform.pivot = new Vector2(0f, 1f); // volteado: el pivote se invierte

        // retrato: marco dorado cuadrado
        var frame = R(panel, "PortraitFrame");
        if (frame != null)
        {
            var img = frame.GetComponent<Image>();
            if (img != null) img.color = new Color32(46, 18, 42, 255);
            var border = Stretch(frame, "Border", -6f);
            var bImg = border.gameObject.AddComponent<Image>();
            bImg.sprite = UiArt.Get("square24", () => UiArt.SquareFrame(24), Vector4.one * 7f);
            bImg.type = Image.Type.Sliced;
            bImg.pixelsPerUnitMultiplier = 1f / Px;
            bImg.raycastTarget = false;
        }

        // nombre: listón carmesí con una flor
        var nameBox = R(panel, "NameBox");
        if (nameBox != null)
        {
            var img = nameBox.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = RibbonSprite(24);
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f / Px;
                img.color = Color.white;
            }
            nameBox.sizeDelta = new Vector2(330f, 48f);
            nameBox.anchoredPosition = new Vector2(270f, 24f);
            Deco(nameBox, "Flower", UiArt.Get("sakura11", () => UiArt.Sakura(11)), new Vector2(0f, 0.5f), new Vector2(50f, 0f), DecoPx);
            var t = Text(nameBox.Find("Name"));
            if (t != null)
            {
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.margin = new Vector4(84f, 0f, 30f, 0f);
                t.fontSize = 32;
                t.color = UiArt.TextCream;
            }
        }

        Tint(panel.Find("Body"), UiArt.TextCream);

        var hint = R(panel, "ContinueHint");
        if (hint != null)
        {
            // más visible: texto crema más grande, teclas doradas (DialogueUI) y la flecha más grande
            var ht = Text(hint);
            if (ht != null)
            {
                ht.color = UiArt.TextCream;
                ht.fontSize = 27;
                ht.enableAutoSizing = false;
            }
            var arrow = Deco(hint, "Arrow", UiArt.Get("arrow", UiArt.DownArrow), new Vector2(1f, 1f), new Vector2(-14f, 18f), 4f);
            arrow.rectTransform.pivot = new Vector2(1f, 0f);
            arrow.gameObject.AddComponent<UiBob>().Setup(DecoPx);
        }
    }

    // ------------------------------------------------------------------ pausa

    private static void StylePause(Transform root)
    {
        var box = R(root, "Panel/Box");
        if (box == null) return;
        ClearImage(box);
        box.sizeDelta = new Vector2(900f, 660f);
        Panel(box, PanelTop, PanelBottom, true);

        var title = R(box, "Title");
        if (title != null)
        {
            title.anchoredPosition = new Vector2(0f, -30f);
            var t = Text(title);
            if (t != null) { GoldTitle(t); t.fontSize = 76; }
            Flourishes(box, new Vector2(0f, -74f), 300f, false, new Vector2(0.5f, 1f));
            var small = Deco(box, "TitleDiamond", UiArt.Get("divider40", () => UiArt.Divider(40)), new Vector2(0.5f, 1f), new Vector2(0f, -124f), Px);
            small.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        // adornos: flor sobre el borde, estandarte colgante y rama de sakura
        Deco(box, "Knot", UiArt.Get("sakura15r", () => UiArt.Sakura(15, true)), new Vector2(0.5f, 1f), new Vector2(0f, 4f), DecoPx);
        var banner = Deco(box, "Banner", UiArt.Get("banner", UiArt.Banner), new Vector2(1f, 1f), new Vector2(-46f, -2f), DecoPx);
        banner.rectTransform.pivot = new Vector2(1f, 1f);
        var branch = Deco(box, "Sakura", UiArt.Get("branch90", () => UiArt.SakuraBranch(90, 50, 11)), new Vector2(0f, 1f), new Vector2(-22f, 26f), DecoPx);
        branch.rectTransform.pivot = new Vector2(0f, 1f);

        var buttons = R(box, "Buttons");
        if (buttons != null)
        {
            buttons.anchoredPosition = new Vector2(0f, -150f);
            buttons.sizeDelta = new Vector2(560f, 300f);
            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.spacing = 12f;
            ThemeButton(R(buttons, "Resume"), "diamond", 560f);
            ThemeButton(R(buttons, "Retry"), "torii", 560f);
            ThemeButton(R(buttons, "Menu"), "home", 560f);
            ThemeButton(R(buttons, "Quit"), "exit", 560f);
        }

        // controles: placa interior con las teclas en dos filas
        var controls = R(box, "Controls");
        if (controls != null)
        {
            var plate = Child(box, "ControlsPlate");
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(0.5f, 0f);
            plate.anchoredPosition = new Vector2(0f, 30f);
            plate.sizeDelta = new Vector2(820f, 140f);
            plate.SetSiblingIndex(controls.GetSiblingIndex());
            var pImg = plate.gameObject.AddComponent<Image>();
            pImg.sprite = PlateSprite;
            pImg.type = Image.Type.Sliced;
            pImg.pixelsPerUnitMultiplier = 1f / Px;
            pImg.color = new Color(1f, 1f, 1f, 0.9f);
            pImg.raycastTarget = false;

            controls.anchoredPosition = new Vector2(0f, 38f);
            controls.sizeDelta = new Vector2(790f, 124f);
            var t = Text(controls);
            if (t != null)
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMin = 14;
                t.fontSize = 22;
                t.fontSizeMax = 22;
                t.lineSpacing = 14f;
                t.color = UiArt.TextCream;
                t.alignment = TextAlignmentOptions.Center;
            }
        }

        Petals(box, 7);
    }

    // ------------------------------------------------------------------ derrota

    private static void StyleDefeat(Transform root)
    {
        var panel = R(root, "Panel");
        if (panel == null) return;

        var card = Child(panel, "Card");
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(0f, 40f);
        card.sizeDelta = new Vector2(1480f, 600f);
        card.SetSiblingIndex(0);
        Panel(card, DefeatTop, DefeatBottom, false);

        // nubes arriba, lirios rojos abajo en las esquinas
        var c1 = Deco(card, "CloudL", UiArt.Get("cloud80", () => UiArt.Cloud(80, 20, 9)), new Vector2(0f, 1f), new Vector2(60f, -40f), DecoPx);
        c1.rectTransform.pivot = new Vector2(0f, 1f);
        var c2 = Deco(card, "CloudR", UiArt.Get("cloud70", () => UiArt.Cloud(70, 18, 4)), new Vector2(1f, 1f), new Vector2(-50f, -60f), DecoPx);
        c2.rectTransform.pivot = new Vector2(1f, 1f);
        var lilyL = Deco(card, "LiliesL", UiArt.Get("lilies7", () => UiArt.LilyField(90, 50, 7)), new Vector2(0f, 0f), new Vector2(12f, 8f), DecoPx);
        lilyL.rectTransform.pivot = new Vector2(0f, 0f);
        var lilyR = Deco(card, "LiliesR", UiArt.Get("lilies12", () => UiArt.LilyField(90, 50, 12)), new Vector2(1f, 0f), new Vector2(-12f, 8f), DecoPx);
        lilyR.rectTransform.pivot = new Vector2(0f, 0f);
        lilyR.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        // flor roja con líneas sobre el título
        var top = Deco(card, "TopDivider", UiArt.Get("divider200", () => UiArt.Divider(200)), new Vector2(0.5f, 1f), new Vector2(0f, -66f), Px);
        top.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Deco(card, "Flower", UiArt.Get("sakura15r", () => UiArt.Sakura(15, true)), new Vector2(0.5f, 1f), new Vector2(0f, -66f), DecoPx)
            .rectTransform.pivot = new Vector2(0.5f, 0.5f);

        var title = R(panel, "Title");
        if (title != null)
        {
            title.anchoredPosition = new Vector2(0f, 200f);
            var t = Text(title);
            if (t != null) { GoldTitle(t); t.fontSize = 92; }
            var under = Deco(panel, "TitleDivider", UiArt.Get("divider120", () => UiArt.Divider(120)), new Vector2(0.5f, 0.5f), new Vector2(0f, 136f), Px);
            under.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            under.rectTransform.SetSiblingIndex(title.GetSiblingIndex() + 1);
        }
        var subtitle = R(panel, "Subtitle");
        if (subtitle != null)
        {
            subtitle.anchoredPosition = new Vector2(0f, 88f);
            Tint(subtitle, UiArt.TextCream);
            var t = Text(subtitle);
            if (t != null) t.fontSize = 30;
        }

        var buttons = R(panel, "Buttons");
        if (buttons != null)
        {
            buttons.anchoredPosition = new Vector2(0f, -70f);
            buttons.sizeDelta = new Vector2(460f, 142f);
            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.spacing = 14f;
            ThemeButton(R(buttons, "Retry"), "diamond", 460f);
            ThemeButton(R(buttons, "Menu"), "home", 460f);
        }

        Petals(card, 9);
    }

    // ------------------------------------------------------------------ forja e inventario

    public static Sprite InsetSprite => UiArt.Get("inset", UiArt.InsetPanel, Vector4.one * UiArt.PlateBorder);
    public static Sprite SlotSprite(bool selected) =>
        UiArt.Get(selected ? "slot_on" : "slot", () => UiArt.Slot(selected), Vector4.one * UiArt.PlateBorder);

    // Ventanas grandes (forja de Kaede e inventario): mismo marco que la pausa, paneles interiores,
    // casillas, filas, pestañas y botones con el estilo nuevo. Se cambia el arte del kit por nombre.
    private static void StyleWindow(Transform root)
    {
        var box = R(root, "Panel/Box");
        if (box == null) return;
        Panel(box, PanelTop, PanelBottom, true);

        var title = Text(box.Find("Title"));
        if (title != null) GoldTitle(title);
        Tint(box.Find("Subtitle"), UiArt.TextMuted);
        Tint(box.Find("Hint"), UiArt.TextMuted);

        Deco(box, "Knot", UiArt.Get("sakura15r", () => UiArt.Sakura(15, true)), new Vector2(0.5f, 1f), new Vector2(0f, 4f), DecoPx);
        var banner = Deco(box, "Banner", UiArt.Get("banner", UiArt.Banner), new Vector2(1f, 1f), new Vector2(-46f, -2f), DecoPx);
        banner.rectTransform.pivot = new Vector2(1f, 1f);
        var branch = Deco(box, "Sakura", UiArt.Get("branch90", () => UiArt.SakuraBranch(90, 50, 11)), new Vector2(0f, 1f), new Vector2(-30f, 30f), DecoPx);
        branch.rectTransform.pivot = new Vector2(0f, 1f);
        branch.rectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var lilies = Deco(box, "Cloud", UiArt.Get("cloud80", () => UiArt.Cloud(80, 20, 9)), new Vector2(1f, 0f), new Vector2(-40f, 14f), Px);
        lilies.rectTransform.pivot = new Vector2(1f, 0f);
        lilies.color = new Color(1f, 1f, 1f, 0.6f);

        // casillas y filas (guardan su arte normal / elegido)
        foreach (var slot in box.GetComponentsInChildren<ItemSlotView>(true))
            slot.Restyle(SlotSprite(false), SlotSprite(true), 1f / Px);
        foreach (var row in box.GetComponentsInChildren<RecipeRowView>(true))
            row.Restyle(PlateSprite, SlotSprite(true), 1f / Px);
        var inventory = root.GetComponent<InventoryScreen>();
        if (inventory != null) inventory.SetTabSprites(PlateSprite, RibbonSprite(40), 1f / Px);

        // el resto del kit, por nombre del sprite
        foreach (var img in box.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null) continue;
            switch (img.sprite.name)
            {
                case "ui_panel":
                case "ui_row":
                    Reskin(img, InsetSprite);
                    break;
                case "ui_slot":
                case "ui_slot_selected":
                    Reskin(img, SlotSprite(img.sprite.name == "ui_slot_selected"));
                    break;
                case "ui_button":
                    var button = img.GetComponent<Button>();
                    if (button != null && img.GetComponent<UiThemeButton>() == null)
                    {
                        var rt = (RectTransform)img.transform;
                        ThemeButton(rt, null, rt.sizeDelta.x);
                    }
                    break;
            }
        }

        // textos: dorado y crema de la nueva paleta, divisores dorados
        foreach (var t in box.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.name == "Hint" && t.transform.parent != box) t.color = new Color(1f, 0.72f, 0.78f);
            else if (t.color.b > 0.85f && t.color.r > 0.85f) t.color = UiArt.TextCream; // blancos
        }
        foreach (var img in box.GetComponentsInChildren<Image>(true))
            if (img.name == "Divider") img.color = new Color(0.87f, 0.66f, 0.36f, 0.8f);
        // barra de vida de estadísticas en carmesí con degradado
        var hp = box.Find("Pages/StatsPage/Character/HealthBar/Fill")?.GetComponent<Image>();
        if (hp != null)
        {
            hp.sprite = UiArt.Get("barfill", () => UiArt.Gradient(UiArt.CrimsonLight, UiArt.CrimsonDark, 8));
            hp.color = Color.white;
        }

        Petals(box, 6);
    }

    private static void Reskin(Image img, Sprite sprite)
    {
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f / Px;
        img.color = Color.white;
    }

    // ------------------------------------------------------------------ menú principal

    // Cuadro "Ya hay una partida guardada": marco, flores y los mismos botones que la pausa.
    public static void StyleConfirm(Transform confirm)
    {
        Safe(() =>
        {
            var box = R(confirm, "Box");
            if (box == null) return;
            ClearImage(box);
            box.sizeDelta = new Vector2(900f, 400f);
            Panel(box, PanelTop, PanelBottom, true);
            Deco(box, "Knot", UiArt.Get("sakura15r", () => UiArt.Sakura(15, true)), new Vector2(0.5f, 1f), new Vector2(0f, 4f), DecoPx);
            var branch = Deco(box, "Sakura", UiArt.Get("branch70", () => UiArt.SakuraBranch(70, 40, 3)), new Vector2(0f, 1f), new Vector2(-20f, 24f), DecoPx);
            branch.rectTransform.pivot = new Vector2(0f, 1f);
            var cluster = Deco(box, "SakuraR", UiArt.Get("cluster3", () => UiArt.SakuraCluster(3)), new Vector2(1f, 0f), new Vector2(10f, -6f), Px);
            cluster.rectTransform.pivot = new Vector2(0.6f, 0.4f);

            var text = R(box, "Text");
            if (text != null)
            {
                text.anchoredPosition = new Vector2(0f, -56f);
                Tint(text, UiArt.TextCream);
                var under = Deco(box, "Divider", UiArt.Get("divider120", () => UiArt.Divider(120)), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), Px);
                under.rectTransform.SetSiblingIndex(text.GetSiblingIndex() + 1);
            }
            ThemeButton(R(box, "Yes"), "diamond", 330f);
            ThemeButton(R(box, "No"), "exit", 330f);
            var yes = R(box, "Yes"); if (yes != null) yes.anchoredPosition = new Vector2(-185f, -122f);
            var no = R(box, "No"); if (no != null) no.anchoredPosition = new Vector2(185f, -122f);
        }, "confirmación del menú");
    }

    // ------------------------------------------------------------------ piezas

    // Botón: placa normal o listón al elegirlo, ícono a la izquierda y rombo en la punta.
    private static void ThemeButton(RectTransform rt, string icon, float width)
    {
        if (rt == null) return;
        const float h = 64f;
        rt.sizeDelta = new Vector2(width, h);
        var le = rt.GetComponent<LayoutElement>();
        if (le != null) { le.preferredWidth = width; le.preferredHeight = h; }

        var bg = rt.GetComponent<Image>();
        if (bg == null) return;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1f / Px;
        bg.color = Color.white;
        bg.sprite = PlateSprite;

        var iconImg = icon != null ? Deco(rt, "Icon", IconSprite(icon), new Vector2(0f, 0.5f), new Vector2(58f, 0f), IconScale) : null;
        var ornament = Deco(rt, "Ornament", Diamond, new Vector2(0f, 0.5f), new Vector2(-2f, 0f), DecoPx);
        var label = rt.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 30;
            label.margin = icon != null ? new Vector4(70f, 0f, 30f, 0f) : new Vector4(24f, 0f, 24f, 0f);
            label.transform.SetAsLastSibling();
        }
        rt.gameObject.AddComponent<UiThemeButton>().Setup(bg, PlateSprite, RibbonSprite(32), iconImg, ornament, label);
    }

    // Fondo con degradado + marco ornamentado (y olas opcionales) detrás de todo lo que ya tenía.
    private static void Panel(RectTransform rt, Color32 top, Color32 bottom, bool waves)
    {
        ClearImage(rt);
        var bg = Stretch(rt, "ThemeFill", UiArt.FrameInset * Px);
        bg.SetSiblingIndex(0);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = UiArt.Get("grad_" + top.r + "_" + bottom.r, () => UiArt.Gradient(top, bottom), default, true);
        bgImg.raycastTarget = false;

        if (waves)
        {
            var w = Child(bg, "Waves");
            w.anchorMin = Vector2.zero;
            w.anchorMax = new Vector2(1f, 0.45f);
            w.offsetMin = w.offsetMax = Vector2.zero;
            var wImg = w.gameObject.AddComponent<Image>();
            wImg.sprite = Waves;
            wImg.type = Image.Type.Tiled;
            wImg.pixelsPerUnitMultiplier = 1f / Px;
            wImg.color = WaveColor;
            wImg.raycastTarget = false;
        }

        var frame = Stretch(rt, "ThemeFrame", 0f);
        frame.SetSiblingIndex(1);
        var fImg = frame.gameObject.AddComponent<Image>();
        fImg.sprite = FrameSprite;
        fImg.type = Image.Type.Sliced;
        fImg.fillCenter = false;
        fImg.pixelsPerUnitMultiplier = 1f / Px;
        fImg.raycastTarget = false;
    }

    private static Sprite waves;
    private static Sprite Waves
    {
        get
        {
            if (waves != null) return waves;
            waves = UiArt.Get("waves", () => UiArt.Seigaiha(new Color32(255, 255, 255, 255)));
            waves.texture.wrapMode = TextureWrapMode.Repeat;
            return waves;
        }
    }

    // Listón con puntas de alto h pixeles del arte, anclado en anchor.
    private static RectTransform Ribbon(RectTransform parent, string name, int h, Vector2 anchor, Vector2 pos, float width)
    {
        var rt = Child(parent, name);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, h * Px);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = RibbonSprite(h);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f / Px;
        img.raycastTarget = false;
        return rt;
    }

    // Líneas con flecha a los dos lados de un título (o a lo largo de un panel con alongEdges)
    private static void Flourishes(RectTransform parent, Vector2 pos, float gap, bool alongEdges, Vector2? anchor = null)
    {
        var sprite = UiArt.Get("flourish", () => UiArt.TitleFlourish());
        var a = anchor ?? new Vector2(0.5f, 0.5f);
        var left = Deco(parent, "FlourishL", sprite, a, pos + new Vector2(-gap / 2f, 0f), Px);
        left.rectTransform.pivot = new Vector2(0f, 0.5f);
        left.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        var right = Deco(parent, "FlourishR", sprite, a, pos + new Vector2(gap / 2f, 0f), Px);
        right.rectTransform.pivot = new Vector2(0f, 0.5f);
        if (alongEdges)
        {
            left.color = right.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private static void Petal(RectTransform parent, Vector2 anchor, float angle)
    {
        var img = Deco(parent, "Petal", PetalSprites[Random.Range(0, PetalSprites.Length)], anchor, Vector2.zero, DecoPx);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round(angle / 45f) * 45f);
        img.color = new Color(1f, 1f, 1f, 0.8f);
    }

    private static void Petals(RectTransform parent, int count)
    {
        var layer = Stretch(parent, "Petals", UiArt.FrameInset * Px + 4f);
        layer.SetSiblingIndex(2);
        layer.gameObject.AddComponent<UiPetals>().Setup(PetalSprites, count, DecoPx);
    }

    // Título dorado con degradado y contorno oscuro
    private static void GoldTitle(TMP_Text t, float outline = 0.18f)
    {
        t.color = Color.white;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(
            new Color(1f, 0.93f, 0.70f), new Color(1f, 0.93f, 0.70f),
            new Color(0.90f, 0.62f, 0.32f), new Color(0.90f, 0.62f, 0.32f));
        t.fontStyle = FontStyles.Bold;
        // El contorno no se toca aquí: cambiarlo antes de que TMP se inicialice (objeto inactivo al
        // arrancar) lanzaba un error que dejaba sin estilo el HUD, la pausa y la derrota.
    }

    // Imagen decorativa del tamaño del sprite por la escala de pixel; queda detrás del texto.
    private static Image Deco(RectTransform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, float scale)
    {
        var rt = Child(parent, name);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sprite.rect.size * scale;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        // encima del fondo y el marco, debajo del contenido que ya existía
        int frameIndex = parent.Find("ThemeFrame") != null ? parent.Find("ThemeFrame").GetSiblingIndex() + 1 : 0;
        rt.SetSiblingIndex(Mathf.Min(frameIndex, parent.childCount - 1));
        return img;
    }

    private static RectTransform Child(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static RectTransform Stretch(Transform parent, string name, float inset)
    {
        var rt = Child(parent, name);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    // La imagen plana original deja de verse pero sigue bloqueando clics si lo hacía
    private static void ClearImage(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
    }

    private static RectTransform R(Transform parent, string path) => parent != null ? parent.Find(path) as RectTransform : null;

    private static TMP_Text Text(Transform t) => t != null ? t.GetComponent<TMP_Text>() : null;

    private static void Tint(Transform t, Color c)
    {
        var text = Text(t);
        if (text != null) text.color = c;
    }
}
