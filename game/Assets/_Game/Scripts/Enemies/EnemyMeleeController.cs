
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMeleeController : MonoBehaviour
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
    [SerializeField] private GameObject attackWarningFX;

    private bool preparingAttack;
    private float warningTimer;

    public bool PlayerDetected { get; private set; }

    private Rigidbody2D rb;
    private float nextAttackTime;
    // GAME-101: Reacción al recibir daño
[Header("Hit Reaction")]
[SerializeField] private float hitStunDuration = 0.15f;
[SerializeField] private float knockbackSpeed = 1.5f;

private float hitStunTimer;
private Vector2 knockbackDirection;

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

    // GAME-16: Buscar automáticamente al Player
    private void Start()
    {
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
            rb.linearVelocity = knockbackDirection * knockbackSpeed;
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

        Vector2 direction =
            rb.position - (Vector2)player.position;

        knockbackDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.up;

        hitStunTimer = hitStunDuration;
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

        Vector2 direction =
            (Vector2)player.position - rb.position;

        float distance = direction.magnitude;

        // Detenerse antes de llegar al centro del jugador
        float stopDistance = Mathf.Max(0f, attackRange - 0.05f);

        if (distance <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Movimiento mediante velocidad física
        rb.linearVelocity = direction.normalized * moveSpeed;
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
        if (hitStunTimer > 0f || !PlayerDetected || player == null)
        {
            CancelAttackWarning();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        // Si está preparando el ataque
        if (preparingAttack)
        {
            // Cancelar si el jugador sale del rango
            if (distance > attackRange)
            {
                CancelAttackWarning();
                return;
            }

            warningTimer -= Time.deltaTime;

            if (warningTimer > 0f)
                return;

            // Termina la advertencia y ejecuta el ataque
            CancelAttackWarning();

            Attack();

            nextAttackTime = Time.time + attackCooldown;
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
        warningTimer = attackWindup;

        if (attackWarningFX != null)
            attackWarningFX.SetActive(true);
    }

    // GAME-101: Cancelar advertencia
    private void CancelAttackWarning()
    {
        preparingAttack = false;
        warningTimer = 0f;

        if (attackWarningFX != null)
            attackWarningFX.SetActive(false);
    }
        // EJECUTAR ATAQUE
    // GAME-15.1: CONECTAR DAÑO REAL DEL ENEMY MELEE AL PLAYER
    private void Attack()
    {
        Debug.Log("Enemy Melee: ataque cuerpo a cuerpo");

        ShowAttackFX();

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
        if (attackPoint == null || player == null)
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
