using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Estatua de la primera doncella. Primero pone a prueba la espada (SanctuaryTrial: oleadas
// de enemigos); al superarla despierta (una sola vez), habla con Tsuki y vuelve a la espada:
// su espíritu vuela hacia Tsuki y desbloquea el Paso de Rayo (PlayerController.HasLightningDash).
// Después, la estatua vacía solo da una pista (Aoi hablando desde la espada).
public class MaidenStatue : Interactable
{
    public const string Flag = "maiden_awakened";

    [SerializeField] private DialogueData awakenDialogue;
    [SerializeField] private DialogueData hintDialogue;
    [SerializeField] private GameObject spirit;
    [SerializeField] private SpriteRenderer statueRenderer;
    [SerializeField] private SanctuaryTrial trial;
    [SerializeField] private DialogueData trialDialogue;
    [Tooltip("Adónde camina Tsuki al terminar la prueba, desde la estatua (el centro del santuario).")]
    [SerializeField] private Vector2 standOffset = new Vector2(0.96f, -0.7f);

    private const string MaterialResource = "Environment/SkyUnlit";
    private const float PushBack = 0.3f;   // cuánto retrocede Tsuki cuando Aoi entra a la espada

    public void Setup(DialogueData awaken, DialogueData hint, GameObject spiritObject, SpriteRenderer statue)
    {
        awakenDialogue = awaken;
        hintDialogue = hint;
        spirit = spiritObject;
        statueRenderer = statue;
    }

    public void SetTrial(SanctuaryTrial sanctuaryTrial, DialogueData intro)
    {
        trial = sanctuaryTrial;
        trialDialogue = intro;
    }

    public override string Prompt => StoryFlags.Has(Flag) ? "Escuchar la espada" : "Tocar la estatua";
    public override bool CanInteract => trial == null || !trial.Running;

    private void Start()
    {
        // Aoi ya volvió a la espada: el espíritu solo aparece durante el despertar
        if (spirit != null) spirit.SetActive(false);
        if (StoryFlags.Has(Flag)) Dim();
    }

    public override void Interact()
    {
        if (StoryFlags.Has(Flag))
        {
            DialogueUI.Play(hintDialogue);
            return;
        }
        if (trial != null && !trial.Done)
        {
            DialogueUI.Play(trialDialogue, () => trial.Begin(() => StartCoroutine(Awaken())));
            return;
        }
        StartCoroutine(Awaken());
    }

    private IEnumerator Awaken()
    {
        GameInput.Lock(this);
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) yield return FadeToCenter(player);

        // Brillo de la estatua antes de que aparezca el espíritu
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (statueRenderer != null) statueRenderer.color = Color.Lerp(Color.white, new Color(0.6f, 0.95f, 1f), Mathf.PingPong(t * 4f, 1f));
            yield return null;
        }
        Dim();
        if (spirit != null) spirit.SetActive(true);
        yield return new WaitForSeconds(0.4f);
        CameraFocus.Release();
        GameInput.Unlock(this);
        // El despertar queda guardado aunque el jugador salga al menú durante el diálogo
        StoryFlags.Set(Flag);
        if (GameFlow.Instance != null) GameFlow.Instance.SaveProgress();
        DialogueUI.Play(awakenDialogue, () => StartCoroutine(JoinSword()));
    }

    // Al terminar la prueba la pantalla se va a negro; cuando vuelve, Tsuki ya está en el centro del
    // santuario mirando a la estatua y la cámara se acerca a las dos.
    private IEnumerator FadeToCenter(PlayerController player)
    {
        var fader = SceneLoader.Instance != null ? SceneLoader.Instance.Fader : null;
        if (fader != null) yield return fader.FadeOut(0.45f);

        player.StopActions();
        Vector2 target = (Vector2)transform.position + standOffset;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = target;
        }
        player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);
        player.FaceTowards((Vector2)transform.position - target);

        // Enfoque entre Tsuki y la estatua; la cámara se acomoda mientras la pantalla sigue negra
        Vector2 focus = Vector2.Lerp(target, transform.position, 0.45f);
        CameraFocus.Show(() => focus, 0.75f, 0.01f);
        yield return new WaitForSeconds(0.35f);

        if (fader != null) yield return fader.FadeIn(0.6f);
        yield return new WaitForSeconds(0.2f);
    }

    // Presentación (como la del Módulo de espada): la cámara se acerca, Tsuki levanta la espada,
    // Aoi vuela hacia él y entra; el empuje hace retroceder a Tsuki con la espada en alto, lo
    // envuelve un aura carmesí con chispas y rayos, y termina con un Paso de Rayo de demostración.
    private IEnumerator JoinSword()
    {
        var player = FindAnyObjectByType<PlayerController>();
        var sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (player == null || sr == null)
        {
            if (spirit != null) spirit.SetActive(false);
            ExplainLightning(player);
            yield break;
        }

        GameInput.Lock(this);
        player.StopActions();
        Vector2 toStatue = (Vector2)transform.position - (Vector2)player.transform.position;
        bool right = toStatue.x >= 0f;
        player.FaceTowards(toStatue);
        CameraFocus.Show(() => sr != null ? (Vector2)sr.bounds.center : (Vector2)transform.position, 0.65f, 0.5f);
        yield return new WaitForSeconds(0.45f);

        // Levanta la espada para recibirla
        player.HoldSlashPose(right, 0);
        var material = Resources.Load<Material>(MaterialResource);
        var glow = SlashUnlockScene.NewSprite("AoiGlow", SlashUnlockScene.SoftCircle(), material, -14);
        var dot = SlashUnlockScene.Dot();
        var sparks = new List<(SpriteRenderer sr, Vector2 vel, float life)>();
        Color crimson = player.LightningColor;
        Color purple = player.LightningHaloColor;
        glow.color = Color.clear;
        yield return new WaitForSeconds(0.3f);

        // Aoi vuela en arco hacia la punta de la espada, dejando chispas
        if (spirit != null)
        {
            var renderers = spirit.GetComponentsInChildren<SpriteRenderer>();
            Vector3 from = spirit.transform.position;
            Vector3 scale = spirit.transform.localScale;
            const float fly = 0.85f;
            float sparkTimer = 0f;
            for (float t = 0f; t < fly; t += Time.deltaTime)
            {
                float k = t / fly;
                float e = k * k * (3f - 2f * k);
                Vector3 tip = SwordTip(sr, right);
                spirit.transform.position = Vector3.Lerp(from, tip, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
                spirit.transform.localScale = scale * Mathf.Lerp(1f, 0.2f, e);
                foreach (var r in renderers)
                    r.color = Color.Lerp(Color.white, new Color(1f, 0.75f, 0.85f, 0.7f), e);
                glow.transform.position = tip;
                glow.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.5f, e);
                glow.color = new Color(crimson.r, crimson.g, crimson.b, 0.6f * e);

                sparkTimer -= Time.deltaTime;
                if (sparkTimer <= 0f)
                {
                    sparkTimer = 0.03f;
                    AddSpark(sparks, dot, material, spirit.transform.position, Random.value < 0.5f ? new Color(0.7f, 0.95f, 1f) : Color.white, 0.15f);
                }
                SlashUnlockScene.UpdateSparks(sparks);
                yield return null;
            }
            spirit.SetActive(false);
            spirit.transform.position = from;
            spirit.transform.localScale = scale;
            foreach (var r in renderers) r.color = Color.white;
        }

        // Entra a la espada: el empuje lo hace retroceder un poco, con la espada en alto
        CameraShake.Impact(0.035f, 0.3f);
        player.PowerUpFlash();
        player.LightningBurst(7, 0.7f);
        Vector3 start = player.transform.position;
        Vector3 back = (Vector3)(-toStatue.normalized) * PushBack;
        const float push = 0.35f;
        for (float t = 0f; t < push; t += Time.deltaTime)
        {
            float k = t / push;
            player.transform.position = start + back * (1f - (1f - k) * (1f - k));
            glow.transform.position = sr.bounds.center;
            glow.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.1f, k);
            SlashUnlockScene.UpdateSparks(sparks);
            yield return null;
        }

        // El poder de Aoi lo envuelve: aura carmesí, chispas que suben y rayos chicos
        const float charge = 1.1f;
        float sparkT = 0f, boltT = 0f;
        for (float t = 0f; t < charge; t += Time.deltaTime)
        {
            float k = t / charge;
            Vector2 c = sr.bounds.center;
            glow.transform.position = c;
            glow.transform.localScale = Vector3.one * (1.1f + Mathf.Sin(t * 18f) * 0.07f);
            glow.color = new Color(crimson.r, crimson.g, crimson.b, Mathf.Lerp(0.45f, 0.75f, k));
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.75f), Mathf.PingPong(t * 4f, 1f) * k);

            sparkT -= Time.deltaTime;
            if (sparkT <= 0f)
            {
                sparkT = 0.025f;
                var pos = c + new Vector2(Random.Range(-0.18f, 0.18f), Random.Range(-0.25f, 0f));
                AddSpark(sparks, dot, material, pos, Random.value < 0.5f ? crimson : purple, 1f);
            }
            boltT -= Time.deltaTime;
            if (boltT <= 0f)
            {
                boltT = 0.16f;
                player.LightningBurst(2, 0.55f);
            }
            SlashUnlockScene.UpdateSparks(sparks);
            yield return null;
        }

        // Destello: el rayo despierta
        CameraShake.Impact(0.03f, 0.35f);
        sr.color = Color.white;
        player.LightningBurst(8, 0.9f);
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            glow.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 2.2f, t / 0.2f);
            glow.color = new Color(1f, 0.85f, 0.92f, 1f - t / 0.2f);
            SlashUnlockScene.UpdateSparks(sparks);
            yield return null;
        }
        Destroy(glow.gameObject);

        // Demostración: Tsuki se vuelve rayo hacia un lado y regresa a donde estaba
        player.HoldSlashPose(right, -1);
        yield return new WaitForSeconds(0.25f);
        Vector3 home = player.transform.position;
        Vector2 sideDir = right ? Vector2.left : Vector2.right;
        player.DemoLightningDash(sideDir, 0.45f);
        yield return WaitDash(player, sparks);
        yield return WaitSparks(sparks, 0.35f);
        player.DemoLightningDash(-sideDir, 0.45f);
        yield return WaitDash(player, sparks);
        // Si algo lo detuvo en el camino, termina de acomodarse en su lugar
        Vector3 from2 = player.transform.position;
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(from2, home, t / 0.12f);
            yield return null;
        }
        player.transform.position = home;
        player.FaceTowards(toStatue);
        yield return WaitSparks(sparks, 0.5f);
        foreach (var s in sparks) if (s.sr != null) Destroy(s.sr.gameObject);

        CameraFocus.Release();
        GameInput.Unlock(this);
        ExplainLightning(player);
    }

    private static IEnumerator WaitDash(PlayerController player, List<(SpriteRenderer sr, Vector2 vel, float life)> sparks)
    {
        for (float timeout = 1f; timeout > 0f && player.IsDashing; timeout -= Time.deltaTime)
        {
            SlashUnlockScene.UpdateSparks(sparks);
            yield return null;
        }
    }

    private static IEnumerator WaitSparks(List<(SpriteRenderer sr, Vector2 vel, float life)> sparks, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            SlashUnlockScene.UpdateSparks(sparks);
            yield return null;
        }
    }

    // Punta de la espada en alto (arriba del sprite, hacia donde mira)
    private static Vector3 SwordTip(SpriteRenderer sr, bool right)
    {
        var b = sr.bounds;
        return new Vector3(b.center.x + (right ? 0.25f : -0.25f) * b.extents.x, b.max.y - b.size.y * 0.1f, 0f);
    }

    private static void AddSpark(List<(SpriteRenderer sr, Vector2 vel, float life)> sparks, Sprite dot, Material material,
        Vector2 pos, Color color, float rise)
    {
        var s = SlashUnlockScene.NewSprite("AoiSpark", dot, material, -13);
        s.transform.position = pos;
        s.transform.localScale = Vector3.one * Random.Range(1f, 2f);
        s.color = color;
        sparks.Add((s, new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.5f, 1.1f) * rise), 0.6f));
    }

    private static void ExplainLightning(PlayerController player)
    {
        var companion = player != null ? player.GetComponent<SwordCompanion>() : null;
        if (companion != null && companion.JoinDialogue != null)
        {
            DialogueUI.Play(companion.JoinDialogue);
            return;
        }
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueData.Line { text = "Aoi volvió a la espada carmesí. Desbloqueaste el Paso de Rayo." });
        data.lines.Add(new DialogueData.Line
        {
            text = "Presiona {dash} para esquivar convertido en rayo: dañas a los enemigos que atraviesas. ¡Pruébalo!"
        });
        data.lines.Add(new DialogueData.Line { text = "Presiona {sword} para hablar con Aoi y pedirle consejo." });
        DialogueUI.Play(data);
    }

    private void Dim()
    {
        if (statueRenderer != null) statueRenderer.color = new Color(0.75f, 0.8f, 0.9f);
    }
}
