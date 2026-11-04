using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMeleeController : MonoBehaviour, IStaggerable
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [Header("Orientación del enemigo")]
[SerializeField] private SpriteRenderer enemySprite;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Detección")]
    [SerializeField] private float detectionRange = 5f;

    [Header("Persecución")]
    [SerializeField] private float moveSpeed = 1.5f;

   [Header("Ataque")]
    [SerializeField] private float attackRange = 0.35f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField, Min(1)] private int attackDamage = 10;

    [Header("Hitbox")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackPointDistance = 0.20f;
    [SerializeField] private float attackRadius = 0.25f;

    [Header("Visual del ataque")]
    [SerializeField] private GameObject attackFX;
    [SerializeField] private float attackFXDuration = 0.15f;
    [SerializeField] private float attackFXRotationOffset = 0f;
    // GAME-101: Advertencia antes del ataque
    [Header("Attack Warning")]
    [SerializeField] private float attackWindup = 0.4f;
    [Tooltip("Multiplica la velocidad de ataque: aviso y espera entre golpes (1.3 = 30% más rápido).")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 1.45f;
    [SerializeField] private GameObject attackWarningFX;

    // Dos patrones: golpe horizontal o vertical, siempre hacia delante (hacia el lado del
    // jugador, nunca a su espalda). Avisa con un destello del propio golem y una franja tenue
    // que crece desde él mientras carga; al golpear, la franja destella.
    [Header("Patrones de ataque (franjas)")]
    [SerializeField] private float bandLength = 0.8f;
    [SerializeField] private float bandWidth = 0.3f;
    [Tooltip("Muestra también el círculo rojo antiguo del golpe (solo para depurar).")]
    [SerializeField] private bool showAttackArea;
    [Tooltip("Color del destello mientras prepara el golpe; parpadea más rápido cuando está por golpear.")]
    [SerializeField] private Color windupColor = new Color(1f, 0.8f, 0.25f, 1f);

    private EnemyHitFeedback hitFeedback;
    private SpriteRenderer body;

    private Telegraph band;
    private Vector2 attackDir;
    private Vector2 attackCenter;

    // Varios golems persiguiendo al jugador se reparten lugares a su alrededor en vez de
    // amontonarse, y se separan entre ellos mientras se acercan.
    [Header("Rodear al jugador")]
    [Tooltip("Distancia entre golems a la que empiezan a separarse.")]
    [SerializeField] private float separationRadius = 0.4f;
    [SerializeField] private float separationWeight = 1.2f;

    private static readonly List<EnemyMeleeController> active = new List<EnemyMeleeController>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActive() => active.Clear();

    private bool preparingAttack;
    private float warningTimer;

    public bool PlayerDetected { get; private set; }

    private Rigidbody2D rb;
    private float nextAttackTime;
    // GAME-101: Reacción al recibir daño
[Header("Hit Reaction")]
[SerializeField] private float hitStunDuration = 0.15f;
[SerializeField] private float knockbackSpeed = 1.5f;

[Tooltip("2do golpe del combo: retroceso un poco mayor y un instante aturdido.")]
[SerializeField] private float staggerKnockbackTime = 0.15f;
[SerializeField] private float staggerKnockbackSpeed = 2.2f;
[SerializeField] private float staggerStunDuration = 0.3f;

private float hitStunTimer;
private float staggerTimer;
private Vector2 knockbackDirection;
private float currentKnockbackSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (attackFX != null)
        {
            attackFX.SetActive(false);
        }
        if (attackWarningFX != null)
        {
            attackWarningFX.SetActive(false);
        }
    }

    private void OnDisable()
    {
        active.Remove(this);
        if (band != null) band.Hide();
    }

    private void OnDestroy()
    {
        if (band != null) Destroy(band.gameObject);
    }

    // Al reaparecer (EnemyRespawner) no debe seguir a mitad de un ataque o aturdido
    private void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
        CancelAttackWarning();
        hitStunTimer = 0f;
        staggerTimer = 0f;
        if (attackFX != null)
            attackFX.SetActive(false);
    }

    // GAME-16: Buscar automáticamente al Player
    private void Start()
    {
        band = Telegraph.Create(Telegraph.BandSprite, name + "_Band");
        band.Opacity = Telegraph.SubtleOpacity;
        hitFeedback = GetComponent<EnemyHitFeedback>();
        body = enemySprite != null ? enemySprite
            : hitFeedback != null && hitFeedback.Renderer != null ? hitFeedback.Renderer
            : GetComponentInChildren<SpriteRenderer>();

        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Enemy Melee: Player asignado automáticamente");
        }
        else
        {
            Debug.LogWarning("Enemy Melee: No se encontró un Player");
        }
    }
    private void Update()
    {
        DetectPlayer();
        UpdateAttackPoint();
        FacePlayer();
        HandleAttack();
    }

  // GAME-101: Controlar empuje y pausa al recibir daño
    private void FixedUpdate()
    {
        if (hitStunTimer > 0f)
        {
            hitStunTimer -= Time.fixedDeltaTime;

            // Retroceder al recibir un golpe
            rb.linearVelocity = knockbackDirection * currentKnockbackSpeed;
            return;
        }

        // Aturdido tras el 2do golpe del combo: quieto un instante
        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Continuar persiguiendo cuando termina la reacción
        ChasePlayer();
    }

    private void FacePlayer()
    {
        if (!PlayerDetected || player == null || enemySprite == null)
            return;

        float differenceX = player.position.x - transform.position.x;

        if (Mathf.Abs(differenceX) < 0.05f)
            return;

        bool playerIsToTheRight = differenceX > 0f;

        enemySprite.flipX = spriteFacesRight
            ? !playerIsToTheRight
            : playerIsToTheRight;
    }
    
    // GAME-101: Empuje y pausa al recibir un golpe
    public void ApplyHitReaction()
    {
        if (player == null)
            return;

        // Super armadura: si ya empezó a preparar el golpe, recibe el daño pero no se
        // interrumpe ni retrocede (antes el combo del jugador cancelaba todos sus ataques)
        if (preparingAttack)
            return;

        Vector2 direction =
            rb.position - (Vector2)player.position;

        knockbackDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.up;

        hitStunTimer = hitStunDuration;
        currentKnockbackSpeed = knockbackSpeed;
    }

    // 2do golpe del combo: retrocede un poco más y queda aturdido un instante.
    // Respeta la super armadura: si ya está preparando su golpe, no se interrumpe.
    public void Stagger(Vector2 from)
    {
        if (!isActiveAndEnabled || preparingAttack)
            return;

        Vector2 direction = rb.position - from;
        knockbackDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        hitStunTimer = staggerKnockbackTime;
        currentKnockbackSpeed = staggerKnockbackSpeed;
        staggerTimer = staggerStunDuration;
    }

    // GAME-09: DETECCIÓN
    // GAME-09: DETECCIÓN
// GAME-15: Evitar detectar jugadores muertos
private void DetectPlayer()
{
    if (player == null)
    {
        PlayerDetected = false;
        return;
    }

    // GAME-15: Verificar si el Player está muerto
    Health playerHealth = player.GetComponentInParent<Health>();

    if (playerHealth != null && playerHealth.IsDead)
    {
        PlayerDetected = false;
        return;
    }

    float distance = Vector2.Distance(
        transform.position,
        player.position
    );

    bool detectedNow = distance <= detectionRange;

    if (detectedNow == PlayerDetected)
        return;

    PlayerDetected = detectedNow;

    Debug.Log(
        PlayerDetected
            ? "Enemy Melee: Player detectado"
            : "Enemy Melee: Player fuera del rango"
    );
}

    // GAME-10: PERSECUCIÓN
    // GAME-10: PERSECUCIÓN
// GAME-16: Movimiento físico estable
    private void ChasePlayer()
    {
        // Detenerse cuando no detecta al jugador
        if (!PlayerDetected || player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // No moverse si el jugador murió
        Health playerHealth = player.GetComponentInParent<Health>();

        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Quieto mientras prepara el golpe: la franja marca dónde cae
        if (preparingAttack)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Va a su lugar alrededor del jugador (no al centro), separándose de los otros golems
        Vector2 target = (Vector2)player.position + SlotOffset();
        Vector2 toTarget = target - rb.position;
        Vector2 steer = toTarget.magnitude > 0.05f ? toTarget.normalized : Vector2.zero;
        steer += Separation() * separationWeight;

        if (steer.sqrMagnitude < 0.0025f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Movimiento mediante velocidad física
        rb.linearVelocity = Vector2.ClampMagnitude(steer, 1f) * moveSpeed;
    }

    // Lugar de este golem alrededor del jugador. Los golems que lo persiguen se ordenan por su
    // ángulo actual y se reparten a partes iguales en una elipse (aplanada por la vista
    // isométrica), así no se cruzan ni se amontonan. Solo, va directo hacia el jugador.
    private Vector2 SlotOffset()
    {
        Vector2 center = player.position;
        float radius = Mathf.Max(0.05f, attackRange * 0.85f);
        float myAngle = AngleAround(center);

        int count = 0, rank = 0;
        int myIndex = active.IndexOf(this);
        float minAngle = float.MaxValue;
        for (int i = 0; i < active.Count; i++)
        {
            var g = active[i];
            if (g == null || !g.PlayerDetected || g.player != player) continue;
            float a = g.AngleAround(center);
            count++;
            minAngle = Mathf.Min(minAngle, a);
            // Empate de ángulo: decide el orden en la lista
            if (g != this && (a < myAngle || (a == myAngle && i < myIndex)))
                rank++;
        }

        float angle = count <= 1 ? myAngle : minAngle + rank * (2f * Mathf.PI / count);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f) * radius;
    }

    private float AngleAround(Vector2 center)
    {
        Vector2 d = rb.position - center;
        return Mathf.Atan2(d.y, d.x);
    }

    // Empuje para no encimarse con otros golems cercanos
    private Vector2 Separation()
    {
        Vector2 push = Vector2.zero;
        foreach (var g in active)
        {
            if (g == null || g == this || g.rb == null) continue;
            Vector2 d = rb.position - g.rb.position;
            float dist = d.magnitude;
            if (dist > 0.001f && dist < separationRadius)
                push += d / dist * (1f - dist / separationRadius);
        }
        return push;
    }

    // ACTUALIZAR HITBOX Y EFECTO VISUAL
    private void UpdateAttackPoint()
    {
        if (player == null)
            return;

        Vector2 direction =
            ((Vector2)player.position -
             (Vector2)transform.position).normalized;

        Vector2 attackPosition =
            (Vector2)transform.position +
            direction * attackPointDistance;

        // GAME-101: Posicionar advertencia del golpe
        if (attackWarningFX != null)
        {
            attackWarningFX.transform.position = attackPosition;
        }
        // La hitbox apunta hacia el jugador.
        if (attackPoint != null)
        {
            attackPoint.position = attackPosition;
        }

        // El slash sigue la misma dirección.
        if (attackFX != null)
        {
            attackFX.transform.position = attackPosition;

            float angle = Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

            attackFX.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle + attackFXRotationOffset
                );
        }
    }

    // GAME-11: CONTROL DEL ATAQUE
    // GAME-11 / GAME-101: Ataque con advertencia previa
    private void HandleAttack()
    {
        // Cancelar si está aturdido o no detecta al jugador
        if (hitStunTimer > 0f || staggerTimer > 0f || !PlayerDetected || player == null)
        {
            CancelAttackWarning();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        // Si está preparando el ataque: ya no se cancela si el jugador se aleja.
        // El golpe cae en la franja marcada; para esquivarlo hay que salir de ella.
        if (preparingAttack)
        {
            warningTimer -= Time.deltaTime;
            WindupFlash();

            if (warningTimer > 0f)
                return;

            // Termina la advertencia y ejecuta el ataque
            CancelAttackWarning();

            Attack();

            nextAttackTime = Time.time + attackCooldown / attackSpeed;
            return;
        }

        // No iniciar ataques fuera del rango
        if (distance > attackRange)
            return;

        // Respetar cooldown
        if (Time.time < nextAttackTime)
            return;

        // Comenzar advertencia
        preparingAttack = true;
        warningTimer = attackWindup / attackSpeed;

        // Patrón según dónde está el jugador: más a los lados = golpe horizontal hacia ese lado,
        // más arriba/abajo = golpe vertical hacia ese lado. Siempre hacia delante.
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        attackDir = Mathf.Abs(toPlayer.x) >= Mathf.Abs(toPlayer.y)
            ? new Vector2(Mathf.Sign(toPlayer.x), 0f)
            : new Vector2(0f, Mathf.Sign(toPlayer.y));
        attackCenter = transform.position;
        if (band != null)
            band.ShowLineGrowing(attackCenter, attackDir, bandLength, bandWidth, warningTimer, Telegraph.Danger);

        if (attackWarningFX != null && showAttackArea)
            attackWarningFX.SetActive(true);
    }

    // Aviso del golpe: el golem parpadea en ámbar, cada vez más rápido. El destello de daño
    // (EnemyHitFeedback) tiene prioridad para que se note que le pegaste.
    private void WindupFlash()
    {
        if (body == null || (hitFeedback != null && hitFeedback.IsFlashing))
            return;

        float total = Mathf.Max(0.01f, attackWindup / attackSpeed);
        float k = 1f - Mathf.Clamp01(warningTimer / total);
        float speed = Mathf.Lerp(10f, 30f, k);
        bool lit = Mathf.Sin((total - warningTimer) * speed) > -0.2f;
        body.color = lit ? windupColor : Color.white;
    }

    // GAME-101: Cancelar advertencia
    private void CancelAttackWarning()
    {
        preparingAttack = false;
        warningTimer = 0f;
        if (band != null)
            band.Hide();
        // Termina el destello de aviso (si no hay un destello de golpe en curso)
        if (body != null && (hitFeedback == null || !hitFeedback.IsFlashing))
            body.color = Color.white;

        if (attackWarningFX != null)
            attackWarningFX.SetActive(false);
    }
        // EJECUTAR ATAQUE
    // GAME-15.1: CONECTAR DAÑO REAL DEL ENEMY MELEE AL PLAYER
    private void Attack()
    {
        Debug.Log("Enemy Melee: ataque cuerpo a cuerpo");

        ShowAttackFX();
        if (band != null)
            band.Impact();

        bool hitPlayer = CheckAttackHitbox();

        if (!hitPlayer)
        {
            Debug.Log("Enemy Melee: ataque fallado");
            return;
        }

        Debug.Log("Enemy Melee: golpe acertado al Player");

        // Obtener el componente Health del jugador
        Health playerHealth = player.GetComponentInParent<Health>();

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "Enemy Melee: el Player no tiene componente Health"
            );
            return;
        }

        // No aplicar daño si el jugador ya murió
        if (playerHealth.IsDead)
            return;

        int previousHealth = playerHealth.CurrentHealth;

        // Aplicar daño usando el sistema Health existente
        playerHealth.TakeDamage(attackDamage);

        if (playerHealth.CurrentHealth < previousHealth)
        {
            Debug.Log(
                $"Enemy Melee: daño aplicado ({attackDamage} base). " +
                $"Vida: {previousHealth} -> {playerHealth.CurrentHealth}"
            );

            // GAME-101: Activar reacción del Player
            if (!playerHealth.IsDead)
            {
                PlayerController playerController =
                    player.GetComponentInParent<PlayerController>();

                if (playerController != null)
                {
                    playerController.ApplyHitReaction(transform.position);
                }
            }
        }
        else
        {
            Debug.Log(
                "Enemy Melee: golpe bloqueado por invulnerabilidad"
            );
        }
    }

    // COMPROBAR HITBOX
    private bool CheckAttackHitbox()
    {
        if (player == null)
            return false;

        // El golpe cubre la franja marcada (horizontal o vertical)
        if (band != null)
            return Telegraph.InsideLine(player.position, attackCenter, attackDir, bandLength, bandWidth);

        if (attackPoint == null)
            return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius
        );

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == player ||
                hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }

    // MOSTRAR EFECTO VISUAL
    private void ShowAttackFX()
    {
        if (attackFX == null)
            return;

        attackFX.SetActive(true);

        CancelInvoke(nameof(HideAttackFX));

        Invoke(
            nameof(HideAttackFX),
            attackFXDuration
        );
    }

    // OCULTAR EFECTO VISUAL
    private void HideAttackFX()
    {
        if (attackFX != null)
        {
            attackFX.SetActive(false);
        }
    }

    // VISUALIZACIÓN DE RANGOS
    private void OnDrawGizmosSelected()
    {
        // Rango de detección.
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        // Rango de ataque.
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        // Hitbox del golpe.
        if (attackPoint != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                attackPoint.position,
                attackRadius
            );
        }
    }
}
