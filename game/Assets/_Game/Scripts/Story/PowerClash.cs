using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Choque de poder al vencer a Varn (lo lanza GuardianArena): cada uno se va a un extremo de la
// arena, se lanzan al centro y chocan espada contra escudo. Tsuki empuja presionando atacar muchas
// veces y Varn empuja solo, con arranques fuertes de vez en cuando; el medidor de abajo muestra
// quién va ganando. Si Varn gana, Tsuki sale empujada, pierde vida y se repite el choque.
// La cámara se acerca a los dos mientras dura.
// Choque de escudo (PlayShieldClash, lo lanza GuardianBoss en la fase 2): si Tsuki golpea a Varn
// mientras se cubre, chocan ahí mismo con el mismo medidor y un solo intento.
public class PowerClash : MonoBehaviour
{
    public enum Outcome { Skipped, Won, Lost, PlayerDied }

    // Hay un choque en curso (los rayos de la tormenta no caen mientras tanto)
    public static bool InProgress { get; private set; }

    private const string MaterialResource = "Environment/SkyUnlit";

    [Header("Posiciones")]
    [Tooltip("Qué tan lejos del centro se ponen, respecto al radio horizontal de la arena.")]
    [SerializeField, Range(0.2f, 0.9f)] private float cornerDistance = 0.55f;
    [Tooltip("Distancia de cada uno al punto de choque.")]
    [SerializeField] private float contactGap = 0.24f;
    [Tooltip("Cuánto se mueve el punto de choque hacia el que va perdiendo.")]
    [SerializeField] private float clashShift = 0.35f;
    [SerializeField] private float varnWalkSpeed = 0.9f;
    [SerializeField] private float rushTime = 0.28f;

    [Header("Dificultad (media, -15%: Varn empuja menos)")]
    [Tooltip("Lo que avanza el medidor con cada botón de ataque (0 a 1; empieza en 0.5).")]
    [SerializeField] private float pushPerPress = 0.05f;
    [Tooltip("Lo que Varn empuja por segundo. Hay que presionar unas 4.5 veces por segundo solo para aguantar.")]
    [SerializeField] private float varnPush = 0.221f;
    [Tooltip("Cada cuánto Varn da un empujón fuerte (segundos, mínimo y máximo).")]
    [SerializeField] private Vector2 surgeInterval = new Vector2(1.4f, 2.4f);
    [SerializeField] private float surgeStrength = 0.068f;
    [Tooltip("Vida que pierde Tsuki si Varn gana el choque (fracción de su vida máxima).")]
    [SerializeField, Range(0f, 1f)] private float loseDamage = 0.25f;

    [Header("Choque de escudo")]
    [Tooltip("Daño del golpe de escudo si Varn gana (Tsuki además queda aturdida).")]
    [SerializeField] private int shieldBashDamage = 15;
    [SerializeField] private float shieldBashDistance = 0.8f;
    [SerializeField] private float playerStunTime = 2f;
    [SerializeField, Range(0.3f, 1f)] private float shieldZoom = 0.75f;

    [Header("Cámara")]
    [Tooltip("Tamaño de la cámara respecto al normal mientras dura (menor = más cerca).")]
    [SerializeField, Range(0.3f, 1f)] private float zoom = 0.6f;
    [SerializeField] private float zoomTime = 0.6f;

    [Header("Medidor")]
    [SerializeField] private Color tsukiColor = new Color(0.87f, 0.24f, 0.36f);
    [SerializeField] private Color varnColor = new Color(0.42f, 0.3f, 0.62f);

    public Outcome Result { get; private set; }

    private PlayerController tsuki;
    private Rigidbody2D tsukiBody;
    private RigidbodyInterpolation2D tsukiInterpolation;
    private Health tsukiHealth;
    private SpriteRenderer tsukiSprite;
    private GuardianVarnAnimator varn;
    private Transform varnMover;   // lo que se mueve: la copia de la derrota o el propio jefe
    private Rigidbody2D varnRb;
    private SpriteRenderer varnSprite;
    private float varnZ;
    private Vector2 center;
    private Vector2 axis; // del centro hacia el lado de Tsuki
    private bool tsukiFacesRight;
    private bool locked;
    private bool running;   // este componente tiene un choque en curso

    // Punto medio entre los dos: ahí apunta la cámara (CameraFocus)
    private Vector2 focus;

    // Efectos
    private class Spark
    {
        public SpriteRenderer sr;
        public Vector2 velocity;
        public float life, maxLife;
    }
    private readonly List<Spark> sparks = new List<Spark>();
    private Material fxMaterial;
    private Sprite dotSprite, glowSprite;
    private SpriteRenderer glow;
    private float glowPulse;

    // Medidor
    private GameObject ui;
    private CanvasGroup meterGroup;
    private RectTransform meterFill, meterMarker;
    private bool meterLeftIsTsuki;
    private TextMeshProUGUI prompt;
    private Image flash;
    private float flashAlpha;

    // Toma a Tsuki y a Varn y bloquea el control. varnRoot: lo que se mueve (si es null, el sprite).
    private bool Prepare(GuardianVarnAnimator varnBody, Transform varnRoot)
    {
        var player = SceneContext.FindPlayer();
        tsuki = player != null ? player.GetComponentInParent<PlayerController>() : null;
        if (tsuki == null || varnBody == null) return false;

        tsukiBody = tsuki.GetComponent<Rigidbody2D>();
        tsukiHealth = tsuki.GetComponent<Health>();
        tsukiSprite = tsuki.GetComponentInChildren<SpriteRenderer>();
        tsukiInterpolation = tsukiBody.interpolation;
        varn = varnBody;
        varnMover = varnRoot != null ? varnRoot : varnBody.transform;
        varnRb = varnMover.GetComponent<Rigidbody2D>();
        varnSprite = varn.GetComponent<SpriteRenderer>();
        varnZ = varnMover.position.z;

        GameInput.Lock(this);
        locked = true;
        running = true;
        InProgress = true;
        tsuki.StopActions();
        return true;
    }

    // Choque de escudo: Tsuki golpeó a Varn mientras se cubría. Un solo intento; Result dice quién ganó.
    public IEnumerator PlayShieldClash(GuardianVarnAnimator varnBody, Transform varnRoot)
    {
        Result = Outcome.Skipped;
        if (!Prepare(varnBody, varnRoot)) yield break;

        Vector2 tsukiFrom = tsukiBody.position;
        Vector2 varnFrom = varnMover.position;
        center = (tsukiFrom + varnFrom) * 0.5f;
        float side = tsukiFrom.x < varnFrom.x ? -1f : 1f;
        axis = new Vector2(side, 0f);
        tsukiFacesRight = side < 0f;

        BuildEffects();
        BuildUi(tsukiFacesRight);
        UpdateFocus();
        CameraFocus.Show(() => focus, shieldZoom, 0.3f);
        SetScripted(true);

        // Se juntan de golpe: la espada de Tsuki queda contra el escudo
        tsuki.FaceTowards(-axis);
        tsuki.HoldSlashPose(tsukiFacesRight, 1);
        varn.Hold(DashClip, 6);
        Vector2 tsukiTo = center + axis * contactGap;
        Vector2 varnTo = center - axis * contactGap;
        const float time = 0.12f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            SetTsuki(Vector2.Lerp(tsukiFrom, tsukiTo, t / time));
            SetVarn(Vector2.Lerp(varnFrom, varnTo, t / time));
            UpdateFocus();
            yield return null;
        }
        SetTsuki(tsukiTo);
        SetVarn(varnTo);
        flashAlpha = 0.6f;
        glowPulse = 1f;
        Burst(ContactPoint(center), 14, 2f);
        CameraShake.Shake(0.04f, 0.25f);

        bool won = false;
        yield return Clash(result => won = result);
        if (won)
        {
            yield return Win(false);
            Result = Outcome.Won;
        }
        else
        {
            yield return ShieldBash();
            Result = tsukiHealth != null && tsukiHealth.IsDead ? Outcome.PlayerDied : Outcome.Lost;
        }
        Cleanup();
        if (Result == Outcome.Lost) tsuki.Stun(playerStunTime);
    }

    // Choque final al vencer a Varn (la copia que queda en la arena)
    public IEnumerator Play(GuardianBoss boss, GuardianVarnAnimator varnBody)
    {
        Result = Outcome.Skipped;
        if (!Prepare(varnBody, null)) yield break;

        center = boss.ArenaCenter;
        float side = tsukiBody.position.x < center.x ? -1f : 1f;
        axis = new Vector2(side, 0f);
        tsukiFacesRight = side < 0f;
        float distance = Mathf.Max(0.5f, boss.ArenaRadii.x * cornerDistance);
        Vector2 tsukiCorner = center + axis * distance;
        Vector2 varnCorner = center - axis * distance;

        BuildEffects();
        BuildUi(tsukiFacesRight);
        UpdateFocus();
        CameraFocus.Show(() => focus, zoom, zoomTime);

        yield return ToCorners(tsukiCorner, varnCorner);
        while (true)
        {
            // Se miran con la espada en alto
            tsuki.FaceTowards(-axis);
            tsuki.HoldSlashPose(tsukiFacesRight, 0);
            varn.PlayFacing("Idle", axis, 6f, true);
            yield return new WaitForSeconds(0.5f);

            yield return Rush(tsukiCorner, varnCorner);
            bool won = false;
            yield return Clash(result => won = result);
            if (won)
            {
                yield return Win(true);
                Result = Outcome.Won;
                break;
            }
            yield return Lose();
            if (tsukiHealth != null && tsukiHealth.IsDead)
            {
                Result = Outcome.PlayerDied;
                break;
            }
            if (GameHud.Instance != null) GameHud.Instance.ShowToast("¡Varn te hizo retroceder! Presiona más rápido.");
            yield return ToCorners(tsukiCorner, varnCorner);
        }

        tsuki.HoldSlashPose(tsukiFacesRight, -1);
        tsuki.SetScriptedMove(Vector2.zero);
        CameraFocus.Release();
        yield return new WaitForSecondsRealtime(zoomTime);
        Cleanup();
    }

    // ---------------------------------------------------------------- fases

    private IEnumerator ToCorners(Vector2 tsukiTarget, Vector2 varnTarget)
    {
        SetScripted(false);
        tsuki.HoldSlashPose(tsukiFacesRight, -1);
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            Vector2 toTsuki = tsukiTarget - tsukiBody.position;
            bool tsukiThere = toTsuki.magnitude < 0.05f;
            tsuki.SetScriptedMove(tsukiThere ? Vector2.zero : toTsuki.normalized);

            Vector2 varnPos = varnMover.position;
            Vector2 toVarn = varnTarget - varnPos;
            bool varnThere = toVarn.magnitude < 0.02f;
            if (varnThere)
                varn.PlayFacing("Idle", axis, 6f, true);
            else
            {
                SetVarn(Vector2.MoveTowards(varnPos, varnTarget, varnWalkSpeed * Time.deltaTime));
                varn.PlayFacing("Walk", toVarn, 10f, true);
            }

            UpdateFocus();
            if (tsukiThere && varnThere) break;
            yield return null;
        }
        tsuki.SetScriptedMove(Vector2.zero);
        SetScripted(true);
        SetTsuki(tsukiTarget);
        SetVarn(varnTarget);
    }

    private IEnumerator Rush(Vector2 tsukiFrom, Vector2 varnFrom)
    {
        varn.PlayRange(DashClip, 1, 6, 14f, true);
        Vector2 tsukiTo = center + axis * contactGap;
        Vector2 varnTo = center - axis * contactGap;
        float ghost = 0f;
        for (float t = 0f; t < rushTime; t += Time.deltaTime)
        {
            float k = t / rushTime;
            k *= k; // acelera hasta el choque
            SetTsuki(Vector2.Lerp(tsukiFrom, tsukiTo, k));
            SetVarn(Vector2.Lerp(varnFrom, varnTo, k));
            ghost -= Time.deltaTime;
            if (ghost <= 0f)
            {
                tsuki.SpawnDashGhost();
                ghost = 0.035f;
            }
            UpdateFocus();
            yield return null;
        }
        SetTsuki(tsukiTo);
        SetVarn(varnTo);
        flashAlpha = 0.7f;
        glowPulse = 1f;
        Burst(ContactPoint(center), 18, 2.2f);
        CameraShake.Shake(0.05f, 0.3f);
    }

    private IEnumerator Clash(Action<bool> done)
    {
        ShowMeter(true);
        varn.Hold(DashClip, 6);
        float power = 0.5f, shown = 0.5f, elapsed = 0f;
        float surgeLeft = 0f, pressPose = 0f, ambient = 0f;
        float nextSurge = UnityEngine.Random.Range(surgeInterval.x, surgeInterval.y);

        while (true)
        {
            float dt = Time.deltaTime;
            if (dt > 0f && AttackPressed())
            {
                power += pushPerPress;
                pressPose = 0.07f;
                glowPulse = Mathf.Max(glowPulse, 0.6f);
                Burst(ContactPoint(ClashPoint(shown)), 4, 1.4f);
                CameraShake.Shake(0.012f, 0.06f);
            }

            elapsed += dt;
            // Varn empieza a empujar de a poco para que no se pierda en cuanto aparece el medidor
            float push = varnPush * Mathf.Clamp01(elapsed / 0.6f);
            if (elapsed >= nextSurge)
            {
                surgeLeft = 0.3f;
                nextSurge = elapsed + UnityEngine.Random.Range(surgeInterval.x, surgeInterval.y);
                CameraShake.Shake(0.02f, 0.25f);
            }
            if (surgeLeft > 0f)
            {
                push += surgeStrength / 0.3f;
                surgeLeft -= dt;
            }
            power = Mathf.Clamp01(power - push * dt);
            shown = Mathf.Lerp(shown, power, 1f - Mathf.Exp(-14f * dt));

            // Los dos tiemblan empujando; el punto de choque se corre hacia el que va perdiendo
            Vector2 contact = ClashPoint(shown);
            Vector2 shake = UnityEngine.Random.insideUnitCircle * 0.006f;
            SetTsuki(contact + axis * contactGap + shake);
            SetVarn(contact - axis * contactGap - shake);
            pressPose -= dt;
            tsuki.HoldSlashPose(tsukiFacesRight, pressPose > 0f ? 2 : 1);

            ambient -= dt;
            if (ambient <= 0f)
            {
                Burst(ContactPoint(contact), 1, 0.9f);
                ambient = 0.08f;
            }
            SetMeter(shown);
            UpdateFocus();

            if (power >= 1f) { done(true); break; }
            if (power <= 0f) { done(false); break; }
            yield return null;
        }
        ShowMeter(false);
    }

    // final: Varn se arrodilla y se queda así; si no, solo sale empujado (el jefe sigue la pelea)
    private IEnumerator Win(bool final)
    {
        flashAlpha = 1f;
        glowPulse = 1.4f;
        Vector2 contact = ClashPoint(1f);
        Burst(ContactPoint(contact), 28, 2.8f);
        CameraShake.Shake(0.07f, 0.45f);

        // Tsuki termina el corte y Varn sale empujado
        Vector2 varnFrom = varnMover.position;
        Vector2 varnTo = varnFrom - axis * (final ? 0.5f : 0.3f);
        int frames = Mathf.Max(1, tsuki.SlashPoseFrames);
        const float time = 0.45f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = 1f - (1f - t / time) * (1f - t / time);
            SetVarn(Vector2.Lerp(varnFrom, varnTo, k));
            tsuki.HoldSlashPose(tsukiFacesRight, Mathf.Min(2 + (int)(t * 16f), frames - 1));
            UpdateFocus();
            yield return null;
        }
        SetVarn(varnTo);
        tsuki.HoldSlashPose(tsukiFacesRight, -1);
        tsuki.FaceTowards(-axis);
        if (!final) yield break;
        varn.Play(GuardianVarnAnimator.Defeat, 8f, false);
        yield return new WaitForSeconds(1.2f);
    }

    // Varn gana el choque de escudo: empuja con el escudo y Tsuki sale volando
    private IEnumerator ShieldBash()
    {
        flashAlpha = 0.5f;
        varn.PlayRange(DashClip, 4, 7, 16f, false);
        Burst(ContactPoint(ClashPoint(0f)), 16, 2.2f);
        CameraShake.Shake(0.06f, 0.35f);
        tsuki.HoldSlashPose(tsukiFacesRight, -1);
        tsuki.FaceTowards(-axis);
        if (tsukiHealth != null) tsukiHealth.TakeDamage(Difficulty.Damage(shieldBashDamage));

        Vector2 from = tsukiBody.position;
        Vector2 to = InsideArena(from + axis * shieldBashDistance);
        const float time = 0.3f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = 1f - (1f - t / time) * (1f - t / time);
            SetTsuki(Vector2.Lerp(from, to, k));
            UpdateFocus();
            yield return null;
        }
        SetTsuki(to);
    }

    // El empujón no saca a Tsuki de la arena (la elipse que usa Varn)
    private Vector2 InsideArena(Vector2 p)
    {
        var boss = varnMover.GetComponent<GuardianBoss>();
        if (boss == null) return p;
        Vector2 c = boss.ArenaCenter;
        Vector2 radii = boss.ArenaRadii * 0.95f;
        Vector2 d = p - c;
        float k = (d.x * d.x) / (radii.x * radii.x) + (d.y * d.y) / (radii.y * radii.y);
        return k > 1f ? c + d / Mathf.Sqrt(k) : p;
    }

    private IEnumerator Lose()
    {
        flashAlpha = 0.5f;
        Burst(ContactPoint(ClashPoint(0f)), 16, 2.2f);
        CameraShake.Shake(0.05f, 0.3f);
        tsuki.HoldSlashPose(tsukiFacesRight, -1);
        tsuki.FaceTowards(-axis);
        if (tsukiHealth != null)
        {
            // La defensa se suma para que la pérdida sea la fracción pedida
            int damage = Mathf.CeilToInt(tsukiHealth.MaxHealth * loseDamage * Difficulty.DamageToPlayer) + tsukiHealth.Defense;
            tsukiHealth.TakeDamage(damage);
        }

        Vector2 from = tsukiBody.position;
        Vector2 to = from + axis * 0.6f;
        const float time = 0.35f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = 1f - (1f - t / time) * (1f - t / time);
            SetTsuki(Vector2.Lerp(from, to, k));
            UpdateFocus();
            yield return null;
        }
        SetTsuki(to);
        varn.PlayFacing("Idle", axis, 6f, true);
        yield return new WaitForSeconds(0.8f);
    }

    // ---------------------------------------------------------------- utilidades

    private string DashClip => "ShieldDash_" + GuardianVarnAnimator.Direction(axis);

    // Punto de choque según el medidor: 0.5 = al centro, 1 = Tsuki llegó hasta Varn
    private Vector2 ClashPoint(float power) => center - axis * (power - 0.5f) * clashShift;

    // Donde se tocan las armas: a media altura de Tsuki
    private Vector2 ContactPoint(Vector2 feet) =>
        feet + Vector2.up * (tsukiSprite != null ? tsukiSprite.bounds.size.y * 0.45f : 0.25f);

    private static bool AttackPressed() =>
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) ||
        GamepadInput.WestPressed;

    // Con la interpolación activa el cuerpo pisaría la posición que se pone a mano
    private void SetScripted(bool scripted)
    {
        tsukiBody.interpolation = scripted ? RigidbodyInterpolation2D.None : tsukiInterpolation;
        tsukiBody.linearVelocity = Vector2.zero;
    }

    private void SetTsuki(Vector2 position)
    {
        tsukiBody.position = position;
        tsukiBody.linearVelocity = Vector2.zero;
        var t = tsuki.transform;
        t.position = new Vector3(position.x, position.y, t.position.z);
    }

    private void SetVarn(Vector2 position)
    {
        if (varnRb != null)
        {
            varnRb.position = position;
            varnRb.linearVelocity = Vector2.zero;
        }
        varnMover.position = new Vector3(position.x, position.y, varnZ);
    }

    private void UpdateFocus()
    {
        Vector2 a = tsukiSprite != null ? (Vector2)tsukiSprite.bounds.center : tsukiBody.position;
        Vector2 b = varnSprite != null ? (Vector2)varnSprite.bounds.center : (Vector2)varnMover.position;
        focus = (a + b) * 0.5f;
    }

    private void Cleanup()
    {
        if (!running) return;
        running = false;
        if (tsukiBody != null)
        {
            tsukiBody.interpolation = tsukiInterpolation;
            tsukiBody.linearVelocity = Vector2.zero;
        }
        if (tsuki != null)
        {
            tsuki.HoldSlashPose(tsukiFacesRight, -1);
            tsuki.SetScriptedMove(Vector2.zero);
        }
        CameraFocus.Release();
        if (ui != null) Destroy(ui);
        foreach (var s in sparks)
            if (s.sr != null) Destroy(s.sr.gameObject);
        sparks.Clear();
        if (glow != null) Destroy(glow.gameObject);
        if (locked)
        {
            GameInput.Unlock(this);
            locked = false;
        }
        InProgress = false;
    }

    private void OnDisable() => Cleanup();

    // ---------------------------------------------------------------- efectos

    private void BuildEffects()
    {
        // El resplandor se crea en cada choque; el material y los sprites se reutilizan
        if (glowSprite != null)
        {
            glow = NewFx("ClashGlow", glowSprite, -11);
            glow.color = new Color(1f, 0.55f, 0.6f, 0f);
            return;
        }
        fxMaterial = Resources.Load<Material>(MaterialResource);
        if (fxMaterial == null) fxMaterial = new Material(Shader.Find("Sprites/Default"));

        var white = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        white.Apply();
        dotSprite = Sprite.Create(white, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);

        // Resplandor redondo y suave en el punto de choque
        const int n = 32;
        var soft = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                soft.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        soft.Apply();
        glowSprite = Sprite.Create(soft, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);

        glow = NewFx("ClashGlow", glowSprite, -11);
        glow.color = new Color(1f, 0.55f, 0.6f, 0f);
    }

    private SpriteRenderer NewFx(string name, Sprite sprite, int order)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = fxMaterial;
        // Por delante de los personajes, por debajo del HUD
        sr.sortingLayerName = "UI";
        sr.sortingOrder = order;
        return sr;
    }

    private void Burst(Vector2 at, int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            var s = new Spark { sr = NewFx("Spark", dotSprite, -10) };
            s.sr.transform.position = at;
            s.sr.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.8f);
            s.sr.color = UnityEngine.Random.value < 0.5f ? new Color(1f, 0.95f, 0.75f) : new Color(1f, 0.45f, 0.5f);
            s.velocity = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.3f, 1f) * speed * 0.5f;
            s.maxLife = s.life = UnityEngine.Random.Range(0.18f, 0.4f);
            sparks.Add(s);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i];
            s.life -= dt;
            if (s.life <= 0f || s.sr == null)
            {
                if (s.sr != null) Destroy(s.sr.gameObject);
                sparks.RemoveAt(i);
                continue;
            }
            s.velocity *= 1f - 4f * dt;
            s.sr.transform.position += (Vector3)(s.velocity * dt);
            var c = s.sr.color;
            c.a = s.life / s.maxLife;
            s.sr.color = c;
        }

        if (glow != null && tsukiBody != null)
        {
            bool clashing = meterGroup != null && meterGroup.alpha > 0f;
            glowPulse = Mathf.MoveTowards(glowPulse, 0f, dt * 3f);
            float strength = Mathf.Clamp01((clashing ? 0.5f : 0f) + glowPulse);
            glow.transform.position = ContactPoint((Vector2)tsukiBody.position - axis * contactGap);
            glow.transform.localScale = Vector3.one * (0.35f + 0.25f * strength + Mathf.Sin(Time.time * 30f) * 0.02f);
            glow.color = new Color(1f, 0.55f, 0.6f, strength * 0.85f);
        }

        if (flash != null)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 3f);
            flash.color = new Color(1f, 0.95f, 0.95f, flashAlpha * 0.8f);
            flash.enabled = flashAlpha > 0f;
        }

        if (promptRect != null && meterGroup != null && meterGroup.alpha > 0f)
            promptRect.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f)) * 0.06f);
    }

    // ---------------------------------------------------------------- medidor

    // Como el HUD: panel oscuro con marco dorado, rombos con la cara de cada uno en los extremos,
    // barra con contorno dorado y rellenos en degradado (carmesí Tsuki, violeta Varn) y un rombo
    // dorado que late donde se juntan. Arriba, un listón carmesí con el botón a presionar.
    private void BuildUi(bool tsukiOnLeft)
    {
        ui = new GameObject("PowerClashUI", typeof(RectTransform));
        var canvas = ui.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Encima del tinte de la cámara lenta (40), debajo de la UI de GameSystems (50)
        canvas.sortingOrder = 45;
        var scaler = ui.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        font = HudFont();

        flash = NewImage(ui.transform, "Flash", Color.clear);
        Stretch(flash.rectTransform, 0f);
        flash.enabled = false;

        var meter = new GameObject("Meter", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)meter.transform;
        rt.SetParent(ui.transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 110f);
        rt.sizeDelta = new Vector2(1000f, 130f);
        meterGroup = meter.GetComponent<CanvasGroup>();
        meterGroup.alpha = 0f;
        meterGroup.blocksRaycasts = false;
        UiSkin.ThemePanel(rt);

        // Barra: contorno dorado, fondo oscuro, relleno de Varn debajo y el de Tsuki encima
        var bar = NewRect(rt, "Bar");
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = new Vector2(0f, -16f);
        bar.sizeDelta = new Vector2(700f, 34f);
        NewImage(bar, "Outline", UiArt.Gold).rectTransform.Fill(0f);
        var back = NewImage(bar, "Back", new Color32(34, 10, 22, 255)).rectTransform;
        back.Fill(3f);
        var varnFill = NewImage(back, "Varn", Color.white);
        varnFill.sprite = UiArt.Get("barfill_varn", () => UiArt.Gradient(new Color32(150, 104, 214, 255), new Color32(58, 28, 98, 255), 8));
        varnFill.rectTransform.Fill(0f);
        var tsukiFill = NewImage(back, "Tsuki", Color.white);
        tsukiFill.sprite = UiArt.Get("barfill", () => UiArt.Gradient(UiArt.CrimsonLight, UiArt.CrimsonDark, 8));
        meterFill = tsukiFill.rectTransform;
        // Brillo en la mitad de arriba y marcas cada cuarto
        var shine = NewImage(back, "Shine", new Color(1f, 0.85f, 0.9f, 0.14f)).rectTransform;
        shine.anchorMin = new Vector2(0f, 0.55f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = shine.offsetMax = Vector2.zero;
        for (int i = 1; i <= 3; i++)
        {
            var tick = NewImage(back, "Tick", new Color(1f, 0.86f, 0.55f, i == 2 ? 0.55f : 0.3f)).rectTransform;
            tick.anchorMin = new Vector2(i * 0.25f, 0f);
            tick.anchorMax = new Vector2(i * 0.25f, 1f);
            tick.sizeDelta = new Vector2(i == 2 ? 4f : 2f, 0f);
        }

        // Rombo dorado que marca dónde chocan, con un resplandor detrás
        var markerGlow = NewImage(bar, "MarkerGlow", new Color(1f, 0.8f, 0.4f, 0.5f));
        markerGlow.sprite = glowSprite;
        markerGlow.rectTransform.sizeDelta = new Vector2(110f, 110f);
        meterGlow = markerGlow.rectTransform;
        var marker = NewImage(bar, "Marker", Color.white);
        marker.sprite = UiArt.Get("diamond", UiArt.Diamond);
        marker.rectTransform.sizeDelta = new Vector2(44f, 44f);
        meterMarker = marker.rectTransform;

        // Rombos con la cara de cada uno, del lado en que están en pantalla
        float tsukiX = tsukiOnLeft ? -1f : 1f;
        var tsukiFace = UiSkin.PortraitDiamond(rt, Resources.Load<Sprite>("Portraits/tsuki_bust"), new Vector2(28f, -118f), 84f, false);
        tsukiFace.anchoredPosition = new Vector2(tsukiX * 420f, 0f);
        // Varn mira hacia Tsuki: el busto original mira a la izquierda
        var varnFace = UiSkin.PortraitDiamond(rt, Resources.Load<Sprite>("Portraits/varn_bust"), new Vector2(-8f, -150f), 84f, !tsukiOnLeft);
        varnFace.anchoredPosition = new Vector2(-tsukiX * 420f, 0f);

        // Nombres sobre cada extremo de la barra, alineados hacia afuera (sin tapar los rombos)
        NewLabel(rt, "Tsuki", new Vector2(tsukiX * 225f, 26f), 30f, tsukiOnLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, true);
        NewLabel(rt, "Varn", new Vector2(-tsukiX * 225f, 26f), 30f, tsukiOnLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, true);

        // Listón carmesí con la instrucción, sobre el panel
        var ribbon = NewRect(rt, "Prompt");
        ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 1f);
        ribbon.anchoredPosition = new Vector2(0f, 26f);
        ribbon.sizeDelta = new Vector2(560f, 58f);
        var ribbonImg = ribbon.gameObject.AddComponent<Image>();
        ribbonImg.sprite = UiSkin.RibbonSprite(24);
        ribbonImg.type = Image.Type.Sliced;
        ribbonImg.pixelsPerUnitMultiplier = 1f / UiSkin.Px;
        ribbonImg.raycastTarget = false;
        prompt = NewLabel(ribbon, "¡Presiona " + ControlHints.Cap("attack") + " rápido!", Vector2.zero, 32f, TextAlignmentOptions.Center, false);
        prompt.rectTransform.sizeDelta = new Vector2(560f, 58f);
        promptRect = ribbon;

        meterLeftIsTsuki = tsukiOnLeft;
        SetMeter(0.5f);
    }

    private TMP_FontAsset font;
    private RectTransform meterGlow;
    private RectTransform promptRect;

    private void SetMeter(float tsukiShare)
    {
        if (meterFill == null) return;
        float x = meterLeftIsTsuki ? tsukiShare : 1f - tsukiShare;
        meterFill.anchorMin = new Vector2(meterLeftIsTsuki ? 0f : x, 0f);
        meterFill.anchorMax = new Vector2(meterLeftIsTsuki ? x : 1f, 1f);
        meterFill.offsetMin = meterFill.offsetMax = Vector2.zero;
        foreach (var m in new[] { meterMarker, meterGlow })
        {
            m.anchorMin = m.anchorMax = new Vector2(x, 0.5f);
            m.anchoredPosition = Vector2.zero;
        }
        // El rombo late más fuerte cuanto más cerca está alguien de ganar
        float danger = Mathf.Abs(tsukiShare - 0.5f) * 2f;
        float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.Lerp(8f, 16f, danger))) * 0.12f;
        meterMarker.localScale = Vector3.one * pulse;
        meterMarker.localRotation = Quaternion.identity;
        meterGlow.localScale = Vector3.one * (pulse + danger * 0.4f);
    }

    private void ShowMeter(bool visible)
    {
        if (meterGroup != null) meterGroup.alpha = visible ? 1f : 0f;
    }

    private static RectTransform NewRect(Transform parent, string name)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static Image NewImage(Transform parent, string name, Color color)
    {
        var image = NewRect(parent, name).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt, float inset) => rt.Fill(inset);

    // Misma fuente que el HUD (la de los números de vida)
    private static TMP_FontAsset HudFont()
    {
        var hud = GameHud.Instance;
        var label = hud != null ? UiNav.FindLabel(hud.transform, "HealthText") : null;
        return label != null ? label.font : null;
    }

    // gold: título dorado en degradado como los del HUD; si no, texto crema
    private TextMeshProUGUI NewLabel(Transform parent, string text, Vector2 pos, float size, TextAlignmentOptions alignment, bool gold)
    {
        var label = NewRect(parent, "Label").gameObject.AddComponent<TextMeshProUGUI>();
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(240f, 44f);
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        if (gold)
        {
            label.color = Color.white;
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(
                new Color(1f, 0.93f, 0.70f), new Color(1f, 0.93f, 0.70f),
                new Color(0.90f, 0.62f, 0.32f), new Color(0.90f, 0.62f, 0.32f));
        }
        else label.color = UiArt.TextCream;
        return label;
    }
}

static class PowerClashRectExtensions
{
    // Ocupa todo el padre dejando un margen
    public static void Fill(this RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
