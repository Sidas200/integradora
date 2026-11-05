using System.Collections;
using UnityEngine;

// El slime revienta al morir: su silueta parpadea en blanco un instante, se infla y explota en
// gotas azules. Daña a Tsuki solo si está dentro de un área del tamaño del propio slime.
public class SlimeExplosion : MonoBehaviour
{
    private const string MaterialResource = "Environment/SkyUnlit";
    private const float FuseTime = 0.3f;
    private static Sprite blobSprite, dropSprite;

    private static readonly Color GooColor = new Color(0.4f, 0.75f, 1f, 1f);
    private static readonly Color GooLight = new Color(0.75f, 0.92f, 1f, 1f);

    private Vector2 point;
    private float radius;
    private int damage;
    private Sprite bodySprite;
    private Vector3 bodyScale;
    private int sortingLayer, sortingOrder;

    // body: el sprite del slime al morir (se copia para que se vea cómo se infla)
    public static void Spawn(SpriteRenderer body, Vector2 point, float radius, int damage)
    {
        var boom = new GameObject("SlimeExplosion").AddComponent<SlimeExplosion>();
        boom.transform.position = point;
        boom.point = point;
        boom.radius = radius;
        boom.damage = damage;
        if (body != null)
        {
            boom.bodySprite = body.sprite;
            boom.bodyScale = body.transform.lossyScale;
            boom.sortingLayer = body.sortingLayerID;
            boom.sortingOrder = body.sortingOrder;
        }
    }

    private IEnumerator Start()
    {
        var material = Resources.Load<Material>(MaterialResource);

        // Copia del slime que parpadea en blanco y se infla antes de reventar
        SpriteRenderer copy = null;
        if (bodySprite != null)
        {
            copy = new GameObject("Body").AddComponent<SpriteRenderer>();
            copy.transform.SetParent(transform, false);
            copy.sprite = bodySprite;
            copy.sortingLayerID = sortingLayer;
            copy.sortingOrder = sortingOrder;
        }
        for (float t = 0f; t < FuseTime; t += Time.deltaTime)
        {
            float k = t / FuseTime;
            if (copy != null)
            {
                copy.transform.localScale = new Vector3(bodyScale.x * (1f + k * 0.35f), bodyScale.y * (1f + k * 0.2f), 1f);
                copy.color = Mathf.Repeat(t, 0.08f) < 0.04f ? Color.white : new Color(1f, 0.55f, 0.55f);
            }
            yield return null;
        }
        if (copy != null) Destroy(copy.gameObject);

        // Explosión: anillo que se abre del tamaño del slime y gotas que salen volando
        var ring = new GameObject("Ring").AddComponent<SpriteRenderer>();
        ring.transform.SetParent(transform, false);
        ring.sprite = BlobSprite();
        ring.sharedMaterial = material;
        ring.sortingLayerName = "UI";
        ring.sortingOrder = -18;
        CameraShake.Shake(0.02f, 0.12f);
        HitPlayer();

        var drops = new SpriteRenderer[10];
        var velocity = new Vector2[drops.Length];
        for (int i = 0; i < drops.Length; i++)
        {
            drops[i] = new GameObject("Drop").AddComponent<SpriteRenderer>();
            drops[i].transform.SetParent(transform, false);
            drops[i].sprite = DropSprite();
            drops[i].sharedMaterial = material;
            drops[i].sortingLayerName = "UI";
            drops[i].sortingOrder = -17;
            drops[i].color = i % 3 == 0 ? GooLight : GooColor;
            float a = (i + Random.value * 0.5f) / drops.Length * Mathf.PI * 2f;
            velocity[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f) * Random.Range(0.6f, 1.1f) * radius * 6f + Vector2.up * radius * 3f;
        }

        const float time = 0.4f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            // El ancho del anillo es el área de daño (isométrico: la mitad de alto)
            float size = Mathf.Lerp(0.4f, 1f, Mathf.Sqrt(k)) * radius * 2f / ring.sprite.bounds.size.x;
            ring.transform.localScale = new Vector3(size, size * 0.5f, 1f);
            ring.color = Color.Lerp(GooLight, GooColor, k) * new Color(1f, 1f, 1f, 1f - k);
            for (int i = 0; i < drops.Length; i++)
            {
                velocity[i] += Vector2.down * radius * 14f * Time.deltaTime;
                drops[i].transform.localPosition += (Vector3)(velocity[i] * Time.deltaTime);
                drops[i].color = new Color(drops[i].color.r, drops[i].color.g, drops[i].color.b, 1f - k * k);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    private void HitPlayer()
    {
        var player = SceneContext.FindPlayer();
        if (player != null && CombatUtil.InsideIsoCircle(player.transform.position, point, radius))
            CombatUtil.DamagePlayer(player, damage, point);
    }

    // Disco suave (anillo de la explosión)
    private static Sprite BlobSprite()
    {
        if (blobSprite != null) return blobSprite;
        const int n = 32;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                // Borde brillante y centro más tenue
                float a = d > 1f ? 0f : d > 0.75f ? 1f : 0.45f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        blobSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return blobSprite;
    }

    // Gota de 3x3 pixeles
    private static Sprite DropSprite()
    {
        if (dropSprite != null) return dropSprite;
        var tex = new Texture2D(3, 3) { filterMode = FilterMode.Point };
        var c = Color.white;
        tex.SetPixels(new[] { Color.clear, c, Color.clear, c, c, c, Color.clear, c, Color.clear });
        tex.Apply();
        dropSprite = Sprite.Create(tex, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), 100f);
        return dropSprite;
    }
}
