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

        private const string BaseMeleePath = "Assets/_Game/Prefabs/enemies/Enemy_Melee.prefab";

        public static void BuildAll()
        {
            BuildMeleeVariant();
            BuildRanged();
            BuildGuardian();
        }

        // Variante del Enemy_Melee de Yahir: conserva su IA y cambios futuros del prefab base.
        private static void BuildMeleeVariant()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseMeleePath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = "Enemy_Melee_Ch1";

            var baseRenderer = root.GetComponent<SpriteRenderer>();
            baseRenderer.enabled = false;
            // El sprite de kratos tiene el pivote al centro: se sube media altura para dejar la raíz en los pies
            var visual = AddVisual(root.transform, baseRenderer.sprite, baseRenderer.sprite.bounds.size.y * 0.5f);

            var col = root.GetComponent<CircleCollider2D>();
            col.radius = 0.1f;
            col.offset = new Vector2(0f, 0.04f);
            Finish(root, visual, 1f, 0.16f, 0.22f);

            // Aviso de ataque: círculo rojo en el suelo que parpadea durante el windup
            var warning = root.transform.Find("AttackWarningFX");
            var wsr = warning.GetComponent<SpriteRenderer>();
            wsr.sprite = PlaceholderArtGenerator.Load("placeholder_telegraph_circle");
            warning.localScale = Vector3.one * (0.35f * 2f / 0.64f);
            var tele = warning.gameObject.AddComponent<Telegraph>();
            tele.SetDefaults(0.6f, Telegraph.Danger);
            warning.gameObject.AddComponent<DetachedChild>();

            SetFloat(root.GetComponent<EnemyMeleeController>(), "attackWindup", 0.6f);
            SetFloat(root.GetComponent<EnemyMeleeController>(), "detectionRange", 1.9f);
            SetInt(root.GetComponent<EnemyHealth>(), "maxHealth", 45);
            UiFactory.SetRef(root.GetComponent<EnemyHitFeedback>(), "spriteRenderer", visual);

            PrefabUtility.SaveAsPrefabAsset(root, MeleePath);
            Object.DestroyImmediate(root);
        }

        private static void BuildRanged()
        {
            var root = NewEnemyRoot("Enemy_Ranged", RigidbodyType2D.Dynamic, 0.09f);
            var visual = AddVisual(root.transform, PlaceholderArtGenerator.Load("placeholder_enemy_ranged"), 0f);
            visual.gameObject.AddComponent<FloatBob>();
            Finish(root, visual, 0.8f, 0.16f, 0.26f);

            var health = root.AddComponent<EnemyHealth>();
            SetInt(health, "maxHealth", 40);
            var feedback = root.AddComponent<EnemyHitFeedback>();
            UiFactory.SetRef(feedback, "spriteRenderer", visual);
            var ranged = root.AddComponent<EnemyRangedController>();
            ranged.Setup(PlaceholderArtGenerator.Load("placeholder_telegraph_circle"), PlaceholderArtGenerator.Load("placeholder_projectile"), visual);

            PrefabUtility.SaveAsPrefabAsset(root, RangedPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildGuardian()
        {
            var root = NewEnemyRoot("Guardian_Varn", RigidbodyType2D.Kinematic, 0.16f);
            // Un cuerpo cinemático empujaría a Tsuki a través de los bordes de la arena:
            // sus pies son trigger y el daño de la embestida se calcula aparte.
            root.GetComponent<CircleCollider2D>().isTrigger = true;
            var visual = AddVisual(root.transform, PlaceholderArtGenerator.Load("placeholder_guardian"), 0f);
            Finish(root, visual, 2.2f, 0.28f, 0.38f);

            var health = root.AddComponent<EnemyHealth>();
            SetInt(health, "maxHealth", 300);
            SetFloat(health, "invulnerableTime", 0.35f);
            var feedback = root.AddComponent<EnemyHitFeedback>();
            UiFactory.SetRef(feedback, "spriteRenderer", visual);
            var boss = root.AddComponent<GuardianBoss>();
            boss.Setup(null, PlaceholderArtGenerator.Load("placeholder_telegraph_circle"), PlaceholderArtGenerator.Load("placeholder_telegraph_rect"), visual);

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
