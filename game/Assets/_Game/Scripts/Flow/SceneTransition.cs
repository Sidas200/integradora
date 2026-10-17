using UnityEngine;

// Trigger de salida: al entrar el jugador, hace fade y lo lleva a otra escena (o a otro
// punto de la misma escena si targetScene está vacío, p. ej. entrar a la cabaña).
[RequireComponent(typeof(Collider2D))]
public class SceneTransition : MonoBehaviour
{
    [Tooltip("Ruta de la escena destino. Vacío = mismo escenario (teletransporte con fade).")]
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnId;
    [Tooltip("Nombre que se muestra en el aviso, p. ej. \"Campamento\".")]
    [SerializeField] private string destinationName;

    [Header("Condición opcional (flag de historia)")]
    [Tooltip("Si no está vacío, solo se puede usar cuando este flag está activo.")]
    [SerializeField] private string requiredFlag;
    [TextArea] [SerializeField] private string blockedMessage;

    private float cooldownUntil;

    public void Configure(string scene, string spawnId, string destination, string flag = null, string blocked = null)
    {
        targetScene = scene;
        targetSpawnId = spawnId;
        destinationName = destination;
        requiredFlag = flag;
        blockedMessage = blocked;
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        // Evita rebotar si el jugador aparece encima del trigger
        cooldownUntil = Time.time + 0.6f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < cooldownUntil) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (SceneLoader.Instance == null || SceneLoader.Instance.IsBusy) return;

        if (!string.IsNullOrEmpty(requiredFlag) && !StoryFlags.Has(requiredFlag))
        {
            if (!string.IsNullOrEmpty(blockedMessage) && GameHud.Instance != null)
                GameHud.Instance.ShowToast(blockedMessage);
            cooldownUntil = Time.time + 1.5f;
            return;
        }

        cooldownUntil = Time.time + 1f;
        if (string.IsNullOrEmpty(targetScene))
            SceneLoader.Instance.TeleportInScene(targetSpawnId);
        else
            SceneLoader.Instance.LoadScene(targetScene, targetSpawnId);
    }
}
