using UnityEngine;
using UnityEngine.UI;

// Pétalos de sakura que caen en espiral sobre el menú. Va en un RectTransform que ocupa toda la pantalla;
// cada pétalo baja meciéndose de lado, gira y "voltea" (escala X) para que parezca que da vueltas en el aire.
public class PetalFall : MonoBehaviour
{
    [SerializeField] private Sprite[] petals;
    [SerializeField] private int count = 26;
    [SerializeField] private Vector2 sizeRange = new Vector2(28f, 70f);
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(55f, 120f);

    private struct Petal
    {
        public RectTransform rt;
        public float x, y, speed, swayAmp, swayFreq, spin, flipFreq, phase;
    }

    private Petal[] items;
    private RectTransform area;

    public void Setup(Sprite[] sprites) => petals = sprites;

    private void Start()
    {
        area = (RectTransform)transform;
        if (petals == null || petals.Length == 0) return;
        items = new Petal[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Petal", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(area, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            items[i].rt = rt;
            Respawn(ref items[i], true);
        }
    }

    private void Respawn(ref Petal p, bool anywhere)
    {
        var size = area.rect.size;
        var img = p.rt.GetComponent<Image>();
        img.sprite = petals[Random.Range(0, petals.Length)];
        float s = Random.Range(sizeRange.x, sizeRange.y);
        float aspect = img.sprite.rect.width / img.sprite.rect.height;
        p.rt.sizeDelta = aspect >= 1f ? new Vector2(s, s / aspect) : new Vector2(s * aspect, s);
        // los más pequeños se ven más lejos: más transparentes y lentos
        float depth = Mathf.InverseLerp(sizeRange.x, sizeRange.y, s);
        img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, depth));

        p.x = Random.Range(-0.1f, 1.05f) * size.x;
        p.y = anywhere ? Random.Range(0f, size.y) : size.y + s;
        p.speed = Random.Range(fallSpeedRange.x, fallSpeedRange.y) * Mathf.Lerp(0.7f, 1f, depth);
        p.swayAmp = Random.Range(30f, 90f);
        p.swayFreq = Random.Range(0.4f, 0.9f);
        p.spin = Random.Range(-70f, 70f);
        p.flipFreq = Random.Range(0.6f, 1.6f);
        p.phase = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (items == null) return;
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        for (int i = 0; i < items.Length; i++)
        {
            ref var p = ref items[i];
            p.y -= p.speed * dt;
            p.x += 25f * dt; // brisa suave hacia la derecha
            if (p.y < -80f || p.x > area.rect.width + 120f)
            {
                Respawn(ref p, false);
                continue;
            }
            float w = t * p.swayFreq + p.phase;
            // espiral: vaivén lateral y un poco de ida y vuelta vertical
            p.rt.anchoredPosition = new Vector2(p.x + Mathf.Sin(w) * p.swayAmp, p.y + Mathf.Cos(w * 2f) * p.swayAmp * 0.15f);
            p.rt.localRotation = Quaternion.Euler(0f, 0f, p.phase * 36f + t * p.spin);
            float flip = Mathf.Cos(t * p.flipFreq + p.phase);
            p.rt.localScale = new Vector3(Mathf.Lerp(0.25f, 1f, Mathf.Abs(flip)), 1f, 1f);
        }
    }
}
