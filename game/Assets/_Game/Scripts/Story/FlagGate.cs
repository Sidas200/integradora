using UnityEngine;

// Paso bloqueado hasta que se activa un flag de historia (p. ej. el camino al campamento
// antes de conocer a Kaede). Al chocar muestra un aviso; con el flag se abre solo.
[RequireComponent(typeof(Collider2D))]
public class FlagGate : MonoBehaviour
{
    [SerializeField] private string requiredFlag;
    [TextArea] [SerializeField] private string message;

    private Collider2D blocker;
    private float nextMessageAt;

    public void Setup(string flag, string blockedMessage)
    {
        requiredFlag = flag;
        message = blockedMessage;
    }

    private void Awake() => blocker = GetComponent<Collider2D>();
    private void OnEnable() => StoryFlags.Changed += OnFlag;
    private void OnDisable() => StoryFlags.Changed -= OnFlag;
    private void Start() => Apply();

    private void OnFlag(string flag)
    {
        if (flag == requiredFlag) Apply();
    }

    private void Apply() => blocker.enabled = !StoryFlags.Has(requiredFlag);

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.unscaledTime < nextMessageAt) return;
        if (collision.collider.GetComponentInParent<PlayerController>() == null) return;
        nextMessageAt = Time.unscaledTime + 2f;
        if (GameHud.Instance != null && !string.IsNullOrEmpty(message)) GameHud.Instance.ShowToast(message);
    }
}
