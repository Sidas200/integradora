using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Sistemas de partículas de los ataques del guardián (fuego, polvo, escombros y chispas).
    // No emiten solos: BossFx llama a Emit() en cada momento del ataque.
    public static class ChapterVfx
    {
        private const string Folder = PlaceholderArtGenerator.Folder + "/VFX";

        public static BossFx AddBossFx(GameObject boss)
        {
            var root = new GameObject("BossFx").transform;
            root.SetParent(boss.transform, false);

            var fire = System(root, "Fire", "vfx_fire", 600, 0f, 20,
                Gradient((Color.white, 0f), (new Color(1f, 0.55f, 0.18f), 0.45f), (new Color(0.75f, 0.12f, 0.08f), 1f)),
                Alpha((1f, 0f), (1f, 0.6f), (0f, 1f)), Curve(1f, 0.35f));
            var dust = System(root, "Dust", "vfx_dust", 300, 0f, 19,
                Gradient((Color.white, 0f), (Color.white, 1f)),
                Alpha((0.9f, 0f), (0.6f, 0.5f), (0f, 1f)), Curve(0.6f, 1.6f));
            var debris = System(root, "Debris", "vfx_rock", 100, 0.35f, 21,
                Gradient((Color.white, 0f), (Color.white, 1f)),
                Alpha((1f, 0f), (1f, 0.8f), (0f, 1f)), Curve(1f, 1f));
            var sparks = System(root, "Sparks", "vfx_spark", 300, -0.02f, 22,
                Gradient((Color.white, 0f), (Color.white, 1f)),
                Alpha((1f, 0f), (1f, 0.5f), (0f, 1f)), Curve(1f, 0.3f));

            var fx = boss.AddComponent<BossFx>();
            fx.Setup(fire, dust, debris, sparks);
            return fx;
        }

        private static ParticleSystem System(Transform parent, string name, string texture, int max, float gravity, int order,
            Gradient color, GradientAlphaKey[] alpha, AnimationCurve size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;          // siempre "vivo" para que Emit() simule las partículas
            main.playOnAwake = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            color.SetKeys(color.colorKeys, alpha);
            col.color = color;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, size);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = Material(texture);
            renderer.sortingLayerName = "Sprites";
            renderer.sortingOrder = order;
            return ps;
        }

        // Material sin luz con la textura del placeholder (las partículas brillan con luz propia).
        private static Material Material(string texture)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(PlaceholderArtGenerator.Folder, "VFX");
            string path = $"{Folder}/{texture}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderArtGenerator.PathOf(texture));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Gradient Gradient(params (Color c, float t)[] keys)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++) ck[i] = new GradientColorKey(keys[i].c, keys[i].t);
            g.SetKeys(ck, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static GradientAlphaKey[] Alpha(params (float a, float t)[] keys)
        {
            var ak = new GradientAlphaKey[keys.Length];
            for (int i = 0; i < keys.Length; i++) ak[i] = new GradientAlphaKey(keys[i].a, keys[i].t);
            return ak;
        }

        private static AnimationCurve Curve(float from, float to) => AnimationCurve.Linear(0f, from, 1f, to);
    }
}
