using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [System.Serializable]
    public struct FrameRange
    {
        [Tooltip("Número del primer frame (empieza en 0)")]
        public int start;
        [Tooltip("Cuántos frames tiene la animación")]
        public int count;

        public FrameRange(int start, int count)
        {
            this.start = start;
            this.count = count;
        }
    }

    private enum AttackState { None, Hit1, Wait1, Hit2 }

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
    [Tooltip("Tiempo de espera después de terminar el combo antes de poder atacar otra vez")]
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float attackVisibleTime = 0.15f;
    [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private LayerMask enemyLayer;

    [Header("Combo")]
    [Tooltip("Segundos que se queda en el último frame del 1er golpe esperando el 2do clic")]
    [SerializeField] private float comboWindow = 0.8f;

    [Header("Animation (opcional por ahora)")]
    [SerializeField] private Animator animator;

    [Header("Sprite Sheet")]
    [Tooltip("Arrastra aquí la imagen ya cortada (Sprite Mode: Multiple)")]
    [SerializeField] private Texture2D spriteSheet;
    [Tooltip("Se llena solo. Úsalo para ver qué número tiene cada frame.")]
    [SerializeField] private Sprite[] allFrames;

    [Header("Rangos - Caminata (inicio y cantidad)")]
    [SerializeField] private FrameRange walkRightFrames = new FrameRange(16, 8);
    [SerializeField] private FrameRange walkFrontFrames = new FrameRange(24, 8);
    [SerializeField] private FrameRange walkLeftFrames = new FrameRange(32, 8);
    [SerializeField] private FrameRange walkBackFrames = new FrameRange(40, 8);

    [Header("Rangos - Ataque (inicio y cantidad)")]
    [SerializeField] private FrameRange attackRight1Frames = new FrameRange(0, 8);
    [SerializeField] private FrameRange attackRight2Frames = new FrameRange(8, 8);
    [SerializeField] private FrameRange attackLeft1Frames = new FrameRange(48, 8);
    [SerializeField] private FrameRange attackLeft2Frames = new FrameRange(56, 8);

    [Header("Velocidad de animación")]
    [SerializeField] private float walkFps = 10f;
    [SerializeField] private float attackFps = 30f;

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
    // GAME-101: Reacción al recibir daño
    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private float hitStunDuration = 0.15f;
    [SerializeField] private float knockbackSpeed = 2f;

    private float hitFlashTimer;
    private float hitStunTimer;
    private Vector2 knockbackDirection;

    // Animaciones armadas a partir de los rangos
    private Sprite[] walkRight;
    private Sprite[] walkFront;
    private Sprite[] walkLeft;
    private Sprite[] walkBack;
    private Sprite[] attackRight1;
    private Sprite[] attackRight2;
    private Sprite[] attackLeft1;
    private Sprite[] attackLeft2;

    // Estado de la animación
    private Sprite[] currentAnim;
    private Sprite[] lastWalkAnim;
    private float animTimer;
    private int animFrame;

    // Estado del combo
    private AttackState attackState = AttackState.None;
    private float attackStateTimer;
    private bool attackFacingRight = true;
    private bool comboQueued; // clic guardado durante el 1er golpe

    private bool IsAttacking => attackState != AttackState.None;

    // Capítulo 1: sin la espada carmesí Tsuki no puede atacar (lo controla PlayerStoryState)
    public bool AttackEnabled { get; set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // El sprite puede estar en un hijo (raíz en los pies para el sorting isométrico)
        sr = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();
        cam = Camera.main;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (dashParticles != null)
        {
            var main = dashParticles.main;
            main.startLifetime = particleDuration;
        }

        CreateAttackBox();
        BuildAnimations();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Pausa, diálogo, forja o transición: el jugador se queda quieto
        if (GameInput.Locked)
        {
            movement = Vector2.zero;
            UpdateSpriteAnimation();
            return;
        }

        // Se sigue leyendo el teclado durante el ataque para que el dash
        // salga hacia donde el jugador está presionando
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

            // El dash cancela el ataque
            if (IsAttacking)
                CancelAttack();
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
            OnAttackPressed();
        }

        UpdateAttackBox();
        UpdateSpriteAnimation();

        // GAME-101: Actualizar destello por daño
        if (hitFlashTimer > 0f)
            hitFlashTimer -= Time.deltaTime;

        // Prioridad al efecto de daño
        if (hitFlashTimer > 0f)
            sr.color = hitFlashColor;
        else if (isDashing)
            sr.color = dashColor;
        else if (dashCooldownTimer > 0f)
            sr.color = cooldownColor;
        else
            sr.color = normalColor;
    }

    
private void FixedUpdate()
{
    // DASH: Tiene prioridad sobre el resto
    if (isDashing)
    {
        hitStunTimer = 0f;

        rb.linearVelocity = lastMoveDirection * dashSpeed;
        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
            isDashing = false;
    }
    // GAME-101: Retroceso cuando el Player recibe daño
    else if (hitStunTimer > 0f)
    {
        rb.linearVelocity = knockbackDirection * knockbackSpeed;
        hitStunTimer -= Time.fixedDeltaTime;
    }
    // DEV: Mantener al jugador quieto durante el combo
    else if (IsAttacking)
    {
        rb.linearVelocity = Vector2.zero;
    }
    // Movimiento normal
    else
    {
        rb.linearVelocity = movement * stats.MoveSpeed;
    }
}


    // ---------- COMBO ----------

    private void OnAttackPressed()
    {
        // No se puede atacar durante el dash
        if (isDashing || !AttackEnabled)
            return;

        switch (attackState)
        {
            case AttackState.None:
                if (attackCooldownTimer > 0)
                    return;
                StartFirstHit();
                break;

            case AttackState.Hit1:
                // Clic durante el 1er golpe: se guarda y el 2do sale al terminar
                comboQueued = true;
                break;

            case AttackState.Wait1:
                // Clic durante la pausa del 1er golpe: sale el 2do
                StartSecondHit();
                break;

            // Hit2: el clic se ignora para que no se acumule
        }
    }

    private void CancelAttack()
    {
        EndAttack();

        // Esconde el cuadro rojo si seguía visible
        attackVisibleTimer = 0f;
        if (attackBox != null)
            attackBox.SetActive(false);
    }

    private void StartFirstHit()
    {
        comboQueued = false;

        if (!DoHit(out Vector2 dir))
        {
            EndAttack();
            return;
        }

        // El lado se decide según dónde está el mouse
        attackFacingRight = dir.x >= 0f;
        StartAttackPart(attackFacingRight ? attackRight1 : attackLeft1, AttackState.Hit1);
    }

    private void StartSecondHit()
    {
        comboQueued = false;

        if (!DoHit(out Vector2 dir))
        {
            EndAttack();
            return;
        }

        // El 2do golpe vuelve a decidir el lado según dónde está el mouse
        attackFacingRight = dir.x >= 0f;
        StartAttackPart(attackFacingRight ? attackRight2 : attackLeft2, AttackState.Hit2);
    }

    private void StartAttackPart(Sprite[] anim, AttackState state)
    {
        if (anim == null || anim.Length == 0)
        {
            EndAttack();
            return;
        }

        attackState = state;
        currentAnim = null; // fuerza a reiniciar aunque sea la misma animación
        SetAnim(anim);
    }

    private void EndAttack()
    {
        attackState = AttackState.None;
        comboQueued = false;
        currentAnim = null;
        attackCooldownTimer = attackCooldown;
    }

    // Devuelve true mientras el ataque controla el sprite
    private bool UpdateAttackAnimation()
    {
        switch (attackState)
        {
            case AttackState.Hit1:
                if (!StepAnim(attackFps, false))
                {
                    if (comboQueued)
                        StartSecondHit();
                    else
                    {
                        attackState = AttackState.Wait1;
                        attackStateTimer = comboWindow;
                    }
                }
                return true;

            case AttackState.Wait1:
                attackStateTimer -= Time.deltaTime;
                if (attackStateTimer <= 0f)
                {
                    EndAttack();
                    return false;
                }
                return true;

            case AttackState.Hit2:
                if (!StepAnim(attackFps, false))
                {
                    // Fin del combo: regresa directo a caminar
                    EndAttack();
                    return false;
                }
                return true;
        }

        return false;
    }

    // ---------- GOLPE (daño + cuadro rojo) ----------

    private bool DoHit(out Vector2 dir)
    {
        dir = lastMoveDirection;

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No hay cámara con el tag MainCamera");
            return false;
        }

        // Dirección desde el centro del sprite hacia el mouse, ajustada a 4 direcciones
        Vector2 origin = sr.bounds.center;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        dir = mouseWorld - origin;
        if (dir.sqrMagnitude < 0.0001f)
            dir = lastMoveDirection;

        attackDirection = SnapToCardinal(dir);

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

        return true;
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
    // GAME-101: Reacción visual y física al recibir daño
    public void ApplyHitReaction(Vector2 attackerPosition)
    {
        // Activar destello rojo
        hitFlashTimer = hitFlashDuration;

        // Calcular dirección contraria al atacante
        Vector2 direction =
            (Vector2)transform.position - attackerPosition;

        knockbackDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : -lastMoveDirection;

        // Activar retroceso
        hitStunTimer = hitStunDuration;
    }
    private void OnDestroy()
    {
        if (attackBox != null)
            Destroy(attackBox);
    }

    private void OnDrawGizmosSelected()
    {
        SpriteRenderer renderer = sr != null ? sr : GetComponentInChildren<SpriteRenderer>();
        GetAttackBox(renderer, attackDirection, out Vector2 center, out Vector2 size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }

    // ---------- SPRITE SHEET ----------

    // Arma cada animación tomando los frames del rango indicado
    private void BuildAnimations()
    {
        walkRight = GetFrames(walkRightFrames, "Walk Right");
        walkFront = GetFrames(walkFrontFrames, "Walk Front");
        walkLeft = GetFrames(walkLeftFrames, "Walk Left");
        walkBack = GetFrames(walkBackFrames, "Walk Back");
        attackRight1 = GetFrames(attackRight1Frames, "Attack Right 1");
        attackRight2 = GetFrames(attackRight2Frames, "Attack Right 2");
        attackLeft1 = GetFrames(attackLeft1Frames, "Attack Left 1");
        attackLeft2 = GetFrames(attackLeft2Frames, "Attack Left 2");

        lastWalkAnim = walkFront;
        if (walkFront.Length > 0)
            sr.sprite = walkFront[0];
    }

    private Sprite[] GetFrames(FrameRange range, string nombre)
    {
        if (allFrames == null || allFrames.Length == 0)
        {
            Debug.LogWarning("No hay frames. Asigna el Sprite Sheet en el Inspector.");
            return new Sprite[0];
        }

        int start = Mathf.Clamp(range.start, 0, allFrames.Length - 1);
        int count = Mathf.Clamp(range.count, 0, allFrames.Length - start);

        if (start != range.start || count != range.count)
            Debug.LogWarning($"{nombre}: el rango {range.start}-{range.start + range.count - 1} se sale del sprite sheet (tiene {allFrames.Length} frames).");

        Sprite[] result = new Sprite[count];
        System.Array.Copy(allFrames, start, result, 0, count);
        return result;
    }

#if UNITY_EDITOR
    // Se ejecuta en el editor cada vez que cambias algo en el Inspector
    private void OnValidate()
    {
        LoadFramesFromSheet();
    }

    [ContextMenu("Recargar frames del Sprite Sheet")]
    private void LoadFramesFromSheet()
    {
        if (spriteSheet == null)
        {
            allFrames = new Sprite[0];
            return;
        }

        string path = UnityEditor.AssetDatabase.GetAssetPath(spriteSheet);

        // Ordena los frames como se leen: de arriba a abajo y de izquierda a derecha
        allFrames = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        if (allFrames.Length == 0)
            Debug.LogWarning("La imagen no tiene sprites. Ponle Sprite Mode: Multiple y córtala en el Sprite Editor.");
    }
#endif

    // ---------- ANIMACIÓN POR SPRITES ----------

    private void UpdateSpriteAnimation()
    {
        // Si hay un ataque en curso, él controla el sprite
        if (UpdateAttackAnimation())
            return;

        // Durante el dash, movement puede ser cero, así que usamos la última dirección
        Vector2 dir = isDashing ? lastMoveDirection : movement;

        if (dir.sqrMagnitude > 0f)
        {
            Sprite[] walk;
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                walk = dir.x > 0 ? walkRight : walkLeft;
            else
                walk = dir.y > 0 ? walkBack : walkFront;

            if (walk == null || walk.Length == 0)
                return;

            lastWalkAnim = walk;
            SetAnim(walk);
            StepAnim(walkFps, true);
        }
        else
        {
            // Quieto: primer frame de la última dirección
            currentAnim = null;
            if (lastWalkAnim != null && lastWalkAnim.Length > 0)
                sr.sprite = lastWalkAnim[0];
        }
    }

    private void SetAnim(Sprite[] anim)
    {
        if (anim == currentAnim)
            return;
        currentAnim = anim;
        animFrame = 0;
        animTimer = 0f;
        sr.sprite = currentAnim[0];
    }

    // Devuelve false cuando una animación sin loop termina (el sprite se queda en el último frame)
    private bool StepAnim(float fps, bool loop)
    {
        animTimer += Time.deltaTime;
        if (animTimer < 1f / fps)
            return true;

        animTimer -= 1f / fps;
        animFrame++;

        if (animFrame >= currentAnim.Length)
        {
            if (!loop)
                return false;
            animFrame = 0;
        }

        sr.sprite = currentAnim[animFrame];
        return true;
    }

    // ---------- DASH ----------

    private void SpawnGhost()
    {
        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = sr.transform.position;
        ghost.transform.rotation = transform.rotation;

        DashGhost dashGhost = ghost.AddComponent<DashGhost>();
        dashGhost.Initialize(sr.sprite, dashColor, transform.localScale, ghostLifetime, sr.material, sr.sortingLayerName, sr.sortingOrder);
    }
}