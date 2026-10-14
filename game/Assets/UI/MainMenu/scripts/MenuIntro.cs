using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuIntro : MonoBehaviour
{
    public enum Side { Izquierda, Derecha, Arriba, Abajo, SoloFade }

    [System.Serializable]
    public class Elemento
    {
        public RectTransform objeto;
        public Side entraDesde = Side.Izquierda;
        public float retraso = 0f;
        public float duracion = 0.6f;
        public bool rebote = false;
    }

    public Elemento[] elementos;
    public Button[] botones;
    public float margenExtra = 150f;

    [Header("Salida")]
    public float duracionSalidaFactor = 0.6f; // 0.6 = sale más rápido de lo que entró
    public float escalonSalida = 0.4f;        // separación entre elementos al salir

    Vector2[] destinos;

    void OnEnable()
    {
        StartCoroutine(Reproducir());
    }

    // ---------- ENTRADA ----------
    IEnumerator Reproducir()
    {
        Canvas.ForceUpdateCanvases();
        Rect c = GetComponent<RectTransform>().rect;

        if (destinos == null)
        {
            destinos = new Vector2[elementos.Length];
            for (int i = 0; i < elementos.Length; i++)
                if (elementos[i].objeto != null)
                    destinos[i] = elementos[i].objeto.anchoredPosition;
        }

        foreach (var b in botones) b.interactable = false;

        float total = 0f;
        for (int i = 0; i < elementos.Length; i++)
        {
            var e = elementos[i];
            if (e.objeto == null) continue;

            Vector2 destino = destinos[i];
            Vector2 inicio = destino + Offset(e.entraDesde, c);

            var cg = ObtenerGrupo(e.objeto);
            e.objeto.anchoredPosition = inicio;
            cg.alpha = 0f;

            StartCoroutine(Animar(e, cg, inicio, destino));
            total = Mathf.Max(total, e.retraso + e.duracion);
        }

        yield return new WaitForSecondsRealtime(total);
        foreach (var b in botones) b.interactable = true;
    }

    IEnumerator Animar(Elemento e, CanvasGroup cg, Vector2 inicio, Vector2 destino)
    {
        yield return new WaitForSecondsRealtime(e.retraso);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, e.duracion);
            float k = Mathf.Clamp01(t);
            float f = e.rebote ? EaseOutBack(k) : EaseOutCubic(k);

            e.objeto.anchoredPosition = Vector2.LerpUnclamped(inicio, destino, f);
            cg.alpha = Mathf.Clamp01(k * 2f);
            yield return null;
        }

        e.objeto.anchoredPosition = destino;
        cg.alpha = 1f;
    }

    // ---------- SALIDA ----------
    // Devuelve cuántos segundos dura la animación de salida
    public float Salir()
    {
        if (destinos == null) return 0f;

        StopAllCoroutines();
        foreach (var b in botones) b.interactable = false;

        Rect c = GetComponent<RectTransform>().rect;

        float maxRetraso = 0f;
        foreach (var e in elementos) maxRetraso = Mathf.Max(maxRetraso, e.retraso);

        float total = 0f;
        for (int i = 0; i < elementos.Length; i++)
        {
            var e = elementos[i];
            if (e.objeto == null) continue;

            var cg = ObtenerGrupo(e.objeto);
            float espera = (maxRetraso - e.retraso) * escalonSalida;
            float dur = Mathf.Max(0.15f, e.duracion * duracionSalidaFactor);
            Vector2 desde = e.objeto.anchoredPosition;
            Vector2 hasta = destinos[i] + Offset(e.entraDesde, c);

            StartCoroutine(AnimarSalida(e, cg, desde, hasta, espera, dur));
            total = Mathf.Max(total, espera + dur);
        }
        return total;
    }

    IEnumerator AnimarSalida(Elemento e, CanvasGroup cg, Vector2 desde, Vector2 hasta, float espera, float dur)
    {
        yield return new WaitForSecondsRealtime(espera);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.Clamp01(t);
            float f = k * k * k; // EaseIn: arranca lento y acelera

            e.objeto.anchoredPosition = Vector2.LerpUnclamped(desde, hasta, f);
            cg.alpha = 1f - Mathf.Clamp01((k - 0.5f) * 2f); // se desvanece en la segunda mitad
            yield return null;
        }

        e.objeto.anchoredPosition = hasta;
        cg.alpha = 0f;
    }

    // ---------- Ayudas ----------
    CanvasGroup ObtenerGrupo(RectTransform rt)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    Vector2 Offset(Side lado, Rect c)
    {
        switch (lado)
        {
            case Side.Izquierda: return new Vector2(-(c.width + margenExtra), 0);
            case Side.Derecha:   return new Vector2(c.width + margenExtra, 0);
            case Side.Arriba:    return new Vector2(0, c.height + margenExtra);
            case Side.Abajo:     return new Vector2(0, -(c.height + margenExtra));
            default:             return Vector2.zero;
        }
    }

    static float EaseOutCubic(float x) { return 1f - Mathf.Pow(1f - x, 3f); }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}