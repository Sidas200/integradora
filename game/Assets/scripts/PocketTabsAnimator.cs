using System.Collections;
using UnityEngine;

public class PocketTabsAnimator : MonoBehaviour
{
    [Header("Animación")]
    [SerializeField] private float hiddenOffsetX = 180f;

    [SerializeField] private float moveDuration = 0.22f;

    [SerializeField] private float delayBetweenTabs = 0.045f;

    [SerializeField] private float overshoot = 8f;

    private RectTransform[] tabs;

    private Vector2[] visiblePositions;
    private Vector2[] hiddenPositions;

    private Coroutine currentAnimation;

    private bool initialized = false;

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize()
    {
        int count = transform.childCount;

        tabs = new RectTransform[count];

        visiblePositions = new Vector2[count];
        hiddenPositions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform tab =
                transform.GetChild(i) as RectTransform;

            tabs[i] = tab;

            // Posición final visible fuera del mapa
            visiblePositions[i] =
                tab.anchoredPosition;

            // Posición escondida detrás del mapa
            hiddenPositions[i] =
                visiblePositions[i]
                + new Vector2(hiddenOffsetX, 0);

            tab.anchoredPosition =
                hiddenPositions[i];

            tab.localScale =
                Vector3.one;
        }

        initialized = true;
    }

    // =========================================================
    // SHOW
    // =========================================================

    public void ShowTabs()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation =
            StartCoroutine(
                ShowRoutine()
            );
    }

    private IEnumerator ShowRoutine()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            StartCoroutine(
                AnimateTabOut(i)
            );

            yield return new WaitForSecondsRealtime(
                delayBetweenTabs
            );
        }
    }

    private IEnumerator AnimateTabOut(
        int index
    )
    {
        RectTransform tab =
            tabs[index];

        Vector2 start =
            hiddenPositions[index];

        Vector2 final =
            visiblePositions[index];

        Vector2 overshootPosition =
            final +
            new Vector2(-overshoot, 0);

        tab.anchoredPosition =
            start;

        // =====================================================
        // SALE DESDE DETRÁS DEL MAPA
        // =====================================================

        float elapsed = 0f;

        float firstDuration =
            moveDuration * 0.78f;

        while (elapsed < firstDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / firstDuration
                );

            // Ease Out Cubic
            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );

            tab.anchoredPosition =
                Vector2.Lerp(
                    start,
                    overshootPosition,
                    eased
                );

            yield return null;
        }

        // =====================================================
        // PEQUEÑO REBOTE
        // =====================================================

        elapsed = 0f;

        float settleDuration =
            moveDuration * 0.22f;

        while (elapsed < settleDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / settleDuration
                );

            tab.anchoredPosition =
                Vector2.Lerp(
                    overshootPosition,
                    final,
                    t
                );

            yield return null;
        }

        tab.anchoredPosition =
            final;
    }

    // =========================================================
    // HIDE
    // =========================================================

    public void HideTabs()
    {
        if (!initialized)
            return;

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation =
            StartCoroutine(
                HideRoutine()
            );
    }

    private IEnumerator HideRoutine()
    {
        // Al cerrar entran en orden inverso
        for (
            int i = tabs.Length - 1;
            i >= 0;
            i--
        )
        {
            StartCoroutine(
                AnimateTabIn(i)
            );

            yield return
                new WaitForSecondsRealtime(
                    delayBetweenTabs * 0.7f
                );
        }
    }

    private IEnumerator AnimateTabIn(
        int index
    )
    {
        RectTransform tab =
            tabs[index];

        Vector2 start =
            tab.anchoredPosition;

        Vector2 end =
            hiddenPositions[index];

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / moveDuration
                );

            // Ease In Cubic
            float eased =
                t * t * t;

            tab.anchoredPosition =
                Vector2.Lerp(
                    start,
                    end,
                    eased
                );

            yield return null;
        }

        tab.anchoredPosition =
            end;
    }

    // =========================================================
    // HIDE INSTANTLY
    // =========================================================

    public void HideInstantly()
    {
        if (!initialized)
        {
            Initialize();
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].anchoredPosition =
                hiddenPositions[i];
        }
    }
}