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
                case ChapterScenes.OpenField:
                    OpenField(root, markers);
                    Enemies(root, markers, scene);
                    Pickups(root, markers, scene);
                    SanctuaryDoor(root, markers);
                    // El campamento de Kaede está dentro de la Llanura (claro escondido al oeste)
                    Camp(root, markers);
                    Forge(props, markers);
                    SaveCrystal(props, markers, "base", "SavePoint_Base");
                    break;
                case ChapterScenes.Sanctuary: Sanctuary(root, props, markers, gridGo.GetComponent<Grid>(), scene); break;
                case ChapterScenes.CombatZone:
                    OnStart(root, "zone2_enter", "zone2_seen");
                    Enemies(root, markers, scene);
                    Pickups(root, markers, scene);
                    Seal(root, markers);
                    SaveCrystal(props, markers, "boss", "SavePoint_Boss");
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
            // Las dos celdas de tierra justo antes del puente (el puente mide 2 de ancho)
            var startCell = (Vector2Int)grid.WorldToCell(bridgeStart.position - ZoneBuilder.SurfaceOffset);
            foreach (var cell in new[] { startCell + new Vector2Int(-2, 0), startCell + new Vector2Int(-2, 1),
                                         startCell + new Vector2Int(-3, 0), startCell + new Vector2Int(-3, 1) })
            {
                var c = blocker.AddComponent<CircleCollider2D>();
                c.offset = ZoneBuilder.SurfaceWorld(cell, grid);
                c.radius = 0.12f;
            }
            var vanish = bridgeStart.gameObject.AddComponent<IslandVanish>();
            vanish.Setup(gridGo.transform.Find("Ground").GetComponent<Tilemap>(), gridGo.transform.Find("Elevations").GetComponent<Tilemap>(),
                props, ChapterTiles.Bridge, Get("island_vanish"), blocker,
                new[] { markers.Find("Door_Cabin").gameObject, markers.Find("Exit_CabinInside").gameObject });
            vanish.SetCollisions(gridGo.transform.Find("Collisions").GetComponent<Tilemap>(), ChapterTiles.Collision, bridgeStart);
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
            // Camino del bosque cerrado hasta conocer a Kaede
            var gateAt = markers.Find("Camp_Gate").position;
            var gate = new GameObject("CampGate");
            gate.transform.SetParent(root, false);
            gate.transform.position = gateAt;
            var box = gate.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.3f, 0.9f); // el camino corre hacia la izquierda: barrera vertical
            gate.AddComponent<FlagGate>().Setup(ResearcherFallEvent.Flag, "El bosque es muy espeso. Mejor explora la llanura primero.");

            var area = new GameObject("CampAreaTitle");
            area.transform.SetParent(root, false);
            area.transform.position = markers.Find("Camp_Area").position;
            var trigger = area.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            area.AddComponent<AreaTitle>().Setup("Campamento oculto");

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

        private static void Sanctuary(Transform root, Transform props, Transform markers, Grid grid, string scene)
        {
            OnStart(root, "sanctuary_enter", "sanctuary_seen");

            var statue = props.Find("Maiden_Statue");
            var spirit = ChapterPrefabs.CreateNpc("Aoi_Spirit", "placeholder_maiden_spirit", statue.position + new Vector3(0.22f, -0.06f, 0f), root, out _);
            Object.DestroyImmediate(spirit.GetComponent<BoxCollider2D>());
            Object.DestroyImmediate(spirit.GetComponent<UnityEngine.Rendering.Universal.ShadowCaster2D>()); // un espíritu no proyecta sombra
            spirit.transform.Find("Visual").gameObject.AddComponent<FloatBob>();

            var interact = statue.gameObject.AddComponent<MaidenStatue>();
            interact.Configure("Tocar la estatua", 0.6f, ChapterPrefabs.CreateMarker(statue, 0.7f));
            interact.Setup(Get("maiden_awaken"), Get("maiden_hint"), spirit, statue.GetComponent<SpriteRenderer>());
            interact.SetTrial(SwordTrial(root, markers, grid, scene), Get("trial_intro"));
        }

        // Prueba de la espada: oleadas Trial_W1_* y Trial_W2_* (inactivas hasta que empieza)
        // y una barrera que sella la entrada mientras dura.
        private static SanctuaryTrial SwordTrial(Transform root, Transform markers, Grid grid, string scene)
        {
            var trialGo = new GameObject("SwordTrial");
            trialGo.transform.SetParent(root, false);
            var waves = new List<SanctuaryTrial.Wave>();
            foreach (var prefix in new[] { "Trial_W1_", "Trial_W2_" })
            {
                var wave = new SanctuaryTrial.Wave();
                foreach (Transform m in markers)
                {
                    if (!m.name.StartsWith(prefix)) continue;
                    string kind = "Enemy_" + m.name.Substring(prefix.Length); // Trial_W1_Melee_A -> Enemy_Melee_A
                    var e = SpawnEnemy(trialGo.transform, m, kind, scene);
                    e.SetActive(false);
                    wave.enemies.Add(e);
                }
                waves.Add(wave);
            }

            // Barrera transversal al pasillo de entrada (las celdas del pasillo van en +x)
            var exitCell = (Vector2Int)grid.WorldToCell(markers.Find("Exit_ToField").position - ZoneBuilder.SurfaceOffset);
            var a = ZoneBuilder.SurfaceWorld(exitCell + new Vector2Int(1, -1), grid);
            var b = ZoneBuilder.SurfaceWorld(exitCell + new Vector2Int(1, 1), grid);
            var barrierGo = new GameObject("TrialBarrier");
            barrierGo.transform.SetParent(trialGo.transform, false);
            barrierGo.transform.position = (a + b) * 0.5f;
            var edge = barrierGo.AddComponent<EdgeCollider2D>();
            var dir = (Vector2)(b - a).normalized;
            edge.points = new[] { (Vector2)(a - barrierGo.transform.position) - dir * 0.12f, (Vector2)(b - barrierGo.transform.position) + dir * 0.12f };
            edge.edgeRadius = 0.03f;
            var sr = barrierGo.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load("placeholder_door_barrier");
            sr.sortingLayerName = "Sprites";
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            sr.color = new Color(1f, 0.75f, 0.8f, 0.9f);

            var trial = trialGo.AddComponent<SanctuaryTrial>();
            // La barrera visual es el mismo objeto del colisionador: se apaga entero al abrir
            trial.Setup(waves, edge, sr, PlaceholderArtGenerator.Load("placeholder_summon_circle"));
            return trial;
        }

        // Puerta del primer Santuario en la Llanura: se abre ofreciendo el Alma pequeña.
        private static void SanctuaryDoor(Transform root, Transform markers)
        {
            var at = markers.Find("Sanctuary_Door").position;
            var door = new GameObject("SanctuaryDoor");
            door.transform.SetParent(root, false);
            door.transform.position = at;
            var barrier = new GameObject("Barrier");
            barrier.transform.SetParent(door.transform, false);
            var sr = barrier.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load("placeholder_door_barrier");
            sr.sortingLayerName = "Sprites";
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            var offering = door.AddComponent<ItemOffering>();
            offering.Configure("Examinar la puerta", 0.6f, ChapterPrefabs.CreateMarker(door.transform, 0.7f));
            offering.Setup(ChapterPrefabs.LoadMaterial("AlmaPequena"), ChapterObjectives.SanctuaryUnlocked, "Ofrecer el Alma pequeña",
                Get("sanctuary_door_locked"), Get("sanctuary_door_open"), sr);
        }

        // Coloca enemigos en los marcadores "Enemy_Melee_*" y "Enemy_Ranged_*".
        private static void Enemies(Transform root, Transform markers, string scene)
        {
            var parent = new GameObject("Enemies").transform;
            parent.SetParent(root, false);
            foreach (Transform m in markers)
                if (m.name.StartsWith("Enemy_")) SpawnEnemy(parent, m, m.name, scene);
        }

        // Instancia el enemigo según su tipo (Enemy_Melee_*, Enemy_Slime_*, Enemy_Ranged_*) con
        // botín garantizado y persistente (HEX-04: cada enemigo es una fuente única).
        private static GameObject SpawnEnemy(Transform parent, Transform marker, string kind, string scene)
        {
            string path = kind.StartsWith("Enemy_Slime") ? ChapterEnemies.SlimePath
                : kind.StartsWith("Enemy_Ranged") ? ChapterEnemies.RangedPath : ChapterEnemies.MeleePath;
            var e = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            e.name = marker.name;
            e.transform.position = marker.position;
            var loot = e.AddComponent<EnemyLoot>();
            e.GetComponent<PersistentId>().SetId($"{scene}/{marker.name}");
            loot.Setup(AssetDatabase.LoadAssetAtPath<MaterialPickup>("Assets/_Game/Prefabs/Pickup_FlorDestello.prefab"), ChapterData.LootForEnemy(kind));
            return e;
        }

        // Pickups en los marcadores "Pickup_*": cada uno tiene su id y se recoge una sola vez.
        private static void Pickups(Transform root, Transform markers, string scene)
        {
            var parent = new GameObject("Pickups").transform;
            parent.SetParent(root, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Pickup_FlorDestello.prefab");
            foreach (Transform m in markers)
            {
                if (!m.name.StartsWith("Pickup_")) continue;
                var material = ChapterData.MaterialForMarker(m.name);
                if (material == null) continue;
                var p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                p.name = m.name;
                p.transform.position = m.position + new Vector3(0f, 0.06f, 0f);
                var so = new SerializedObject(p.GetComponent<MaterialPickup>());
                so.FindProperty("material").objectReferenceValue = material;
                so.FindProperty("amount").intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                var sr = p.GetComponent<SpriteRenderer>();
                sr.sprite = material.icon;
                sr.color = Color.white;
                p.AddComponent<FloatBob>();
                p.AddComponent<PersistentId>().SetId($"{scene}/{m.name}");
                ChapterPrefabs.AddBlobShadow(p.transform, 0.5f);
            }
        }

        private static void Forge(Transform props, Transform markers)
        {
            var forge = props.Find("Forge");
            var station = forge.gameObject.AddComponent<ForgeStation>();
            station.Configure("Usar la forja", 0.75f, ChapterPrefabs.CreateMarker(forge, 0.5f));
        }

        private static void SaveCrystal(Transform props, Transform markers, string id, string respawnMarker)
        {
            var crystal = props.Find("SaveCrystal");
            var save = crystal.gameObject.AddComponent<SavePoint>();
            save.Configure("Guardar partida", 0.55f, ChapterPrefabs.CreateMarker(crystal, 0.42f));
            save.Setup(id, markers.Find(respawnMarker), crystal.GetComponent<SpriteRenderer>());
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
            // Elipse de la arena en el mundo (un círculo de R celdas mide 0.2263R x 0.1131R), con margen
            float r = ChapterScenes.ArenaRadius - 1.5f;
            boss.SetArena(markers.Find("Arena_Center"), new Vector2(0.2263f * r, 0.1131f * r));

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
            var arena = encounter.AddComponent<GuardianArena>();
            arena.Setup(boss, permit, Get("guardian_sleeping"), Get("guardian_intro"), Get("guardian_again"), Get("guardian_defeat"));
            arena.SetRetreatExit(markers.Find("Exit_ToZone2").gameObject);

            // Ofrenda de la Gota de Amrita frente al guardián dormido
            var altarGo = new GameObject("AmritaAltar");
            altarGo.transform.SetParent(guardian.transform, false);
            var altar = altarGo.AddComponent<AmritaAltar>();
            altar.Configure("Ofrecer la Gota de Amrita", 0.75f, ChapterPrefabs.CreateMarker(guardian.transform, 0.9f));
            altar.Setup(arena, Get("guardian_no_amrita"));

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
