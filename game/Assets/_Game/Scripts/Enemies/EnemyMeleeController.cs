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
    [SerializeField, Min(1)] private int attackDamage = 20;

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

    // Con animador (Lobo del Velo) se ve en 8 direcciones: se pone alerta al ver al jugador, se
    // agacha mientras carga y se lanza a morder en la dirección en la que mira.
    [Header("Animación por direcciones (opcional)")]
    [SerializeField] private float walkFps = 6f;
    [SerializeField] private float deathFps = 5f;
    [Tooltip("Pausa al descubrir al jugador (frame de alerta) antes de perseguirlo.")]
    [SerializeField] private float alertDuration = 0.35f;
    [Tooltip("Tiempo que se ve el frame del mordisco.")]
    [SerializeField] private float attackPoseDuration = 0.25f;
    [Tooltip("Distancia que avanza al morder, dentro de la franja marcada.")]
    [SerializeField] private float lungeDistance = 0.3f;
    [SerializeField] private float lungeDuration = 0.1f;

    private GuardianVarnAnimator anim;
    private EnemyHealth health;
    private Vector2 facing = Vector2.down;
    private float alertTimer;
    private float attackPoseTimer;
    private float lungeTimer;

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

    private EnemyLeash leash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        leash = new EnemyLeash(transform.position);
        anim = GetComponentInChildren<GuardianVarnAnimator>();
        health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.Died += OnDied;
            health.OnDamaged += _ => leash.Provoke();
        }

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
        if (health != null) health.Died -= OnDied;
        if (band != null) Destroy(band.gameObject);
    }

    // Al reaparecer (EnemyRespawner) no debe seguir a mitad de un ataque o aturdido
    private void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
        CancelAttackWarning();
        hitStunTimer = 0f;
        staggerTimer = 0f;
        alertTimer = 0f;
        attackPoseTimer = 0f;
        lungeTimer = 0f;
        PlayerDetected = false;
        leash?.Reset();
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
        UpdateAnimation();
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

        // Mordisco: se lanza hacia delante dentro de la franja que marcó
        if (lungeTimer > 0f)
        {
            lungeTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = attackDir * (lungeDistance / Mathf.Max(0.01f, lungeDuration));
            return;
        }

        // Continuar persiguiendo cuando termina la reacción
        ChasePlayer();
    }

    private void FacePlayer()
    {
        // Con animación por direcciones no se voltea: cada dirección tiene su propio dibujo
        if (anim != null || !PlayerDetected || player == null || enemySprite == null)
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

    // Pierde el rastro en la zona segura o lejos de su lugar, y vuelve a él (EnemyLeash)
    bool detectedNow = leash.Track(distance <= detectionRange, rb.position, player.position);

    if (detectedNow == PlayerDetected)
        return;

    PlayerDetected = detectedNow;
    if (PlayerDetected && anim != null)
        alertTimer = alertDuration;

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
            rb.linearVelocity = leash.ReturnVelocity(rb.position, moveSpeed, Time.fixedDeltaTime);
            return;
        }

        // No moverse si el jugador murió
        Health playerHealth = player.GetComponentInParent<Health>();

        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Quieto mientras prepara el golpe (la franja marca dónde cae) o mientras se pone alerta
        if (preparingAttack || alertTimer > 0f || attackPoseTimer > 0f)
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
        if (alertTimer > 0f) alertTimer -= Time.deltaTime;
        if (attackPoseTimer > 0f) attackPoseTimer -= Time.deltaTime;

        // Cancelar si está aturdido o no detecta al jugador
        if (hitStunTimer > 0f || staggerTimer > 0f || !PlayerDetected || player == null)
        {
            CancelAttackWarning();
            return;
        }

        // Recién alerta: todavía no ataca
        if (alertTimer > 0f)
            return;

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
        // Con animación por direcciones el mordisco sale en la dirección (de 8) en la que mira.
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        attackDir = anim != null
            ? Snap8(toPlayer)
            : Mathf.Abs(toPlayer.x) >= Mathf.Abs(toPlayer.y)
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
        if (anim != null)
        {
            attackPoseTimer = attackPoseDuration;
            lungeTimer = lungeDuration;
        }

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
        playerHealth.TakeDamage(Difficulty.Damage(SceneContext.EnemyDamage(attackDamage)));

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

    private static Vector2 Snap8(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return Vector2.down;
        float angle = Mathf.Round(Mathf.Atan2(dir.y, dir.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // Elige el clip según lo que está haciendo y hacia dónde mira
    private void UpdateAnimation()
    {
        if (anim == null)
            return;

        if (preparingAttack || attackPoseTimer > 0f)
            facing = attackDir;
        else if (PlayerDetected && player != null)
            facing = (Vector2)player.position - rb.position;
        else if (rb.linearVelocity.sqrMagnitude > 0.01f)
            facing = rb.linearVelocity;

        if (hitStunTimer > 0f || staggerTimer > 0f)
            anim.PlayFacing("Hurt", facing, 1f, false);
        else if (attackPoseTimer > 0f)
            anim.PlayFacing("Attack", facing, 1f, false);
        else if (preparingAttack)
            anim.PlayFacing("Windup", facing, 1f, false);
        else if (alertTimer > 0f)
            anim.PlayFacing("Alert", facing, 1f, false);
        else if (rb.linearVelocity.sqrMagnitude > 0.01f)
            anim.PlayFacing("Walk", facing, walkFps * Mathf.Max(0.5f, rb.linearVelocity.magnitude / moveSpeed), true);
        else
            anim.PlayFacing("Idle", facing, 1f, false);
    }

    // EnemyHealth apaga al enemigo al morir: una copia del sprite cae y desaparece
    private void OnDied()
    {
        if (anim == null)
            return;

        var copy = anim.SpawnCopy();
        copy.name = name + "_Muerte";
        copy.PlayFacing("Death", facing, deathFps, false);
        Destroy(copy.gameObject, 2f / deathFps + 0.8f);
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
