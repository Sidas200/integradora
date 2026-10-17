using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using static Game.EditorTools.Chapter1.ChapterDialogues;

namespace Game.EditorTools.Chapter1
{
    // Coloca en cada escena los elementos de historia: NPC, eventos, diálogos y objetos clave.
    public static class ChapterStory
    {
        public static void Populate(string scene, GameObject gridGo)
        {
            var markers = gridGo.transform.Find("Markers");
            var props = gridGo.transform.Find("Props");
            var root = new GameObject("Story").transform;

            switch (scene)
            {
                case ChapterScenes.FlowerIsland: FlowerIsland(root, gridGo, markers, props); break;
                case ChapterScenes.OpenField: OpenField(root, markers); Enemies(root, markers); break;
                case ChapterScenes.Camp: Camp(root, markers); break;
                case ChapterScenes.Sanctuary: Sanctuary(root, props); break;
                case ChapterScenes.CombatZone:
                    OnStart(root, "zone2_enter", "zone2_seen");
                    Enemies(root, markers);
                    Seal(root, markers);
                    break;
                case ChapterScenes.GuardianArena: Arena(root, markers); break;
            }
        }

        // ---------------------------------------------------------------- escenas

        private static void FlowerIsland(Transform root, GameObject gridGo, Transform markers, Transform props)
        {
            OnStart(root, "island_wake", "woke_up");

            var cabinInside = AreaTrigger(root, "CabinInsideTrigger", markers.Find("Spawn_CabinInside").position, 0.3f);
            cabinInside.Configure(StoryTrigger.Mode.OnPlayerEnter, default, Get("cabin_inside"), "cabin_seen");

            var rack = props.Find("SwordRack");
            var pickup = rack.gameObject.AddComponent<SwordPickup>();
            pickup.Configure("Examinar la espada", 0.45f, ChapterPrefabs.CreateMarker(rack, 0.45f));
            pickup.Setup(Get("sword_pickup"), rack.GetComponent<SpriteRenderer>(), PlaceholderArtGenerator.Load("placeholder_sword_rack_empty"));

            // Puente: la isla se desvanece al pisarlo con la espada
            var bridgeStart = markers.Find("Bridge_Start");
            var col = bridgeStart.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.4f);
            var grid = gridGo.GetComponent<Grid>();
            var blocker = new GameObject("BridgeBackBlocker");
            blocker.transform.SetParent(root, false);
            foreach (var cell in new[] { new Vector2Int(17, 3), new Vector2Int(17, 4), new Vector2Int(16, 3), new Vector2Int(16, 4) })
            {
                var c = blocker.AddComponent<CircleCollider2D>();
                c.offset = ZoneBuilder.SurfaceWorld(cell, grid);
                c.radius = 0.12f;
            }
            var vanish = bridgeStart.gameObject.AddComponent<IslandVanish>();
            vanish.Setup(gridGo.transform.Find("Ground").GetComponent<Tilemap>(), gridGo.transform.Find("Elevations").GetComponent<Tilemap>(),
                props, ChapterTiles.Bridge, Get("island_vanish"), blocker,
                new[] { markers.Find("Door_Cabin").gameObject, markers.Find("Exit_CabinInside").gameObject });
        }

        private static void OpenField(Transform root, Transform markers)
        {
            var land = markers.Find("Researcher_Land");
            var npc = ChapterPrefabs.CreateNpc("Kaede_Falling", "placeholder_npc_researcher", land.position, root, out _);
            var fallGo = new GameObject("ResearcherFallEvent");
            fallGo.transform.SetParent(root, false);
            fallGo.transform.position = land.position;
            var trigger = fallGo.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            fallGo.AddComponent<ResearcherFallEvent>().Setup(npc, land, Get("researcher_fall"));
        }

        private static void Camp(Transform root, Transform markers)
        {
            var npc = ChapterPrefabs.CreateNpc("Kaede", "placeholder_npc_researcher", markers.Find("NPC_Researcher").position, root, out var marker);
            var talk = npc.AddComponent<NpcDialogue>();
            talk.Configure("Hablar con Kaede", 0.5f, marker);
            talk.SetEntries(new List<NpcDialogue.Entry>
            {
                Entry(new FlagCondition(null, ChapterObjectives.CampBriefing), "camp_briefing", ChapterObjectives.CampBriefing),
                Entry(new FlagCondition(ChapterObjectives.PermitObtained, null), "camp_after_permit"),
                Entry(new FlagCondition(ChapterObjectives.SwordUpgraded, null), "camp_upgraded"),
                Entry(new FlagCondition(MaidenStatue.Flag, null), "camp_after_maiden"),
                Entry(default, "camp_materials"),
            });
        }

        private static void Sanctuary(Transform root, Transform props)
        {
            OnStart(root, "sanctuary_enter", "sanctuary_seen");

            var statue = props.Find("Maiden_Statue");
            var spirit = ChapterPrefabs.CreateNpc("Aoi_Spirit", "placeholder_maiden_spirit", statue.position + new Vector3(0.22f, -0.06f, 0f), root, out _);
            Object.DestroyImmediate(spirit.GetComponent<BoxCollider2D>());
            spirit.transform.Find("Visual").gameObject.AddComponent<FloatBob>();

            var interact = statue.gameObject.AddComponent<MaidenStatue>();
            interact.Configure("Tocar la estatua", 0.6f, ChapterPrefabs.CreateMarker(statue, 0.7f));
            interact.Setup(Get("maiden_awaken"), Get("maiden_hint"), spirit, statue.GetComponent<SpriteRenderer>());
        }

        // Coloca enemigos en los marcadores "Enemy_Melee_*" y "Enemy_Ranged_*".
        private static void Enemies(Transform root, Transform markers)
        {
            var parent = new GameObject("Enemies").transform;
            parent.SetParent(root, false);
            var melee = AssetDatabase.LoadAssetAtPath<GameObject>(ChapterEnemies.MeleePath);
            var ranged = AssetDatabase.LoadAssetAtPath<GameObject>(ChapterEnemies.RangedPath);
            foreach (Transform m in markers)
            {
                GameObject prefab = m.name.StartsWith("Enemy_Melee") ? melee : m.name.StartsWith("Enemy_Ranged") ? ranged : null;
                if (prefab == null) continue;
                var e = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                e.name = m.name;
                e.transform.position = m.position;
            }
        }

        private static void Seal(Transform root, Transform markers)
        {
            var at = markers.Find("Seal_Gate").position;
            var gate = new GameObject("SealGate");
            gate.transform.SetParent(root, false);
            gate.transform.position = at;
            var sr = gate.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load("placeholder_seal_gate");
            sr.sortingLayerName = "Sprites";
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            var barrier = new GameObject("Barrier");
            barrier.transform.SetParent(gate.transform, false);
            barrier.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            var bsr = barrier.AddComponent<SpriteRenderer>();
            bsr.sprite = PlaceholderArtGenerator.Load("placeholder_seal_barrier");
            bsr.sortingLayerName = "Sprites";
            bsr.sortingOrder = 1;
            bsr.spriteSortPoint = SpriteSortPoint.Pivot;

            var block = gate.AddComponent<BoxCollider2D>();
            block.size = new Vector2(0.9f, 0.12f);
            block.offset = new Vector2(0f, 0.04f);

            var seal = gate.AddComponent<SealGate>();
            seal.Configure("Examinar el sello", 0.6f, ChapterPrefabs.CreateMarker(gate.transform, 0.95f));
            seal.Setup(bsr, block, Get("seal_reject"), Get("seal_open"));
        }

        private static void Arena(Transform root, Transform markers)
        {
            var guardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChapterEnemies.GuardianPath);
            var guardian = (GameObject)PrefabUtility.InstantiatePrefab(guardianPrefab, root);
            guardian.transform.position = markers.Find("Guardian").position;
            var boss = guardian.GetComponent<GuardianBoss>();
            var so = new SerializedObject(boss);
            so.FindProperty("arenaCenter").objectReferenceValue = markers.Find("Arena_Center");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Permiso (aparece al vencer)
            var permitGo = new GameObject("GuardianPermit");
            permitGo.transform.SetParent(root, false);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(permitGo.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            var psr = visual.AddComponent<SpriteRenderer>();
            psr.sprite = PlaceholderArtGenerator.Load("placeholder_icon_permit");
            psr.sortingLayerName = "Sprites";
            visual.AddComponent<FloatBob>();
            var permit = permitGo.AddComponent<PermitPickup>();
            permit.Configure("Tomar el permiso del guardián", 0.5f, ChapterPrefabs.CreateMarker(permitGo.transform, 0.45f));
            permit.Setup(Get("permit_get"));

            // Encuentro: al acercarse empieza la presentación
            var intro = markers.Find("Arena_Intro");
            var encounter = new GameObject("GuardianArena");
            encounter.transform.SetParent(root, false);
            encounter.transform.position = intro.position;
            var trigger = encounter.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            encounter.AddComponent<GuardianArena>().Setup(boss, permit, Get("guardian_intro"), Get("guardian_again"), Get("guardian_defeat"));

            // La salida al cierre solo aparece con el permiso (al continuar una partida ya terminada)
            var exitToggle = new GameObject("VictoryExitToggle");
            exitToggle.transform.SetParent(root, false);
            exitToggle.AddComponent<ActiveWhenFlag>().Setup(ChapterObjectives.PermitObtained, true, markers.Find("Exit_Victory").gameObject);
        }

        // ---------------------------------------------------------------- helpers

        private static NpcDialogue.Entry Entry(FlagCondition condition, string dialogueId, string setFlag = null) =>
            new NpcDialogue.Entry { condition = condition, dialogue = Get(dialogueId), setFlag = setFlag };

        private static void OnStart(Transform root, string dialogueId, string flag)
        {
            var go = new GameObject("Story_" + dialogueId);
            go.transform.SetParent(root, false);
            go.AddComponent<StoryTrigger>().Configure(StoryTrigger.Mode.OnSceneStart, default, Get(dialogueId), flag);
        }

        private static StoryTrigger AreaTrigger(Transform root, string name, Vector3 position, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = radius;
            return go.AddComponent<StoryTrigger>();
        }
    }
}
