using UnityEngine;
using UnityEngine.UI;

// Pétalos pixel que caen despacio dentro de un panel (pausa, derrota). Usa tiempo sin escala porque
// la pausa detiene el juego. Al salir por abajo vuelven a entrar por arriba.
public class UiPetals : MonoBehaviour
{
    private struct Petal
    {
        public RectTransform rt;
        public Image img;
        public float x, y, speed, sway, freq, spin, phase;
    }

    private Petal[] items;
    private RectTransform area;
    private Sprite[] sprites;
    private float scale = 3f;

    public void Setup(Sprite[] petalSprites, int count, float pixelScale)
    {
        sprites = petalSprites;
        scale = pixelScale;
        area = (RectTransform)transform;
        items = new Petal[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Petal", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(area, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            items[i].rt = rt;
            items[i].img = img;
            Respawn(ref items[i], true);
        }
    }

    private void Respawn(ref Petal p, bool anywhere)
    {
        var size = area.rect.size;
        var sprite = sprites[Random.Range(0, sprites.Length)];
        p.img.sprite = sprite;
        p.rt.sizeDelta = sprite.rect.size * scale;
        p.x = Random.Range(0.02f, 0.98f) * size.x;
        p.y = anywhere ? Random.Range(0.05f, 0.95f) * size.y : size.y - 4f;
        p.speed = Random.Range(14f, 30f);
        p.sway = Random.Range(10f, 26f);
        p.freq = Random.Range(0.5f, 1.1f);
        p.spin = Random.Range(-40f, 40f);
        p.phase = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (items == null) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        float t = Time.unscaledTime;
        var size = area.rect.size;
        for (int i = 0; i < items.Length; i++)
        {
            ref var p = ref items[i];
            p.y -= p.speed * dt;
            p.x += 6f * dt;
            if (p.y < 6f || p.x > size.x - 6f) { Respawn(ref p, false); continue; }
            float w = t * p.freq + p.phase;
            // aparece y desaparece cerca de los bordes de arriba y abajo
            float fade = Mathf.Clamp01(Mathf.Min(p.y / 40f, (size.y - p.y) / 40f));
            p.img.color = new Color(1f, 1f, 1f, 0.85f * fade);
            // posiciones redondeadas al pixel del arte para que no se vea borroso
            float px = Mathf.Round((p.x + Mathf.Sin(w) * p.sway) / scale) * scale;
            float py = Mathf.Round(p.y / scale) * scale;
            p.rt.anchoredPosition = new Vector2(px, py);
            p.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round((p.phase * 36f + t * p.spin) / 45f) * 45f);
        }
    }
}
