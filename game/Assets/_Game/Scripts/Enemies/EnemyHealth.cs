
using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Vida del enemigo")]
    [SerializeField, Min(1)] private int maxHealth = 100;

    [Header("Invulnerabilidad breve tras recibir daño (0 = sin)")]
    [SerializeField, Min(0f)] private float invulnerableTime = 0f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    // Capítulo 1: invulnerabilidad controlada desde fuera (p. ej. el guardián mientras ataca).
    public bool Invulnerable { get; set; }

    // GAME-101: Evento para efectos de golpe
    public event Action<int> OnDamaged;
    // (actual, máximo) para barras de vida
    public event Action<int, int> HealthChanged;
    // Golpe que no hizo daño por invulnerabilidad (para mostrar que "rebota")
    public event Action Blocked;
    public event Action Died;

    private float invulnerableTimer;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    private void Update()
    {
        if (invulnerableTimer > 0f)
            invulnerableTimer -= Time.deltaTime;
    }

    // Recibir daño
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        if (Invulnerable || invulnerableTimer > 0f)
        {
            Blocked?.Invoke();
            return;
        }

        int actualDamage = Mathf.Min(damage, CurrentHealth);

        CurrentHealth = Mathf.Max(
            CurrentHealth - damage,
            0
        );
        invulnerableTimer = invulnerableTime;

        Debug.Log(
            $"{name}: recibió {actualDamage} de daño. " +
            $"Vida: {CurrentHealth}/{maxHealth}"
        );

        // GAME-101: Avisar que recibió un golpe
        OnDamaged?.Invoke(actualDamage);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log($"{name}: ha muerto");

        PlayStats.EnemyDefeated(TryGetComponent<PersistentId>(out var pid) ? pid.Id : name);
        Died?.Invoke();
        gameObject.SetActive(false);
    }

    [ContextMenu("TEST - Recibir 25 de daño")]
    private void TestDamage()
    {
        TakeDamage(25);
    }
}
