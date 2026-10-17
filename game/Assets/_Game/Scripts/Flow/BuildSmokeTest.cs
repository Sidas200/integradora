using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Prueba de humo del build (sin abrir Unity): ejecutar el juego con  -smoketest
// Recorre todas las escenas del capítulo, mide los FPS promedio de cada una,
// lo escribe en el log del jugador ([SMOKE]) y cierra el juego. Sin el argumento no hace nada.
public class BuildSmokeTest : MonoBehaviour
{
    [SerializeField] private string[] scenes;
    [SerializeField] private float secondsPerScene = 5f;

    public void Setup(string[] scenePaths) => scenes = scenePaths;

    private void Start()
    {
        if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-smoketest")) return;
        // Solo en la prueba: que el juego siga corriendo aunque su ventana no tenga foco
        Application.runInBackground = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        var report = new StringBuilder("[SMOKE] Inicio de la prueba de humo\n");
        var fpsList = new List<float>();
        yield return new WaitForSecondsRealtime(2f);

        foreach (var path in scenes)
        {
            SceneLoader.Instance.LoadSceneFresh(path, null);
            yield return new WaitForSecondsRealtime(1.5f);
            while (SceneLoader.Instance.IsBusy) yield return null;
            if (DialogueUI.Instance != null) DialogueUI.Instance.Cancel();

            // Mide los FPS reales de la escena
            int frames = 0;
            float t = 0f;
            while (t < secondsPerScene)
            {
                if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen) DialogueUI.Instance.Cancel();
                t += Time.unscaledDeltaTime;
                frames++;
                yield return null;
            }
            float fps = frames / t;
            fpsList.Add(fps);
            report.AppendLine($"[SMOKE] {SceneManager.GetActiveScene().name}: {fps:0.0} FPS {(fps >= 30f ? "OK" : "BAJO")}");
        }

        float min = float.MaxValue;
        foreach (var f in fpsList) min = Mathf.Min(min, f);
        report.AppendLine($"[SMOKE] Mínimo: {min:0.0} FPS · {(min >= 30f ? "CUMPLE ≥30" : "NO CUMPLE")} · Fin");
        Debug.Log(report.ToString());
        Application.Quit();
    }
}
