using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Dash Visuals")]
    [SerializeField] private Color dashColor = new Color(0.5f, 0.8f, 1f, 1f);
    [SerializeField] private Color cooldownColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float ghostSpawnInterval = 0.03f;
    [SerializeField] private float ghostLifetime = 0.25f;

    [Header("Dash Particles")]
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private float particleDuration = 1.2f;

    [Header("Attack")]
    [Tooltip("Si está activo, alcance y ancho se miden en 'alturas del jugador'. Si no, en unidades de Unity.")]
    [SerializeField] private bool usePlayerSize = true;
    [Tooltip("Qué tan lejos llega el cuadro desde el centro del jugador")]
    [SerializeField] private float attackReach = 0.9f;
    [Tooltip("Ancho del cuadro, perpendicular a la dirección del ataque")]
    [SerializeField] private float attackWidth = 1.6f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackVisibleTime = 0.15f;
    [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation (opcional por ahora)")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerStats stats;
    private Camera cam;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.down;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float ghostTimer;
    private float attackCooldownTimer;

    private GameObject attackBox;
    private float attackVisibleTimer;
    private Vector2 attackDirection = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();
        cam = Camera.main;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (dashParticles != null)
        {
            var main = dashParticles.main;
            main.startLifetime = particleDuration;
        }

        CreateAttackBox();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        movement = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            movement.y += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            movement.y -= 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            movement.x -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            movement.x += 1;

        movement = movement.normalized;

        if (movement.sqrMagnitude > 0)
            lastMoveDirection = movement;

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isDashing && dashCooldownTimer <= 0)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            ghostTimer = 0f;
            if (dashParticles != null)
                dashParticles.Play();
        }

        if (isDashing)
        {
            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0f)
            {
                SpawnGhost();
                ghostTimer = ghostSpawnInterval;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }

        UpdateAttackBox();

        if (isDashing)
            sr.color = dashColor;
        else if (dashCooldownTimer > 0)
            sr.color = cooldownColor;
        else
            sr.color = normalColor;
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = lastMoveDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
                isDashing = false;
        }
        else
        {
            rb.linearVelocity = movement * stats.MoveSpeed;
        }
    }

    // ---------- ATAQUE ----------

    private void TryAttack()
    {
        if (attackCooldownTimer > 0)
            return;

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No hay cámara con el tag MainCamera");
            return;
        }

        // Dirección desde el centro del sprite hacia el mouse, ajustada a 4 direcciones
        Vector2 origin = sr.bounds.center;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = mouseWorld - origin;
        if (dir.sqrMagnitude < 0.0001f)
            dir = lastMoveDirection;

        attackDirection = SnapToCardinal(dir);
        attackCooldownTimer = attackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");

        GetAttackBox(sr, attackDirection, out Vector2 center, out Vector2 size);

        // Detección de golpes con el mismo cuadro que se dibuja
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(stats.Attack);
        }

        // Mostrar el cuadro rojo
        attackBox.transform.localScale = new Vector3(size.x, size.y, 1f);
        attackBox.SetActive(true);
        attackVisibleTimer = attackVisibleTime;
        PlaceAttackBox();
    }

    private static Vector2 SnapToCardinal(Vector2 d)
    {
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            return new Vector2(Mathf.Sign(d.x), 0f);
        return new Vector2(0f, Mathf.Sign(d.y));
    }

    private float GetBaseSize(SpriteRenderer renderer)
    {
        if (!usePlayerSize || renderer == null || renderer.sprite == null)
            return 1f;

        float h = renderer.bounds.size.y;
        return h > 0.01f ? h : 1f;
    }

    // Cuadro que empieza en el centro del jugador y se extiende hacia la dirección del ataque
    private void GetAttackBox(SpriteRenderer renderer, Vector2 dir, out Vector2 center, out Vector2 size)
    {
        float baseSize = GetBaseSize(renderer);
        float reach = attackReach * baseSize;
        float width = attackWidth * baseSize;

        Vector2 origin = renderer != null ? (Vector2)renderer.bounds.center : (Vector2)transform.position;
        bool horizontal = Mathf.Abs(dir.x) > 0f;

        size = horizontal ? new Vector2(reach, width) : new Vector2(width, reach);
        center = origin + dir * (reach * 0.5f);
    }

    private void CreateAttackBox()
    {
        attackBox = new GameObject("AttackBox");
        SpriteRenderer boxSr = attackBox.AddComponent<SpriteRenderer>();

        // Sprite blanco de 1x1 unidad, se tiñe con el color
        Texture2D tex = Texture2D.whiteTexture;
        boxSr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        boxSr.sharedMaterial = sr.sharedMaterial;
        boxSr.color = attackColor;
        boxSr.sortingLayerID = sr.sortingLayerID;
        boxSr.sortingOrder = sr.sortingOrder - 1; // detrás del jugador para que se le vea encima

        attackBox.SetActive(false);
    }

    private void PlaceAttackBox()
    {
        GetAttackBox(sr, attackDirection, out Vector2 center, out _);
        attackBox.transform.position = new Vector3(center.x, center.y, transform.position.z);
    }

    private void UpdateAttackBox()
    {
        if (attackVisibleTimer <= 0f)
            return;

        // El cuadro sigue al jugador mientras está visible
        PlaceAttackBox();

        attackVisibleTimer -= Time.deltaTime;
        if (attackVisibleTimer <= 0f)
            attackBox.SetActive(false);
    }

    private void OnDestroy()
    {
        if (attackBox != null)
            Destroy(attackBox);
    }

    private void OnDrawGizmosSelected()
    {
        SpriteRenderer renderer = sr != null ? sr : GetComponent<SpriteRenderer>();
        GetAttackBox(renderer, attackDirection, out Vector2 center, out Vector2 size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }

    // ---------- DASH ----------

    private void SpawnGhost()
    {
        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;

        DashGhost dashGhost = ghost.AddComponent<DashGhost>();
        dashGhost.Initialize(sr.sprite, dashColor, transform.localScale, ghostLifetime, sr.material, sr.sortingLayerName, sr.sortingOrder);
    }
}