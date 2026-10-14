using System;
using UnityEngine;

// Se ejecuta antes que Health para que MaxHealth ya esté calculado
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Health))]
public class PlayerStats : MonoBehaviour, IHealthProvider
{
    [Header("Base Stats")]
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private int baseAttack = 10;
    [SerializeField] private int baseDefense = 0;
    [SerializeField] private float baseMoveSpeed = 1.4f;

    public int MaxHealth { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public float MoveSpeed { get; private set; }

    // Puente para scripts que ya usaban PlayerStats para la vida
    public int CurrentHealth => health != null ? health.CurrentHealth : MaxHealth;

    public event Action OnStatsChanged;
    public event Action<int, int> OnHealthChanged; // (current, max)

    private Health health;
    private EquipmentItem weapon;
    private EquipmentItem armor;
    private EquipmentItem accessory;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.HealthChanged += HandleHealthChanged;
        health.Died += HandleDeath;

        RecalculateStats();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= HandleHealthChanged;
            health.Died -= HandleDeath;
        }
    }

    public void EquipItem(EquipmentItem item)
    {
        if (item == null) return;

        switch (item.slot)
        {
            case EquipmentSlot.Weapon: weapon = item; break;
            case EquipmentSlot.Armor: armor = item; break;
            case EquipmentSlot.Accessory: accessory = item; break;
        }
        RecalculateStats();
    }

    public void UnequipSlot(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: weapon = null; break;
            case EquipmentSlot.Armor: armor = null; break;
            case EquipmentSlot.Accessory: accessory = null; break;
        }
        RecalculateStats();
    }

    private void RecalculateStats()
    {
        int bonusHealth = 0, bonusAttack = 0, bonusDefense = 0;
        float bonusSpeed = 0f;

        foreach (var item in new[] { weapon, armor, accessory })
        {
            if (item == null) continue;
            bonusHealth += item.bonusMaxHealth;
            bonusAttack += item.bonusAttack;
            bonusDefense += item.bonusDefense;
            bonusSpeed += item.bonusMoveSpeed;
        }

        int previousMax = MaxHealth;
        MaxHealth = baseMaxHealth + bonusHealth;
        Attack = baseAttack + bonusAttack;
        Defense = baseDefense + bonusDefense;
        MoveSpeed = baseMoveSpeed + bonusSpeed;

        if (MaxHealth != previousMax)
            health.ApplyMaxHealthChange(previousMax);

        OnStatsChanged?.Invoke();
    }

    // Puentes: redirigen a Health
    public void Heal(int amount) => health.Heal(amount);
    public void TakeDamage(int amount) => health.TakeDamage(amount);

    private void HandleHealthChanged(int current, int max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void HandleDeath()
    {
        Debug.Log("El jugador murió.");
    }
}