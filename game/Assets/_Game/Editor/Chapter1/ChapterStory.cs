using System.Collections.Generic;
using UnityEngine;
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
                case ChapterScenes.OpenField: OpenField(root, markers); break;
                case ChapterScenes.Camp: Camp(root, markers); break;
                case ChapterScenes.Sanctuary: Sanctuary(root, props); break;
                case ChapterScenes.CombatZone: OnStart(root, "zone2_enter", "zone2_seen"); break;
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
