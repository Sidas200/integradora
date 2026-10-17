using UnityEngine;

// Movimiento suave de arriba abajo para cosas que flotan (espíritus, cristales, marcadores).
public class FloatBob : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.03f;
    [SerializeField] private float speed = 2f;

    private Vector3 basePosition;
    private float phase;

    private void Awake()
    {
        basePosition = transform.localPosition;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        transform.localPosition = basePosition + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
    }
}
