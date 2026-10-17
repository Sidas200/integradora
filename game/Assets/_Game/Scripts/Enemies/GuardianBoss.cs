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
    [SerializeField] private int chargeDamage = 20;

    [Header("Golpe del juicio (área)")]
    [SerializeField] private float slamTelegraph = 1.2f;
    [SerializeField] private float slamRadius = 0.95f;
    [SerializeField] private int slamDamage = 25;

    [Header("Ventana vulnerable")]
    [SerializeField] private float vulnerableTime = 1.8f;
    [SerializeField, Range(0.3f, 1f)] private float enragedTimeScale = 0.75f;

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
        health.Died += () => SetState(State.Dead);
    }

    private void OnDestroy()
    {
        if (telegraph != null) Destroy(telegraph.gameObject);
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

    private float Scale => enraged ? enragedTimeScale : 1f;

    private IEnumerator Loop()
    {
        while (Current != State.Dead)
        {
            if (!PlayerAlive()) { SetState(State.Dormant); yield break; }

            yield return Approach();
            if (patternIndex++ % 2 == 0) yield return Charge();
            else yield return Slam();
            if (Current == State.Dead) yield break;
            yield return Vulnerable();
        }
    }

    private IEnumerator Approach()
    {
        SetState(State.Approach);
        for (float t = 0f; t < approachTime * Scale; t += Time.fixedDeltaTime)
        {
            Vector2 dir = ((Vector2)player.position - rb.position);
            if (dir.magnitude > 0.5f) MoveTo(rb.position + dir.normalized * walkSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator Charge()
    {
        SetState(State.Telegraph);
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        telegraph.GetComponent<SpriteRenderer>().sprite = lineSprite;
        telegraph.ShowLine(rb.position, dir, chargeLength, chargeWidth, chargeTelegraph * Scale, Telegraph.Danger);
        if (fx != null) fx.LineFlames(rb.position, dir, chargeLength, chargeWidth, chargeTelegraph * Scale);
        yield return Shake(chargeTelegraph * Scale);
        telegraph.Hide();

        SetState(State.Attack);
        Vector2 start = rb.position;
        bool hit = false;
        float speed = chargeSpeed / Scale;
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
        telegraph.ShowCircle(rb.position, slamRadius, slamTelegraph * Scale, Telegraph.Danger);
        if (fx != null) fx.TelegraphFlames(rb.position, slamRadius, slamTelegraph * Scale);
        yield return Shake(slamTelegraph * Scale);
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
        bool weakSword = playerStats != null && playerStats.SwordLevel < 1;
        health.Invulnerable = s != State.Vulnerable || weakSword;
        if (body != null) body.color = s == State.Vulnerable ? vulnerableTint : (s == State.Dead ? Color.white : armoredTint);
        if (s == State.Vulnerable && weakSword && !warnedWeakSword && GameHud.Instance != null)
        {
            warnedWeakSword = true;
            GameHud.Instance.ShowToast("La espada no puede herir a un guardián sin la mejora de la forja.");
        }
        StateChanged?.Invoke(s);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (!enraged && current <= max / 2)
        {
            enraged = true;
            if (GameHud.Instance != null) GameHud.Instance.ShowToast("¡Varn se enfurece! Sus ataques son más rápidos.");
        }
    }

    private void OnBlocked()
    {
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
