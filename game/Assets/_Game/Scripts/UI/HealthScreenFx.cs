using UnityEngine;
using UnityEngine.UI;

// Bordes de pantalla según la vida de Tsuki (legible sin sonido):
// - se enrojecen poco a poco conforme pierde vida, sin tapar el centro;
// - con 10 de vida o menos laten en rojo, dejando ver el juego;
// - al curarse destellan en verde.
// Lo crea GameHud. Va debajo del HUD (orden 39; el tinte de esquiva usa 40 y la UI 50) y no bloquea clics.
public class HealthScreenFx : MonoBehaviour
{
    private const int LowHealth = 10;
    private const float MaxDamageAlpha = 0.45f;   // con la vida casi en 0 (sin contar el latido)
    private const float FadeSpeed = 0.6f;         // alfa por segundo al subir o bajar el rojo
    private const float LowPulseMin = 0.3f;
    private const float LowPulseMax = 0.7f;
    private const float LowPulseRate = 1.2f;      // latidos por segundo
    private const float HealTime = 0.9f;
    private const float HealAlpha = 0.5f;

    private static readonly Color DamageColor = new Color(0.75f, 0.02f, 0.02f);
    private static readonly Color HealColor = new Color(0.3f, 1f, 0.4f);
    private static Sprite vignette;

    private Image damage;
    private Image heal;
    private Health health;
    private bool visible = true;
    private bool snap;
    private float shown;
    private float pulsePhase;
    private float healTimer;

    public static HealthScreenFx Create(Transform parent)
    {
        var go = new GameObject("HealthScreenFx", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 39;

        var fx = go.AddComponent<HealthScreenFx>();
        fx.damage = fx.AddLayer("Damage", DamageColor);
        fx.heal = fx.AddLayer("Heal", HealColor);
        return fx;
    }

    public void Bind(Health target)
    {
        if (health != null) health.Healed -= HandleHealed;
        health = target;
        if (health != null) health.Healed += HandleHealed;
        healTimer = 0f;
        snap = true; // al cambiar de escena se muestra la vida actual sin transición
    }

    public void SetVisible(bool value) => visible = value;

    private void OnDestroy() => Bind(null);

    private void HandleHealed(int amount) => healTimer = HealTime;

    private void Update()
    {
        bool alive = visible && health != null && !health.IsDead;
        float target = 0f;
        bool low = false;
        if (alive)
        {
            target = MaxDamageAlpha * Mathf.Pow(1f - health.Normalized, 1.4f);
            low = health.CurrentHealth <= LowHealth;
        }

        // Tiempo escalado: en pausa se congela y el destello verde se ve al volver al juego
        float dt = Time.deltaTime;
        shown = snap ? target : Mathf.MoveTowards(shown, target, FadeSpeed * dt);
        snap = false;

        float alpha = shown;
        if (low)
        {
            pulsePhase += dt * LowPulseRate;
            float beat = 0.5f - 0.5f * Mathf.Cos(pulsePhase * Mathf.PI * 2f);
            alpha = Mathf.Max(shown, Mathf.Lerp(LowPulseMin, LowPulseMax, beat));
        }
        else pulsePhase = 0f;
        SetAlpha(damage, alpha);

        if (healTimer > 0f) healTimer -= dt;
        float k = alive ? Mathf.Clamp01(healTimer / HealTime) : 0f;
        SetAlpha(heal, HealAlpha * k * k);
    }

    private Image AddLayer(string name, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        image.rectTransform.SetParent(transform, false);
        Stretch(image.rectTransform);
        image.sprite = Vignette();
        image.color = color;
        image.raycastTarget = false;
        SetAlpha(image, 0f);
        return image;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        var c = image.color;
        c.a = alpha;
        image.color = c;
        image.enabled = alpha > 0.001f;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Degradado radial: transparente en el centro y opaco en las orillas y esquinas
    private static Sprite Vignette()
    {
        if (vignette != null) return vignette;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f) / size * 2f - 1f;
            float dy = (y + 0.5f) / size * 2f - 1f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d));
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        vignette = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return vignette;
    }
}
