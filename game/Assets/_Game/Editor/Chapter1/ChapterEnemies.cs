using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTools.Chapter1
{
    // Prefabs de enemigos del capítulo. Misma convención que el jugador: raíz en los pies,
    // sprite en "Visual", sombra blob, y un "Hurtbox" (trigger en el cuerpo) para recibir golpes.
    public static class ChapterEnemies
    {
        public const string MeleePath = ChapterPrefabs.Folder + "/Enemy_Melee_Ch1.prefab";
        public const string RangedPath = ChapterPrefabs.Folder + "/Enemy_Ranged.prefab";
        public const string GuardianPath = ChapterPrefabs.Folder + "/Guardian_Varn.prefab";
        public const string SlimePath = ChapterPrefabs.Folder + "/Enemy_Slime.prefab";
        public const string FragmentoPath = ChapterPrefabs.Folder + "/Enemy_Fragmento.prefab";

        private const string BaseMeleePath = "Assets/_Game/Prefabs/enemies/Enemy_Melee.prefab";

        // Vida de los enemigos comunes: 4 golpes cuerpo a cuerpo con la espada sin mejorar (15 de daño),
        // 3 con la mejorada (25) y 3 dobles cortes a distancia (12, solo con la mejorada)
        private const int CommonEnemyHealth = 60;

        public static void BuildAll()
        {
            BuildMeleeVariant();
            BuildSlime();
            BuildRanged();
            BuildFragmento();
            BuildGuardian();
        }

        // Variante del Enemy_Melee de Yahir: conserva su IA y cambios futuros del prefab base.
        // Se ve como el Lobo del Velo (8 direcciones, pivote en las patas).
        private static void BuildMeleeVariant()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseMeleePath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = "Enemy_Melee_Ch1";

            root.GetComponent<SpriteRenderer>().enabled = false;
            var clips = EnemyFramesImport.LoadClips(EnemyFramesImport.Lobo);
            var idle = System.Array.Find(clips, c => c.name == "Idle_Sur");
            var visual = AddVisual(root.transform, idle.frames[0], 0f);
            visual.gameObject.AddComponent<GuardianVarnAnimator>().SetClips(clips);

            var col = root.GetComponent<CircleCollider2D>();
            col.radius = 0.1f;
            col.offset = new Vector2(0f, 0.04f);
            // Cuerpo de cuatro patas: más ancho y bajo que el humano de antes
            Finish(root, visual, 1.2f, 0.17f, 0.16f);

            // El zarpazo ya viene dibujado en el frame de ataque: sin el slash suelto del prefab base
            var melee = root.GetComponent<EnemyMeleeController>();
            UiFactory.SetRef(melee, "attackFX", null);
            root.transform.Find("AttackFX").gameObject.SetActive(false);

            // Aviso de ataque: círculo rojo en el suelo que parpadea durante el windup
            var warning = root.transform.Find("AttackWarningFX");
            var wsr = warning.GetComponent<SpriteRenderer>();
            wsr.sprite = PlaceholderArtGenerator.Load("placeholder_telegraph_circle");
            warning.localScale = Vector3.one * (0.35f * 2f / 0.64f);
            var tele = warning.gameObject.AddComponent<Telegraph>();
            tele.SetDefaults(0.6f, Telegraph.Danger);
            warning.gameObject.AddComponent<DetachedChild>();

            SetFloat(melee, "attackWindup", 0.6f);
            SetFloat(melee, "detectionRange", 1.9f);
            SetInt(root.GetComponent<EnemyHealth>(), "maxHealth", CommonEnemyHealth);
            UiFactory.SetRef(root.GetComponent<EnemyHitFeedback>(), "spriteRenderer", visual);

            PrefabUtility.SaveAsPrefabAsset(root, MeleePath);
            Object.DestroyImmediate(root);
        }

        // Slime azul (DropTable_SlimeAzul del equipo): parte del enemigo cuerpo a cuerpo pero con su
        // propia IA (SlimeController: carga y se lanza contra el jugador), sprite y animación de rebote.
        // Suelta flor y restos de slime.
        private static void BuildSlime()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseMeleePath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = "Enemy_Slime";
            root.GetComponent<SpriteRenderer>().enabled = false;
            var visual = AddVisual(root.transform, PlaceholderArtGenerator.Load("placeholder_enemy_slime"), 0f);

            var col = root.GetComponent<CircleCollider2D>();
            col.radius = 0.09f;
            col.offset = new Vector2(0f, 0.04f);
            Finish(root, visual, 0.9f, 0.13f, 0.1f);

            var warning = root.transform.Find("AttackWarningFX");
            warning.GetComponent<SpriteRenderer>().sprite = PlaceholderArtGenerator.Load("placeholder_telegraph_circle");
            warning.localScale = Vector3.one * (0.3f * 2f / 0.64f);
            warning.gameObject.AddComponent<Telegraph>().SetDefaults(0.6f, Telegraph.Danger);
            warning.gameObject.AddComponent<DetachedChild>();

            Object.DestroyImmediate(root.GetComponent<EnemyMeleeController>());
            root.AddComponent<SlimeBounce>().Setup(visual.transform);
            var ai = root.AddComponent<SlimeController>();
            ai.Setup(visual.transform, warning.GetComponent<Telegraph>());
            SetFloat(ai, "detectionRange", 1.5f);
            SetFloat(ai, "moveSpeed", 1.0f);
            SetInt(ai, "attackDamage", 20);
            SetInt(root.GetComponent<EnemyHealth>(), "maxHealth", CommonEnemyHealth);
            UiFactory.SetRef(root.GetComponent<EnemyHitFeedback>(), "spriteRenderer", visual);

            PrefabUtility.SaveAsPrefabAsset(root, SlimePath);
            Object.DestroyImmediate(root);
        }

        private static void BuildRanged()
        {
            var root = NewEnemyRoot("Enemy_Ranged", RigidbodyType2D.Dynamic, 0.09f);
            var visual = AddVisual(root.transform, PlaceholderArtGenerator.Load("placeholder_enemy_ranged"), 0f);
            visual.gameObject.AddComponent<FloatBob>();
            Finish(root, visual, 0.8f, 0.16f, 0.26f);

            var health = root.AddComponent<EnemyHealth>();
            SetInt(health, "maxHealth", CommonEnemyHealth);
            var feedback = root.AddComponent<EnemyHitFeedback>();
            UiFactory.SetRef(feedback, "spriteRenderer", visual);
            var ranged = root.AddComponent<EnemyRangedController>();
            ranged.Setup(PlaceholderArtGenerator.Load("placeholder_telegraph_circle"), PlaceholderArtGenerator.Load("placeholder_projectile"), visual);

            PrefabUtility.SaveAsPrefabAsset(root, RangedPath);
            Object.DestroyImmediate(root);
        }

        // Fragmento Errante: el refuerzo que invoca Varn. Levita hacia Tsuki y suelta una onda de
        // choque a su alrededor (FragmentoErranteController); animación en 8 direcciones.
        private static void BuildFragmento()
        {
            var root = NewEnemyRoot("Enemy_Fragmento", RigidbodyType2D.Dynamic, 0.09f);
            var clips = EnemyFramesImport.LoadClips(EnemyFramesImport.Fragmento);
            var idle = System.Array.Find(clips, c => c.name == "Idle_Sur");
            var visual = AddVisual(root.transform, idle.frames[0], 0f);
            visual.gameObject.AddComponent<GuardianVarnAnimator>().SetClips(clips);
            // Levita: el sprite sube y baja sobre su sombra
            visual.gameObject.AddComponent<FloatBob>();
            Finish(root, visual, 0.9f, 0.18f, 0.3f);

            var health = root.AddComponent<EnemyHealth>();
            SetInt(health, "maxHealth", CommonEnemyHealth);
            var feedback = root.AddComponent<EnemyHitFeedback>();
            UiFactory.SetRef(feedback, "spriteRenderer", visual);
            root.AddComponent<FragmentoErranteController>().Setup(PlaceholderArtGenerator.Load("placeholder_telegraph_circle"), visual);

            PrefabUtility.SaveAsPrefabAsset(root, FragmentoPath);
            Object.DestroyImmediate(root);
        }

        public static void BuildGuardian()
        {
            var root = NewEnemyRoot("Guardian_Varn", RigidbodyType2D.Kinematic, 0.16f);
            // Un cuerpo cinemático empujaría a Tsuki a través de los bordes de la arena:
            // sus pies son trigger y el daño de la embestida se calcula aparte.
            root.GetComponent<CircleCollider2D>().isTrigger = true;
            GuardianVarnImport.EnsureAtlas();
            var clips = GuardianVarnImport.LoadClips();
            var intro = System.Array.Find(clips, c => c.name == GuardianVarnAnimator.Intro);
            var visual = AddVisual(root.transform, intro.frames[0], 0f);
            visual.gameObject.AddComponent<GuardianVarnAnimator>().SetClips(clips);
            Finish(root, visual, 2.2f, 0.28f, 0.38f);

            var health = root.AddComponent<EnemyHealth>();
            // 18 golpes con la espada despertada (25 de daño)
            SetInt(health, "maxHealth", 450);
            SetFloat(health, "invulnerableTime", 0.35f);
            var feedback = root.AddComponent<EnemyHitFeedback>();
            UiFactory.SetRef(feedback, "spriteRenderer", visual);
            var boss = root.AddComponent<GuardianBoss>();
            boss.Setup(null, PlaceholderArtGenerator.Load("placeholder_telegraph_circle"), PlaceholderArtGenerator.Load("placeholder_telegraph_rect"), visual);
            boss.SetFx(ChapterVfx.AddBossFx(root));

            PrefabUtility.SaveAsPrefabAsset(root, GuardianPath);
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- helpers

        private static GameObject NewEnemyRoot(string name, RigidbodyType2D bodyType, float feetRadius)
        {
            var root = new GameObject(name);
            root.layer = LayerMask.NameToLayer("Enemy");
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = bodyType;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = root.AddComponent<CircleCollider2D>();
            col.radius = feetRadius;
            col.offset = new Vector2(0f, 0.04f);
            return root;
        }

        private static SpriteRenderer AddVisual(Transform root, Sprite sprite, float yOffset)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root, false);
            visual.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Sprites";
            return sr;
        }

        // SortingGroup, sombra y hurtbox (trigger en el cuerpo, capa Enemy).
        private static void Finish(GameObject root, SpriteRenderer visual, float shadowScale, float hurtRadius, float hurtHeight)
        {
            if (root.GetComponent<SortingGroup>() == null)
                root.AddComponent<SortingGroup>().sortingLayerName = "Sprites";
            ChapterPrefabs.AddBlobShadow(root.transform, shadowScale);
            ChapterLighting.ShadowCaster(root, 0.12f * shadowScale, 0.05f * shadowScale, new Vector2(0f, 0.02f));

            var hurt = new GameObject("Hurtbox");
            hurt.layer = LayerMask.NameToLayer("Enemy");
            hurt.transform.SetParent(root.transform, false);
            var c = hurt.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = hurtRadius;
            c.offset = new Vector2(0f, hurtHeight);
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
