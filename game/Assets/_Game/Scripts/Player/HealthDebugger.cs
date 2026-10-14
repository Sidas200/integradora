using UnityEngine;
using UnityEngine.InputSystem;

// Script SOLO de prueba: quítalo cuando ya no lo necesites
[RequireComponent(typeof(Health))]
public class HealthDebugger : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private int healAmount = 10;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.HealthChanged -= OnHealthChanged;
        health.Died -= OnDied;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
            health.TakeDamage(damageAmount);

        if (Keyboard.current.hKey.wasPressedThisFrame)
            health.Heal(healAmount);

        if (Keyboard.current.lKey.wasPressedThisFrame)
            health.Kill();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            health.ResetHealth();
    }

    private void OnHealthChanged(int current, int max)
    {
        Debug.Log($"[{name}] Vida: {current}/{max}");
    }

    private void OnDied()
    {
        Debug.Log($"[{name}] MURIÓ");
    }

    // Texto en pantalla para ver la vida en tiempo real
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 25),
            $"{name}  Vida: {health.CurrentHealth}/{health.MaxHealth}  Def: {health.Defense}");
        GUI.Label(new Rect(10, 30, 400, 25),
            "K = daño   H = curar   L = matar   R = revivir");
    }
}