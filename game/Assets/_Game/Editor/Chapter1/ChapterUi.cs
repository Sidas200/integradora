using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Game.EditorTools.Chapter1.UiFactory;

namespace Game.EditorTools.Chapter1
{
    // Construye la UI del capítulo: el prefab persistente GameSystems (HUD, pausa, derrota, fade)
    // y la UI propia de las escenas de menú, intro y cierre.
    public static class ChapterUi
    {
        public const string ResourcesFolder = "Assets/_Game/Resources/Chapter1";
        public const string SystemsPath = ResourcesFolder + "/GameSystems.prefab";

        public const string Controls =
            "WASD / flechas: mover   ·   Clic: atacar   ·   Espacio: esquivar   ·   E: interactuar   ·   Q: poción   ·   Esc: pausa";

        public static void BuildSystemsPrefab()
        {
            EnsureFolder();
            var root = new GameObject("GameSystems");
            root.AddComponent<GameSystems>();
            root.AddComponent<SessionState>();
            var loader = root.AddComponent<SceneLoader>();
            var flow = root.AddComponent<GameFlow>();

            var es = new GameObject("EventSystem");
            es.transform.SetParent(root.transform, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("UI", typeof(RectTransform));
            canvasGo.transform.SetParent(root.transform, false);
            Canvas(canvasGo, 50);
            var canvas = canvasGo.transform;

            BuildHud(canvas);
            BuildDialogue(canvas);
            var pause = BuildPause(canvas);
            var defeat = BuildDefeat(canvas);
            var fader = BuildFader(canvas);

            SetRef(loader, "fader", fader);
            SetRef(flow, "defeatScreen", defeat);
            SetString(flow, "mainMenuScene", ChapterScenes.PathOf(ChapterScenes.MainMenu));
            SetString(flow, "introScene", ChapterScenes.PathOf(ChapterScenes.Intro));
            SetString(flow, "firstZoneScene", ChapterScenes.PathOf(ChapterScenes.FlowerIsland));
            SetString(flow, "firstZoneSpawn", "Spawn_Wake");
            SetString(flow, "demoEndScene", ChapterScenes.PathOf(ChapterScenes.DemoEnd));

            PrefabUtility.SaveAsPrefabAsset(root, SystemsPath);
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildHud(Transform canvas)
        {
            var hudRt = Stretch(canvas, "HUD");
            var group = hudRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var hud = hudRt.gameObject.AddComponent<GameHud>();
            SetRef(hud, "root", group);

            // Vida (arriba a la izquierda)
            var frame = Box(hudRt, "HealthFrame", new Vector2(0, 1), new Vector2(32, -28), new Vector2(460, 92));
            Image(frame, PanelColor);
            Label(frame, "Name", "Tsuki", 30, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(18, -8), new Vector2(200, 40));
            var barBg = Box(frame, "BarBg", new Vector2(0, 0), new Vector2(18, 14), new Vector2(330, 26));
            Image(barBg, new Color(0.18f, 0.08f, 0.12f, 1f));
            var fill = Stretch(barBg, "Fill");
            var fillImg = Image(fill, Crimson);
            fillImg.type = UnityEngine.UI.Image.Type.Filled;
            fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var hpText = Label(frame, "HealthText", "100/100", 26, TextAlignmentOptions.Right, new Vector2(1, 0), new Vector2(-16, 10), new Vector2(110, 34));
            SetRef(hud, "healthFill", fillImg);
            SetRef(hud, "healthText", hpText);

            // Objetivo actual (arriba a la derecha)
            var objective = Box(hudRt, "Objective", new Vector2(1, 1), new Vector2(-32, -28), new Vector2(560, 150));
            Image(objective, PanelColor);
            Label(objective, "Header", "OBJETIVO", 22, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(18, -10), new Vector2(300, 30), new Color(1f, 0.55f, 0.62f));
            var objectiveText = Label(objective, "Text", "", 26, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(18, -42), new Vector2(524, 98));
            objectiveText.enableAutoSizing = true;
            objectiveText.fontSizeMin = 18;
            objectiveText.fontSizeMax = 26;
            SetRef(hud, "objectiveText", objectiveText);

            // Aviso de interacción "E: Hablar" (centro abajo)
            var prompt = Box(hudRt, "Prompt", new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(560, 64));
            var promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
            Image(prompt, new Color(0.10f, 0.08f, 0.16f, 0.95f));
            var promptText = Text(Stretch(prompt, "Text"), "", 30, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.7f));
            SetRef(hud, "promptGroup", promptGroup);
            SetRef(hud, "promptText", promptText);

            // Título de zona (centro arriba)
            var zone = Box(hudRt, "ZoneTitle", new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1000, 90));
            var zoneGroup = zone.gameObject.AddComponent<CanvasGroup>();
            Image(zone, PanelColor);
            var zoneText = Text(Stretch(zone, "Text"), "", 56, TextAlignmentOptions.Center);
            zoneText.fontStyle = FontStyles.Bold;
            SetRef(hud, "zoneTitleGroup", zoneGroup);
            SetRef(hud, "zoneTitleText", zoneText);

            // Avisos (centro abajo)
            var toast = Box(hudRt, "Toast", new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(1100, 80));
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            Image(toast, PanelColor);
            var toastText = Text(Stretch(toast, "Text"), "", 32, TextAlignmentOptions.Center);
            SetRef(hud, "toastGroup", toastGroup);
            SetRef(hud, "toastText", toastText);
        }

        // ---------------------------------------------------------------- diálogo

        private static void BuildDialogue(Transform canvas)
        {
            var rootRt = Stretch(canvas, "Dialogue");
            var ui = rootRt.gameObject.AddComponent<DialogueUI>();
            var panel = Box(rootRt, "Panel", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1560, 280));
            Image(panel, new Color(0.05f, 0.04f, 0.09f, 1f)).raycastTarget = true;

            var frame = Box(panel, "PortraitFrame", new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(220, 220));
            Image(frame, new Color(0.16f, 0.12f, 0.22f, 1f));
            var portrait = Box(frame, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(192, 192));
            var portraitImg = Image(portrait, Color.white, null);
            portraitImg.preserveAspect = true;

            var nameBox = Box(panel, "NameBox", new Vector2(0, 1), new Vector2(280, 30), new Vector2(360, 56));
            Image(nameBox, Crimson);
            var name = Text(Stretch(nameBox, "Name"), "", 32, TextAlignmentOptions.Center);
            name.fontStyle = FontStyles.Bold;

            var body = Label(panel, "Body", "", 36, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(280, -44), new Vector2(1240, 190));
            var hint = Label(panel, "ContinueHint", "E / Clic: continuar", 24, TextAlignmentOptions.Right, new Vector2(1, 0), new Vector2(-24, 14), new Vector2(300, 34), MutedText);

            SetRef(ui, "panel", panel.gameObject);
            SetRef(ui, "portrait", portraitImg);
            SetRef(ui, "portraitFrame", frame.gameObject);
            SetRef(ui, "nameBox", nameBox.gameObject);
            SetRef(ui, "speakerText", name);
            SetRef(ui, "bodyText", body);
            SetRef(ui, "continueHint", hint.gameObject);
        }

        // ---------------------------------------------------------------- pausa y derrota

        private static PauseMenu BuildPause(Transform canvas)
        {
            var rootRt = Stretch(canvas, "Pause");
            var menu = rootRt.gameObject.AddComponent<PauseMenu>();
            var panel = Stretch(rootRt, "Panel");
            Image(panel, DimColor).raycastTarget = true;

            var box = Box(panel, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 700));
            Image(box, PanelColor);
            Label(box, "Title", "Pausa", 64, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(600, 90));

            var col = Box(box, "Buttons", new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(520, 360));
            Column(col, 18);
            var resume = Button(col, "Resume", "Continuar", new Vector2(520, 74));
            var retry = Button(col, "Retry", "Volver al punto seguro", new Vector2(520, 74));
            var toMenu = Button(col, "Menu", "Menú principal", new Vector2(520, 74));
            var quit = Button(col, "Quit", "Salir del juego", new Vector2(520, 74));

            Label(box, "Controls", Controls.Replace("   ·   ", "\n"), 24, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(660, 170), MutedText);

            SetRef(menu, "panel", panel.gameObject);
            SetRef(menu, "resumeButton", resume);
            SetRef(menu, "retryButton", retry);
            SetRef(menu, "menuButton", toMenu);
            SetRef(menu, "quitButton", quit);
            return menu;
        }

        private static DefeatScreen BuildDefeat(Transform canvas)
        {
            var rootRt = Stretch(canvas, "Defeat");
            var screen = rootRt.gameObject.AddComponent<DefeatScreen>();
            var panel = Stretch(rootRt, "Panel");
            Image(panel, new Color(0.10f, 0.01f, 0.04f, 0.86f)).raycastTarget = true;

            Label(panel, "Title", "Tsuki ha caído", 80, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1200, 110), new Color(1f, 0.55f, 0.6f));
            Label(panel, "Subtitle", "La promesa sigue en pie. Vuelve a intentarlo desde el último punto seguro.", 32, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1200, 80), MutedText);

            var col = Box(panel, "Buttons", new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(520, 180));
            Column(col, 18);
            var retry = Button(col, "Retry", "Reintentar", new Vector2(520, 74));
            var toMenu = Button(col, "Menu", "Menú principal", new Vector2(520, 74));

            SetRef(screen, "panel", panel.gameObject);
            SetRef(screen, "retryButton", retry);
            SetRef(screen, "menuButton", toMenu);
            return screen;
        }

        private static ScreenFader BuildFader(Transform canvas)
        {
            var rt = Stretch(canvas, "Fader");
            var img = Image(rt, Color.black);
            img.raycastTarget = true;
            rt.gameObject.AddComponent<CanvasGroup>();
            return rt.gameObject.AddComponent<ScreenFader>();
        }

        // ---------------------------------------------------------------- escenas de UI

        public static void BuildMainMenu(Transform sceneRoot)
        {
            var go = new GameObject("MainMenuCanvas", typeof(RectTransform));
            go.transform.SetParent(sceneRoot, false);
            Canvas(go, 10);
            var c = go.transform;

            var bg = Stretch(c, "Background");
            Image(bg, Color.white, LoadSprite("Assets/UI/MainMenu/Generado/Fondo_Gradiente.png"));
            var moon = Box(c, "Moon", new Vector2(1, 1), new Vector2(-160, -60), new Vector2(380, 380));
            Image(moon, Color.white, LoadSprite("Assets/UI/MainMenu/Generado/Luna.png")).preserveAspect = true;
            var character = Box(c, "Character", new Vector2(1, 0), new Vector2(-120, 0), new Vector2(760, 950));
            Image(character, Color.white, LoadSprite("Assets/UI/MainMenu/01_Personaje.png")).preserveAspect = true;
            var title = Box(c, "Title", new Vector2(0, 1), new Vector2(80, -50), new Vector2(900, 540));
            Image(title, Color.white, LoadSprite("Assets/UI/MainMenu/02_Titulo.png")).preserveAspect = true;

            var col = Box(c, "Buttons", new Vector2(0, 0), new Vector2(180, 150), new Vector2(520, 330));
            Column(col, 18);
            var newGame = Button(col, "NewGame", "Nueva partida", new Vector2(520, 80));
            var cont = Button(col, "Continue", "Continuar", new Vector2(520, 80));
            var quit = Button(col, "Quit", "Salir", new Vector2(520, 80));
            var hint = Label(c, "ContinueHint", "", 26, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(720, 315), new Vector2(420, 40), MutedText);

            Label(c, "Footer", "Demo · Capítulo 1   —   Se juega sin conexión", 24, TextAlignmentOptions.Left,
                new Vector2(0, 0), new Vector2(40, 30), new Vector2(900, 40), MutedText);

            // Confirmación al empezar de cero con una partida guardada
            var confirm = Stretch(c, "ConfirmNewGame");
            Image(confirm, DimColor).raycastTarget = true;
            var box = Box(confirm, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 380));
            Image(box, PanelColor);
            Label(box, "Text", "Ya hay una partida guardada.\n¿Empezar una nueva? Se perderá el progreso guardado.", 34,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(780, 160));
            var yes = Button(box, "Yes", "Sí, empezar", new Vector2(320, 74));
            ((RectTransform)yes.transform).anchoredPosition = new Vector2(-180, -100);
            var no = Button(box, "No", "Cancelar", new Vector2(320, 74));
            ((RectTransform)no.transform).anchoredPosition = new Vector2(180, -100);

            var controller = go.AddComponent<MainMenuController>();
            SetRef(controller, "newGameButton", newGame);
            SetRef(controller, "continueButton", cont);
            SetRef(controller, "quitButton", quit);
            SetRef(controller, "continueHint", hint);
            SetRef(controller, "confirmPanel", confirm.gameObject);
            SetRef(controller, "confirmYes", yes);
            SetRef(controller, "confirmNo", no);
        }

        // Fondo de cielo para la intro y el cierre. Con endCard agrega la tarjeta final
        // (oculta hasta que termina el diálogo) que vuelve al menú con cualquier tecla.
        public static GameObject BuildNarrativeScreen(Transform sceneRoot, bool endCard)
        {
            var go = new GameObject("NarrativeCanvas", typeof(RectTransform));
            go.transform.SetParent(sceneRoot, false);
            Canvas(go, 10);
            var c = go.transform;
            Image(Stretch(c, "Background"), Color.white, PlaceholderArtGenerator.Load("placeholder_intro_sky"));
            Image(Stretch(c, "Dim"), new Color(0f, 0f, 0.03f, 0.35f));
            if (!endCard) return null;

            var card = Stretch(c, "EndCard");
            Image(card, new Color(0.02f, 0.02f, 0.06f, 0.85f));
            Label(card, "Title", "Fin de la demo", 80, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1400, 110));
            Label(card, "Body",
                "Capítulo 1 completado: obtuviste el permiso del primer guardián.\n\n" +
                "Las otras tres zonas, sus guardianes y el Gran Juzgado esperan en la versión completa.\n\n" +
                "Gracias por jugar The Day I Made That Promise.", 34, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1400, 320), MutedText);
            Label(card, "Hint", "Pulsa cualquier tecla para volver al menú", 28, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(1000, 44));
            var cont = card.gameObject.AddComponent<ClickToContinue>();
            SetEnum(cont, "onContinue", (int)ClickToContinue.Action.ReturnToMenu);
            card.gameObject.SetActive(false);
            return card.gameObject;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[Chapter1] No se encontró el sprite " + path);
            return sprite;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources"))
                AssetDatabase.CreateFolder("Assets/_Game", "Resources");
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Resources", "Chapter1");
        }
    }
}
