using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.EditorTools.Chapter1
{
    // Iluminación 2D y sombras del capítulo.
    // - Global Light 2D por zona (ambiente) + luces puntuales en fuegos, cristales, forja y estatua.
    // - ShadowCaster2D con forma en la BASE del sprite (huella isométrica), no en toda la silueta:
    //   así la sombra sale del pie del objeto como corresponde en isométrico.
    // - En exteriores, sombra proyectada en diagonal (silueta oscura) para leer la profundidad de día.
    // Se limita la cantidad de luces con sombra para mantener ≥ 30 FPS.
    public static class ChapterLighting
    {
        public static readonly Color Fire = new Color(1f, 0.62f, 0.3f);
        public static readonly Color CrystalCyan = new Color(0.45f, 0.85f, 1f);
        public static readonly Color Crimson = new Color(0.95f, 0.18f, 0.3f);

        private struct Profile
        {
            public Color ambient;
            public float intensity;
            public float playerLight;
            public bool projectedShadows;
        }

        private static Profile For(string scene)
        {
            switch (scene)
            {
                // Isla de flores: cálida
                case ChapterScenes.FlowerIsland: return new Profile { ambient = new Color(1f, 0.94f, 0.82f), intensity = 1f, playerLight = 0.2f, projectedShadows = true };
                // Campo abierto: de día
                case ChapterScenes.OpenField: return new Profile { ambient = new Color(1f, 1f, 0.96f), intensity = 1f, playerLight = 0.15f, projectedShadows = true };
                // Campamento: atardecer, la fogata y la forja iluminan
                case ChapterScenes.Camp: return new Profile { ambient = new Color(1f, 0.82f, 0.68f), intensity = 0.72f, playerLight = 0.3f, projectedShadows = true };
                // Santuario: frío y místico
                case ChapterScenes.Sanctuary: return new Profile { ambient = new Color(0.5f, 0.6f, 0.95f), intensity = 0.42f, playerLight = 0.5f, projectedShadows = false };
                // Bosque Quebrado: crepúsculo
                case ChapterScenes.CombatZone: return new Profile { ambient = new Color(0.78f, 0.72f, 0.92f), intensity = 0.78f, playerLight = 0.35f, projectedShadows = true };
                // Arena del guardián: dramática, solo los braseros
                case ChapterScenes.GuardianArena: return new Profile { ambient = new Color(0.5f, 0.42f, 0.7f), intensity = 0.34f, playerLight = 0.4f, projectedShadows = false };
                default: return new Profile { ambient = Color.white, intensity = 1f, playerLight = 0.3f };
            }
        }

        // ---------------------------------------------------------------- escena

        public static void ApplyToScene(string scene, GameObject gridGo, Light2D global)
        {
            var profile = For(scene);
            global.color = profile.ambient;
            global.intensity = profile.intensity;
            new GameObject("ZoneLighting").AddComponent<ZoneLighting>().Setup(profile.playerLight);

            var props = gridGo.transform.Find("Props");
            if (props != null)
                foreach (Transform p in props) DecorateProp(p, profile);

            var story = GameObject.Find("Story");
            if (story != null)
            {
                var barrier = story.transform.Find("SealGate/Barrier");
                if (barrier != null) Point(barrier, new Vector3(0f, 0.25f, 0f), Crimson, 0.8f, 0.9f, false, true);
                var spirit = story.transform.Find("Aoi_Spirit");
                if (spirit != null) Point(spirit, new Vector3(0f, 0.3f, 0f), CrystalCyan, 0.6f, 0.7f, false, false);
                var permit = story.transform.Find("GuardianPermit");
                if (permit != null) Point(permit, new Vector3(0f, 0.2f, 0f), new Color(1f, 0.85f, 0.4f), 0.8f, 0.6f, false, false);
            }
        }

        private static void DecorateProp(Transform prop, Profile profile)
        {
            var sr = prop.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            string n = prop.name.ToLowerInvariant();
            float w = sr.sprite.bounds.size.x;

            // Luces
            if (n.StartsWith("campfire")) Point(prop, new Vector3(0f, 0.08f, 0f), Fire, 0.8f, 1.6f, true, true);
            else if (n.StartsWith("forge")) Point(prop, new Vector3(-0.1f, 0.12f, 0f), Fire, 0.9f, 1.0f, true, true);
            else if (n.StartsWith("brazier")) Point(prop, new Vector3(0f, 0.24f, 0f), Fire, 1.25f, 1.5f, true, true);
            else if (n.StartsWith("savecrystal")) Point(prop, new Vector3(0f, 0.2f, 0f), CrystalCyan, 0.7f, 0.65f, false, false);
            else if (n.StartsWith("crystal")) Point(prop, new Vector3(0f, 0.12f, 0f), CrystalCyan, 0.65f, 0.75f, true, false);
            else if (n.StartsWith("maiden_statue")) Point(prop, new Vector3(0f, 0.35f, 0f), CrystalCyan, 0.95f, 1.4f, true, false);
            else if (n.StartsWith("cabin")) Point(prop, new Vector3(0.14f, 0.22f, 0f), Fire, 0.45f, 0.5f, false, true);

            // Sombras proyectadas por luces (forma en la base)
            if (n.StartsWith("tree")) ShadowCaster(prop.gameObject, 0.1f, 0.05f, Vector2.zero);
            else if (n.StartsWith("rock_large")) ShadowCaster(prop.gameObject, w * 0.8f, 0.1f, new Vector2(0f, 0.04f));
            else if (n.StartsWith("cabin")) ShadowCaster(prop.gameObject, 0.66f, 0.32f, new Vector2(0f, 0.16f), diamond: true);
            else if (n.StartsWith("tent")) ShadowCaster(prop.gameObject, 0.38f, 0.14f, new Vector2(0f, 0.06f));
            else if (n.StartsWith("forge")) ShadowCaster(prop.gameObject, 0.32f, 0.16f, new Vector2(0f, 0.08f), diamond: true);
            else if (n.StartsWith("pillar")) ShadowCaster(prop.gameObject, 0.14f, 0.07f, new Vector2(0f, 0.035f), diamond: true);
            else if (n.StartsWith("maiden_statue")) { }
            else if (n.StartsWith("crate") || n.StartsWith("barrel") || n.StartsWith("desk")) ShadowCaster(prop.gameObject, w * 0.7f, 0.06f, new Vector2(0f, 0.03f));

            // Sombra proyectada de día (exteriores)
            if (profile.projectedShadows && (n.StartsWith("tree") || n.StartsWith("rock") || n.StartsWith("cabin") ||
                n.StartsWith("tent") || n.StartsWith("bush") || n.StartsWith("pillar") || n.StartsWith("forge")))
                ProjectedShadow(sr, profile.ambient);
        }

        // ---------------------------------------------------------------- piezas

        public static Light2D Point(Transform parent, Vector3 localPos, Color color, float intensity, float radius, bool shadows, bool flicker)
        {
            var go = new GameObject("Light2D");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = radius * 0.15f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.6f;
            light.shadowsEnabled = shadows;
            light.shadowIntensity = 0.65f;
            light.shadowSoftness = 0.25f;
            AllLayers(light);
            if (flicker) go.AddComponent<LightFlicker>();
            return light;
        }

        public static void AllLayers(Light2D light)
        {
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            light.targetSortingLayers = ids;
        }

        // Caster con forma de huella en el suelo (elipse o rombo isométrico) en coordenadas locales.
        public static void ShadowCaster(GameObject go, float width, float depth, Vector2 offset, bool diamond = false)
        {
            var caster = go.AddComponent<ShadowCaster2D>();
            var so = new SerializedObject(caster);
            so.FindProperty("m_ShadowCastingSource").enumValueIndex = 1; // ShapeEditor
            so.FindProperty("m_CastingOption").enumValueIndex = 1;       // CastShadow (sin auto-sombra)
            so.FindProperty("m_UseRendererSilhouette").boolValue = false;
            so.FindProperty("m_SelfShadows").boolValue = false;
            var path = so.FindProperty("m_ShapePath");
            int count = diamond ? 4 : 8;
            path.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                float a = diamond ? i * Mathf.PI * 0.5f : i * Mathf.PI * 2f / count;
                var p = new Vector3(offset.x + Mathf.Cos(a) * width * 0.5f, offset.y + Mathf.Sin(a) * depth * 0.5f, 0f);
                path.GetArrayElementAtIndex(i).vector3Value = p;
            }
            so.FindProperty("m_ShapePathHash").intValue = 0; // fuerza a regenerar la malla de sombra
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Silueta oscura "tumbada" en diagonal desde la base (sol arriba a la izquierda).
        private static void ProjectedShadow(SpriteRenderer source, Color ambient)
        {
            var go = new GameObject("ProjectedShadow");
            go.transform.SetParent(source.transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -125f);
            go.transform.localScale = new Vector3(1f, 0.5f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.color = new Color(0.04f, 0.04f, 0.14f, 0.28f);
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 8; // encima del suelo, debajo de personajes y avisos
        }

        // ---------------------------------------------------------------- personajes

        public static void AddPlayerLights(GameObject player)
        {
            var aura = Point(player.transform, new Vector3(0f, 0.2f, 0f), new Color(1f, 0.93f, 0.82f), 0.3f, 1.1f, false, false);
            aura.gameObject.name = "Aura";
            var glow = Point(player.transform, new Vector3(0.07f, 0.17f, 0f), Crimson, 0.35f, 0.3f, false, false);
            glow.gameObject.name = "SwordGlow";
            player.AddComponent<PlayerLights>().Setup(aura, glow);
            ShadowCaster(player, 0.12f, 0.05f, new Vector2(0f, 0.02f));
        }
    }
}
