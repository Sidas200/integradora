using UnityEngine;

// GAME-47: al morir el enemigo, suelta materiales según su DropTable.
// Se engancha a OnDisable porque EnemyHealth desactiva el objeto al morir.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyDrop : MonoBehaviour
{
    [Header("Pickup base (cualquier Pickup_*)")]
    [SerializeField] private MaterialPickup pickupPrefab;

    [Header("Tabla de drops de este enemigo")]
    [SerializeField] private DropTable dropTable;

    [Header("Dispersión al soltar")]
    [SerializeField] private float scatterRadius = 0.3f;

    private EnemyHealth health;
    private bool dropped;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    private void OnDisable()
    {
        // Solo suelta si el enemigo murió (no al cerrar la escena).
        if (dropped || health == null || !health.IsDead) return;
        if (dropTable == null || pickupPrefab == null) return;
        dropped = true;

        foreach (var drop in dropTable.drops)
        {
            if (drop.material == null) continue;
            if (Random.value > drop.chance) continue;

            int min = Mathf.Max(1, drop.minAmount);
            int max = Mathf.Max(min, drop.maxAmount);
            int amount = Random.Range(min, max + 1); // el máximo es inclusivo

            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            Vector3 pos = transform.position + (Vector3)offset;

            MaterialPickup pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);
            pickup.Setup(drop.material, amount);
            Debug.Log($"Drop: {drop.material.displayName} x{amount}");
        }
    }
}