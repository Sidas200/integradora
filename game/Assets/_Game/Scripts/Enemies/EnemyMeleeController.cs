
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMeleeController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

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

    public bool PlayerDetected { get; private set; }

    private Rigidbody2D rb;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (attackFX != null)
        {
            attackFX.SetActive(false);
        }
    }

    private void Update()
    {
        DetectPlayer();
        UpdateAttackPoint();
        HandleAttack();
    }

    private void FixedUpdate()
    {
        ChasePlayer();
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
    private void ChasePlayer()
    {
        if (!PlayerDetected || player == null)
            return;

        float distance = Vector2.Distance(
            rb.position,
            player.position
        );

        // Detenerse cuando esté suficientemente cerca.
        if (distance <= attackRange)
            return;

        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        Vector2 nextPosition =
            rb.position +
            direction * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
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
    private void HandleAttack()
    {
        if (!PlayerDetected || player == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        // No atacar si está lejos.
        if (distance > attackRange)
            return;

        // Respetar el cooldown.
        if (Time.time < nextAttackTime)
            return;

        Attack();

        nextAttackTime = Time.time + attackCooldown;
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
