using UnityEngine;

// IA del slime: persigue mientras ve a Tsuki, se carga un momento (aviso en el punto de caída)
// y se lanza en línea recta hacia ella.
// - Si la golpea: hace daño, rebota hacia atrás y queda atontado.
// - Si falla: queda atontado igual (un poco más), lo que abre una ventana para pegarle.
// - Si Tsuki sale de su rango de vista, deja de seguirla.
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeController : MonoBehaviour
{
    private enum State { Idle, Chase, Charge, Lunge, Recoil, Stunned }

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;
    [Tooltip("Aviso en el suelo donde va a caer el salto")]
    [SerializeField] private Telegraph warning;

    [Header("Detección")]
    [SerializeField] private float detectionRange = 1.5f;
    [Tooltip("Al salir de esta distancia la pierde de vista (un poco mayor que la detección para no titubear)")]
    [SerializeField] private float loseSightRange = 1.9f;

    [Header("Persecución")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("Carga y salto")]
    [Tooltip("Distancia a la que empieza a cargar el salto")]
    [SerializeField] private float lungeTriggerRange = 0.75f;
    [SerializeField] private float chargeTime = 0.7f;
    [SerializeField] private float lungeSpeed = 4.2f;
    [SerializeField] private float lungeDistance = 1f;
    [SerializeField] private float hitRadius = 0.12f;
    [SerializeField, Min(1)] private int attackDamage = 6;
    [SerializeField] private float attackCooldown = 0.4f;

    [Header("Después del salto")]
    [SerializeField] private float recoilSpeed = 1.6f;
    [SerializeField] private float recoilTime = 0.25f;
    [SerializeField] private float stunAfterHit = 1.5f;
    [SerializeField] private float stunAfterMiss = 2f;

    [Header("Reacción al recibir daño")]
    [SerializeField] private float hitStunDuration = 0.15f;
    [SerializeField] private float knockbackSpeed = 1.5f;

    public bool PlayerDetected { get; private set; }

    private Rigidbody2D rb;
    private SlimeBounce bounce;
    private Health playerHealth;
    private State state;
    private float stateTimer;
    private float nextAttackTime;
    private Vector2 lungeDir;
    private Vector2 lungeStart;
    private float hitTimer;
    private Vector2 knockbackDir;
    private readonly Collider2D[] overlap = new Collider2D[8];

    public void Setup(Transform visualTransform, Telegraph warningTelegraph)
    {
        visual = visualTransform;
        warning = warningTelegraph;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bounce = GetComponent<SlimeBounce>();
    }

    private void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (player != null) playerHealth = player.GetComponentInParent<Health>();
        HideWarning();
    }

    private void OnDisable()
    {
        HideWarning();
        ResetVisual();
    }

    private bool PlayerAlive => player != null && (playerHealth == null || !playerHealth.IsDead);

    private void Update()
    {
        float distance = PlayerAlive ? Vector2.Distance(rb.position, player.position) : float.MaxValue;
        PlayerDetected = PlayerDetected ? distance <= loseSightRange : distance <= detectionRange;

        stateTimer -= Time.deltaTime;
        switch (state)
        {
            case State.Idle:
            case State.Chase:
                state = PlayerDetected ? State.Chase : State.Idle;
                if (state == State.Chase && distance <= lungeTriggerRange && Time.time >= nextAttackTime) BeginCharge();
                break;

            case State.Charge:
                // Si la pierde de vista mientras se carga, desiste
                if (!PlayerDetected) { HideWarning(); state = State.Idle; break; }
                if (stateTimer <= 0f) BeginLunge();
                break;

            case State.Recoil:
                if (stateTimer <= 0f) Stun(stunAfterHit);
                break;

            case State.Stunned:
                if (stateTimer <= 0f)
                {
                    state = PlayerDetected ? State.Chase : State.Idle;
                    nextAttackTime = Time.time + attackCooldown;
                }
                break;
        }
        AnimateVisual();
    }

    private void FixedUpdate()
    {
        if (hitTimer > 0f && state != State.Lunge)
        {
            hitTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackDir * knockbackSpeed;
            return;
        }

        switch (state)
        {
            case State.Chase:
                Vector2 toPlayer = (Vector2)player.position - rb.position;
                rb.linearVelocity = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized * moveSpeed : Vector2.zero;
                break;

            case State.Lunge:
                rb.linearVelocity = lungeDir * lungeSpeed;
                if (TouchingPlayer())
                {
                    CombatUtil.DamagePlayer(player.gameObject, attackDamage, rb.position);
                    HideWarning();
                    state = State.Recoil;
                    stateTimer = recoilTime;
                    break;
                }
                // Falla si recorre todo el salto o choca con algo y se queda corto
                bool done = Vector2.Distance(lungeStart, rb.position) >= lungeDistance;
                if (done || stateTimer <= 0f) { HideWarning(); Stun(stunAfterMiss); }
                break;

            case State.Recoil:
                rb.linearVelocity = -lungeDir * recoilSpeed;
                break;

            default:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void BeginCharge()
    {
        state = State.Charge;
        stateTimer = chargeTime;
        if (warning != null)
        {
            // Apunta al jugador en el momento de cargar: si se mueve a tiempo, lo esquiva
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            warning.ShowCircle(rb.position + dir * lungeDistance, 0.14f, chargeTime, Telegraph.Danger);
        }
    }

    private void BeginLunge()
    {
        Vector2 target = warning != null && warning.gameObject.activeSelf ? (Vector2)warning.transform.position : (Vector2)player.position;
        Vector2 dir = target - rb.position;
        lungeDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        lungeStart = rb.position;
        state = State.Lunge;
        // Tope de tiempo por si choca con una pared y no avanza
        stateTimer = lungeDistance / lungeSpeed + 0.2f;
    }

    private void Stun(float time)
    {
        state = State.Stunned;
        stateTimer = time;
        rb.linearVelocity = Vector2.zero;
    }

    private bool TouchingPlayer()
    {
        int n = Physics2D.OverlapCircle(rb.position + new Vector2(0f, 0.05f), hitRadius, ContactFilter2D.noFilter, overlap);
        for (int i = 0; i < n; i++)
            if (overlap[i] != null && (overlap[i].transform == player || overlap[i].transform.IsChildOf(player))) return true;
        return false;
    }

    // Llamado por EnemyHitFeedback al recibir daño: empujón breve. Un golpe durante la carga la cancela.
    public void ApplyHitReaction()
    {
        if (player == null) return;
        Vector2 d = rb.position - (Vector2)player.position;
        knockbackDir = d.sqrMagnitude > 0.001f ? d.normalized : Vector2.up;
        hitTimer = hitStunDuration;
        if (state == State.Charge)
        {
            HideWarning();
            state = State.Chase;
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void HideWarning()
    {
        if (warning != null) warning.Hide();
    }

    // Carga: se aplasta y tiembla. Atontado: se tambalea de lado a lado.
    private void AnimateVisual()
    {
        if (visual == null) return;
        float squash = 0f;
        float tilt = 0f;
        Vector3 shake = Vector3.zero;
        if (state == State.Charge)
        {
            float k = 1f - Mathf.Clamp01(stateTimer / chargeTime);
            squash = 0.25f * k;
            shake = new Vector3(Mathf.Sin(Time.time * 70f) * 0.01f * k, 0f, 0f);
        }
        else if (state == State.Lunge)
        {
            squash = -0.2f; // estirado en el aire
        }
        else if (state == State.Stunned)
        {
            tilt = Mathf.Sin(Time.time * 9f) * 14f;
        }
        if (bounce != null) bounce.ExtraSquash = squash;
        visual.localRotation = Quaternion.Euler(0f, 0f, tilt);
        visual.localPosition = new Vector3(shake.x, visual.localPosition.y, 0f);
    }

    private void ResetVisual()
    {
        if (bounce != null) bounce.ExtraSquash = 0f;
        if (visual != null) visual.localRotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseSightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lungeTriggerRange);
    }
}
