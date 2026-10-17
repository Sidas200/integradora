using UnityEngine;

// Ajustes de luz de la escena que dependen de la zona (los lee la luz que sigue a Tsuki).
public class ZoneLighting : MonoBehaviour
{
    public static ZoneLighting Current { get; private set; }

    [Tooltip("Intensidad de la luz suave alrededor de Tsuki en esta zona")]
    [SerializeField, Range(0f, 1.5f)] private float playerLightIntensity = 0.3f;

    public float PlayerLightIntensity => playerLightIntensity;

    public void Setup(float playerIntensity) => playerLightIntensity = playerIntensity;

    private void Awake() => Current = this;

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }
}
