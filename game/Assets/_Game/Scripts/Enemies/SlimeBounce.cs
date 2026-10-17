using UnityEngine;

// Animación del slime: se aplasta y se estira al moverse, y respira quieto.
public class SlimeBounce : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float speed = 7f;

    private Rigidbody2D rb;
    private float phase;

    public void Setup(Transform visualTransform) => visual = visualTransform;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void Update()
    {
        if (visual == null) return;
        bool moving = rb != null && rb.linearVelocity.sqrMagnitude > 0.01f;
        phase += Time.deltaTime * (moving ? speed : 2.5f);
        float k = Mathf.Sin(phase) * (moving ? 0.14f : 0.05f);
        visual.localScale = new Vector3(1f + k, 1f - k, 1f);
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
        {
            var s = visual.localScale;
            s.x = Mathf.Abs(s.x) * (rb.linearVelocity.x < 0f ? -1f : 1f);
            visual.localScale = s;
        }
    }
}
