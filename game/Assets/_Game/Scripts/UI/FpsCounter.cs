using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// RNF (≥ 30 FPS): contador de FPS que se muestra con F3. Promedia medio segundo.
public class FpsCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private float elapsed;
    private int frames;
    private bool visible;

    public float LastFps { get; private set; }

    public void Setup(TMP_Text text) => label = text;

    private void Awake()
    {
        if (label != null) label.gameObject.SetActive(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f3Key.wasPressedThisFrame && label != null)
        {
            visible = !visible;
            label.gameObject.SetActive(visible);
        }

        elapsed += Time.unscaledDeltaTime;
        frames++;
        if (elapsed < 0.5f) return;
        LastFps = frames / elapsed;
        elapsed = 0f;
        frames = 0;
        if (visible && label != null)
        {
            label.text = $"{LastFps:0} FPS";
            label.color = LastFps >= 30f ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.5f, 0.5f);
        }
    }
}
