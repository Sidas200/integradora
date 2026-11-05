using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Escombro que cae del cielo (golpe al suelo de Varn en la fase 2): un círculo rojo avisa dónde
// caerá, la roca baja en el último tramo del aviso y al tocar el suelo daña a Tsuki y también a
// los enemigos que estén debajo (los refuerzos de Varn). Después se queda un momento y se desvanece.
public class FallingDebris : MonoBehaviour
{
    private const float FallTime = 0.35f;
    private const float FallHeight = 1.6f;
    private const float LingerTime = 0.5f;
    private static Sprite rockSprite;

    private Vector2 point;
    private float warnTime;
    private float radius;
    private int playerDamage;
    private int enemyDamage;
    private Sprite circleSprite;
    private BossFx fx;
    private EnemyHealth ignore;

    // ignore: la vida del propio Varn, que no se daña con sus escombros
    public static void Spawn(Vector2 point, float warnTime, float radius, int playerDamage, int enemyDamage,
        Sprite circleSprite, BossFx fx, EnemyHealth ignore)
    {
        var debris = new GameObject("FallingDebris").AddComponent<FallingDebris>();
        debris.transform.position = point;
        debris.point = point;
        debris.warnTime = Mathf.Max(FallTime, warnTime);
        debris.radius = radius;
        debris.playerDamage = playerDamage;
        debris.enemyDamage = enemyDamage;
        debris.circleSprite = circleSprite;
        debris.fx = fx;
        debris.ignore = ignore;
    }

    private IEnumerator Start()
    {
        var warning = Telegraph.Create(circleSprite, "DebrisWarning");
        warning.ShowCircle(point, radius, warnTime, Telegraph.Danger);

        // La raíz queda en el suelo para ordenarse con los personajes; la roca baja como hijo
        var group = gameObject.AddComponent<SortingGroup>();
        group.sortingLayerName = "Sprites";
        var rock = new GameObject("Rock").AddComponent<SpriteRenderer>();
        rock.transform.SetParent(transform, false);
        rock.sprite = RockSprite();
        rock.transform.localScale = Vector3.one * (radius * 1.4f / Mathf.Max(0.01f, rock.sprite.bounds.size.x));
        rock.enabled = false;

        yield return new WaitForSeconds(warnTime - FallTime);
        rock.enabled = true;
        for (float t = 0f; t < FallTime; t += Time.deltaTime)
        {
            float k = t / FallTime;
            rock.transform.localPosition = new Vector3(0f, FallHeight * (1f - k * k), 0f);
            yield return null;
        }
        rock.transform.localPosition = Vector3.zero;

        warning.Impact(0.2f);
        if (fx != null) fx.RockImpact(point, radius);
        HitTargets();

        yield return new WaitForSeconds(LingerTime * 0.5f);
        for (float t = 0f; t < LingerTime * 0.5f; t += Time.deltaTime)
        {
            rock.color = new Color(1f, 1f, 1f, 1f - t / (LingerTime * 0.5f));
            yield return null;
        }
        Destroy(warning.gameObject);
        Destroy(gameObject);
    }

    private void HitTargets()
    {
        var player = SceneContext.FindPlayer();
        if (player != null && CombatUtil.InsideIsoCircle(player.transform.position, point, radius))
            CombatUtil.DamagePlayer(player, playerDamage, point);

        // Un poco más de alcance con los enemigos para que se note que también les pega
        foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy == ignore || enemy.IsDead) continue;
            if (CombatUtil.InsideIsoCircle(enemy.transform.position, point, radius * 1.2f))
                enemy.TakeDamage(enemyDamage);
        }
    }

    // Roca de pixel art generada (12x10): piedra gris con contorno oscuro y brillo arriba
    private static Sprite RockSprite()
    {
        if (rockSprite != null) return rockSprite;
        string[] rows =
        {
            "...oooooo...",
            "..ollllllo..",
            ".ollhhlllmo.",
            "ollhhllllmmo",
            "olllllllmmmo",
            "ollllllmmmdo",
            "omllllmmmddo",
            ".ommmmmmddo.",
            "..oddddddo..",
            "...oooooo...",
        };
        var colors = new System.Collections.Generic.Dictionary<char, Color32>
        {
            { '.', new Color32(0, 0, 0, 0) },
            { 'o', new Color32(38, 30, 36, 255) },
            { 'h', new Color32(196, 186, 176, 255) },
            { 'l', new Color32(150, 140, 132, 255) },
            { 'm', new Color32(112, 102, 98, 255) },
            { 'd', new Color32(78, 68, 70, 255) },
        };
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - y, colors[rows[y][x]]);
        tex.Apply();
        // Pivote abajo al centro: la roca se apoya en el punto de caída
        rockSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 100f);
        return rockSprite;
    }
}
