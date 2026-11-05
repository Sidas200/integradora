using System.Collections;
using UnityEngine;

// Arquetipo 2 (Centinela): enemigo a distancia. Mantiene distancia con el jugador y ataca así:
// 1) marca en el suelo un círculo rojo donde está el jugador (aviso, parpadea),
// 2) tras el aviso dispara un orbe que viaja hasta la marca,
// 3) al llegar explota: daña una sola vez si el jugador sigue dentro del círculo.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class EnemyRangedController : MonoBehaviour, IStaggerable
{
    [Header("Detección y distancia")]
    [SerializeField] private float detectionRange = 3.2f;
    [SerializeField] private float preferredMin = 1.3f;
    [SerializeField] private float preferredMax = 2.1f;
    [SerializeField] private float moveSpeed = 0.7f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 2.4f;
    [SerializeField] private float telegraphTime = 0.9f;
    [Tooltip("Multiplica la velocidad de ataque: carga y espera entre disparos (1.3 = 30% más rápido).")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 1.45f;
    [SerializeField] private float projectileSpeed = 2.6f;
    [SerializeField, Min(1)] private int damage = 20;

    [Header("Aturdido (2do golpe del combo)")]
    [SerializeField] private float staggerKnockbackTime = 0.15f;
    [SerializeField] private float staggerKnockbackSpeed = 2.2f;
    [SerializeField] private float staggerStunDuration = 0.3f;

    // La bola hace daño al tocar al jugador mientras avanza; si la esquiva, rebota una vez y desaparece
    [Header("Disparo")]
    [Tooltip("Radio con el que la bola toca al jugador.")]
    [SerializeField] private float projectileRadius = 0.14f;
    [Tooltip("Cuánto sigue de largo después del punto donde estaba el jugador antes de rebotar.")]
    [SerializeField] private float shotOvershoot = 0.8f;
    [Tooltip("Largo del tramo después del rebote; al terminarlo desaparece.")]
    [SerializeField] private float bounceLength = 1.6f;
    [Tooltip("Altura del centro del cuerpo del jugador sobre sus pies.")]
    [SerializeField] private float playerBodyHeight = 0.15f;

    [Header("Visual")]
    [SerializeField] private Sprite telegraphSprite;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Color chargeColor = new Color(1f, 0.55f, 0.6f);
    [Tooltip("Altura desde la que sale el orbe (el núcleo del Centinela)")]
    [SerializeField] private float muzzleHeight = 0.22f;

    private Rigidbody2D rb;
    private EnemyHealth health;
    private EnemyHitFeedback feedback;
    private Transform player;
    private GameObject projectile;
    private float nextAttack;
    private bool attacking;
    private float stunTimer;
    private float staggerTimer;
    private float knockbackTimer;
    private Vector2 knockbackDir;

    public void Setup(Sprite telegraph, Sprite orb, SpriteRenderer bodyRenderer)
    {
        telegraphSprite = telegraph;
        projectileSprite = orb;
        body = bodyRenderer;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        feedback = GetComponent<EnemyHitFeedback>();
        health.OnDamaged += _ =>
        {
            stunTimer = 0.2f;
            // Al recibir un golpe responde con un disparo en cuanto pueda (no espera su recarga)
            if (!attacking) nextAttack = Mathf.Min(nextAttack, Time.time);
        };
    }

    // 2do golpe del combo: retrocede un poco y queda aturdido un instante (no dispara mientras).
    // Respeta la super armadura: si ya está cargando o disparando, no se interrumpe.
    public void Stagger(Vector2 from)
    {
        if (!isActiveAndEnabled || attacking) return;
        Vector2 d = rb.position - from;
        knockbackDir = d.sqrMagnitude > 0.001f ? d.normalized : Vector2.up;
        knockbackTimer = staggerKnockbackTime;
        staggerTimer = staggerStunDuration;
    }

    private bool Flashing => feedback != null && feedback.IsFlashing;

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        projectile = new GameObject(name + "_Orb");
        var sr = projectile.AddComponent<SpriteRenderer>();
        sr.sprite = projectileSprite;
        sr.sortingLayerName = "Sprites";
        sr.sortingOrder = 50;
        projectile.SetActive(false);
        nextAttack = Time.time + 0.4f;
    }

    private void OnDisable()
    {
        // Muerto o escena descargada: limpia el orbe
        StopAllCoroutines();
        attacking = false;
        staggerTimer = 0f;
        knockbackTimer = 0f;
        if (projectile != null) projectile.SetActive(false);
        if (body != null) body.color = Color.white;
    }

    private void OnDestroy()
    {
        if (projectile != null) Destroy(projectile);
    }

    private bool PlayerAlive()
    {
        if (player == null) return false;
        var h = player.GetComponent<Health>();
        return h == null || !h.IsDead;
    }

    private void FixedUpdate()
    {
        if (knockbackTimer > 0f) { knockbackTimer -= Time.fixedDeltaTime; rb.linearVelocity = knockbackDir * staggerKnockbackSpeed; return; }
        if (staggerTimer > 0f) { staggerTimer -= Time.fixedDeltaTime; rb.linearVelocity = Vector2.zero; return; }
        if (stunTimer > 0f) { stunTimer -= Time.fixedDeltaTime; rb.linearVelocity = Vector2.zero; return; }
        if (!PlayerAlive() || attacking) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;
        if (dist > detectionRange) { rb.linearVelocity = Vector2.zero; return; }

        // Se aleja si el jugador está demasiado cerca y se acerca si está lejos
        Vector2 dir = toPlayer.normalized;
        if (dist < preferredMin) rb.linearVelocity = -dir * moveSpeed;
        else if (dist > preferredMax) rb.linearVelocity = dir * moveSpeed * 0.8f;
        else rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (attacking || staggerTimer > 0f || knockbackTimer > 0f || !PlayerAlive() || Time.time < nextAttack) return;
        if (Vector2.Distance(player.position, transform.position) > detectionRange) return;
        StartCoroutine(Attack());
    }

    private IEnumerator Attack()
    {
        attacking = true;
        float charge = telegraphTime / attackSpeed;

        // Carga: el núcleo parpadea (no se marca el suelo; el peligro es la bola)
        for (float t = 0f; t < charge; t += Time.deltaTime)
        {
            // Si acaba de recibir un golpe, el destello tiene prioridad sobre el parpadeo de carga
            if (body != null && !Flashing) body.color = Mathf.PingPong(t * 8f, 1f) > 0.5f ? chargeColor : Color.white;
            yield return null;
        }
        if (body != null && !Flashing) body.color = Color.white;
        if (!PlayerAlive()) { FinishAttack(); yield break; }

        // Disparo: la bola sale hacia donde está el jugador y hace daño si lo toca mientras avanza.
        // Si la esquiva, sigue de largo, rebota una vez (en una pared o al final de su tramo,
        // de vuelta hacia el jugador), avanza otro tramo y desaparece.
        Vector2 pos = (Vector2)transform.position + Vector2.up * muzzleHeight;
        Vector2 dir = (PlayerBody() - pos).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        float legLength = Vector2.Distance(pos, PlayerBody()) + shotOvershoot;
        float travelled = 0f;
        bool bounced = false;
        bool overlapping = false; // ya pasó por el jugador sin dañarlo (esquiva): no repetir
        projectile.transform.position = pos;
        projectile.transform.localScale = Vector3.one;
        projectile.SetActive(true);

        while (true)
        {
            float step = projectileSpeed * Time.deltaTime;

            // Pared: rebota (o desaparece si ya rebotó)
            if (WallAhead(pos, dir, step, out RaycastHit2D wall))
            {
                if (bounced) break;
                dir = Vector2.Reflect(dir, wall.normal);
                pos = wall.point + wall.normal * 0.03f;
                Bounce(ref bounced, ref travelled, ref legLength);
                yield return null;
                continue;
            }

            pos += dir * step;
            travelled += step;
            projectile.transform.position = pos;

            bool touching = player != null && Vector2.Distance(pos, PlayerBody()) <= projectileRadius;
            if (touching && !overlapping)
            {
                if (CombatUtil.DamagePlayer(player.gameObject, damage, pos)) break;
                overlapping = true; // esquiva perfecta o invulnerable: la bola sigue
            }
            else if (!touching)
            {
                overlapping = false;
            }

            if (travelled >= legLength)
            {
                if (bounced) break;
                // Rebote al final del tramo: vuelve hacia donde está ahora el jugador
                Vector2 back = PlayerBody() - pos;
                dir = back.sqrMagnitude > 0.01f ? back.normalized : -dir;
                Bounce(ref bounced, ref travelled, ref legLength);
            }
            yield return null;
        }

        projectile.SetActive(false);
        FinishAttack();
    }

    private void FinishAttack()
    {
        nextAttack = Time.time + attackCooldown / attackSpeed;
        attacking = false;
    }

    private void Bounce(ref bool bounced, ref float travelled, ref float legLength)
    {
        bounced = true;
        travelled = 0f;
        legLength = bounceLength;
        projectile.transform.localScale = Vector3.one * 1.3f; // pequeño golpe visual
        StartCoroutine(RestoreProjectileScale());
    }

    private IEnumerator RestoreProjectileScale()
    {
        yield return new WaitForSeconds(0.08f);
        if (projectile != null) projectile.transform.localScale = Vector3.one;
    }

    // Centro del cuerpo del jugador (su posición está en los pies)
    private Vector2 PlayerBody() => player != null ? (Vector2)player.position + Vector2.up * playerBodyHeight : Vector2.zero;

    // Paredes y obstáculos sólidos; ignora triggers, enemigos y al jugador
    private static readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];
    private bool WallAhead(Vector2 from, Vector2 dir, float distance, out RaycastHit2D wall)
    {
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = Physics2D.Raycast(from, dir, filter, wallHits, distance + 0.02f);
        for (int i = 0; i < count; i++)
        {
            var c = wallHits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<EnemyHealth>() != null) continue;
            if (player != null && c.transform.IsChildOf(player)) continue;
            wall = wallHits[i];
            return true;
        }
        wall = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredMin);
        Gizmos.DrawWireSphere(transform.position, preferredMax);
    }
}
