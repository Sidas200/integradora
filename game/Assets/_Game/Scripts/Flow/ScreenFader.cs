using System.Collections;
using UnityEngine;

// Fundido a negro de pantalla completa. Usa tiempo sin escala para funcionar en pausa.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 0.35f;

    private CanvasGroup group;

    public bool IsOpaque => group != null && group.alpha >= 0.99f;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public IEnumerator FadeOut(float duration = -1f) => Fade(1f, duration);

    public IEnumerator FadeIn(float duration = -1f) => Fade(0f, duration);

    public void SetOpaque()
    {
        group.alpha = 1f;
        group.blocksRaycasts = true;
    }

    private IEnumerator Fade(float target, float duration)
    {
        if (duration < 0f) duration = defaultDuration;
        group.blocksRaycasts = true;
        float start = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        group.alpha = target;
        group.blocksRaycasts = target > 0.5f;
    }
}
