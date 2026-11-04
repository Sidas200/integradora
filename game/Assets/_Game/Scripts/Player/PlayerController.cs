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

    [Header("Esquiva perfecta (dash a tiempo)")]
    [Tooltip("Segundos desde que empieza el dash en los que un golpe se esquiva sin daño.")]
    [SerializeField] private float perfectDodgeWindow = 0.18f;
    [Tooltip("Velocidad del mundo durante la cámara lenta (Tsuki se mueve normal).")]
    [SerializeField, Range(0.05f, 1f)] private float slowMotionScale = 0.3f;
    [Tooltip("Duración de la cámara lenta en segundos reales.")]
    [SerializeField] private float slowMotionDuration = 0.8f;
    [Tooltip("Segundos reales antes de poder activar otra cámara lenta.")]
    [SerializeField] private float slowMotionCooldown = 1.5f;
    [SerializeField] private Color perfectDodgeColor = new Color(0.75f, 0.95f, 1f, 1f);
    [Tooltip("Tinte de toda la pantalla mientras dura la cámara lenta (el HUD queda encima).")]
    [SerializeField] private Color slowMotionTint = new Color(0.25f, 0.5f, 1f, 0.3f);

    [Header("Dash Particles")]
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private float particleDuration = 1.2f;

    [Header("Attack")]
    [Tooltip("Si está activo, alcance y ancho se miden en 'alturas del jugador'. Si no, en unidades de Unity.")]
    [SerializeField] private bool usePlayerSize = true;
    [Tooltip("Qué tan lejos llega el cuadro desde el centro del jugador")]
    [SerializeField] private float attackReach = 0.78f;
    [Tooltip("Ancho del cuadro, perpendicular a la dirección del ataque")]
    [SerializeField] private float attackWidth = 1.04f;
    [Tooltip("Tiempo de espera después de terminar el combo antes de poder atacar otra vez")]
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float attackVisibleTime = 0.15f;
    [Tooltip("Muestra el cuadro rojo del golpe (solo para depurar). El área siempre se ve en la escena con el jugador seleccionado.")]
    [SerializeField] private bool showAttackBox;
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

    [Header("Parado (idle)")]
    [Tooltip("Imagen de Tsuki parado ya cortada (Sprite Mode: Multiple). Si está vacía usa el 1er frame de caminar.")]
    [SerializeField] private Texture2D idleSheet;
    [Tooltip("Se llena solo. Úsalo para ver qué número tiene cada frame.")]
    [SerializeField] private Sprite[] idleAllFrames;
    [SerializeField] private FrameRange idleFrontFrames = new FrameRange(24, 12);
    [SerializeField] private FrameRange idleRightFrames = new FrameRange(0, 12);
    [SerializeField] private FrameRange idleLeftFrames = new FrameRange(12, 12);
    [SerializeField] private FrameRange idleBackFrames = new FrameRange(36, 12);

    [Header("Velocidad de animación")]
    [SerializeField] private float walkFps = 10f;
    [SerializeField] private float attackFps = 30f;
    [SerializeField] private float idleFps = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerStats stats;
    private Health health;

    // Esquiva perfecta y cámara lenta
    private static float baseFixedDeltaTime = -1f;
    private float dashStartTime = -10f;
    private bool slowMotion;
    private float nextSlowMotionTime;
    private float dodgeFlashTimer;
    private Coroutine slowMotionRoutine;
    private float slowMotionEnd;
    private UnityEngine.UI.Image screenTint;

    // Durante la cámara lenta Tsuki va a velocidad normal: compensa la escala de tiempo
    private float PlayerTimeScale => slowMotion && Time.timeScale > 0f ? 1f / Time.timeScale : 1f;
    private float PlayerDelta => Time.deltaTime * PlayerTimeScale;
    private float PlayerFixedDelta => Time.fixedDeltaTime * PlayerTimeScale;
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
    private bool aimWithKeyboard;
    // GAME-101: Reacción al recibir daño
    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [Tooltip("Al recibir daño Tsuki parpadea en rojo este tiempo y la pantalla destella en rojo.")]
    [SerializeField] private float hurtBlinkTime = 0.4f;
    [SerializeField] private Color hurtScreenTint = new Color(1f, 0.1f, 0.1f, 0.35f);
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
    private Sprite[] idleRight;
    private Sprite[] idleFront;
    private Sprite[] idleLeft;
    private Sprite[] idleBack;

    // Estado de la animación
    private Sprite[] currentAnim;
    private Sprite[] lastWalkAnim;
    private Sprite[] lastIdleAnim;
    private float animTimer;
    private int animFrame;

    // Estado del combo
    private AttackState attackState = AttackState.None;
    private float attackStateTimer;
    private bool attackFacingRight = true;
    private bool comboQueued; // clic guardado durante el 1er golpe

    private bool IsAttacking => attackState != AttackState.None;
    private readonly System.Collections.Generic.HashSet<IDamageable> hitThisSwing = new System.Collections.Generic.HashSet<IDamageable>();

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

        health = GetComponent<Health>();
        if (health != null)
        {
            health.DodgeCheck = IsPerfectDodge;
            health.Dodged += OnPerfectDodge;
            // Cualquier daño (de cualquier enemigo) se nota: parpadeo rojo y destello de pantalla
            if (health.onDamaged == null) health.onDamaged = new UnityEngine.Events.UnityEvent<int>();
            health.onDamaged.AddListener(OnHurt);
        }
    }

    // ---------- ESQUIVA PERFECTA ----------

    // Un golpe que llega durante el dash (o justo al empezarlo) se esquiva
    private bool IsPerfectDodge() =>
        isDashing || Time.time - dashStartTime <= perfectDodgeWindow;

    private void OnPerfectDodge()
    {
        dodgeFlashTimer = 0.25f;
        SpawnGhost();
        CameraShake.Shake(0.015f, 0.12f);

        if (Time.timeScale <= 0f)
            return;
        if (slowMotion)
        {
            // Otra esquiva perfecta durante la cámara lenta la reinicia: encadenar esquivas la mantiene
            slowMotionEnd = Time.unscaledTime + slowMotionDuration;
            return;
        }
        if (Time.unscaledTime < nextSlowMotionTime)
        {
            // Sin cámara lenta (espera entre una y otra), pero con un destello azul para que se note
            StartCoroutine(TintPulse());
            return;
        }
        slowMotionRoutine = StartCoroutine(SlowMotion());
    }

    private System.Collections.IEnumerator TintPulse() => ScreenPulse(slowMotionTint, 0.2f);

    private void OnHurt(int damage)
    {
        hitFlashTimer = hurtBlinkTime;
        CameraShake.Shake(0.02f, 0.15f);
        if (!slowMotion && isActiveAndEnabled) StartCoroutine(ScreenPulse(hurtScreenTint, 0.3f));
    }

    // Destello de pantalla que se desvanece (no se usa durante la cámara lenta, que tiene su tinte)
    private System.Collections.IEnumerator ScreenPulse(Color color, float duration)
    {
        EnsureScreenTint();
        for (float t = 0f; t < duration && !slowMotion; t += Time.unscaledDeltaTime)
        {
            SetScreenTint(color, 1f - t / duration);
            yield return null;
        }
        if (!slowMotion) SetScreenTint(0f);
    }

    private System.Collections.IEnumerator SlowMotion()
    {
        if (baseFixedDeltaTime < 0f) baseFixedDeltaTime = Time.fixedDeltaTime;

        slowMotion = true;
        Time.timeScale = slowMotionScale;
        // Pasos de física proporcionales para que el movimiento siga suave
        Time.fixedDeltaTime = baseFixedDeltaTime * slowMotionScale;

        float start = Time.unscaledTime;
        slowMotionEnd = start + slowMotionDuration;
        EnsureScreenTint();
        // Si se abre la pausa o un diálogo (escala 0) se espera a que se cierre.
        // slowMotionEnd se alarga si hay otra esquiva perfecta (OnPerfectDodge).
        while (Time.unscaledTime < slowMotionEnd || Time.timeScale == 0f)
        {
            // Entra rápido (0.08 s) y se desvanece en los últimos 0.25 s
            float fadeIn = Mathf.Clamp01((Time.unscaledTime - start) / 0.08f);
            float fadeOut = Mathf.Clamp01((slowMotionEnd - Time.unscaledTime) / 0.25f);
            SetScreenTint(Time.timeScale == 0f ? 0f : Mathf.Min(fadeIn, fadeOut));
            yield return null;
        }

        EndSlowMotion();
    }

    // Capa azul sobre el juego para que se note la esquiva perfecta. Va debajo del HUD
    // (orden 40; la UI de GameSystems usa 50) y no bloquea clics.
    private void EnsureScreenTint()
    {
        if (screenTint != null) return;
        var go = new GameObject("SlowMotionTint", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        screenTint = new GameObject("Tint", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        var rt = screenTint.rectTransform;
        rt.SetParent(go.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        screenTint.raycastTarget = false;
        SetScreenTint(0f);
    }

    private void SetScreenTint(float amount) => SetScreenTint(slowMotionTint, amount);

    private void SetScreenTint(Color color, float amount)
    {
        if (screenTint == null) return;
        var c = color;
        c.a *= amount;
        screenTint.color = c;
        screenTint.enabled = amount > 0f;
    }

    private void EndSlowMotion()
    {
        SetScreenTint(0f);
        if (!slowMotion) return;
        slowMotion = false;
        slowMotionRoutine = null;
        // Solo se devuelve la escala si nadie más la cambió (pausa, diálogo, cambio de escena)
        if (Mathf.Approximately(Time.timeScale, slowMotionScale)) Time.timeScale = 1f;
        if (baseFixedDeltaTime > 0f) Time.fixedDeltaTime = baseFixedDeltaTime;
        nextSlowMotionTime = Time.unscaledTime + slowMotionCooldown;
    }

    private void OnDisable()
    {
        if (slowMotionRoutine != null) StopCoroutine(slowMotionRoutine);
        EndSlowMotion();
    }


    private void Update()
    {
    // Pausa, diálogo, forja o transición (o el frame en que se cerró uno de ellos,
    // para que la A que cerró el diálogo no haga también un dash)
    if (GameInput.GameplayBlocked)
    {
        movement = Vector2.zero;
        UpdateSpriteAnimation();
        return;
    }

    // =========================
    // MOVIMIENTO TECLADO
    // =========================
    movement = Vector2.zero;

    if (Keyboard.current != null)
    {
        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
            movement.y += 1;

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
            movement.y -= 1;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
            movement.x -= 1;

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
            movement.x += 1;
    }

    // =========================
    // MOVIMIENTO CONTROL PS5 / XBOX / AYN THOR
    // =========================
    // La cruceta tiene prioridad sobre el stick (se leen todos los mandos conectados)
    Vector2 stick = GamepadInput.Move;

    // Si se utiliza el control, tiene prioridad
    if (stick.sqrMagnitude > 0.01f)
        movement = stick;

    // Limitar velocidad y permitir movimiento analógico
    movement = Vector2.ClampMagnitude(movement, 1f);

    if (movement.sqrMagnitude > 0f)
        lastMoveDirection = movement.normalized;

    // =========================
    // COOLDOWNS
    // =========================
    if (dashCooldownTimer > 0)
        dashCooldownTimer -= PlayerDelta;

    if (attackCooldownTimer > 0)
        attackCooldownTimer -= PlayerDelta;

    // =========================
    // DASH: ESPACIO / X PS5 / A XBOX
    // =========================
    bool dashPressed =
        (Keyboard.current != null &&
         Keyboard.current.spaceKey.wasPressedThisFrame) ||
        GamepadInput.SouthPressed;

    if (dashPressed && !isDashing && dashCooldownTimer <= 0)
    {
        isDashing = true;
        dashStartTime = Time.time;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        ghostTimer = 0f;

        if (dashParticles != null)
            dashParticles.Play();

        // El dash cancela el ataque
        if (IsAttacking)
            CancelAttack();
    }

    // =========================
    // EFECTO VISUAL DEL DASH
    // =========================
    if (isDashing)
    {
        ghostTimer -= PlayerDelta;

        if (ghostTimer <= 0f)
        {
            SpawnGhost();
            ghostTimer = ghostSpawnInterval;
        }
    }

    // =========================
    // ATAQUE: MOUSE / J / CUADRADO PS5
    // =========================
    if (Mouse.current != null &&
        Mouse.current.leftButton.wasPressedThisFrame)
    {
        aimWithKeyboard = false;
        OnAttackPressed();
    }
    else if (Keyboard.current != null &&
             Keyboard.current.jKey.wasPressedThisFrame)
    {
        aimWithKeyboard = true;
        OnAttackPressed();
    }
    else if (GamepadInput.WestPressed)
    {
        // Cuadrado PS5 / X Xbox
        aimWithKeyboard = true;
        OnAttackPressed();
    }

    // =========================
    // ACTUALIZAR ATAQUE Y ANIMACIONES
    // =========================
    UpdateAttackBox();
    UpdateSpriteAnimation();

    // =========================
    // GAME-101: HIT FEEDBACK
    // =========================
    if (hitFlashTimer > 0f)
        hitFlashTimer -= PlayerDelta;

    if (dodgeFlashTimer > 0f)
        dodgeFlashTimer -= PlayerDelta;

    // Prioridad de efectos visuales
    if (dodgeFlashTimer > 0f)
        sr.color = perfectDodgeColor;
    else if (hitFlashTimer > 0f)
        // Parpadeo rojo / normal cada 0.07 s para que se note aunque el sprite sea oscuro
        sr.color = Mathf.Repeat(hitFlashTimer, 0.14f) > 0.07f ? hitFlashColor : normalColor;
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

        rb.linearVelocity = lastMoveDirection * dashSpeed * PlayerTimeScale;
        dashTimer -= PlayerFixedDelta;

        if (dashTimer <= 0f)
            isDashing = false;
    }
    // GAME-101: Retroceso cuando el Player recibe daño
    else if (hitStunTimer > 0f)
    {
        rb.linearVelocity = knockbackDirection * knockbackSpeed * PlayerTimeScale;
        hitStunTimer -= PlayerFixedDelta;
    }
    // DEV: Mantener al jugador quieto durante el combo
    else if (IsAttacking)
    {
        rb.linearVelocity = Vector2.zero;
    }
    // Movimiento normal
    else
    {
        rb.linearVelocity = movement * stats.MoveSpeed * PlayerTimeScale;
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
                attackStateTimer -= PlayerDelta;
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

        // =========================
        // DIRECCIÓN DEL ATAQUE
        // =========================
        Vector2 origin = sr.bounds.center;

        if (aimWithKeyboard || Mouse.current == null)
        {
            // Apuntar con joystick derecho
            Vector2 rightStick = GamepadInput.RightStick;

            // Si el stick derecho no se utiliza,
            // atacar hacia donde caminó Tsuki
            if (rightStick.sqrMagnitude > 0.09f)
                dir = rightStick;
            else
                dir = lastMoveDirection;
        }
        else
        {
            // Apuntar con mouse
            dir = (Vector2)cam.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            ) - origin;
        }

        if (dir.sqrMagnitude < 0.0001f)
            dir = lastMoveDirection;

        // Ajustar ataque a 4 direcciones
        attackDirection = SnapToCardinal(dir);

        if (animator != null)
            animator.SetTrigger("Attack");

        // =========================
        // HITBOX DEL ATAQUE
        // =========================
        GetAttackBox(
            sr,
            attackDirection,
            out Vector2 center,
            out Vector2 size
        );

        // Detectar enemigos alcanzados
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            enemyLayer
        );

        // HEX-03: Evitar daño duplicado
        // cuando un enemigo tiene varios colliders
        hitThisSwing.Clear();

        foreach (var hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable != null &&
                hitThisSwing.Add(damageable))
            {
                damageable.TakeDamage(stats.Attack);
            }
        }

        // =========================
        // MOSTRAR HITBOX VISUAL
        // =========================
        attackBox.transform.localScale =
            new Vector3(size.x, size.y, 1f);

        attackBox.SetActive(showAttackBox);

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

        attackVisibleTimer -= PlayerDelta;
        if (attackVisibleTimer <= 0f)
            attackBox.SetActive(false);
    }
    // GAME-101: Reacción visual y física al recibir daño
    public void ApplyHitReaction(Vector2 attackerPosition)
    {
        // Activar destello rojo (sin acortar el parpadeo de OnHurt)
        hitFlashTimer = Mathf.Max(hitFlashTimer, hitFlashDuration);

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
        if (screenTint != null)
            Destroy(screenTint.canvas.gameObject);
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
        idleFront = GetFrames(idleAllFrames, idleFrontFrames, "Idle Front");
        idleRight = GetFrames(idleAllFrames, idleRightFrames, "Idle Right");
        idleLeft = GetFrames(idleAllFrames, idleLeftFrames, "Idle Left");
        idleBack = GetFrames(idleAllFrames, idleBackFrames, "Idle Back");

        lastWalkAnim = walkFront;
        lastIdleAnim = idleFront;
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
        return GetFrames(allFrames, range, nombre);
    }

    private Sprite[] GetFrames(Sprite[] frames, FrameRange range, string nombre)
    {
        // El sheet de parado es opcional: sin él se usa el 1er frame de caminar
        if (frames == null || frames.Length == 0)
            return new Sprite[0];

        int start = Mathf.Clamp(range.start, 0, frames.Length - 1);
        int count = Mathf.Clamp(range.count, 0, frames.Length - start);

        if (start != range.start || count != range.count)
            Debug.LogWarning($"{nombre}: el rango {range.start}-{range.start + range.count - 1} se sale del sprite sheet (tiene {frames.Length} frames).");

        Sprite[] result = new Sprite[count];
        System.Array.Copy(frames, start, result, 0, count);
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
        allFrames = LoadSheet(spriteSheet);
        idleAllFrames = LoadSheet(idleSheet);
    }

    private static Sprite[] LoadSheet(Texture2D sheet)
    {
        if (sheet == null)
            return new Sprite[0];

        string path = UnityEditor.AssetDatabase.GetAssetPath(sheet);

        // Ordena los frames como se leen: de arriba a abajo y de izquierda a derecha
        Sprite[] frames = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        if (frames.Length == 0)
            Debug.LogWarning($"{sheet.name} no tiene sprites. Ponle Sprite Mode: Multiple y córtala en el Sprite Editor.");
        return frames;
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
            Sprite[] idle;
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                walk = dir.x > 0 ? walkRight : walkLeft;
                idle = dir.x > 0 ? idleRight : idleLeft;
            }
            else
            {
                walk = dir.y > 0 ? walkBack : walkFront;
                idle = dir.y > 0 ? idleBack : idleFront;
            }

            if (walk == null || walk.Length == 0)
                return;

            lastWalkAnim = walk;
            lastIdleAnim = idle;
            SetAnim(walk);
            StepAnim(walkFps, true);
        }
        else if (lastIdleAnim != null && lastIdleAnim.Length > 0)
        {
            // Quieto: animación de parado hacia la última dirección
            SetAnim(lastIdleAnim);
            StepAnim(idleFps, true);
        }
        else
        {
            // Sin sheet de parado: primer frame de la última dirección
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
        animTimer += PlayerDelta;
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