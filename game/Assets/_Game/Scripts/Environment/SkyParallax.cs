using System.Collections.Generic;
using UnityEngine;

// Cielo pixelado pegado a la cámara y tres capas de nubes con parallax:
// dos detrás de la isla (siguen casi por completo a la cámara, las más lejanas más) y una delante
// (se mueve más rápido que el mundo). No hay nubes "en medio" de la isla.
// Corre después de CameraFollow para no temblar.
[DefaultExecutionOrder(100)]
public class SkyParallax : MonoBehaviour
{
    private const string SkyResource = "Environment/sky_day";
    private const string CloudResource = "Environment/cloud_";
    private const string MaterialResource = "Environment/SkyUnlit";

    private class Layer
    {
        public float follow;        // 1 = pegada a la cámara, 0 = fija al mundo, < 0 = delante del mundo
        public float speed;         // deriva horizontal (unidades/s)
        public Vector2 margin;      // cuánto puede salir de la vista antes de dar la vuelta
        public bool foreground;
        public float alpha;
        public readonly List<Cloud> clouds = new List<Cloud>();
    }

    private class Cloud
    {
        public SpriteRenderer sr;
        public Vector2 local;
        public float speedMul;
    }

    private OutdoorAmbience.Profile profile;
    private Camera cam;
    private SpriteRenderer sky;
    private Vector3 camStart;
    private readonly List<Layer> layers = new List<Layer>();

    public void Setup(OutdoorAmbience.Profile p) => profile = p;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null) { enabled = false; return; }
        camStart = cam.transform.position;

        var material = Resources.Load<Material>(MaterialResource);
        var skySprite = Resources.Load<Sprite>(SkyResource);
        var cloudSprites = new List<Sprite>();
        for (int i = 1; ; i++)
        {
            var s = Resources.Load<Sprite>(CloudResource + i);
            if (s == null) break;
            cloudSprites.Add(s);
        }

        if (skySprite != null)
        {
            sky = NewRenderer("Sky", skySprite, material, "Background", -300);
            sky.color = profile.skyTint;
        }
        if (cloudSprites.Count == 0) return;

        var rng = new System.Random(gameObject.scene.name.GetHashCode());
        // Lejanas: chicas, lentas, casi pegadas a la cámara
        AddLayer(rng, cloudSprites, material, count: 5, follow: 0.9f, speed: 0.04f, scale: new Vector2(0.18f, 0.3f),
            alpha: 0.75f, order: -280, foreground: false, margin: new Vector2(1.6f, 1f));
        // Cercanas detrás de la isla
        AddLayer(rng, cloudSprites, material, count: 4, follow: 0.65f, speed: 0.08f, scale: new Vector2(0.32f, 0.5f),
            alpha: 0.95f, order: -260, foreground: false, margin: new Vector2(2.2f, 1.4f));
        // Delante de la isla: pocas, grandes y rápidas; aparecen de vez en cuando
        AddLayer(rng, cloudSprites, material, count: 2, follow: -0.35f, speed: 0.22f, scale: new Vector2(0.6f, 0.85f),
            alpha: 0.8f, order: -10, foreground: true, margin: new Vector2(6f, 2.5f));

        LateUpdate();
    }

    private void AddLayer(System.Random rng, List<Sprite> sprites, Material material, int count, float follow, float speed,
        Vector2 scale, float alpha, int order, bool foreground, Vector2 margin)
    {
        var layer = new Layer { follow = follow, speed = speed, margin = margin, foreground = foreground, alpha = alpha };
        Vector2 half = HalfSpan(layer);
        Vector2 cam2 = cam.transform.position;
        for (int i = 0; i < count; i++)
        {
            var sprite = sprites[rng.Next(sprites.Count)];
            // Las nubes de adelante van en la capa UI (por encima del mundo, debajo del HUD y de las etiquetas)
            var sr = NewRenderer($"Cloud_{(foreground ? "Front" : "Back")}_{i}", sprite, material, foreground ? "UI" : "Background", order + i);
            float s = Mathf.Lerp(scale.x, scale.y, (float)rng.NextDouble());
            sr.transform.localScale = new Vector3(rng.NextDouble() < 0.5 ? -s : s, s, 1f);
            // Repartidas a lo ancho para que no aparezcan todas juntas
            var rel = new Vector2(Mathf.Lerp(-half.x, half.x, (i + (float)rng.NextDouble()) / count),
                                  Mathf.Lerp(-half.y, half.y, (float)rng.NextDouble()));
            layer.clouds.Add(new Cloud { sr = sr, local = rel + cam2 * (1f - follow), speedMul = Mathf.Lerp(0.7f, 1.3f, (float)rng.NextDouble()) });
        }
        layers.Add(layer);
    }

    private SpriteRenderer NewRenderer(string name, Sprite sprite, Material material, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (material != null) sr.sharedMaterial = material; // sin luz: el cielo no se oscurece con las luces del mundo
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return sr;
    }

    private Vector2 View => new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);

    private Vector2 HalfSpan(Layer layer) => View + layer.margin;

    private void LateUpdate()
    {
        if (cam == null) return;
        Vector2 camPos = cam.transform.position;
        Vector2 view = View;

        if (sky != null)
        {
            // Cubre la vista con margen y se desplaza apenas, sin dejar ver sus bordes
            Vector2 size = sky.sprite.bounds.size;
            float scale = Mathf.Max(view.x * 2.2f / size.x, view.y * 2.2f / size.y);
            sky.transform.localScale = new Vector3(scale, scale, 1f);
            Vector2 room = size * scale * 0.5f - view;
            Vector2 drift = -(camPos - (Vector2)camStart) * 0.03f;
            drift.x = Mathf.Clamp(drift.x, -room.x, room.x);
            drift.y = Mathf.Clamp(drift.y, -room.y, room.y);
            sky.transform.position = new Vector3(camPos.x + drift.x, camPos.y + drift.y, 0f);
        }

        float dt = Time.deltaTime;
        foreach (var layer in layers)
        {
            Vector2 half = HalfSpan(layer);
            foreach (var c in layer.clouds)
            {
                c.local.x += layer.speed * c.speedMul * dt;
                // Posición relativa a la cámara; al salir por un lado vuelve a entrar por el otro
                Vector2 rel = camPos * layer.follow + c.local - camPos;
                if (rel.x > half.x) c.local.x -= half.x * 2f;
                else if (rel.x < -half.x) c.local.x += half.x * 2f;
                if (rel.y > half.y) c.local.y -= half.y * 2f;
                else if (rel.y < -half.y) c.local.y += half.y * 2f;

                Vector2 pos = camPos * layer.follow + c.local;
                c.sr.transform.position = new Vector3(pos.x, pos.y, 0f);

                float alpha = layer.alpha;
                if (layer.foreground)
                {
                    // Se vuelve casi transparente al pasar sobre Tsuki (centro de la vista) para no estorbar
                    Vector2 ext = c.sr.bounds.extents;
                    Vector2 d = pos - camPos;
                    float k = Mathf.Max(Mathf.Abs(d.x) / (ext.x + 0.4f), Mathf.Abs(d.y) / (ext.y + 0.4f));
                    alpha = Mathf.Lerp(0.25f, layer.alpha, Mathf.SmoothStep(0f, 1f, k));
                }
                var tint = profile.cloudTint;
                c.sr.color = new Color(tint.r, tint.g, tint.b, alpha);
            }
        }
    }
}
