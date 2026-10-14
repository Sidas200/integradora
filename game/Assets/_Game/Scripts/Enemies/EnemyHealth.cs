
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida del enemigo")]
    [SerializeField, Min(1)] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

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

        CurrentHealth = Mathf.Max(
            CurrentHealth - damage,
            0
        );

        Debug.Log(
            $"Enemy Melee: recibió {damage} de daño. " +
            $"Vida: {CurrentHealth}/{maxHealth}"
        );

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    // Muerte del enemigo
    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("Enemy Melee: ha muerto");

        // Desactivar enemigo y todos sus componentes
        gameObject.SetActive(false);
    }

    // Prueba manual desde el Inspector
    [ContextMenu("TEST - Recibir 25 de daño")]
    private void TestDamage()
    {
        TakeDamage(25);
    }
}
