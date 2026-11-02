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
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class GuardianBoss : MonoBehaviour
{
    public enum State { Dormant, Approach, Telegraph, Attack, Vulnerable, Dead }

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
    [SerializeField] private int chargeDamage = 32;

    [Header("Golpe del juicio (área)")]
    [SerializeField] private float slamTelegraph = 1.2f;
    [SerializeField] private float slamRadius = 0.95f;
    [SerializeField] private int slamDamage = 36;

    [Header("Fase 2 (bajo 50% de vida)")]
    [Tooltip("Onda del golpe al suelo: radio respecto al golpe y aviso antes de que llegue.")]
    [SerializeField] private float shockwaveRadiusScale = 1.8f;
    [SerializeField] private float shockwaveDelay = 0.45f;
    [SerializeField] private int shockwaveDamage = 20;

    [Header("Contacto")]
    [Tooltip("Daño al tocar a Varn mientras camina; una vez por segundo como máximo.")]
    [SerializeField] private int contactDamage = 12;
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

    public event Action<State> StateChanged;

    private Rigidbody2D rb;
    private EnemyHealth health;
    private Transform player;
    private PlayerStats playerStats;
    private Telegraph telegraph;
    private bool enraged;
    private bool warnedWeakSword;
    private int patternIndex;
    private Coroutine breathing;

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
        health.Invulnerable = true;
        health.Blocked += OnBlocked;
        health.HealthChanged += OnHealthChanged;
        health.Died += () =>
        {
            SetState(State.Dead);
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
        if (!shielded || shieldGlow == null || body == null) return;
        shieldGlow.sprite = body.sprite;
        shieldGlow.flipX = body.flipX;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        shieldGlow.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, Mathf.Lerp(0.35f, 0.85f, pulse));
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

            yield return Approach();
            bool chargeFirst = patternIndex++ % 2 == 0;
            yield return chargeFirst ? Charge() : Slam();
            if (Current == State.Dead) yield break;
            // Fase 2: encadena el otro ataque antes de quedar vulnerable
            if (enraged)
            {
                yield return chargeFirst ? Slam() : Charge();
                if (Current == State.Dead) yield break;
            }
            yield return Vulnerable();
        }
    }

    private IEnumerator Approach()
    {
        SetState(State.Approach);
        for (float t = 0f; t < approachTime * AttackScale; t += Time.fixedDeltaTime)
        {
            Vector2 dir = ((Vector2)player.position - rb.position);
            if (dir.magnitude > 0.5f) MoveTo(rb.position + dir.normalized * walkSpeed * Time.fixedDeltaTime);
            ContactDamage();
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator Charge()
    {
        SetState(State.Telegraph);
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        telegraph.GetComponent<SpriteRenderer>().sprite = lineSprite;
        telegraph.ShowLine(rb.position, dir, chargeLength, chargeWidth, chargeTelegraph * AttackScale, Telegraph.Danger);
        if (fx != null) fx.LineFlames(rb.position, dir, chargeLength, chargeWidth, chargeTelegraph * AttackScale);
        yield return Shake(chargeTelegraph * AttackScale);
        telegraph.Hide();

        SetState(State.Attack);
        Vector2 start = rb.position;
        bool hit = false;
        float speed = chargeSpeed / AttackScale;
        while (Vector2.Distance(start, rb.position) < chargeLength)
        {
            Vector2 next = rb.position + dir * speed * Time.fixedDeltaTime;
            if (!MoveTo(next)) break; // borde de la arena
            if (!hit && Vector2.Distance(rb.position, player.position) < 0.3f)
                hit = CombatUtil.DamagePlayer(player.gameObject, chargeDamage, rb.position);
            if (fx != null) fx.ChargeStep(rb.position, dir);
            yield return new WaitForFixedUpdate();
        }
        if (fx != null) fx.ChargeEnd(rb.position);
    }

    private IEnumerator Slam()
    {
        SetState(State.Telegraph);
        telegraph.GetComponent<SpriteRenderer>().sprite = circleSprite;
        telegraph.ShowCircle(rb.position, slamRadius, slamTelegraph * AttackScale, Telegraph.Danger);
        if (fx != null) fx.TelegraphFlames(rb.position, slamRadius, slamTelegraph * AttackScale);
        yield return Shake(slamTelegraph * AttackScale);
        telegraph.Hide();

        SetState(State.Attack);
        if (fx != null) fx.SlamImpact(rb.position, slamRadius);
        if (CombatUtil.InsideIsoCircle(player.position, rb.position, slamRadius))
            CombatUtil.DamagePlayer(player.gameObject, slamDamage, rb.position);
        // Pequeño salto visual del golpe
        if (body != null)
        {
            var basePos = body.transform.localPosition;
            body.transform.localPosition = basePos + Vector3.down * 0.03f;
            yield return new WaitForSeconds(0.15f);
            body.transform.localPosition = basePos;
        }

        if (enraged && Current != State.Dead) yield return Shockwave();
    }

    // Fase 2: tras el golpe sale una onda más grande. Castiga alejarse poco y antes de tiempo:
    // hay que salir del círculo grande, que se avisa en el suelo.
    private IEnumerator Shockwave()
    {
        float radius = slamRadius * shockwaveRadiusScale;
        float warn = shockwaveDelay * AttackScale;
        telegraph.GetComponent<SpriteRenderer>().sprite = circleSprite;
        telegraph.ShowCircle(rb.position, radius, warn, Telegraph.Danger);
        if (fx != null) fx.TelegraphFlames(rb.position, radius, warn);
        yield return new WaitForSeconds(warn);
        telegraph.Hide();
        if (Current == State.Dead) yield break;

        if (fx != null) fx.SlamImpact(rb.position, radius);
        CameraShake.Shake(0.03f, 0.25f);
        if (CombatUtil.InsideIsoCircle(player.position, rb.position, radius))
            CombatUtil.DamagePlayer(player.gameObject, shockwaveDamage, rb.position);
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
        if (!enraged && current <= max / 2)
        {
            enraged = true;
            if (GameHud.Instance != null) GameHud.Instance.ShowToast("¡Varn se enfurece! Encadena ataques y su golpe al suelo lanza una onda.");
        }
    }

    private void OnBlocked()
    {
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
