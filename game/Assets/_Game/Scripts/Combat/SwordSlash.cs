using System.Collections.Generic;
using UnityEngine;

// Corte a distancia de Tsuki: sale de la espada, avanza en línea recta hasta su alcance,
// atraviesa a los enemigos y daña una sola vez a cada uno. Crece al salir y se desvanece al final.
// Se ve en capas: resplandor carmesí detrás, el arte rojo del corte y un filo blanco rosado encima;
// deja una estela de copias que se apagan y destella al pegarle a alguien.
public class SwordSlash : MonoBehaviour
{
    private const string MaterialResource = "Environment/SkyUnlit";
    private const float TrailInterval = 0.025f;
    private const float TrailLife = 0.16f;
    private static Sprite coreSprite, glowSprite;

    private static readonly Color GlowColor = new Color(1f, 0.12f, 0.35f, 0.55f);
    private static readonly Color CoreColor = new Color(1f, 0.88f, 0.94f, 1f);

    private readonly HashSet<IDamageable> hits = new HashSet<IDamageable>();
    private readonly List<SpriteRenderer> trail = new List<SpriteRenderer>();
    private SpriteRenderer body, core, glow;
    private Material material;
    private Color color;
    private Vector2 direction;
    private Vector2 hitSize;
    private float angle;
    private float range;
    private float speed;
    private float travelled;
    private float baseScale;
    private float flipY = 1f;
    private float trailTimer;
    private float hitFlash;
    private int damage;
    private LayerMask enemies;

    // sprite: el arte apunta hacia la derecha (la curva por delante). flip: media luna al revés
    // (el segundo corte del doble). owner: el sprite de Tsuki, para el material de respaldo.
    public static SwordSlash Spawn(Sprite sprite, Color color, SpriteRenderer owner, Vector2 position, Vector2 direction,
        float range, float speed, float width, int damage, LayerMask enemies, bool flip = false)
    {
        var go = new GameObject("SwordSlash");
        var slash = go.AddComponent<SwordSlash>();
        slash.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        slash.angle = Mathf.Atan2(slash.direction.y, slash.direction.x) * Mathf.Rad2Deg;
        slash.range = range;
        slash.speed = speed;
        slash.damage = damage;
        slash.enemies = enemies;
        slash.color = color;
        slash.flipY = flip ? -1f : 1f;
        // Caja de golpe: el ancho va de lado a lado del corte, el grosor en la dirección en que vuela
        slash.hitSize = new Vector2(width * 0.45f, width);

        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, slash.angle));
        var template = Resources.Load<Material>(MaterialResource);
        slash.material = template != null ? template : owner.sharedMaterial;

        if (sprite == null) sprite = CoreSprite();
        slash.glow = slash.Layer("Glow", GlowSprite(), 0, GlowColor);
        slash.body = slash.Layer("Body", sprite, 1, color);
        slash.core = slash.Layer("Core", CoreSprite(), 2, CoreColor);
        // El resplandor es más grande y alargado; el filo, más delgado y un poco adelante
        slash.glow.transform.localScale = new Vector3(0.9f, 1.5f, 1f) * (sprite.bounds.size.y / GlowSprite().bounds.size.y);
        float coreScale = sprite.bounds.size.y / CoreSprite().bounds.size.y * 0.85f;
        slash.core.transform.localScale = new Vector3(coreScale * 0.7f, coreScale, 1f);
        slash.core.transform.localPosition = new Vector3(sprite.bounds.size.x * 0.12f, 0f, 0f);

        slash.baseScale = width / Mathf.Max(0.01f, sprite.bounds.size.y);
        slash.ApplyScale(0.6f);
        slash.CheckHits();
        return slash;
    }

    private SpriteRenderer Layer(string name, Sprite sprite, int order, Color tint)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.sprite = sprite;
        sr.sharedMaterial = material;
        // Por encima del escenario (vuela sobre todo), debajo de los efectos del HUD
        sr.sortingLayerName = "UI";
        sr.sortingOrder = -16 + order;
        sr.color = tint;
        return sr;
    }

    private void ApplyScale(float grow) =>
        transform.localScale = new Vector3(baseScale * grow, baseScale * grow * flipY, 1f);

    private void Update()
    {
        float dt = Time.deltaTime;
        float step = speed * dt;
        travelled += step;
        transform.position += (Vector3)(direction * step);

        float k = Mathf.Clamp01(travelled / Mathf.Max(0.01f, range));
        // Crece rápido al salir y se desvanece en el último 35% del alcance
        float grow = Mathf.Lerp(0.6f, 1.05f, Mathf.Clamp01(k / 0.2f));
        ApplyScale(grow);
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, k));
        hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 6f);
        float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 50f);
        body.color = new Color(color.r, color.g, color.b, color.a * fade);
        core.color = Color.Lerp(CoreColor, Color.white, hitFlash) * new Color(1f, 1f, 1f, fade * flicker);
        glow.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, Mathf.Min(1f, GlowColor.a * fade * flicker + hitFlash * 0.4f));

        UpdateTrail(dt, fade);
        CheckHits();
        if (k >= 1f) Destroy(gameObject);
    }

    // Copias del corte que se quedan atrás y se apagan
    private void UpdateTrail(float dt, float fade)
    {
        trailTimer -= dt;
        if (trailTimer <= 0f && fade > 0.2f)
        {
            trailTimer = TrailInterval;
            var ghost = new GameObject("SlashTrail").AddComponent<SpriteRenderer>();
            ghost.transform.SetPositionAndRotation(body.transform.position, body.transform.rotation);
            ghost.transform.localScale = transform.localScale;
            ghost.sprite = body.sprite;
            ghost.sharedMaterial = material;
            ghost.sortingLayerID = body.sortingLayerID;
            ghost.sortingOrder = body.sortingOrder - 2;
            ghost.color = new Color(1f, 0.3f, 0.45f, 0.45f * fade);
            trail.Add(ghost);
            Destroy(ghost.gameObject, TrailLife);
        }
        for (int i = trail.Count - 1; i >= 0; i--)
        {
            var g = trail[i];
            if (g == null) { trail.RemoveAt(i); continue; }
            var c = g.color;
            c.a = Mathf.MoveTowards(c.a, 0f, dt * (0.45f / TrailLife));
            g.color = c;
        }
    }

    private void OnDestroy()
    {
        foreach (var g in trail)
            if (g != null) Destroy(g.gameObject);
    }

    private void CheckHits()
    {
        var found = Physics2D.OverlapBoxAll(transform.position, hitSize, angle, enemies);
        foreach (var col in found)
        {
            var target = col.GetComponentInParent<IDamageable>();
            if (target != null && hits.Add(target))
            {
                target.TakeDamage(damage);
                hitFlash = 1f;
                CameraShake.Shake(0.015f, 0.08f);
            }
        }
    }

    // Media luna blanca suave (filo del corte, se tiñe)
    private static Sprite CoreSprite()
    {
        if (coreSprite != null) return coreSprite;
        const int w = 24, h = 48;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                float u = (x + 0.5f) / w;
                float edge = 1f - dy * dy;
                // Más gruesa en el centro y fina en las puntas
                float thick = 0.32f * (1f - Mathf.Abs(dy) * 0.7f);
                bool inside = u <= edge && u >= edge - thick;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        coreSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return coreSprite;
    }

    // Resplandor redondo y suave (se estira detrás del corte)
    private static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 32;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return glowSprite;
    }
}
