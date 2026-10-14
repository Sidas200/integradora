using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [Tooltip("Se usa solo si el objeto NO tiene un componente de stats (IHealthProvider)")]
    [SerializeField] private int maxHealth = 100;
    [Tooltip("Daño mínimo que siempre se recibe, aunque la defensa sea alta")]
    [SerializeField] private int minDamage = 1;
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Invulnerabilidad tras recibir daño")]
    [SerializeField] private float invulnerableTime = 0f;

    [Header("Eventos (para conectar UI, sonidos, etc.)")]
    public UnityEvent<int, int> onHealthChanged; // (vidaActual, vidaMax)
    public UnityEvent<int> onDamaged;             // daño recibido
    public UnityEvent<int> onHealed;              // vida curada
    public UnityEvent onDeath;

    // Eventos C# para usarlos desde otros scripts
    public event Action<int, int> HealthChanged;
    public event Action Died;

    private IHealthProvider provider;
    private int currentHealth;
    private float invulnerableTimer;
    private bool initialized;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => provider != null ? provider.MaxHealth : maxHealth;
    public int Defense => provider != null ? provider.Defense : 0;
    public float Normalized => MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => invulnerableTimer > 0f;

    private void Awake()
    {
        provider = GetComponent<IHealthProvider>();
        currentHealth = MaxHealth;
        initialized = true;
    }

    private void Start()
    {
        NotifyChanged();
    }

    private void Update()
    {
        if (invulnerableTimer > 0f)
            invulnerableTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || IsInvulnerable || amount <= 0)
            return;

        int damage = Mathf.Max(amount - Defense, minDamage);
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        invulnerableTimer = invulnerableTime;

        onDamaged?.Invoke(damage);
        NotifyChanged();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);

        onHealed?.Invoke(currentHealth - before);
        NotifyChanged();
    }

    public void Kill()
    {
        if (IsDead)
            return;

        currentHealth = 0;
        NotifyChanged();
        Die();
    }

    // Revivir / reiniciar con la vida al máximo
    public void ResetHealth()
    {
        IsDead = false;
        currentHealth = MaxHealth;
        NotifyChanged();
    }

    // Cuando cambia la vida máxima (equipo), suma o resta la diferencia a la vida actual
    public void ApplyMaxHealthChange(int previousMax)
    {
        if (!initialized || IsDead)
            return;

        int diff = MaxHealth - previousMax;
        currentHealth = Mathf.Clamp(currentHealth + diff, 1, MaxHealth);
        NotifyChanged();
    }

    private void Die()
    {
        IsDead = true;
        onDeath?.Invoke();
        Died?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    private void NotifyChanged()
    {
        onHealthChanged?.Invoke(currentHealth, MaxHealth);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }
}