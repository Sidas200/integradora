using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Usa items consumibles del inventario: Poción (Q) y Gota de Amrita (G).
// Va en el mismo objeto que el componente Health del jugador.
public class ItemUser : MonoBehaviour
{
    [Header("Poción (tecla Q)")]
    [SerializeField] private MaterialData potion;
    [SerializeField] private int potionHeal = 30;

    [Header("Gota de Amrita (tecla G)")]
    [SerializeField] private MaterialData amritaDrop;
    public UnityEvent onAmritaUsed;

    // Para que el jefe u otros scripts se suscriban desde código
    public event Action AmritaUsed;

    private Health health;
    private Inventory inventory;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInChildren<Health>();
        if (health == null)
            Debug.LogWarning("ItemUser: no se encontró un componente Health.");
    }

    private void Update()
    {
        // Con el juego en pausa (panel de crafteo, menú) no se usan items
        if (Time.timeScale == 0f) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame) UsePotion();
        if (keyboard.gKey.wasPressedThisFrame) UseAmrita();
    }

    private Inventory GetInventory()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        return inventory;
    }

    public bool UsePotion()
    {
        if (potion == null || health == null || health.IsDead) return false;

        if (health.CurrentHealth >= health.MaxHealth)
        {
            Debug.Log("Vida llena: no se gasta la Poción.");
            return false;
        }

        var inv = GetInventory();
        if (inv == null || !inv.TryConsume(potion.id, 1))
        {
            Debug.Log("No tienes Pociones.");
            return false;
        }

        health.Heal(potionHeal);
        Debug.Log("Poción usada: +" + potionHeal + " de vida.");
        return true;
    }

    public bool UseAmrita()
    {
        if (amritaDrop == null || health == null || health.IsDead) return false;

        var inv = GetInventory();
        if (inv == null || !inv.TryConsume(amritaDrop.id, 1))
        {
            Debug.Log("No tienes Gotas de Amrita.");
            return false;
        }

        Debug.Log("Gota de Amrita usada: el jefe despierta.");
        AmritaUsed?.Invoke();
        onAmritaUsed?.Invoke();
        return true;
    }
}