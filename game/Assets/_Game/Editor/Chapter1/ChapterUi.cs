using System.Collections.Generic;
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
            "WASD / flechas: mover   ·   Clic / J: atacar   ·   Espacio: esquivar   ·   E: interactuar   ·   Q: poción   ·   Tab: inventario   ·   Esc: pausa";

        public const string DialogueHint = "E / Clic: continuar   ·   X: saltar";

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
            ChapterUiScreens.BuildForge(canvas);
            ChapterUiScreens.BuildInventory(canvas);
            BuildDialogue(canvas);
            var pause = BuildPause(canvas);
            var defeat = BuildDefeat(canvas);
            var fader = BuildFader(canvas);

            // F3: contador de FPS (RNF ≥ 30 FPS)
            var fpsLabel = Label(canvas, "FpsLabel", "", 26, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(36, -160), new Vector2(200, 36));
            root.AddComponent<FpsCounter>().Setup(fpsLabel);

            // -smoketest: recorre las escenas del build, mide FPS y cierra
            var paths = new string[ChapterScenes.Order.Length];
            for (int i = 0; i < paths.Length; i++) paths[i] = ChapterScenes.PathOf(ChapterScenes.Order[i]);
            root.AddComponent<BuildSmokeTest>().Setup(paths);

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

        // Vida de Tsuki (arriba a la izquierda): marco dorado compacto con nombre, números y barra.
        private static void HealthBar(RectTransform hudRt, GameHud hud)
        {
            var frame = Box(hudRt, "HealthFrame", new Vector2(0, 1), new Vector2(28, -24), new Vector2(380, 76));
            HudFrame(frame);
            var name = Label(frame, "Name", "Tsuki", 24, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(22, -12), new Vector2(160, 30), ChapterUiScreens.GoldText);
            name.fontStyle = FontStyles.Bold;
            var hpText = Label(frame, "HealthText", "100/100", 20, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-22, -14), new Vector2(140, 28));
            var fill = Bar(frame, new Vector2(0, 16), new Vector2(336, 18), Crimson, new Color(0.22f, 0.06f, 0.10f, 1f));
            SetRef(hud, "healthFill", fill);
            SetRef(hud, "healthText", hpText);
        }

        // Barra del guardián (centro abajo, lejos de la vida y del objetivo): nombre, estado y vida en oro.
        private static void BossBar(RectTransform hudRt, GameHud hud)
        {
            var boss = Box(hudRt, "BossBar", new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(1000, 74));
            var bossGroup = boss.gameObject.AddComponent<CanvasGroup>();
            HudFrame(boss);
            var bossName = Label(boss, "Name", "", 24, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(26, -11), new Vector2(560, 30), ChapterUiScreens.GoldText);
            bossName.fontStyle = FontStyles.Bold;
            var bossState = Label(boss, "State", "", 20, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-26, -13), new Vector2(400, 28));
            var bossFill = Bar(boss, new Vector2(0, 14), new Vector2(952, 18), new Color(0.95f, 0.72f, 0.28f), new Color(0.20f, 0.10f, 0.06f, 1f));
            SetRef(hud, "bossGroup", bossGroup);
            SetRef(hud, "bossName", bossName);
            SetRef(hud, "bossFill", bossFill);
            SetRef(hud, "bossState", bossState);
        }

        // Marco del kit de UI a media escala: borde dorado fino para el HUD.
        private static void HudFrame(RectTransform rt)
        {
            var img = ChapterUiScreens.Kit(rt, "ui_frame");
            img.pixelsPerUnitMultiplier = 0.5f;
        }

        // Barra con contorno oscuro, fondo, relleno horizontal y un brillo arriba. Devuelve el relleno.
        private static Image Bar(RectTransform parent, Vector2 position, Vector2 size, Color fillColor, Color backColor)
        {
            var outline = Box(parent, "Bar", new Vector2(0.5f, 0), position, size + new Vector2(4, 4));
            Image(outline, new Color(0.04f, 0.03f, 0.07f, 1f));
            var back = Rect(outline, "Back", Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            Image(back, backColor);
            var fill = Image(Rect(back, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), fillColor);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var shine = Rect(back, "Shine", new Vector2(0, 0.55f), Vector2.one, Vector2.zero, Vector2.zero);
            Image(shine, new Color(1f, 1f, 1f, 0.14f));
            return fill;
        }

        private static void BuildHud(Transform canvas)
        {
            var hudRt = Stretch(canvas, "HUD");
            var group = hudRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var hud = hudRt.gameObject.AddComponent<GameHud>();
            SetRef(hud, "root", group);

            HealthBar(hudRt, hud);

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

            // Avisos al recoger cualquier item ("Alma antigua: 4"); el detalle está en el inventario (Tab)
            var trackedNames = new[] { "AlmaPequena", "AlmaAntigua", "CircuitoAntiguo", "CueroAntiguo", "FlorDestello",
                                       "ConcentradoSlime", "RestoSlime", "Pocion", "GotaAmrita", "CascoCuero", "PecheraCuero", "BotasCuero" };
            var tracked = System.Array.ConvertAll(trackedNames, ChapterPrefabs.LoadMaterial);
            var hudSo = new SerializedObject(hud);
            var matsProp = hudSo.FindProperty("trackedMaterials");
            var goalsProp = hudSo.FindProperty("materialGoals");
            matsProp.arraySize = goalsProp.arraySize = tracked.Length;
            var goals = new int[tracked.Length];
            for (int i = 0; i < tracked.Length; i++)
            {
                matsProp.GetArrayElementAtIndex(i).objectReferenceValue = tracked[i];
                goalsProp.GetArrayElementAtIndex(i).intValue = goals[i];
            }
            hudSo.FindProperty("materialTexts").arraySize = 0;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Pista del inventario bajo la vida
            Label(hudRt, "InventoryHint", "Tab: Inventario   ·   Esc: Pausa", 20, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(34, -106), new Vector2(460, 28), MutedText);

            BossBar(hudRt, hud);

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
            var hint = Label(panel, "ContinueHint", DialogueHint, 24, TextAlignmentOptions.Right, new Vector2(1, 0), new Vector2(-24, 14), new Vector2(520, 34), MutedText);

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

            var box = Box(panel, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 860));
            Image(box, PanelColor);
            Label(box, "Title", "Pausa", 64, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(600, 90));

            var col = Box(box, "Buttons", new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(520, 360));
            Column(col, 18);
            var resume = Button(col, "Resume", "Continuar", new Vector2(520, 74));
            var retry = Button(col, "Retry", "Volver al punto seguro", new Vector2(520, 74));
            var toMenu = Button(col, "Menu", "Menú principal", new Vector2(520, 74));
            var quit = Button(col, "Quit", "Salir del juego", new Vector2(520, 74));

            Label(box, "Controls", Controls.Replace("   ·   ", "\n"), 24, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(660, 290), MutedText);

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

            // La ilustración ya trae el título, los personajes y la luna
            var bg = Stretch(c, "Background");
            Image(bg, Color.white, LoadSprite("Assets/UI/MainMenu/Fondo_Promesa.png"));

            var col = Box(c, "Buttons", new Vector2(0, 0), new Vector2(200, 230), new Vector2(520, 330));
            Column(col, 18);
            var newGame = Button(col, "NewGame", "Nueva partida", new Vector2(520, 80));
            var cont = Button(col, "Continue", "Continuar", new Vector2(520, 80));
            var quit = Button(col, "Quit", "Salir", new Vector2(520, 80));
            var hint = Label(c, "ContinueHint", "", 26, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(740, 395), new Vector2(420, 40), MutedText);

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

            // Pétalos cayendo y botones con brillo
            var petals = new List<Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/UI/MainMenu/Petalos" }))
                petals.Add(AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)));
            var flower = LoadSprite("Assets/UI/MainMenu/Sakura_Flor.png");
            go.AddComponent<MainMenuStyle>().Setup(bg, petals.ToArray(),
                new[] { flower, LoadSprite("Assets/UI/MainMenu/Icono_Continuar.png"), LoadSprite("Assets/UI/MainMenu/Icono_Salir.png") },
                new[] { newGame, cont, quit }, flower, LoadSprite("Assets/UI/MainMenu/Icono_Nube.png"));
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
