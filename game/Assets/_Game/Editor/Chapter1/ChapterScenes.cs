using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using V = UnityEngine.Vector2Int;

namespace Game.EditorTools.Chapter1
{
    // Genera las escenas del capítulo 1 en Scenes/Chapter1. Volver a ejecutarlo SOBRESCRIBE
    // las escenas: los cambios de layout se hacen aquí (o se deja de usar el builder para esa escena).
    public static class ChapterScenes
    {
        public const string Folder = "Assets/_Game/Scenes/Chapter1";
        private const string IslandSource = Folder + "/_Source/FlowerIsland_Grid.prefab";

        public const string MainMenu = "MainMenu";
        public const string Intro = "00_Intro";
        public const string FlowerIsland = "01_FlowerIsland";
        public const string OpenField = "02_OpenField";
        public const string Camp = "03_Camp";
        public const string Sanctuary = "04_Sanctuary";
        public const string CombatZone = "05_CombatZone";
        public const string GuardianArena = "06_GuardianArena";
        public const string DemoEnd = "07_DemoEnd";

        public static readonly string[] Order = { MainMenu, Intro, FlowerIsland, OpenField, Camp, Sanctuary, CombatZone, GuardianArena, DemoEnd };

        public static string PathOf(string scene) => $"{Folder}/{scene}.unity";

        // Colores de fondo (cielo) por escena
        private static readonly Color SkyDay = new Color(0.192f, 0.302f, 0.475f);
        private static readonly Color SkyDusk = new Color(0.16f, 0.17f, 0.32f);
        private static readonly Color Interior = new Color(0.04f, 0.04f, 0.07f);

        [MenuItem("Tools/Claude/Chapter 1/4 - Build Scenes")]
        public static void BuildAll()
        {
            // Solo pregunta por escenas guardadas; una escena nueva sin título se descarta
            var active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path) && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ChapterTiles.ClearCache();

            BuildMainMenuScene();
            BuildNarrativeScene(Intro, "intro", DialogueSequence.Then.StartChapter, false);

            BuildZoneScene(FlowerIsland, FlowerIslandZone, "Spawn_Wake", SkyDay, true, "Isla de las Flores",
                new Exit("Door_Cabin", null, "Spawn_CabinInside", "Cabaña"),
                new Exit("Exit_CabinInside", null, "Spawn_CabinDoor", "Salir"),
                new Exit("Exit_Bridge", OpenField, "Spawn_FromIsland", "Cruzar el puente",
                    SwordPickup.Flag, "Antes de irte, revisa la cabaña."));
            BuildZoneScene(OpenField, OpenFieldZone, "Spawn_FromIsland", SkyDay, true, "Llanura del Eco",
                new Exit("Exit_ToCamp", Camp, "Spawn_FromField", "Campamento",
                    ResearcherFallEvent.Flag, "El bosque es muy espeso. Mejor explora la llanura primero."),
                new Exit("Exit_ToSanctuary", Sanctuary, "Spawn_FromField", "Santuario",
                    ChapterObjectives.CampBriefing, "Kaede dijo que primero fueras a su campamento (oeste)."),
                new Exit("Exit_ToCombat", CombatZone, "Spawn_FromField", "Bosque Quebrado",
                    ChapterObjectives.CampBriefing, "Kaede dijo que primero fueras a su campamento (oeste)."));
            BuildZoneScene(Camp, CampZone, "Spawn_FromField", SkyDay, true, "Campamento oculto",
                new Exit("Exit_ToField", OpenField, "Spawn_FromCamp", "Llanura del Eco"));
            BuildZoneScene(Sanctuary, SanctuaryZone, "Spawn_FromField", Interior, false, "Santuario",
                new Exit("Exit_ToField", OpenField, "Spawn_FromSanctuary", "Llanura del Eco"));
            BuildZoneScene(CombatZone, CombatZoneDef, "Spawn_FromField", SkyDusk, true, "Bosque Quebrado",
                new Exit("Exit_ToField", OpenField, "Spawn_FromCombat", "Llanura del Eco"),
                new Exit("Exit_ToArena", GuardianArena, "Spawn_FromZone2", "Umbral del Guardián",
                    ChapterObjectives.SealOpened, "Un sello carmesí bloquea el paso."));
            BuildZoneScene(GuardianArena, ArenaZone, "Spawn_FromZone2", SkyDusk, true, "Umbral del Guardián",
                new Exit("Exit_Victory", DemoEnd, null, "Continuar", ChapterObjectives.PermitObtained, null));

            BuildNarrativeScene(DemoEnd, "demo_end", DialogueSequence.Then.ShowObject, true);

            AssetDatabase.SaveAssets();
            Debug.Log("[Chapter1] Escenas generadas en " + Folder);
        }

        // ------------------------------------------------------------ zonas

        private static ZoneBuilder FlowerIslandZone()
        {
            var z = new ZoneBuilder(FlowerIsland, 101);
            z.Blob(new V(10, 6), 9, 8);
            z.Patch(new V(11, 7), 4.5f, Ground.Flower, 0.9f);
            z.Patch(new V(6, 11), 3f, Ground.Flower, 0.9f);
            z.Patch(new V(15, 2), 3f, Ground.Flower, 0.85f);
            z.Patch(new V(8, 3), 2.5f, Ground.Flower, 0.9f);

            // Isla de Omar (chore/isometric-sorting-setting) como núcleo del escenario
            var island = AssetDatabase.LoadAssetAtPath<GameObject>(IslandSource);
            if (island != null) z.Stamp(island, V.zero);
            else Debug.LogWarning("[Chapter1] No se encontró " + IslandSource);

            z.Marker("Spawn_Wake", new V(9, 5), 2);
            z.Prop("placeholder_cabin", new V(13, 11), true, "Cabin", Square(2));
            z.Marker("Door_Cabin", new V(12, 11));
            z.Marker("Spawn_CabinDoor", new V(10, 9));
            z.Path(Ground.Sand, 1, new V(9, 5), new V(11, 10));
            z.Path(Ground.Sand, 1, new V(9, 5), new V(17, 3));
            z.Bridge(new V(18, 3), new V(1, 0), 9);
            z.Marker("Bridge_Start", new V(19, 3), 0);
            z.Marker("Exit_Bridge", new V(25, 3), 0);

            z.TreeRing(0.45f, "placeholder_tree_tall", "placeholder_tree_short");
            z.Scatter(5, false, "placeholder_bush");
            z.Scatter(3, true, "placeholder_rock_small", "placeholder_rock_large");

            // Interior de la cabaña (área aparte en la misma escena)
            var min = new V(58, -2); var max = new V(63, 2);
            z.Rect(min, max, Ground.Wood);
            z.Walls(min, max, false);
            z.Prop("placeholder_sword_rack", new V(62, 1), true, "SwordRack");
            z.Prop("placeholder_bed", new V(60, 2), true, "Bed");
            z.Prop("placeholder_table", new V(62, -1), true, "Table");
            z.Prop("placeholder_barrel", new V(63, -2), true, "Barrel");
            z.Marker("Spawn_CabinInside", new V(60, 0), 0);
            z.Marker("Exit_CabinInside", new V(58, -1), 0);
            return z;
        }

        private static ZoneBuilder OpenFieldZone()
        {
            var z = new ZoneBuilder(OpenField, 202);
            z.Blob(V.zero, 15, 13);
            z.Blob(new V(-9, 10), 6, 5);
            z.Blob(new V(11, 9), 5, 5);
            z.Blob(new V(12, -9), 5, 4);
            z.Blob(new V(-13, -5), 4, 3);
            z.Patch(new V(5, 6), 3f, Ground.Flower, 0.8f);
            z.Patch(new V(-3, -8), 3f, Ground.Flower, 0.8f);
            z.Patch(new V(8, -3), 2.5f, Ground.Flower, 0.8f);

            z.Bridge(new V(-24, -6), new V(1, 0), 8);
            z.Marker("Spawn_FromIsland", new V(-15, -5), 2);
            z.Path(Ground.Sand, 1, new V(-16, -5), new V(-5, -2), V.zero);
            z.Path(Ground.Sand, 1, V.zero, new V(-9, 10), new V(-12, 14));
            z.Path(Ground.Sand, 1, V.zero, new V(11, 9), new V(14, 13));
            z.Path(Ground.Sand, 1, V.zero, new V(12, -9), new V(16, -12));

            z.Prop("placeholder_tree_tall", new V(-4, 1), true, "Tree_Researcher");
            z.Marker("Researcher_Land", new V(-5, -1), 1);

            z.Marker("Exit_ToCamp", new V(-12, 14), 1);
            z.Marker("Spawn_FromCamp", new V(-10, 12), 1);
            z.Marker("Exit_ToSanctuary", new V(14, 13), 1);
            z.Marker("Spawn_FromSanctuary", new V(12, 11), 1);
            z.Marker("Exit_ToCombat", new V(16, -12), 1);
            z.Marker("Spawn_FromCombat", new V(14, -10), 1);
            z.Rect(new V(13, 12), new V(15, 14), Ground.Stone);
            z.Prop("placeholder_pillar", new V(13, 14), true, "Pillar_L");
            z.Prop("placeholder_pillar", new V(15, 12), true, "Pillar_R");

            // Lejos de los 4 puntos de aparición (verificado en unidades del mundo)
            z.Marker("Enemy_Melee_A", new V(2, -8));
            z.Marker("Enemy_Melee_B", new V(-4, -8));
            z.Marker("Pickup_Flor_A", new V(5, 7));
            z.Marker("Pickup_Flor_B", new V(-3, -8));
            // Zona 1: 2 flores + 2 cueros (de los melee) + 1 poción. No alcanza para la receta (3/3):
            // hay que visitar la zona 2 y volver a la base (HEX-02).
            z.Marker("Pickup_Pocion_A", new V(9, -3));

            z.RaiseRect(new V(2, 10), new V(4, 11), ChapterTiles.Grass(34));
            z.RaiseRect(new V(-2, -12), new V(0, -11), ChapterTiles.Grass(19));
            z.TreeRing(0.5f, "placeholder_tree_tall", "placeholder_tree_short");
            z.Scatter(10, true, "placeholder_tree_tall", "placeholder_tree_short");
            z.Scatter(5, true, "placeholder_rock_small", "placeholder_rock_large");
            z.Scatter(8, false, "placeholder_bush");
            return z;
        }

        private static ZoneBuilder CampZone()
        {
            var z = new ZoneBuilder(Camp, 303);
            z.Blob(V.zero, 8, 7, Ground.Grass, 0.12f);
            z.Path(Ground.Sand, 1, V.zero, new V(7, -5), new V(10, -8));
            z.Marker("Spawn_FromField", new V(7, -5), 1);
            z.Marker("Exit_ToField", new V(10, -8), 1);

            z.Prop("placeholder_campfire", V.zero, true, "Campfire");
            z.Prop("placeholder_tent", new V(-4, 3), true, "Tent_A", new[] { V.zero, new V(1, 0) });
            z.Prop("placeholder_tent", new V(1, 5), true, "Tent_B", new[] { V.zero, new V(1, 0) });
            z.Prop("placeholder_forge", new V(-5, -2), true, "Forge", Square(2));
            z.Marker("Forge_Front", new V(-3, -3), 1);
            z.Prop("placeholder_desk", new V(4, 3), true, "Desk", new[] { V.zero, new V(1, 0) });
            z.Marker("NPC_Researcher", new V(3, 2), 1);
            z.Prop("placeholder_save_crystal", new V(-1, -4), true, "SaveCrystal");
            z.Marker("SavePoint_Base", new V(0, -3), 0);
            z.Prop("placeholder_crate", new V(-6, 1), true);
            z.Prop("placeholder_crate", new V(-6, 2), true);
            z.Prop("placeholder_barrel", new V(3, 6), true);

            z.TreeRing(0.85f, "placeholder_tree_tall", "placeholder_tree_short");
            z.Scatter(4, false, "placeholder_bush");
            return z;
        }

        private static ZoneBuilder SanctuaryZone()
        {
            var z = new ZoneBuilder(Sanctuary, 404);
            var min = V.zero; var max = new V(8, 10);
            z.Rect(min, max, Ground.Stone);
            z.Rect(new V(3, 4), new V(5, 6), Ground.Rune);
            z.Walls(min, max, true);
            z.Rect(new V(-2, 1), new V(-1, 2), Ground.Stone);

            z.Prop("placeholder_maiden_statue", new V(4, 9), true, "Maiden_Statue");
            z.Marker("Maiden_Front", new V(4, 7), 0);
            z.Prop("placeholder_pillar", new V(1, 8), true, "Pillar_1");
            z.Prop("placeholder_pillar", new V(7, 8), true, "Pillar_2");
            z.Prop("placeholder_pillar", new V(1, 3), true, "Pillar_3");
            z.Prop("placeholder_pillar", new V(7, 3), true, "Pillar_4");
            z.Prop("placeholder_crystal", new V(2, 10), true, "Crystal_L");
            z.Prop("placeholder_crystal", new V(6, 10), true, "Crystal_R");
            z.Marker("Spawn_FromField", new V(1, 1), 0);
            z.Marker("Exit_ToField", new V(-2, 1), 0);
            return z;
        }

        private static ZoneBuilder CombatZoneDef()
        {
            var z = new ZoneBuilder(CombatZone, 505) { GroundTint = new Color(0.82f, 0.9f, 0.86f) };
            z.Blob(V.zero, 16, 12, Ground.Ruin);
            z.Blob(new V(-14, -4), 4, 3, Ground.Ruin);
            z.Blob(new V(11, 10), 5, 4, Ground.Ruin);

            z.Marker("Spawn_FromField", new V(-15, -4), 1);
            z.Marker("Exit_ToField", new V(-18, -4), 1);
            z.Path(Ground.Sand, 1, new V(-18, -4), V.zero, new V(11, 10));
            // Pasillo único hacia la arena: el sello lo bloquea
            z.Path(Ground.Stone, 3, new V(12, 11), new V(18, 17));
            z.Blob(new V(20, 19), 2.5f, 2.5f, Ground.Stone, 0.05f);
            z.Marker("SavePoint_Boss", new V(10, 9), 1);
            z.Prop("placeholder_save_crystal", new V(9, 10), true, "SaveCrystal");
            z.Marker("Seal_Gate", new V(16, 16), 0);
            z.Marker("Seal_Front", new V(14, 14), 0);
            z.Marker("Exit_ToArena", new V(20, 19), 0);

            // Fuera del rango de detección desde la entrada (en isométrico la distancia
            // en celdas engaña: se verificó en unidades del mundo, ≥ 2.8 desde Spawn_FromField)
            z.Marker("Enemy_Melee_A", new V(4, 3));
            z.Marker("Enemy_Melee_B", new V(6, -5));
            z.Marker("Enemy_Ranged_A", new V(11, 4));
            z.Marker("Enemy_Ranged_B", new V(9, 0));
            z.Marker("Pickup_Cuero_A", new V(-6, -6));
            z.Marker("Pickup_Flor_A", new V(10, -6));
            z.Marker("Pickup_Pocion_A", new V(8, 7));

            var stone = ChapterTiles.Stone;
            z.RaiseRect(new V(-2, -8), new V(-1, -7), stone);
            z.RaiseRect(new V(5, 2), new V(5, 4), stone);
            z.RaiseRect(new V(-9, 6), new V(-8, 6), stone);
            z.TreeRing(0.6f, "placeholder_tree_dark", "placeholder_tree_dead");
            z.Scatter(6, true, "placeholder_tree_dead", "placeholder_tree_dark");
            z.Scatter(6, true, "placeholder_rock_small", "placeholder_rock_large");
            z.Scatter(3, true, "placeholder_crystal");
            return z;
        }

        private static ZoneBuilder ArenaZone()
        {
            var z = new ZoneBuilder(GuardianArena, 606);
            z.Blob(V.zero, 9, 9, Ground.Stone, 0.04f);
            for (int y = -9; y <= 9; y++)
                for (int x = -9; x <= 9; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d >= 6.4f && d <= 7.1f) z.Set(new V(x, y), Ground.Rune);
                }
            z.Path(Ground.Stone, 2, new V(-12, -12), new V(-6, -6));
            z.Marker("Spawn_FromZone2", new V(-8, -8), 1);
            z.Marker("Guardian", new V(2, 2), 2);
            z.Marker("Arena_Center", V.zero, 0);
            z.Marker("Arena_Intro", new V(-5, -5), 0);
            z.Marker("Exit_Victory", new V(-2, -2), 0);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + Mathf.PI / 8f;
                z.Prop("placeholder_brazier", new V(Mathf.RoundToInt(Mathf.Cos(a) * 8.2f), Mathf.RoundToInt(Mathf.Sin(a) * 8.2f)), true, $"Brazier_{i}");
            }
            return z;
        }

        private static V[] Square(int n)
        {
            var list = new List<V>();
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    list.Add(new V(x, y));
            return list.ToArray();
        }

        // ------------------------------------------------------------ ensamblado de escenas

        // Salida: marcador de la zona -> escena destino (null = misma escena) y spawn de llegada.
        private struct Exit
        {
            public string marker, scene, spawn, label, flag, blocked;

            public Exit(string marker, string scene, string spawn, string label, string flag = null, string blocked = null)
            {
                this.marker = marker; this.scene = scene; this.spawn = spawn;
                this.label = label; this.flag = flag; this.blocked = blocked;
            }
        }

        // La zona se define DESPUÉS de crear la escena: NewScene descarga los assets sin usar
        // y dejaría inválidos los tiles leídos antes (p. ej. los de la isla de Omar).
        private static void BuildZoneScene(string name, System.Func<ZoneBuilder> define, string defaultSpawn, Color sky, bool outdoor, string zoneName, params Exit[] exits)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var zone = define();
            var gridGo = zone.Build();
            var grid = gridGo.GetComponent<Grid>();

            if (outdoor) AddClouds(zone, grid);

            var context = new GameObject("SceneContext").AddComponent<SceneContext>();
            context.Configure(zoneName, defaultSpawn, true, true, true);

            var markers = gridGo.transform.Find("Markers");
            foreach (Transform m in markers)
                if (m.name.StartsWith("Spawn_")) m.gameObject.AddComponent<SpawnPoint>().SetId(m.name);
            foreach (var exit in exits)
                AddExit(markers.Find(exit.marker), exit);

            ChapterStory.Populate(name, gridGo);

            var spawn = markers.Find(defaultSpawn);
            var playerPrefab = ChapterPrefabs.LoadPlayer();
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = spawn != null ? spawn.position : Vector3.zero;

            var cam = CreateCamera(sky);
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.FindProperty("smoothSpeed").floatValue = 6f;
            so.FindProperty("offset").vector3Value = new Vector3(0f, 0.25f, -10f);
            so.ApplyModifiedPropertiesWithoutUndo();
            cam.transform.position = player.transform.position + new Vector3(0f, 0.25f, -10f);

            var global = CreateGlobalLight(1f);
            ChapterLighting.ApplyToScene(name, gridGo, global);
            context.SetGlobalLight(global);
            SaveScene(scene, name);
        }

        private static void AddExit(Transform marker, Exit exit)
        {
            if (marker == null) { Debug.LogError("[Chapter1] Falta el marcador " + exit.marker); return; }
            var col = marker.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.36f, 0.2f);
            var transition = marker.gameObject.AddComponent<SceneTransition>();
            transition.Configure(exit.scene != null ? PathOf(exit.scene) : "", exit.spawn, exit.label, exit.flag, exit.blocked);

            // Marca en el suelo (legible sin sonido) y etiqueta del destino
            var glow = new GameObject("ExitGlow");
            glow.transform.SetParent(marker, false);
            glow.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            var sr = glow.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load("placeholder_exit_marker");
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 5;

            var label = new GameObject("ExitLabel");
            label.transform.SetParent(marker, false);
            label.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            var tmp = label.AddComponent<TMPro.TextMeshPro>();
            tmp.text = exit.label;
            tmp.fontSize = 0.9f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.96f, 0.85f);
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
            tmp.sortingLayerID = SortingLayer.NameToID("UI");
        }

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color(0.04f, 0.05f, 0.12f));
            var global = CreateGlobalLight(1f);
            var context = new GameObject("SceneContext").AddComponent<SceneContext>();
            context.Configure("", "", false, false, false);
            context.SetGlobalLight(global);
            ChapterUi.BuildMainMenu(context.transform);
            SaveScene(scene, MainMenu);
        }

        // Intro y cierre: fondo de cielo + diálogo. El cierre muestra una tarjeta final con "volver al menú".
        private static void BuildNarrativeScene(string name, string dialogueId, DialogueSequence.Then then, bool endCard)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color(0.04f, 0.05f, 0.12f));
            var global = CreateGlobalLight(1f);
            var context = new GameObject("SceneContext").AddComponent<SceneContext>();
            context.Configure("", "", false, false, false);
            context.SetGlobalLight(global);
            var card = ChapterUi.BuildNarrativeScreen(context.transform, endCard);
            var sequence = context.gameObject.AddComponent<DialogueSequence>();
            sequence.Setup(ChapterDialogues.Get(dialogueId), then, card, name == Intro ? "intro_seen" : "demo_complete");
            SaveScene(scene, name);
        }

        private static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1.35f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>();
            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PlaceholderArtGenerator.PixelsPerUnit;
            ppc.refResolutionX = 480;
            ppc.refResolutionY = 270;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            go.transform.position = new Vector3(0, 0, -10);
            return cam;
        }

        private static Light2D CreateGlobalLight(float intensity)
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = intensity;
            // Se guarda apagada; ver GlobalLightEditorPreview y SceneContext
            go.AddComponent<GlobalLightEditorPreview>();
            return light;
        }

        // Nubes decorativas bajo la isla para reforzar que flota en el cielo.
        private static void AddClouds(ZoneBuilder zone, Grid grid)
        {
            var root = new GameObject("Clouds").transform;
            var rng = new System.Random(zone.Name.GetHashCode());
            var sprite = PlaceholderArtGenerator.Load("placeholder_cloud");
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject("Cloud_" + i);
                go.transform.SetParent(root, false);
                float x = (float)(rng.NextDouble() * 16 - 8), y = (float)(rng.NextDouble() * 10 - 6);
                go.transform.position = new Vector3(x, y, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -100;
                sr.color = new Color(1f, 1f, 1f, 0.55f);
            }
        }

        private static void SaveScene(Scene scene, string name)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Game/Scenes", "Chapter1");
            EditorSceneManager.SaveScene(scene, PathOf(name));
        }
    }
}
