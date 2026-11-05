using System.Collections;
using UnityEngine;

// Mini escena al forjar el Módulo de espada (una sola vez, la agrega PlayerStoryState): al cerrar la
// forja la cámara se acerca a Tsuki, levanta la espada envuelta en un aura carmesí con chispas que
// suben, la espada destella y lanza un doble corte de demostración. Después un aviso explica el corte
// a distancia y el doble corte, y la invita a probarlo.
public class SlashUnlockScene : MonoBehaviour
{
    private const string MaterialResource = "Environment/SkyUnlit";
    private static readonly Color Aura = new Color(1f, 0.15f, 0.38f, 1f);

    private int attack;

    public void Setup(int newAttack) => attack = newAttack;

    private IEnumerator Start()
    {
        var tsuki = GetComponent<PlayerController>();
        var sr = GetComponentInChildren<SpriteRenderer>();
        // Espera a que se cierre la forja (y cualquier otro panel)
        while (GameInput.Locked) yield return null;
        yield return new WaitForSeconds(0.2f);

        GameInput.Lock(this);
        tsuki.StopActions();
        bool right = true;
        tsuki.FaceTowards(Vector2.right);
        CameraFocus.Show(() => sr != null ? (Vector2)sr.bounds.center : (Vector2)transform.position, 0.65f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // Levanta la espada: aura que crece, chispas que suben y la espada que se tiñe
        var material = Resources.Load<Material>(MaterialResource);
        var glow = NewSprite("UnlockGlow", SoftCircle(), material, -14);
        var sparks = new System.Collections.Generic.List<(SpriteRenderer sr, Vector2 vel, float life)>();
        var dot = Dot();
        tsuki.HoldSlashPose(right, 0);
        const float charge = 1.3f;
        float sparkTimer = 0f;
        for (float t = 0f; t < charge; t += Time.deltaTime)
        {
            float k = t / charge;
            Vector2 c = sr != null ? (Vector2)sr.bounds.center : (Vector2)transform.position;
            glow.transform.position = c;
            float pulse = 1f + Mathf.Sin(t * 18f) * 0.06f;
            glow.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.1f, Mathf.SmoothStep(0f, 1f, k)) * pulse;
            glow.color = new Color(Aura.r, Aura.g, Aura.b, Mathf.Lerp(0.2f, 0.7f, k));
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.7f), Mathf.PingPong(t * 4f, 1f) * k);

            sparkTimer -= Time.deltaTime;
            if (sparkTimer <= 0f)
            {
                sparkTimer = 0.025f;
                var s = NewSprite("UnlockSpark", dot, material, -13);
                s.transform.position = c + new Vector2(Random.Range(-0.18f, 0.18f), Random.Range(-0.25f, 0f));
                s.transform.localScale = Vector3.one * Random.Range(1f, 2f);
                s.color = Random.value < 0.5f ? new Color(1f, 0.85f, 0.9f) : Aura;
                sparks.Add((s, new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.5f, 1.1f)), 0.6f));
            }
            UpdateSparks(sparks);
            yield return null;
        }

        // Destello: la espada despierta
        CameraShake.Impact(0.03f, 0.35f);
        if (sr != null) sr.color = Color.white;
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            glow.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 2f, t / 0.18f);
            glow.color = new Color(1f, 0.9f, 0.95f, 1f - t / 0.18f);
            UpdateSparks(sparks);
            yield return null;
        }
        Destroy(glow.gameObject);

        // Doble corte de demostración hacia delante
        tsuki.HoldSlashPose(right, -1);
        tsuki.DemoSlash(Vector2.right);
        yield return new WaitForSeconds(0.12f);
        tsuki.DemoSlash(Vector2.right);
        for (float t = 0f; t < 1.1f; t += Time.deltaTime)
        {
            UpdateSparks(sparks);
            yield return null;
        }
        foreach (var s in sparks) if (s.sr != null) Destroy(s.sr.gameObject);

        CameraFocus.Release();
        GameInput.Unlock(this);

        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueData.Line
        {
            text = $"¡La espada carmesí despertó! Daño: 15 → {attack}. Desbloqueaste el corte a distancia."
        });
        data.lines.Add(new DialogueData.Line
        {
            text = "Presiona {slash} para lanzar un corte que vuela. Presiónalo dos veces seguidas para un doble corte. ¡Pruébalo!"
        });
        DialogueUI.Play(data, () => Destroy(this));
    }

    private static void UpdateSparks(System.Collections.Generic.List<(SpriteRenderer sr, Vector2 vel, float life)> sparks)
    {
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i];
            s.life -= Time.deltaTime;
            if (s.life <= 0f || s.sr == null)
            {
                if (s.sr != null) Destroy(s.sr.gameObject);
                sparks.RemoveAt(i);
                continue;
            }
            s.sr.transform.position += (Vector3)(s.vel * Time.deltaTime);
            var c = s.sr.color;
            c.a = s.life / 0.6f;
            s.sr.color = c;
            sparks[i] = s;
        }
    }

    private static SpriteRenderer NewSprite(string name, Sprite sprite, Material material, int order)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (material != null) sr.sharedMaterial = material;
        // Por delante del escenario, debajo del HUD
        sr.sortingLayerName = "UI";
        sr.sortingOrder = order;
        return sr;
    }

    private static Sprite SoftCircle()
    {
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
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 0.8f);
    }

    private static Sprite Dot()
    {
        var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
    }
}
