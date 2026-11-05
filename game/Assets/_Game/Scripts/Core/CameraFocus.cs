using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Enfoque de cámara para momentos de historia (Varn despierta, saca la espada, choque de poder):
// la cámara deja de seguir a Tsuki, va hacia un punto y se acerca. Al soltarla regresa sola a
// Tsuki y devuelve el control a CameraFollow y a la Pixel Perfect Camera (que fija el tamaño, por
// eso se apaga mientras dura). Corre con tiempo sin escala: sigue moviéndose durante un diálogo.
[DefaultExecutionOrder(900)] // después de CameraFollow, antes de CameraShake (1000)
public class CameraFocus : MonoBehaviour
{
    private static CameraFocus instance;

    private Camera cam;
    private CameraFollow follow;
    private PixelPerfectCamera pixelPerfect;
    private bool followWasEnabled, pixelPerfectWasEnabled;
    private bool active;
    private bool releasing;
    private float baseSize;
    private float amount;   // 0 = cámara normal, 1 = enfocada
    private float zoom = 1f;
    private float time = 0.6f;
    private Func<Vector2> target;

    // Mientras dura el enfoque la cámara sigue al punto que devuelve target.
    // zoom: tamaño respecto al normal (menor = más cerca). time: segundos en acercarse.
    public static void Show(Func<Vector2> target, float zoom, float time = 0.6f)
    {
        var cam = Camera.main;
        if (cam == null || target == null) return;
        if (instance == null || instance.cam != cam)
        {
            instance = cam.GetComponent<CameraFocus>();
            if (instance == null) instance = cam.gameObject.AddComponent<CameraFocus>();
            instance.cam = cam;
        }
        instance.Begin(target, zoom, time);
    }

    // Regresa a Tsuki con la misma rapidez con que se acercó
    public static void Release()
    {
        if (instance != null && instance.active) instance.releasing = true;
    }

    public static bool Active => instance != null && instance.active;

    private void Begin(Func<Vector2> focus, float zoomTo, float seconds)
    {
        target = focus;
        zoom = zoomTo;
        time = Mathf.Max(0.05f, seconds);
        releasing = false;
        if (active) return;

        follow = cam.GetComponent<CameraFollow>();
        pixelPerfect = cam.GetComponent<PixelPerfectCamera>();
        followWasEnabled = follow != null && follow.enabled;
        pixelPerfectWasEnabled = pixelPerfect != null && pixelPerfect.enabled;
        if (follow != null) follow.enabled = false;
        if (pixelPerfect != null) pixelPerfect.enabled = false;
        baseSize = cam.orthographicSize;
        amount = 0f;
        active = true;
    }

    private void LateUpdate()
    {
        if (!active) return;
        float dt = Time.unscaledDeltaTime;
        amount = Mathf.MoveTowards(amount, releasing ? 0f : 1f, dt / time);

        // Al soltar, el punto vuelve hacia Tsuki para que CameraFollow retome sin salto
        Vector2 home = HomePosition();
        Vector2 focus = target != null ? target() : home;
        float k = Mathf.SmoothStep(0f, 1f, amount);
        Vector2 goal = Vector2.Lerp(home, focus, k);
        Vector3 p = cam.transform.position;
        Vector2 next = Vector2.Lerp(p, goal, 1f - Mathf.Exp(-8f * dt));
        cam.transform.position = new Vector3(next.x, next.y, p.z);
        cam.orthographicSize = baseSize * Mathf.Lerp(1f, zoom, k);

        if (releasing && amount <= 0f) End();
    }

    private Vector2 HomePosition()
    {
        if (follow != null && follow.Target != null) return follow.Target.position + follow.Offset;
        return cam.transform.position;
    }

    private void End()
    {
        active = false;
        releasing = false;
        target = null;
        cam.orthographicSize = baseSize;
        if (pixelPerfect != null) pixelPerfect.enabled = pixelPerfectWasEnabled;
        if (follow != null) follow.enabled = followWasEnabled;
    }

    // Un diálogo cancelado por cambio de escena no llama a Release: al cargar otra escena se suelta
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (active) End();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (active) End();
    }
}
