// =====================================================================
//  Script de compilación del videojuego (Unity 6.6) para CI.
//  GameCI lo invoca con:  buildMethod: TDIMP.CI.BuildScript.Build
//  y le pasa los argumentos -customBuildPath, -buildTarget y -buildVersion.
//  También se puede usar desde el menú: Build > CI > Windows 64.
//  Termina el proceso con código 0 (éxito) o 1 (falla) para que el pipeline lo detecte.
// =====================================================================
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TDIMP.CI
{
    public static class BuildScript
    {
        private const string DefaultOutput = "build/StandaloneWindows64/TheDayIMadeThatPromise.exe";

        [MenuItem("Build/CI/Windows 64")]
        public static void BuildFromMenu()
        {
            Run(DefaultOutput, BuildTarget.StandaloneWindows64, PlayerSettings.bundleVersion, exitOnFinish: false);
        }

        /// <summary>Punto de entrada para GameCI (modo batch).</summary>
        public static void Build()
        {
            string output = Arg("-customBuildPath") ?? DefaultOutput;
            string version = Arg("-buildVersion") ?? PlayerSettings.bundleVersion;
            BuildTarget target = Enum.TryParse(Arg("-buildTarget"), out BuildTarget parsed)
                ? parsed
                : BuildTarget.StandaloneWindows64;
            Run(output, target, version, exitOnFinish: true);
        }

        private static void Run(string output, BuildTarget target, string version, bool exitOnFinish)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Fail("No hay escenas habilitadas en File > Build Profiles (EditorBuildSettings).", exitOnFinish);
                return;
            }

            PlayerSettings.bundleVersion = version;
            Debug.Log($"[CI] Compilando {target} v{version} → {output}\n[CI] Escenas: {string.Join(", ", scenes)}");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.StrictMode   // cualquier error de compilación detiene el build
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[CI] Resultado: {summary.result} · {summary.totalSize / (1024 * 1024)} MB · " +
                      $"{summary.totalTime.TotalSeconds:F0} s · errores: {summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"El build terminó con estado {summary.result}", exitOnFinish);
                return;
            }

            if (exitOnFinish)
            {
                EditorApplication.Exit(0);
            }
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static void Fail(string message, bool exitOnFinish)
        {
            Debug.LogError("[CI] " + message);
            if (exitOnFinish)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
