using UnityEngine;

// Temblor de cámara para impactos. Se aplica después de CameraFollow (LateUpdate tardío)
// y se retira en Update, así el seguimiento nunca acumula el desplazamiento.
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;

    private Vector3 applied;
    private float timeLeft;
    private float duration;
    private float strength;

    public static void Shake(float strength, float duration)
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (instance == null || instance.gameObject != cam.gameObject)
        {
            instance = cam.GetComponent<CameraShake>();
            if (instance == null) instance = cam.gameObject.AddComponent<CameraShake>();
        }
        if (strength >= instance.strength * (instance.timeLeft / Mathf.Max(instance.duration, 0.01f)))
        {
            instance.strength = strength;
            instance.duration = duration;
            instance.timeLeft = duration;
        }
    }

    private void Update()
    {
        transform.position -= applied;
        applied = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f) return;
        timeLeft -= Time.unscaledDeltaTime;
        float k = strength * Mathf.Clamp01(timeLeft / duration);
        applied = (Vector3)(Random.insideUnitCircle * k);
        transform.position += applied;
    }
}
