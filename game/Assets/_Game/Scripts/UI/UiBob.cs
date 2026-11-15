using UnityEngine;

// Sube y baja un adorno en pasos de pixel (flecha de "continuar" del diálogo). Tiempo sin escala:
// el diálogo detiene el juego.
public class UiBob : MonoBehaviour
{
    [SerializeField] private float amplitude = 4f;
    [SerializeField] private float speed = 4f;
    [SerializeField] private float step = 2f;

    private RectTransform rt;
    private Vector2 basePos;

    private void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
    }

    public void Setup(float pixelStep)
    {
        step = pixelStep;
        if (rt == null) rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Round(y / step) * step);
    }
}
