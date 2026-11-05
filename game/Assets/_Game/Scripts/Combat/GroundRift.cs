using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Grieta que abre el piso (corte de espada de Varn, fase 2): sale de los pies del guardián y
// avanza en línea recta hasta casi el borde de la arena. Es una grieta en zigzag con el fondo
// oscuro, un filo carmesí brillante y un aura que late a su alrededor; suelta brasas y chispas al
// avanzar. Daña una vez a Tsuki si la alcanza mientras se abre o mientras sigue encendida.
public class GroundRift : MonoBehaviour
{
    private const string MaterialResource = "Environment/SkyUnlit";
    private static Material glowMaterial, solidMaterial;

    private static readonly Color AuraColor = new Color(1f, 0.1f, 0.36f, 0.8f);
    private static readonly Color EdgeColor = new Color(1f, 0.45f, 0.62f, 1f);
    private static readonly Color CoreColor = new Color(0.07f, 0.01f, 0.04f, 1f);

    private Vector2 origin, direction;
    private float length, width, speed;
    private int damage;
    private BossFx fx;
    private bool hitPlayer;

    private LineRenderer aura, edge, core;
    private readonly List<Vector3> points = new List<Vector3>();

    public static void Spawn(Vector2 origin, Vector2 direction, float length, float width, float speed, int damage, BossFx fx)
    {
        var rift = new GameObject("GroundRift").AddComponent<GroundRift>();
        rift.origin = origin;
        rift.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        rift.length = length;
        rift.width = width;
        rift.speed = speed;
        rift.damage = damage;
        rift.fx = fx;
    }

    private IEnumerator Start()
    {
        EnsureMaterials();
        // De abajo hacia arriba: aura ancha y suave, filo brillante y el fondo oscuro de la grieta
        aura = NewLine("Aura", glowMaterial, width * 2.6f, AuraColor, 24);
        edge = NewLine("Edge", solidMaterial, width * 0.4f, EdgeColor, 25);
        core = NewLine("Core", solidMaterial, width * 0.13f, CoreColor, 26);

        // Zigzag: un quiebre cada 0.12 unidades, desviado a los lados
        var side = new Vector2(-direction.y, direction.x);
        var path = new List<Vector3>();
        for (float d = 0f; d < length; d += 0.12f)
        {
            float jitter = d < 0.05f ? 0f : Random.Range(-1f, 1f) * width * 0.22f;
            path.Add(origin + direction * d + side * jitter);
        }
        path.Add(origin + direction * length);

        // Se abre desde Varn hacia delante
        float travelled = 0f;
        int next = 0;
        float emberTimer = 0f;
        while (next < path.Count)
        {
            travelled += speed * Time.deltaTime;
            while (next < path.Count && next * 0.12f <= travelled)
            {
                points.Add(path[next]);
                next++;
            }
            Vector2 front = points.Count > 0 ? (Vector2)points[points.Count - 1] : origin;
            SetPoints();
            // El aura late mientras se abre
            aura.widthMultiplier = width * (2.6f + 0.5f * Mathf.Sin(Time.time * 25f));

            emberTimer -= Time.deltaTime;
            if (fx != null && emberTimer <= 0f)
            {
                fx.RiftSparks(front, direction);
                emberTimer = 0.03f;
            }
            CheckHit(Mathf.Min(travelled, length));
            yield return null;
        }
        CameraShake.Shake(0.03f, 0.2f);

        // Sigue encendida un momento, latiendo, y se cierra desvaneciéndose
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(t * 30f);
            SetAlpha(pulse);
            if (fx != null && Random.value < 0.4f)
                fx.RiftSparks(points[Random.Range(0, points.Count)], direction);
            CheckHit(length);
            yield return null;
        }
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            SetAlpha(1f - t / 0.45f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void CheckHit(float reach)
    {
        if (hitPlayer) return;
        var player = SceneContext.FindPlayer();
        if (player == null) return;
        if (Telegraph.InsideLine(player.transform.position, origin, direction, reach, width))
            hitPlayer = CombatUtil.DamagePlayer(player, damage, origin);
    }

    private void SetPoints()
    {
        var array = points.ToArray();
        foreach (var line in new[] { aura, edge, core })
        {
            line.positionCount = array.Length;
            line.SetPositions(array);
        }
    }

    private void SetAlpha(float k)
    {
        aura.startColor = aura.endColor = new Color(AuraColor.r, AuraColor.g, AuraColor.b, AuraColor.a * k);
        edge.startColor = edge.endColor = new Color(EdgeColor.r, EdgeColor.g, EdgeColor.b, k);
        core.startColor = core.endColor = new Color(CoreColor.r, CoreColor.g, CoreColor.b, k);
    }

    private LineRenderer NewLine(string name, Material material, float lineWidth, Color color, int order)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.useWorldSpace = true;
        line.sharedMaterial = material;
        line.widthMultiplier = lineWidth;
        line.numCapVertices = 2;
        line.numCornerVertices = 1;
        line.textureMode = LineTextureMode.Stretch;
        line.startColor = line.endColor = color;
        // Sobre el piso y los avisos (Background 20), debajo de los personajes
        line.sortingLayerName = "Background";
        line.sortingOrder = order;
        line.positionCount = 0;
        return line;
    }

    private static void EnsureMaterials()
    {
        if (glowMaterial != null) return;
        var template = Resources.Load<Material>(MaterialResource);
        var shader = template != null ? template.shader : Shader.Find("Sprites/Default");

        // Aura: degradado suave de un lado al otro de la línea (transparente en los bordes)
        const int n = 16;
        var soft = new Texture2D(1, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        {
            float d = Mathf.Abs((y + 0.5f) / n - 0.5f) * 2f;
            soft.SetPixel(0, y, new Color(1f, 1f, 1f, (1f - d) * (1f - d)));
        }
        soft.Apply();
        glowMaterial = new Material(shader) { mainTexture = soft };

        var white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
        solidMaterial = new Material(shader) { mainTexture = white };
    }
}
