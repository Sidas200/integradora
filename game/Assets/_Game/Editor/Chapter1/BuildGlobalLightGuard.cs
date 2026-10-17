using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.EditorTools.Chapter1
{
    // Al compilar desde el editor, Unity carga cada escena del build mientras la escena abierta
    // sigue con su Global Light 2D encendida, y URP reporta "More than one global light on layer…".
    // Se apagan temporalmente las luces globales de las escenas abiertas y se restauran al terminar
    // (sin marcar la escena como modificada). No afecta al juego compilado.
    public class BuildGlobalLightGuard : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static readonly List<Light2D> disabled = new List<Light2D>();

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            disabled.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var light in root.GetComponentsInChildren<Light2D>())
                    {
                        if (light.lightType != Light2D.LightType.Global || !light.enabled) continue;
                        light.enabled = false;
                        disabled.Add(light);
                    }
            }
            // Si el build falla, Unity no llama al postproceso: se restaura en el siguiente update
            EditorApplication.delayCall += Restore;
        }

        public void OnPostprocessBuild(BuildReport report) => Restore();

        private static void Restore()
        {
            foreach (var light in disabled)
                if (light != null) light.enabled = true;
            disabled.Clear();
        }
    }
}
