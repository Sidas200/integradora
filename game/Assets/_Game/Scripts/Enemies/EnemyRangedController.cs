using System.Collections;
using UnityEngine;

// Arquetipo 2 (Centinela): enemigo a distancia. Mantiene distancia con el jugador y ataca así:
// 1) marca en el suelo un círculo rojo donde está el jugador (aviso, parpadea),
// 2) tras el aviso dispara un orbe que viaja hasta la marca,
// 3) al llegar explota: daña una sola vez si el jugador sigue dentro del círculo.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class EnemyRangedController : MonoBehaviour
{
    [Header("Detección y distancia")]
    [SerializeField] private float detectionRange = 3.2f;
    [SerializeField] private float preferredMin = 1.3f;
    [SerializeField] private float preferredMax = 2.1f;
    [SerializeField] private float moveSpeed = 0.7f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 2.4f;
    [SerializeField] private float telegraphTime = 0.9f;
    [SerializeField] private float projectileSpeed = 2.6f;
    [SerializeField] private float impactRadius = 0.32f;
    [SerializeField, Min(1)] private int damage = 12;

    [Header("Visual")]
    [SerializeField] private Sprite telegraphSprite;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Color chargeColor = new Color(1f, 0.55f, 0.6f);
    [Tooltip("Altura desde la que sale el orbe (el núcleo del Centinela)")]
    [SerializeField] private float muzzleHeight = 0.22f;

    private Rigidbody2D rb;
    private EnemyHealth health;
    private Transform player;
    private Telegraph marker;
    private GameObject projectile;
    private float nextAttack;
    private bool attacking;
    private float stunTimer;

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
        health.OnDamaged += _ => stunTimer = 0.2f;
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        marker = Telegraph.Create(telegraphSprite, name + "_Telegraph");
        projectile = new GameObject(name + "_Orb");
        var sr = projectile.AddComponent<SpriteRenderer>();
        sr.sprite = projectileSprite;
        sr.sortingLayerName = "Sprites";
        sr.sortingOrder = 50;
        projectile.SetActive(false);
        nextAttack = Time.time + 1f;
    }

    private void OnDisable()
    {
        // Muerto o escena descargada: limpia el aviso y el orbe
        StopAllCoroutines();
        attacking = false;
        if (marker != null) marker.Hide();
        if (projectile != null) projectile.SetActive(false);
        if (body != null) body.color = Color.white;
    }

    private void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
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
        if (attacking || !PlayerAlive() || Time.time < nextAttack) return;
        if (Vector2.Distance(player.position, transform.position) > detectionRange) return;
        StartCoroutine(Attack());
    }

    private IEnumerator Attack()
    {
        attacking = true;
        Vector2 target = player.position;
        marker.ShowCircle(target, impactRadius, telegraphTime + 0.4f, Telegraph.Danger);

        // Carga: el núcleo parpadea mientras el aviso está en el suelo
        for (float t = 0f; t < telegraphTime; t += Time.deltaTime)
        {
            if (body != null) body.color = Mathf.PingPong(t * 8f, 1f) > 0.5f ? chargeColor : Color.white;
            yield return null;
        }
        if (body != null) body.color = Color.white;

        // Disparo: el orbe viaja hasta la marca
        Vector2 from = (Vector2)transform.position + Vector2.up * muzzleHeight;
        projectile.transform.position = from;
        projectile.SetActive(true);
        float travel = Vector2.Distance(from, target) / projectileSpeed;
        for (float t = 0f; t < travel; t += Time.deltaTime)
        {
            projectile.transform.position = Vector2.Lerp(from, target, t / travel);
            yield return null;
        }
        projectile.SetActive(false);
        marker.Hide();

        // Impacto: una sola vez
        if (player != null && CombatUtil.InsideIsoCircle(player.position, target, impactRadius))
            CombatUtil.DamagePlayer(player.gameObject, damage, target);

        nextAttack = Time.time + attackCooldown;
        attacking = false;
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
