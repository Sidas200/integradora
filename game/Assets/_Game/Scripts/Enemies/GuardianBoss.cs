using System;
using System.Collections;
using UnityEngine;

// Primer guardián (Varn). Ciclo: acercarse → patrón telegrafiado → ventana vulnerable.
//  - Embestida: franja roja en el suelo hacia el jugador, luego carga en línea recta.
//  - Golpe del juicio: círculo rojo alrededor del guardián, luego golpe en área.
// Solo recibe daño en la ventana vulnerable (EnemyHealth.Invulnerable) y tiene invulnerabilidad
// breve tras cada golpe (invulnerableTime de EnemyHealth). Bajo 50% de vida es más rápido.
// Las armas normales no lo hieren: sin la espada mejorada es invulnerable todo el tiempo.
// Duerme hasta que le ofrecen una Gota de Amrita (GuardianArena). Los ataques tienen
// fuego, polvo y escombros (BossFx) además de la zona roja.
// Sprites (GuardianVarnAnimator en "Visual"): fase 1 pelea a puños en vista sur; al cruzar el 50%
// saca la espada del pecho y pasa a los sprites de 8 direcciones con espada y escudo.
// Fase 1 (puños): cuerpo a cuerpo de alcance corto, un ataque y queda vulnerable.
// Fase 2 (espada): más alcance, dos ataques seguidos, onda tras el golpe, lluvia de escombros y
// el corte que abre el piso en línea recta hasta casi el borde de la arena.
// En la fase 2, uno de cada dos ciclos se cubre con el escudo un momento (brilla en dorado): si
// Tsuki lo golpea de cerca en ese momento chocan (PowerClash). Si gana Tsuki, Varn queda aturdido
// y vulnerable; si gana Varn, la empuja con el escudo y la aturde.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class GuardianBoss : MonoBehaviour
{
    public enum State { Dormant, Approach, Telegraph, Attack, Vulnerable, Dead, Guard }

    [SerializeField] private string displayName = "Varn, Guardián del Umbral";
    [SerializeField] private Transform arenaCenter;
    [Tooltip("Semiejes de la arena en el mundo: un círculo de celdas se ve como elipse en isométrico")]
    [SerializeField] private Vector2 arenaRadii = new Vector2(2.1f, 1.05f);

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 0.65f;
    [SerializeField] private float approachTime = 1.1f;

    [Header("Embestida")]
    [SerializeField] private float chargeTelegraph = 1.0f;
    [SerializeField] private float chargeSpeed = 5f;
    [SerializeField] private float chargeLength = 2.6f;
    [SerializeField] private float chargeWidth = 0.36f;
    // Con la armadura completa (100 de vida, 6 de defensa) la embestida y el golpe matan en 4 golpes
    [SerializeField] private int chargeDamage = 36;

    [Header("Golpe del juicio (área)")]
    [SerializeField] private float slamTelegraph = 1.2f;
    [SerializeField] private float slamRadius = 0.95f;
    [SerializeField] private int slamDamage = 40;

    [Header("Fase 2 (bajo 50% de vida)")]
    [Tooltip("Onda del golpe al suelo: radio respecto al golpe y aviso antes de que llegue.")]
    [SerializeField] private float shockwaveRadiusScale = 1.8f;
    [SerializeField] private float shockwaveDelay = 0.45f;
    [SerializeField] private int shockwaveDamage = 24;

    [Header("Fase 1: puños (cuerpo a cuerpo)")]
    [Tooltip("Largo del puñetazo con embestida respecto a la embestida completa.")]
    [SerializeField, Range(0.3f, 1f)] private float fistReachScale = 0.6f;
    [Tooltip("Radio del golpe al suelo con los puños respecto al golpe del juicio.")]
    [SerializeField, Range(0.4f, 1f)] private float fistSlamScale = 0.8f;
    [Tooltip("Distancia a la que el puñetazo alcanza a Tsuki.")]
    [SerializeField] private float fistHitRange = 0.28f;

    [Header("Fase 2: espada")]
    [SerializeField, Range(1f, 2f)] private float swordSlamScale = 1.15f;
    [Tooltip("Ancho de la embestida con escudo respecto al normal.")]
    [SerializeField, Range(1f, 2f)] private float swordChargeWidthScale = 1.3f;
    [SerializeField] private float swordHitRange = 0.4f;

    [Header("Fase 2: lluvia de escombros")]
    [Tooltip("Apagada por ahora: en su lugar usa el golpe al suelo. Activar para volver a usarla.")]
    [SerializeField] private bool useDebrisRain = false;
    [Tooltip("Varn clava la espada en el suelo y caen escombros cerca de Tsuki (uno justo encima de ella).")]
    [SerializeField, Min(1)] private int debrisCount = 4;
    [Tooltip("Segundos que avisa cada escombro antes de caer (no se acelera con la fase 2).")]
    [SerializeField] private float debrisWarning = 1f;
    [SerializeField] private float debrisInterval = 0.18f;
    [SerializeField] private float debrisRadius = 0.24f;
    [Tooltip("Qué tan lejos de Tsuki pueden caer los demás escombros.")]
    [SerializeField] private float debrisSpread = 0.9f;
    [SerializeField] private int debrisDamage = 22;
    [Tooltip("Daño a los enemigos que queden debajo (sus refuerzos también).")]
    [SerializeField] private int debrisEnemyDamage = 40;

    [Header("Fase 2: se cubre con el escudo (choque)")]
    [Tooltip("Cada cuántos ciclos de ataque se cubre (1 = siempre).")]
    [SerializeField, Min(1)] private int guardEvery = 2;
    [Tooltip("Segundos que mantiene el escudo arriba: es el momento para golpearlo.")]
    [SerializeField] private float guardTime = 2f;
    [Tooltip("Qué tan cerca tiene que estar Tsuki al golpearlo para que haya choque.")]
    [SerializeField] private float guardHitRange = 0.8f;
    [Tooltip("Segundos que queda aturdido (y vulnerable) si pierde el choque.")]
    [SerializeField] private float stunTime = 2f;

    [Header("Fase 2: escena de la espada")]
    [Tooltip("Segundos que dura la animación de sacarse la espada del pecho.")]
    [SerializeField] private float drawSwordTime = 2f;
    [SerializeField] private Color guardColor = new Color(1f, 0.82f, 0.3f, 1f);

    [Header("Fase 2: corte que abre el piso")]
    [Tooltip("Varn clava la espada y una grieta sale de él hacia Tsuki hasta casi el borde de la arena.")]
    [SerializeField] private float riftTelegraph = 0.75f;
    [SerializeField] private float riftWidth = 0.3f;
    [Tooltip("Velocidad con la que se abre la grieta (unidades por segundo).")]
    [SerializeField] private float riftSpeed = 7f;
    [SerializeField] private int riftDamage = 30;

    [Header("Contacto")]
    [Tooltip("Daño al tocar a Varn mientras camina; una vez por segundo como máximo.")]
    [SerializeField] private int contactDamage = 16;
    [SerializeField] private float contactRange = 0.3f;

    [Header("Ventana vulnerable")]
    [SerializeField] private float vulnerableTime = 1.3f;
    [SerializeField, Range(0.3f, 1f)] private float enragedTimeScale = 0.75f;
    [Tooltip("Multiplica la velocidad de sus ataques: acercarse, avisar y embestir (1.32 ≈ dos veces +15%). " +
             "La ventana en que es vulnerable no cambia.")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 1.32f;

    [Header("Escudo mientras hay refuerzos vivos")]
    [SerializeField] private Color shieldColor = new Color(0.55f, 0.8f, 1f, 1f);
    [SerializeField] private float shieldGlowScale = 1.18f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite lineSprite;
    [SerializeField] private Color armoredTint = new Color(1f, 0.88f, 0.55f);
    [SerializeField] private Color vulnerableTint = new Color(0.7f, 0.75f, 0.95f);
    [SerializeField] private Color sleepingTint = new Color(0.55f, 0.58f, 0.72f);
    [SerializeField] private BossFx fx;

    public State Current { get; private set; } = State.Dormant;
    public string DisplayName => displayName;
    public EnemyHealth Health => health;
    public Vector2 ArenaCenter => arenaCenter != null ? (Vector2)arenaCenter.position : (Vector2)transform.position;
    public Vector2 ArenaRadii => arenaRadii;
    public Sprite CircleSprite => circleSprite;
    // Copia del sprite que queda en la arena al morir (el jefe se apaga); la usa el choque de poder
    public GuardianVarnAnimator DefeatedBody { get; private set; }
    // Centro de su sprite (para enfocar la cámara)
    public Vector2 FocusPoint => body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;

    public event Action<State> StateChanged;
    // Terminó de sacarse la espada (segunda fase). GuardianArena pone el diálogo;
    // Varn espera a que se cierre el diálogo antes de atacar.
    // Escena al cruzar el 50% (la pone GuardianArena: pausa, cámara, diálogo y PlayDrawSword).
    // Sin ella solo se reproduce la animación.
    public Func<IEnumerator> SwordCutscene { get; set; }
    public float DrawSwordTime => drawSwordTime;

    private Rigidbody2D rb;
    private EnemyHealth health;
    private Transform player;
    private PlayerStats playerStats;
    private Telegraph telegraph;
    private bool enraged;
    private bool warnedWeakSword;
    private int patternIndex;
    private Coroutine breathing;
    private GuardianVarnAnimator anim;
    private bool swordDrawn;
    private int guardCycle;
    private bool guardHit;
    private bool warnedGuard;
    private PowerClash.Outcome guardResult;

    private const float Fps = 10f;
    private Vector2 Facing => player != null ? (Vector2)player.position - rb.position : Vector2.down;

    public void SetArena(Transform center, Vector2 radii)
    {
        arenaCenter = center;
        arenaRadii = radii;
    }

    public void SetFx(BossFx effects) => fx = effects;

    // Dormido: color apagado y respiración lenta. No ataca ni recibe daño.
    public void SetSleeping(bool sleeping)
    {
        if (breathing != null) StopCoroutine(breathing);
        breathing = null;
        if (body == null) return;
        body.transform.localScale = Vector3.one;
        body.color = sleeping ? sleepingTint : armoredTint;
        if (sleeping) breathing = StartCoroutine(Breathe());
        // Dormido en el trono: primer frame de la intro
        if (anim != null)
        {
            if (sleeping) anim.Hold(GuardianVarnAnimator.Intro, 0);
            else Idle();
        }
    }

    private void Idle()
    {
        if (anim == null) return;
        if (swordDrawn) anim.PlayFacing("Idle", Facing, 6f, true);
        else anim.Hold(GuardianVarnAnimator.FistCombo, 0);
    }

    private IEnumerator Breathe()
    {
        for (float t = 0f; ; t += Time.deltaTime)
        {
            body.transform.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.6f) * 0.025f, 1f);
            yield return null;
        }
    }

    // Despertar con la Gota de Amrita: brilla, tiembla y se levanta.
    public IEnumerator WakeUp()
    {
        SetSleeping(false);
        if (fx != null) fx.WakeBurst(rb.position);
        if (body == null) yield break;
        // Se levanta del trono y el trono desaparece (12 frames en 1.2 s)
        if (anim != null) anim.Play(GuardianVarnAnimator.Intro, Fps, false);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            float k = t / 1.2f;
            body.color = Color.Lerp(Color.white, armoredTint, k);
            body.transform.localPosition = new Vector3(Mathf.Sin(t * 50f) * 0.01f * (1f - k), 0f, 0f);
            yield return null;
        }
        body.transform.localPosition = Vector3.zero;
        body.color = armoredTint;
    }

    public void Setup(Transform center, Sprite circle, Sprite line, SpriteRenderer bodyRenderer)
    {
        arenaCenter = center;
        circleSprite = circle;
        lineSprite = line;
        body = bodyRenderer;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        anim = body != null ? body.GetComponent<GuardianVarnAnimator>() : null;
        health.Invulnerable = true;
        health.Blocked += OnBlocked;
        health.HealthChanged += OnHealthChanged;
        health.Died += () =>
        {
            SetState(State.Dead);
            if (anim != null) DefeatedBody = anim.SpawnCopy();
            PlayStats.BossDefeated(TryGetComponent<PersistentId>(out var pid) ? pid.Id : name);
        };
    }

    private void OnDestroy()
    {
        if (telegraph != null) Destroy(telegraph.gameObject);
    }

    // ---------------------------------------------------------------- escudo

    private bool shielded;
    private SpriteRenderer shieldGlow;
    private float nextShieldToast;

    public bool Shielded => shielded;

    // GuardianSummons: mientras viven sus refuerzos Varn no recibe daño y brilla en azul.
    public void SetShielded(bool value)
    {
        if (shielded == value) return;
        shielded = value && Current != State.Dead;
        ApplyProtection();
        EnsureShieldGlow();
        if (shieldGlow != null) shieldGlow.enabled = shielded;
        if (!value && Current != State.Dead && GameHud.Instance != null)
            GameHud.Instance.ShowToast("¡El escudo de Varn se rompió! Ya puedes herirlo.");
    }

    // Copia del sprite detrás del cuerpo, un poco más grande, que late: se ve como un aura
    private void EnsureShieldGlow()
    {
        if (shieldGlow != null || body == null) return;
        var go = new GameObject("ShieldGlow");
        go.transform.SetParent(body.transform, false);
        go.transform.localScale = Vector3.one * shieldGlowScale;
        shieldGlow = go.AddComponent<SpriteRenderer>();
        shieldGlow.sharedMaterial = body.sharedMaterial;
        shieldGlow.sortingLayerID = body.sortingLayerID;
        shieldGlow.sortingOrder = body.sortingOrder - 1;
        shieldGlow.enabled = false;
    }

    private void Update()
    {
        bool guarding = Current == State.Guard;
        if ((!shielded && !guarding) || shieldGlow == null || body == null) return;
        shieldGlow.sprite = body.sprite;
        shieldGlow.flipX = body.flipX;
        // Cubriéndose: dorado y más rápido, para que se note que es el momento de golpear
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (guarding ? 14f : 6f));
        var glowColor = guarding ? guardColor : shieldColor;
        shieldGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(0.35f, 0.85f, pulse));
        // El cuerpo también destella en dorado para que se vea aunque el aura quede tapada
        if (guarding) body.color = Color.Lerp(armoredTint, guardColor, pulse * 0.7f);
        shieldGlow.transform.localScale = Vector3.one * Mathf.Lerp(shieldGlowScale, shieldGlowScale + 0.06f, pulse);
    }

    private void OnDisable()
    {
        if (telegraph != null) telegraph.Hide();
    }

    // Lo llama la arena después del diálogo de presentación.
    public void Begin()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        player = p.transform;
        playerStats = p.GetComponent<PlayerStats>();
        telegraph = Telegraph.Create(circleSprite, "GuardianTelegraph");
        StartCoroutine(Loop());
    }

    private bool PlayerAlive()
    {
        var h = player != null ? player.GetComponent<Health>() : null;
        return h != null && !h.IsDead;
    }

    private float nextContactTime;

    private float Scale => enraged ? enragedTimeScale : 1f;
    // Tiempos del ataque (acercamiento, aviso, embestida); el tiempo vulnerable usa solo Scale
    private float AttackScale => Scale / attackSpeed;

    private IEnumerator Loop()
    {
        while (Current != State.Dead)
        {
            if (!PlayerAlive()) { SetState(State.Dormant); yield break; }
            if (enraged && !swordDrawn) yield return DrawSwordFromChest();

            yield return Approach();

            // Fase 2: a veces se cubre con el escudo; si Tsuki gana el choque queda aturdido
            if (swordDrawn && !shielded && guardCycle++ % guardEvery == 0)
            {
                yield return Guard();
                if (Current == State.Dead) yield break;
                if (guardResult == PowerClash.Outcome.Won)
                {
                    yield return Stunned();
                    continue;
                }
                // Ganó Varn: espera un poco para que los golpes lleguen cuando Tsuki ya se mueve
                if (guardResult == PowerClash.Outcome.Lost)
                {
                    Idle();
                    yield return new WaitForSeconds(1.2f);
                }
            }

            bool first = patternIndex++ % 2 == 0;
            if (!swordDrawn)
            {
                // Fase 1: un ataque a puños y queda vulnerable
                yield return first ? Charge() : Slam();
            }
            else
            {
                // Fase 2: dos ataques seguidos, rotando entre tres combos
                int combo = (patternIndex - 1) % 3;
                yield return combo == 0 ? Charge() : combo == 1 ? (useDebrisRain ? DebrisRain() : Slam()) : RiftSlash();
                if (Current == State.Dead) yield break;
                yield return combo == 0 ? Slam() : combo == 1 ? RiftSlash() : Charge();
            }
            if (Current == State.Dead) yield break;
            yield return Vulnerable();
        }
    }

    // Al cruzar el 50% de vida se saca la espada del pecho; desde aquí usa los sprites con espada.
    // Mientras dura la animación está protegido (no es su ventana vulnerable).
    private IEnumerator DrawSwordFromChest()
    {
        SetState(State.Attack);
        swordDrawn = true;
        yield return SwordCutscene != null ? SwordCutscene() : PlayDrawSword(drawSwordTime);
    }

    // La animación de sacarse la espada estirada a 'seconds' para apreciarla. Corre con tiempo sin
    // escala: se ve aunque el juego esté detenido.
    public IEnumerator PlayDrawSword(float seconds)
    {
        if (anim == null) yield break;
        anim.UnscaledTime = true;
        if (fx != null) fx.SetUnscaledTime(true);
        int frames = Mathf.Max(1, Mathf.RoundToInt(anim.Duration(GuardianVarnAnimator.DrawSword, 1f)));
        anim.Play(GuardianVarnAnimator.DrawSword, frames / Mathf.Max(0.1f, seconds), false);
        if (fx != null) fx.WakeBurst(rb.position);
        yield return new WaitForSecondsRealtime(seconds * 0.6f);
        // Al salir la espada: segundo estallido
        if (fx != null) fx.SlamImpact(rb.position, 0.35f);
        yield return new WaitForSecondsRealtime(seconds * 0.4f);
        anim.UnscaledTime = false;
        if (fx != null) fx.SetUnscaledTime(false);
    }

    private IEnumerator Approach()
    {
        SetState(State.Approach);
        Idle();
        for (float t = 0f; t < approachTime * AttackScale; t += Time.fixedDeltaTime)
        {
            Vector2 dir = ((Vector2)player.position - rb.position);
            if (dir.magnitude > 0.5f)
            {
                MoveTo(rb.position + dir.normalized * walkSpeed * Time.fixedDeltaTime);
                // Fase 1 no tiene caminata dibujada: la pose de guardia da pasos con un rebote
                if (anim != null && swordDrawn) anim.PlayFacing("Walk", dir, Fps, true);
                else if (anim != null) body.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 9f)) * 0.02f, 0f);
            }
            ContactDamage();
            yield return new WaitForFixedUpdate();
        }
        if (anim != null) body.transform.localPosition = Vector3.zero;
    }

    private IEnumerator Charge()
    {
        SetState(State.Telegraph);
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        telegraph.GetComponent<SpriteRenderer>().sprite = lineSprite;
        // Puños: embestida corta y angosta; escudo: completa y más ancha
        float length = chargeLength * (swordDrawn ? 1f : fistReachScale);
        float width = chargeWidth * (swordDrawn ? swordChargeWidthScale : 1f);
        float hitRange = swordDrawn ? swordHitRange : fistHitRange;
        telegraph.ShowLine(rb.position, dir, length, width, chargeTelegraph * AttackScale, Telegraph.Danger);
        string dash = "ShieldDash_" + GuardianVarnAnimator.Direction(dir);
        if (anim != null)
        {
            // Fase 1 carga el puño; fase 2 encara la línea con el escudo
            if (swordDrawn) anim.PlayFacing("Idle", dir, 6f, true);
            else anim.PlayRange(GuardianVarnAnimator.FistCombo, 1, 3, 3f / (chargeTelegraph * AttackScale), false);
        }
        if (fx != null) fx.LineFlames(rb.position, dir, length, width, chargeTelegraph * AttackScale);
        yield return Shake(chargeTelegraph * AttackScale);
        telegraph.Hide();

        SetState(State.Attack);
        if (anim != null)
        {
            if (swordDrawn) anim.PlayRange(dash, 1, 6, 14f, true);
            else anim.Hold(GuardianVarnAnimator.FistCombo, 4);
        }
        Vector2 start = rb.position;
        bool hit = false;
        float speed = chargeSpeed / AttackScale;
        while (Vector2.Distance(start, rb.position) < length)
        {
            Vector2 next = rb.position + dir * speed * Time.fixedDeltaTime;
            if (!MoveTo(next)) break; // borde de la arena
            if (!hit && Vector2.Distance(rb.position, player.position) < hitRange)
                hit = CombatUtil.DamagePlayer(player.gameObject, chargeDamage, rb.position);
            if (fx != null) fx.ChargeStep(rb.position, dir);
            yield return new WaitForFixedUpdate();
        }
        if (fx != null) fx.ChargeEnd(rb.position);
        if (anim != null)
        {
            if (swordDrawn) anim.Hold(dash, 7);
            else anim.PlayRange(GuardianVarnAnimator.FistCombo, 5, 7, 12f, false);
        }
    }

    private IEnumerator Slam()
    {
        SetState(State.Telegraph);
        float radius = slamRadius * (swordDrawn ? swordSlamScale : fistSlamScale);
        telegraph.GetComponent<SpriteRenderer>().sprite = circleSprite;
        telegraph.ShowCircle(rb.position, radius, slamTelegraph * AttackScale, Telegraph.Danger);
        // Levanta los puños (fase 1) o la espada sobre la cabeza (fase 2) mientras dura el aviso
        string slamClip = swordDrawn ? GuardianVarnAnimator.SwordVertical : GuardianVarnAnimator.FistSlam;
        if (anim != null) anim.PlayRange(slamClip, 0, 2, 3f / (slamTelegraph * AttackScale), false);
        if (fx != null) fx.TelegraphFlames(rb.position, radius, slamTelegraph * AttackScale);
        yield return Shake(slamTelegraph * AttackScale);
        telegraph.Hide();

        SetState(State.Attack);
        if (fx != null) fx.SlamImpact(rb.position, radius);
        if (anim != null) anim.PlayRange(slamClip, 3, swordDrawn ? 6 : 5, Fps, false);
        if (CombatUtil.InsideIsoCircle(player.position, rb.position, radius))
            CombatUtil.DamagePlayer(player.gameObject, slamDamage, rb.position);
        // Pequeño salto visual del golpe
        if (body != null)
        {
            var basePos = body.transform.localPosition;
            body.transform.localPosition = basePos + Vector3.down * 0.03f;
            yield return new WaitForSeconds(0.15f);
            body.transform.localPosition = basePos;
        }

        if (swordDrawn && Current != State.Dead) yield return Shockwave();
    }

    // Fase 2: tras el golpe sale una onda más grande. Castiga alejarse poco y antes de tiempo:
    // hay que salir del círculo grande, que se avisa en el suelo.
    private IEnumerator Shockwave()
    {
        float radius = slamRadius * shockwaveRadiusScale;
        float warn = shockwaveDelay * AttackScale;
        telegraph.GetComponent<SpriteRenderer>().sprite = circleSprite;
        telegraph.ShowCircle(rb.position, radius, warn, Telegraph.Danger);
        // Corte horizontal en círculo: la onda sale al terminar el giro
        if (anim != null && swordDrawn) anim.Play(GuardianVarnAnimator.SwordHorizontal, 8f / warn, false);
        if (fx != null) fx.TelegraphFlames(rb.position, radius, warn);
        yield return new WaitForSeconds(warn);
        telegraph.Hide();
        if (Current == State.Dead) yield break;

        if (fx != null) fx.SlamImpact(rb.position, radius);
        if (CombatUtil.InsideIsoCircle(player.position, rb.position, radius))
            CombatUtil.DamagePlayer(player.gameObject, shockwaveDamage, rb.position);
    }

    // Levanta el escudo hacia Tsuki un momento. Un golpe de cerca mientras dura empieza el choque.
    private IEnumerator Guard()
    {
        guardHit = false;
        guardResult = PowerClash.Outcome.Skipped;
        SetState(State.Guard);
        if (anim != null) anim.Hold("ShieldDash_" + GuardianVarnAnimator.Direction(Facing), 1);
        EnsureShieldGlow();
        if (shieldGlow != null) shieldGlow.enabled = true;
        if (!warnedGuard && GameHud.Instance != null)
        {
            warnedGuard = true;
            GameHud.Instance.ShowToast("¡Varn se cubre con el escudo! Golpéalo de cerca ahora para chocar.");
        }

        for (float t = 0f; t < guardTime && !guardHit; t += Time.deltaTime)
            yield return null;

        SetState(State.Attack);
        if (!guardHit || anim == null) yield break;

        if (!TryGetComponent<PowerClash>(out var clash)) clash = gameObject.AddComponent<PowerClash>();
        yield return clash.PlayShieldClash(anim, transform);
        guardResult = clash.Result;
    }

    // Perdió el choque: se arrodilla aturdido y se le puede pegar
    private IEnumerator Stunned()
    {
        SetState(State.Vulnerable);
        if (anim != null) anim.PlayRange(GuardianVarnAnimator.Defeat, 0, 3, 10f, false);
        if (GameHud.Instance != null) GameHud.Instance.ShowToast("¡Varn está aturdido! Atácalo.", 1.5f);
        yield return new WaitForSeconds(stunTime);
        Idle();
    }

    // Fase 2: clava la espada en el suelo y caen escombros cerca de Tsuki, uno justo donde está.
    // Los escombros también lastiman a los enemigos que queden debajo.
    private IEnumerator DebrisRain()
    {
        SetState(State.Telegraph);
        float warn = 0.6f * AttackScale;
        if (anim != null) anim.PlayRange(GuardianVarnAnimator.SwordVertical, 0, 2, 3f / warn, false);
        if (fx != null) fx.TelegraphFlames(rb.position, 0.4f, warn);
        yield return Shake(warn);

        SetState(State.Attack);
        if (anim != null) anim.PlayRange(GuardianVarnAnimator.SwordVertical, 3, 6, Fps, false);
        if (fx != null) fx.SlamImpact(rb.position, 0.45f);

        foreach (var point in DebrisPoints())
        {
            FallingDebris.Spawn(point, debrisWarning, debrisRadius, debrisDamage, debrisEnemyDamage, circleSprite, fx, health);
            yield return new WaitForSeconds(debrisInterval);
        }
        // El siguiente ataque empieza mientras los últimos escombros siguen cayendo
        yield return new WaitForSeconds(debrisWarning * 0.5f);
    }

    // Fase 2: levanta la espada, la clava y el piso se abre en línea recta hacia Tsuki hasta casi
    // el borde de la arena. La dirección se fija al empezar el aviso: hay que salirse de la franja.
    private IEnumerator RiftSlash()
    {
        SetState(State.Telegraph);
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
        Vector2 origin = rb.position;
        float length = DistanceToArenaEdge(origin, dir) * 0.95f;
        float warn = Mathf.Max(0.55f, riftTelegraph * AttackScale);

        telegraph.GetComponent<SpriteRenderer>().sprite = lineSprite;
        telegraph.ShowLineGrowing(origin, dir, length, riftWidth, warn, Telegraph.Danger);
        if (anim != null) anim.PlayRange(GuardianVarnAnimator.SwordVertical, 0, 2, 3f / warn, false);
        if (fx != null) fx.LineFlames(origin, dir, length, riftWidth, warn);
        yield return Shake(warn);
        telegraph.Hide();
        if (Current == State.Dead) yield break;

        SetState(State.Attack);
        if (anim != null) anim.PlayRange(GuardianVarnAnimator.SwordVertical, 3, 6, Fps, false);
        if (fx != null) fx.SlamImpact(origin, 0.4f);
        GroundRift.Spawn(origin, dir, length, riftWidth, riftSpeed, riftDamage, fx);
        // Mientras se abre la grieta Varn se queda con la espada clavada
        yield return new WaitForSeconds(length / riftSpeed + 0.2f);
    }

    // Distancia desde un punto de la arena hasta su borde (la elipse) en una dirección
    private float DistanceToArenaEdge(Vector2 from, Vector2 dir)
    {
        if (arenaCenter == null) return 3f;
        Vector2 o = from - (Vector2)arenaCenter.position;
        float rx2 = arenaRadii.x * arenaRadii.x, ry2 = arenaRadii.y * arenaRadii.y;
        float a = dir.x * dir.x / rx2 + dir.y * dir.y / ry2;
        float b = 2f * (o.x * dir.x / rx2 + o.y * dir.y / ry2);
        float c = o.x * o.x / rx2 + o.y * o.y / ry2 - 1f;
        float disc = b * b - 4f * a * c;
        if (disc < 0f) return 1f;
        return Mathf.Max(0.5f, (-b + Mathf.Sqrt(disc)) / (2f * a));
    }

    private Vector2[] DebrisPoints()
    {
        var points = new Vector2[debrisCount];
        Vector2 target = player.position;
        points[0] = InsideArena(target);
        for (int i = 1; i < debrisCount; i++)
        {
            Vector2 p = target;
            // Separados entre sí para que se pueda pasar entre ellos
            for (int tries = 0; tries < 8; tries++)
            {
                var r = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.35f, debrisSpread);
                p = InsideArena(target + new Vector2(r.x, r.y * 0.5f));
                bool clear = true;
                for (int j = 0; j < i; j++)
                    if (Vector2.Distance(p, points[j]) < debrisRadius * 2.2f) { clear = false; break; }
                if (clear) break;
            }
            points[i] = p;
        }
        return points;
    }

    private Vector2 InsideArena(Vector2 p)
    {
        if (arenaCenter == null) return p;
        Vector2 c = arenaCenter.position;
        Vector2 radii = arenaRadii * 0.9f;
        Vector2 d = p - c;
        float k = (d.x * d.x) / (radii.x * radii.x) + (d.y * d.y) / (radii.y * radii.y);
        return k > 1f ? c + d / Mathf.Sqrt(k) : p;
    }

    // Tocar a Varn mientras camina hace daño (una vez por segundo como máximo)
    private void ContactDamage()
    {
        if (Time.time < nextContactTime || player == null) return;
        if (Vector2.Distance(rb.position, player.position) > contactRange) return;
        if (CombatUtil.DamagePlayer(player.gameObject, contactDamage, rb.position))
            nextContactTime = Time.time + 1f;
    }

    private IEnumerator Vulnerable()
    {
        SetState(State.Vulnerable);
        Idle();
        yield return new WaitForSeconds(vulnerableTime * Scale);
    }

    private IEnumerator Shake(float time)
    {
        if (body == null) { yield return new WaitForSeconds(time); yield break; }
        var basePos = body.transform.localPosition;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            body.transform.localPosition = basePos + new Vector3(Mathf.Sin(t * 60f) * 0.01f, 0f, 0f);
            yield return null;
        }
        body.transform.localPosition = basePos;
    }

    // Mueve sin salir de la elipse de la arena. Devuelve false si chocó con el borde.
    private bool MoveTo(Vector2 target)
    {
        if (arenaCenter != null)
        {
            Vector2 c = arenaCenter.position;
            Vector2 offset = target - c;
            float k = (offset.x * offset.x) / (arenaRadii.x * arenaRadii.x) + (offset.y * offset.y) / (arenaRadii.y * arenaRadii.y);
            if (k > 1f)
            {
                rb.MovePosition(c + offset / Mathf.Sqrt(k));
                return false;
            }
        }
        rb.MovePosition(target);
        return true;
    }

    private void SetState(State s)
    {
        Current = s;
        if (s == State.Dead) shielded = false;
        bool weakSword = ApplyProtection();
        if (s == State.Vulnerable && weakSword && !warnedWeakSword && GameHud.Instance != null)
        {
            warnedWeakSword = true;
            GameHud.Instance.ShowToast("La espada no puede herir a un guardián sin la mejora de la forja.");
        }
        StateChanged?.Invoke(s);
    }

    // Vulnerable solo en su ventana, con la espada mejorada y sin refuerzos vivos.
    // Con escudo no toma el color de "vulnerable", para no confundir.
    private bool ApplyProtection()
    {
        bool weakSword = playerStats != null && playerStats.SwordLevel < 1;
        health.Invulnerable = Current != State.Vulnerable || weakSword || shielded;
        if (body != null)
            body.color = Current == State.Vulnerable && !shielded ? vulnerableTint : (Current == State.Dead ? Color.white : armoredTint);
        if (shieldGlow != null && !shielded) shieldGlow.enabled = false;
        return weakSword;
    }

    private void OnHealthChanged(int current, int max)
    {
        // El aviso de la segunda fase va en el diálogo de la espada (GuardianArena)
        if (!enraged && current <= max / 2) enraged = true;
    }

    private void OnBlocked()
    {
        // Golpe mientras se cubre: empieza el choque de escudo (Guard)
        if (Current == State.Guard && !guardHit && player != null &&
            Vector2.Distance(rb.position, player.position) <= guardHitRange)
        {
            guardHit = true;
            return;
        }
        if (shielded && Time.time >= nextShieldToast && GameHud.Instance != null)
        {
            nextShieldToast = Time.time + 3f;
            GameHud.Instance.ShowToast("Varn está protegido: derrota primero a sus refuerzos.");
        }
        if (body != null && isActiveAndEnabled) StartCoroutine(BlockFlash());
    }

    private IEnumerator BlockFlash()
    {
        var prev = body.color;
        body.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        if (Current != State.Vulnerable) body.color = prev;
    }
}
