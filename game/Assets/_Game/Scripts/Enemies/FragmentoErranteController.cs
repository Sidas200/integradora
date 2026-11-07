using UnityEngine;

// Fragmento Errante: núcleo de roca que flota (refuerzo que invoca Varn). Ataca como dicen sus frames:
// - Al ver a Tsuki se pone alerta un instante y luego levita hacia ella.
// - Cerca, se detiene y carga: en el suelo crece el círculo de la onda (aviso).
// - Suelta la onda de choque a su alrededor: daña si Tsuki sigue dentro del círculo.
// - Después queda unos instantes quieto, la ventana para pegarle.
// - Al morir se agrieta y se desmorona en escombros.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class FragmentoErranteController : MonoBehaviour, IStaggerable
{
    private enum State { Idle, Alert, Chase, Charge, Wave, Recover }

    [Header("Detección")]
    [SerializeField] private float detectionRange = 2.4f;
    [Tooltip("Al salir de esta distancia la pierde de vista (un poco mayor que la detección para no titubear)")]
    [SerializeField] private float loseSightRange = 3f;
    [SerializeField] private float alertDuration = 0.35f;

    [Header("Levitación")]
    [SerializeField] private float moveSpeed = 0.75f;
    [SerializeField] private float walkFps = 4f;

    [Header("Onda de choque")]
    [Tooltip("Distancia a la que se detiene y empieza a cargar la onda")]
    [SerializeField] private float triggerRange = 0.4f;
    [Tooltip("Radio de la onda (círculo isométrico, como el aviso)")]
    [SerializeField] private float waveRadius = 0.5f;
    [SerializeField] private float chargeTime = 0.8f;
    [Tooltip("Multiplica la velocidad de ataque: carga y espera entre ondas (1.3 = 30% más rápido).")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 1.45f;
    [SerializeField] private float waveDuration = 0.3f;
    [Tooltip("Quieto tras la onda: ventana para pegarle")]
    [SerializeField] private float recoverTime = 0.6f;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField, Min(1)] private int attackDamage = 20;

    [Header("Reacción al recibir daño")]
    [SerializeField] private float hitStunDuration = 0.15f;
    [SerializeField] private float knockbackSpeed = 1.3f;
    [Tooltip("2do golpe del combo: retroceso un poco mayor y un instante aturdido.")]
    [SerializeField] private float staggerKnockbackTime = 0.15f;
    [SerializeField] private float staggerKnockbackSpeed = 2f;
    [SerializeField] private float staggerStunDuration = 0.3f;

    [Header("Visual")]
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Color chargeColor = new Color(1f, 0.75f, 0.45f);
    [SerializeField] private float deathFps = 4f;

    public bool PlayerDetected { get; private set; }

    private Rigidbody2D rb;
    private EnemyHealth health;
    private EnemyHitFeedback feedback;
    private GuardianVarnAnimator anim;
    private Transform player;
    private Health playerHealth;
    private Telegraph warning;
    private State state;
    private float stateTimer;
    private float nextAttackTime;
    private Vector2 waveCenter;
    private Vector2 facing = Vector2.down;
    private float hitTimer;
    private float stunTimer;
    private Vector2 knockbackDir;
    private float currentKnockbackSpeed;

    public void Setup(Sprite circle, SpriteRenderer bodyRenderer)
    {
        circleSprite = circle;
        body = bodyRenderer;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        feedback = GetComponent<EnemyHitFeedback>();
        anim = GetComponentInChildren<GuardianVarnAnimator>();
        health.OnDamaged += OnDamaged;
        health.Died += OnDied;
    }

    private void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            player = go.transform;
            playerHealth = player.GetComponentInParent<Health>();
        }
        warning = Telegraph.Create(circleSprite, name + "_Wave");
    }

    // Al reaparecer empieza tranquilo, no a mitad de una onda
    private void OnEnable()
    {
        state = State.Idle;
        stateTimer = 0f;
        hitTimer = 0f;
        stunTimer = 0f;
        PlayerDetected = false;
    }

    private void OnDisable()
    {
        if (warning != null) warning.Hide();
        if (body != null) body.color = Color.white;
    }

    private void OnDestroy()
    {
        health.OnDamaged -= OnDamaged;
        health.Died -= OnDied;
        if (warning != null) Destroy(warning.gameObject);
    }

    private bool PlayerAlive => player != null && (playerHealth == null || !playerHealth.IsDead);
    private bool Armored => state == State.Charge || state == State.Wave;
    private bool Flashing => feedback != null && feedback.IsFlashing;

    private void Update()
    {
        float distance = PlayerAlive ? Vector2.Distance(rb.position, player.position) : float.MaxValue;
        bool wasDetected = PlayerDetected;
        PlayerDetected = PlayerDetected ? distance <= loseSightRange : distance <= detectionRange;

        stateTimer -= Time.deltaTime;
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
            case State.Chase:
                if (PlayerDetected && !wasDetected) { state = State.Alert; stateTimer = alertDuration; break; }
                state = PlayerDetected ? State.Chase : State.Idle;
                if (state == State.Chase && stunTimer <= 0f && distance <= triggerRange && Time.time >= nextAttackTime)
                    BeginCharge();
                break;

            case State.Alert:
                if (stateTimer <= 0f) state = PlayerDetected ? State.Chase : State.Idle;
                break;

            case State.Charge:
                ChargeFlash();
                if (stateTimer <= 0f) ReleaseWave();
                break;

            case State.Wave:
                if (stateTimer <= 0f) { state = State.Recover; stateTimer = recoverTime; }
                break;

            case State.Recover:
                if (stateTimer <= 0f)
                {
                    state = PlayerDetected ? State.Chase : State.Idle;
                    nextAttackTime = Time.time + attackCooldown / attackSpeed;
                }
                break;
        }

        if (PlayerAlive && (state == State.Alert || state == State.Chase))
            facing = (Vector2)player.position - rb.position;
        Animate();
    }

    private void FixedUpdate()
    {
        if (hitTimer > 0f)
        {
            hitTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackDir * currentKnockbackSpeed;
            return;
        }
        if (state == State.Chase && stunTimer <= 0f && PlayerAlive)
        {
            Vector2 toPlayer = (Vector2)player.position - rb.position;
            rb.linearVelocity = toPlayer.magnitude > triggerRange * 0.8f ? toPlayer.normalized * moveSpeed : Vector2.zero;
            return;
        }
        rb.linearVelocity = Vector2.zero;
    }

    // La onda sale de donde está al empezar a cargar: para esquivarla hay que salir del círculo
    private void BeginCharge()
    {
        state = State.Charge;
        Sfx.Play(Sfx.Fragmento.Charge);
        stateTimer = chargeTime / attackSpeed;
        waveCenter = rb.position;
        if (warning != null)
        {
            warning.Opacity = Telegraph.SubtleOpacity;
            warning.ShowCircle(waveCenter, waveRadius, stateTimer, Telegraph.Danger);
        }
    }

    private void ReleaseWave()
    {
        state = State.Wave;
        stateTimer = waveDuration;
        if (body != null && !Flashing) body.color = Color.white;
        if (warning != null) warning.Impact(waveDuration);
        CameraShake.Shake(0.015f, 0.15f);
        Sfx.Play(Sfx.Fragmento.Wave);
        if (PlayerAlive && CombatUtil.InsideIsoCircle(player.position, waveCenter, waveRadius) &&
            CombatUtil.DamagePlayer(player.gameObject, attackDamage, waveCenter))
            Sfx.Play(Sfx.Fragmento.RockHeavy, 0.7f);
    }

    // Aviso de la onda: el núcleo parpadea cada vez más rápido. El destello de daño tiene prioridad.
    private void ChargeFlash()
    {
        if (body == null || Flashing) return;
        float total = Mathf.Max(0.01f, chargeTime / attackSpeed);
        float k = 1f - Mathf.Clamp01(stateTimer / total);
        float speed = Mathf.Lerp(10f, 30f, k);
        body.color = Mathf.Sin((total - stateTimer) * speed) > -0.2f ? chargeColor : Color.white;
    }

    // Llamado al recibir daño: empujón breve. Cargando o soltando la onda no se interrumpe.
    private void OnDamaged(int damage)
    {
        // La espada contra piedra: grieta o golpe seco
        Sfx.PlayAny(Sfx.Fragmento.Hurt, Sfx.Fragmento.RockDry);
        if (player == null || Armored) return;
        Vector2 d = rb.position - (Vector2)player.position;
        knockbackDir = d.sqrMagnitude > 0.001f ? d.normalized : Vector2.up;
        hitTimer = hitStunDuration;
        currentKnockbackSpeed = knockbackSpeed;
    }

    // 2do golpe del combo: retrocede un poco más y queda aturdido un instante.
    public void Stagger(Vector2 from)
    {
        if (!isActiveAndEnabled || Armored) return;
        Sfx.Play(Sfx.Fragmento.RockHuge, 0.8f); // 2do golpe del combo
        Vector2 d = rb.position - from;
        knockbackDir = d.sqrMagnitude > 0.001f ? d.normalized : Vector2.up;
        hitTimer = staggerKnockbackTime;
        currentKnockbackSpeed = staggerKnockbackSpeed;
        stunTimer = staggerKnockbackTime + staggerStunDuration;
    }

    private void Animate()
    {
        if (anim == null) return;
        if (hitTimer > 0f || stunTimer > 0f)
            anim.PlayFacing("Hurt", facing, 1f, false);
        else if (state == State.Wave)
            anim.PlayFacing("Attack", facing, 1f, false);
        else if (state == State.Charge)
            anim.PlayFacing("Windup", facing, 1f, false);
        else if (state == State.Alert)
            anim.PlayFacing("Alert", facing, 1f, false);
        else if (rb.linearVelocity.sqrMagnitude > 0.01f)
            anim.PlayFacing("Walk", facing, walkFps, true);
        else
            anim.PlayFacing("Idle", facing, 1f, false);
    }

    // EnemyHealth lo apaga al morir: una copia se agrieta y se desmorona
    private void OnDied()
    {
        Sfx.Play(Sfx.Fragmento.Crumble);
        if (warning != null) warning.Hide();
        if (anim == null) return;
        var copy = anim.SpawnCopy();
        copy.name = name + "_Escombros";
        copy.PlayFacing("Death", facing, deathFps, false);
        Destroy(copy.gameObject, 2f / deathFps + 0.8f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseSightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, waveRadius);
    }
}
