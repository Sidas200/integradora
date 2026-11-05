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

    // Golpe al suelo: rebote vertical que se apaga rápido (se nota sin marear)
    private float impactTime = -1f;
    private float impactDuration;
    private float impactStrength;

    public static void Shake(float strength, float duration)
    {
        if (!Ensure()) return;
        if (strength >= instance.strength * (instance.timeLeft / Mathf.Max(instance.duration, 0.01f)))
        {
            instance.strength = strength;
            instance.duration = duration;
            instance.timeLeft = duration;
        }
    }

    // Impacto contra el suelo: la cámara baja de golpe y rebota un par de veces hasta quedarse quieta.
    // Más suave que Shake para golpes grandes que se repiten durante la pelea.
    public static void Impact(float strength, float duration)
    {
        if (!Ensure()) return;
        var s = instance;
        // Uno nuevo solo reemplaza al actual si es más fuerte que lo que queda de él
        float remaining = s.impactTime >= 0f ? s.impactStrength * Decay(s.impactTime / s.impactDuration) : 0f;
        if (strength < remaining) return;
        s.impactStrength = strength;
        s.impactDuration = Mathf.Max(0.05f, duration);
        s.impactTime = 0f;
    }

    private static bool Ensure()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        if (instance == null || instance.gameObject != cam.gameObject)
        {
            instance = cam.GetComponent<CameraShake>();
            if (instance == null) instance = cam.gameObject.AddComponent<CameraShake>();
        }
        return true;
    }

    private static float Decay(float k) => Mathf.Clamp01(1f - k) * Mathf.Clamp01(1f - k);

    private void Update()
    {
        transform.position -= applied;
        applied = Vector3.zero;
    }

    private void LateUpdate()
    {
        Vector3 offset = Vector3.zero;
        if (timeLeft > 0f)
        {
            timeLeft -= Time.unscaledDeltaTime;
            float k = strength * Mathf.Clamp01(timeLeft / duration);
            offset += (Vector3)(Random.insideUnitCircle * k);
        }
        if (impactTime >= 0f)
        {
            impactTime += Time.unscaledDeltaTime;
            float k = impactTime / impactDuration;
            if (k >= 1f) impactTime = -1f;
            else
            {
                // Baja primero (coseno desde -1) y rebota unas 3 veces; un poco de lado para que no se vea mecánico
                float a = impactStrength * Decay(k);
                float t = impactTime * Mathf.PI * 2f;
                offset += new Vector3(Mathf.Sin(t * 11f) * a * 0.18f, -Mathf.Cos(t * 7f) * a, 0f);
            }
        }
        applied = offset;
        transform.position += applied;
    }
}
