using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// La Global Light 2D de cada escena se guarda APAGADA: durante el build Unity carga las escenas
// una tras otra y URP reporta "More than one global light…" (con StrictMode eso falla el build).
// En el juego la enciende SceneContext; en el editor este componente la enciende para poder
// editar la escena iluminada, la apaga justo al guardar y no la toca durante un build.
[ExecuteAlways]
[RequireComponent(typeof(Light2D))]
public class GlobalLightEditorPreview : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnEnable()
    {
        if (Application.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer) return;
        SetLight(true);
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSaving;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSaved;
    }

    private void OnDisable()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSaving;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSaved;
    }

    private void OnSaving(Scene scene, string path)
    {
        if (this != null && scene == gameObject.scene) SetLight(false);
    }

    private void OnSaved(Scene scene)
    {
        if (this != null && scene == gameObject.scene && !Application.isPlaying) SetLight(true);
    }

    private void SetLight(bool on)
    {
        var light = GetComponent<Light2D>();
        if (light != null) light.enabled = on;
    }
#endif
}
