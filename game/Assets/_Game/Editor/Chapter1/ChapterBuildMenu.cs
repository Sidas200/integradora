using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Punto de entrada: Tools/Claude/Build Chapter 1 ejecuta todos los pasos en orden.
    public static class ChapterBuildMenu
    {
        [MenuItem("Tools/Claude/Build Chapter 1", priority = 0)]
        public static void BuildAll()
        {
            VerifyProjectSettings();
            PlaceholderArtGenerator.GenerateAll();
            ChapterPrefabs.BuildAll();
            ChapterDialogues.GenerateAll();
            ChapterScenes.BuildAll();
            ConfigureBuildSettings();
            EditorSceneManagerOpenFirstPlayable();
            Debug.Log("[Chapter1] Build Chapter 1 terminado.");
        }

        [MenuItem("Tools/Claude/Chapter 1/1 - Verify Project Settings")]
        public static void VerifyProjectSettings()
        {
            var expected = new[] { "Default", "Background", "Sprites", "Objects", "UI" };
            var actual = SortingLayer.layers.Select(l => l.name).ToArray();
            if (!expected.SequenceEqual(actual))
                Debug.LogError("[Chapter1] Sorting layers inesperados: " + string.Join(", ", actual));

            var mode = UnityEngine.Rendering.GraphicsSettings.transparencySortMode;
            var axis = UnityEngine.Rendering.GraphicsSettings.transparencySortAxis;
            if (mode != TransparencySortMode.CustomAxis || axis != new Vector3(0f, 1f, -0.26f))
                Debug.LogError($"[Chapter1] Transparency sort incorrecto: {mode} {axis}");

            if (LayerMask.NameToLayer("Player") < 0 || LayerMask.NameToLayer("Enemy") < 0)
                Debug.LogError("[Chapter1] Faltan las capas físicas Player/Enemy.");
        }

        // Menú primero; Prototype_Isometric se conserva en la lista pero deshabilitada.
        [MenuItem("Tools/Claude/Chapter 1/5 - Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var name in ChapterScenes.Order)
                scenes.Add(new EditorBuildSettingsScene(ChapterScenes.PathOf(name), true));

            foreach (var s in EditorBuildSettings.scenes)
            {
                if (scenes.Any(x => x.path == s.path)) continue;
                scenes.Add(new EditorBuildSettingsScene(s.path, false));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Chapter1] Build Settings: " + string.Join(", ", ChapterScenes.Order));
        }

        private static void EditorSceneManagerOpenFirstPlayable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ChapterScenes.PathOf(ChapterScenes.MainMenu));
        }
    }
}
