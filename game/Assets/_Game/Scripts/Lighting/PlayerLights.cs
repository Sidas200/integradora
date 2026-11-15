using UnityEngine;
using UnityEngine.Rendering.Universal;

// Luces de Tsuki: un halo suave que lo sigue y el brillo carmesí de la espada.
// El brillo solo aparece con la espada y es más fuerte después de la mejora en la forja.
public class PlayerLights : MonoBehaviour
{
    [SerializeField] private Light2D aura;
    [SerializeField] private Light2D swordGlow;

    [Header("Brillo de la espada")]
    [SerializeField] private float baseIntensity = 0.35f;
    [SerializeField] private float baseRadius = 0.3f;
    [SerializeField] private float upgradedIntensity = 0.95f;
    [SerializeField] private float upgradedRadius = 0.6f;
    [SerializeField] private float pulseSpeed = 3f;

    private PlayerController controller;
    private PlayerStats stats;

    public void Setup(Light2D auraLight, Light2D glowLight)
    {
        aura = auraLight;
        swordGlow = glowLight;
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        if (aura != null && ZoneLighting.Current != null)
            aura.intensity = ZoneLighting.Current.PlayerLightIntensity;
    }

    private void Update()
    {
        if (swordGlow == null) return;
        bool hasSword = controller == null || controller.AttackEnabled;
        swordGlow.enabled = hasSword;
        if (!hasSword) return;

        bool upgraded = stats != null && stats.SwordLevel >= 1;
        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * pulseSpeed);
        swordGlow.intensity = (upgraded ? upgradedIntensity : baseIntensity) * pulse;
        swordGlow.pointLightOuterRadius = upgraded ? upgradedRadius : baseRadius;
    }
}
