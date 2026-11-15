using UnityEngine;
using UnityEngine.Rendering.Universal;

// Parpadeo suave para fuegos (fogata, forja, braseros). Usa ruido para que no se vea mecánico.
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float amount = 0.15f;
    [SerializeField] private float speed = 6f;

    private Light2D light2D;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        light2D.intensity = baseIntensity * (1f + n * amount);
    }
}
