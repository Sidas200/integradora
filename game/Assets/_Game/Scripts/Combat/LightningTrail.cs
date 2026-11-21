using System.Collections.Generic;
using UnityEngine;

// Rayo del Paso de Rayo (la esquiva que da Aoi): una línea quebrada que va de donde empezó la
// esquiva hasta Tsuki, cambia de forma muy rápido y se apaga al terminar. Mientras sigue a Tsuki
// lleva una chispa brillante en la punta (Tsuki convertido en rayo).
// Va en los colores de Tsuki: núcleo rosa pálido, brillo carmesí y un halo morado. Se arma con
// sprites de un pixel estirados. Bolt / Burst hacen rayos sueltos (destellos al salir y al llegar).
public class LightningTrail : MonoBehaviour
{
    private const string MaterialResource = "Environment/SkyUnlit";
    private const float FadeTime = 0.24f;
    private const float ReshapeEvery = 0.035f;

    private static Sprite pixel;

    private readonly List<SpriteRenderer> outer = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> glow = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> core = new List<SpriteRenderer>();
    private readonly List<Vector3> points = new List<Vector3>();

    private Material material;
    private Transform follow;
    private Vector3 followOffset, start, end;
    private Color glowColor, outerColor;
    private float width, jitter, reshapeTimer, fade = -1f;
    private string sortingLayer;
    private int sortingOrder;
    private SpriteRenderer head;

    // Rayo que sigue a Tsuki hasta Release(). scale: tamaño de Tsuki en unidades.
    public static LightningTrail Spawn(Vector3 start, Transform follow, Vector3 followOffset,
        Color glowColor, Color outerColor, float scale, SpriteRenderer owner)
    {
        var trail = Create(start, start, glowColor, outerColor, scale, owner);
        trail.follow = follow;
        trail.followOffset = followOffset;
        trail.head = trail.NewPiece("Head", 3);
        return trail;
    }

    // Rayo suelto de un punto a otro que se apaga solo
    public static LightningTrail Bolt(Vector3 from, Vector3 to, Color glowColor, Color outerColor, float scale, SpriteRenderer owner)
    {
        var trail = Create(from, to, glowColor, outerColor, scale * 0.7f, owner);
        trail.fade = 0f;
        return trail;
    }

    // Varios rayos cortos que salen de un punto hacia todos lados
    public static void Burst(Vector3 center, int count, float length, Color glowColor, Color outerColor, float scale, SpriteRenderer owner)
    {
        float baseAngle = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float a = (baseAngle + i * 360f / count + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Bolt(center, center + dir * length * Random.Range(0.6f, 1.1f), glowColor, outerColor, scale, owner);
        }
    }

    private static LightningTrail Create(Vector3 from, Vector3 to, Color glowColor, Color outerColor, float scale, SpriteRenderer owner)
    {
        var go = new GameObject("LightningTrail");
        var trail = go.AddComponent<LightningTrail>();
        trail.start = from;
        trail.end = to;
        trail.glowColor = glowColor;
        trail.outerColor = outerColor;
        trail.width = 0.06f * scale;
        trail.jitter = 0.14f * scale;
        var template = Resources.Load<Material>(MaterialResource);
        trail.material = template != null ? template : owner.sharedMaterial;
        trail.sortingLayer = owner.sortingLayerName;
        trail.sortingOrder = owner.sortingOrder + 1;
        return trail;
    }

    // Deja de seguir a Tsuki y se apaga
    public void Release()
    {
        if (follow != null) end = follow.position + followOffset;
        follow = null;
        if (fade < 0f) fade = 0f;
    }

    private void LateUpdate()
    {
        if (follow != null) end = follow.position + followOffset;

        reshapeTimer -= Time.deltaTime;
        if (reshapeTimer <= 0f)
        {
            reshapeTimer = ReshapeEvery;
            Reshape();
        }

        float alpha = 1f;
        if (fade >= 0f)
        {
            fade += Time.deltaTime;
            alpha = 1f - fade / FadeTime;
            if (alpha <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }
        Draw(alpha);
    }

    // Puntos nuevos con quiebres al azar (los extremos quedan fijos)
    private void Reshape()
    {
        points.Clear();
        Vector3 d = end - start;
        float length = d.magnitude;
        int n = Mathf.Clamp(Mathf.CeilToInt(length / (jitter * 1.6f)), 2, 40);
        Vector3 side = length > 0.0001f ? new Vector3(-d.y, d.x, 0f) / length : Vector3.up;
        for (int i = 0; i <= n; i++)
        {
            float offset = i == 0 || i == n ? 0f : Random.Range(-jitter, jitter);
            points.Add(start + d * ((float)i / n) + side * offset);
        }
    }

    private void Draw(float alpha)
    {
        int segments = Mathf.Max(0, points.Count - 1);
        var outerC = new Color(outerColor.r, outerColor.g, outerColor.b, 0.25f * alpha);
        var glowC = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f * alpha);
        var coreC = Color.Lerp(new Color(1f, 0.92f, 0.96f), glowColor, 0.15f);
        coreC.a = alpha;

        for (int i = 0; i < Mathf.Max(segments, glow.Count); i++)
        {
            if (i >= segments)
            {
                outer[i].enabled = false;
                glow[i].enabled = false;
                core[i].enabled = false;
                continue;
            }
            if (i >= glow.Count)
            {
                outer.Add(NewPiece("Outer", 0));
                glow.Add(NewPiece("Glow", 1));
                core.Add(NewPiece("Core", 2));
            }
            Place(outer[i], points[i], points[i + 1], width * 7f, outerC);
            Place(glow[i], points[i], points[i + 1], width * 3.5f, glowC);
            Place(core[i], points[i], points[i + 1], width, coreC);
        }

        // La punta: Tsuki hecha rayo
        if (head != null)
        {
            head.enabled = follow != null;
            if (head.enabled)
            {
                head.transform.position = end;
                head.transform.rotation = Quaternion.Euler(0f, 0f, 45f + Random.Range(-15f, 15f));
                head.transform.localScale = Vector3.one * width * Random.Range(2.2f, 3.4f);
                head.color = coreC;
            }
        }
    }

    private SpriteRenderer NewPiece(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Pixel();
        sr.sharedMaterial = material;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder + order;
        return sr;
    }

    private static void Place(SpriteRenderer sr, Vector3 a, Vector3 b, float w, Color c)
    {
        sr.enabled = true;
        Vector3 d = b - a;
        sr.transform.position = (a + b) * 0.5f;
        sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        // Un poco más largo para que los tramos se unan sin huecos en los quiebres
        sr.transform.localScale = new Vector3(d.magnitude + w * 0.5f, w, 1f);
        sr.color = c;
    }

    // Sprite blanco de 1x1 que mide una unidad
    private static Sprite Pixel()
    {
        if (pixel != null) return pixel;
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        pixel = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return pixel;
    }
}
