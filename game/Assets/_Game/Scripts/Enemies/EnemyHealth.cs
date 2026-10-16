
using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Vida del enemigo")]
    [SerializeField, Min(1)] private int maxHealth = 100;



    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    // GAME-101: Evento para efectos de golpe
    public event Action<int> OnDamaged;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    // Recibir daño
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        int actualDamage = Mathf.Min(damage, CurrentHealth);

        CurrentHealth = Mathf.Max(
            CurrentHealth - damage,
            0
        );

        Debug.Log(
            $"Enemy Melee: recibió {actualDamage} de daño. " +
            $"Vida: {CurrentHealth}/{maxHealth}"
        );

        // GAME-101: Avisar que recibió un golpe
        OnDamaged?.Invoke(actualDamage);

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

        Debug.Log("Enemy Melee: ha muerto");

        gameObject.SetActive(false);
    }

    [ContextMenu("TEST - Recibir 25 de daño")]
    private void TestDamage()
    {
        TakeDamage(25);
    }
}
