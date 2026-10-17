using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTools.Chapter1
{
    // Prefabs del capítulo. Convención isométrica: la raíz está en los pies (punto de sorting)
    // y el sprite vive en un hijo "Visual" desplazado hacia arriba.
    public static class ChapterPrefabs
    {
        public const string Folder = "Assets/_Game/Prefabs/Chapter1";
        public const string PlayerPath = Folder + "/Player_Tsuki.prefab";

        private const string SpriteSheetPath = "Assets/_Game/Animations/sprite sheet.png";

        [MenuItem("Tools/Claude/Chapter 1/3 - Build Prefabs")]
        public static void BuildAll()
        {
            EnsureFolder();
            MoveSharedPrefabsToSpritesLayer();
            BuildPlayer();
            ChapterUi.BuildSystemsPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[Chapter1] Prefabs listos.");
        }

        public static GameObject LoadPlayer() => AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);

        private static void BuildPlayer()
        {
            var root = new GameObject("Player_Tsuki");
            root.tag = "Player";
            root.layer = LayerMask.NameToLayer("Player");

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Colisión solo en los pies: permite pasar "detrás" de árboles y muros.
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.14f, 0.07f);
            col.offset = new Vector2(0f, 0.035f);

            var group = root.AddComponent<SortingGroup>();
            group.sortingLayerName = "Sprites";

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            var frames = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            sr.sprite = frames.Length > 24 ? frames[24] : null;
            sr.sortingLayerName = "Sprites";

            AddBlobShadow(root.transform, 0.9f);

            root.AddComponent<PlayerStats>(); // agrega Health
            var controller = root.AddComponent<PlayerController>();
            root.AddComponent<Inventory>();
            var itemUser = root.AddComponent<ItemUser>();
            root.AddComponent<SessionSync>();

            // Mismos valores que el jugador de Test_Inventory_Emmanuel, con la capa Enemy ya asignada.
            var so = new SerializedObject(controller);
            so.FindProperty("dashSpeed").floatValue = 5f;
            so.FindProperty("dashDuration").floatValue = 0.15f;
            so.FindProperty("dashCooldown").floatValue = 1f;
            so.FindProperty("dashColor").colorValue = new Color(0.478f, 0.071f, 0.114f, 1f);
            so.FindProperty("ghostSpawnInterval").floatValue = 0.05f;
            so.FindProperty("ghostLifetime").floatValue = 0.3f;
            so.FindProperty("attackCooldown").floatValue = 0.4f;
            so.FindProperty("attackColor").colorValue = new Color(1f, 0.2f, 0.3f, 0.45f);
            so.FindProperty("enemyLayer").intValue = LayerMask.GetMask("Enemy");
            so.FindProperty("walkFps").floatValue = 8f;
            so.FindProperty("attackFps").floatValue = 16f;
            so.FindProperty("spriteSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(SpriteSheetPath);
            var allFrames = so.FindProperty("allFrames");
            allFrames.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) allFrames.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var itemSo = new SerializedObject(itemUser);
            itemSo.FindProperty("potion").objectReferenceValue = LoadMaterial("Pocion");
            itemSo.FindProperty("amritaDrop").objectReferenceValue = LoadMaterial("GotaAmrita");
            itemSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
        }

        // Sombra "blob" bajo los pies para leer la profundidad.
        public static void AddBlobShadow(Transform parent, float scale)
        {
            var shadow = new GameObject("BlobShadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0.015f, 0.005f, 0f);
            shadow.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = shadow.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load("placeholder_shadow_blob");
            sr.sortingLayerName = "Sprites";
            sr.sortingOrder = -5;
        }

        public static MaterialData LoadMaterial(string assetName) =>
            AssetDatabase.LoadAssetAtPath<MaterialData>($"Assets/_Game/Scripts/ScriptableObjects/{assetName}.asset");

        // Decisión 3 del análisis: los prefabs compartidos pasan del layer Default a Sprites,
        // para que no queden debajo del suelo (Background) en las escenas isométricas.
        private static void MoveSharedPrefabsToSpritesLayer()
        {
            string[] paths =
            {
                "Assets/_Game/Prefabs/enemies/Enemy_Melee.prefab",
                "Assets/_Game/Prefabs/Pickup_CueroAntiguo.prefab",
                "Assets/_Game/Prefabs/Pickup_FlorDestello.prefab",
            };
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (r.sortingLayerName == "Sprites") continue;
                    r.sortingLayerName = "Sprites";
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Chapter1");
        }
    }
}
