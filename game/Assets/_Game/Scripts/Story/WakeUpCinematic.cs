using System.Collections;
using UnityEngine;

// Inicio del juego en la Isla de las Flores: Tsuki aparece tirada entre las flores, la cámara
// la enfoca de cerca, se levanta y la cámara regresa a su tamaño normal. Solo la primera vez
// (mientras no exista el flag); el diálogo de despertar (StoryTrigger) espera a que termine.
public class WakeUpCinematic : MonoBehaviour
{
    [Tooltip("Se reproduce mientras no exista este flag (el mismo que activa el diálogo de despertar).")]
    [SerializeField] private string skipIfFlag = "woke_up";

    [Header("Frames")]
    [SerializeField, Min(1)] private int frameCount = 18;
    [SerializeField] private float fps = 8f;
    [Tooltip("Pixeles vacíos debajo de los pies en cada frame de la hoja.")]
    [SerializeField, Min(0)] private int feetPadding = 0;

    [Header("Tiempos")]
    [Tooltip("Segundos tirada en el piso antes de empezar a levantarse (incluye el fundido de entrada).")]
    [SerializeField] private float lieDuration = 1.6f;
    [SerializeField] private float holdAfter = 0.5f;

    [Header("Cámara")]
    [Tooltip("Tamaño de la cámara respecto al normal (menor = más cerca).")]
    [SerializeField, Range(0.3f, 1f)] private float zoom = 0.55f;
    [Tooltip("Segundos en alejarse al terminar.")]
    [SerializeField] private float zoomOutTime = 1.2f;

    // StoryTrigger no arranca el diálogo de despertar mientras esto sea true
    public static bool Playing { get; private set; }

    private IEnumerator Start()
    {
        if (!string.IsNullOrEmpty(skipIfFlag) && StoryFlags.Has(skipIfFlag)) yield break;
        var player = SceneContext.FindPlayer();
        var controller = player != null ? player.GetComponent<PlayerController>() : null;
        var body = controller != null ? controller.GetComponentInChildren<SpriteRenderer>() : null;
        var frames = TsukiSheet.Load(TsukiSheet.Wake, frameCount, body, feetPadding);
        if (controller == null || frames == null) yield break;

        Playing = true;
        GameInput.Lock(this);
        controller.StopActions();
        controller.FaceTowards(Vector2.down);
        controller.HoldPose(frames[0]);

        // SceneContext acomoda la cámara en su primer frame; el enfoque va después
        yield return null;
        Vector2 focus = body.bounds.center;
        CameraFocus.Show(() => focus, zoom, 0.05f);

        yield return new WaitForSecondsRealtime(lieDuration);
        while (SceneLoader.Instance != null && SceneLoader.Instance.IsBusy) yield return null;

        float frameTime = 1f / Mathf.Max(1f, fps);
        for (int i = 1; i < frames.Length; i++)
        {
            controller.HoldPose(frames[i]);
            yield return new WaitForSecondsRealtime(frameTime);
        }
        yield return new WaitForSecondsRealtime(holdAfter);

        controller.HoldPose(null);
        controller.FaceTowards(Vector2.down);
        CameraFocus.Show(() => focus, zoom, zoomOutTime);
        CameraFocus.Release();
        while (CameraFocus.Active) yield return null;

        Finish();
    }

    private void Finish()
    {
        GameInput.Unlock(this);
        Playing = false;
    }

    private void OnDestroy()
    {
        if (Playing) Finish();
    }
}
