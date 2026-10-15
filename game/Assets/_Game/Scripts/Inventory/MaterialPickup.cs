using UnityEngine;

// GAME-44 / GAME-46: objeto del mapa que el jugador recoge al tocarlo.
// Necesita un Collider2D con "Is Trigger" activado.
[RequireComponent(typeof(Collider2D))]
public class MaterialPickup : MonoBehaviour
{
    [SerializeField] private MaterialData material;
    [SerializeField] private int amount = 1;

    private bool collected;
    private SpriteRenderer spriteRenderer;

    private void Reset()
    {
        // Al agregar el script, deja el collider como trigger automáticamente.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        ApplyVisual();
    }

    // Lo usa EnemyDrop para configurar el pickup al soltarlo.
    public void Setup(MaterialData newMaterial, int newAmount)
    {
        material = newMaterial;
        amount = Mathf.Max(1, newAmount);
        ApplyVisual();
    }

    // Muestra el ícono del material en el suelo (si ya tiene uno asignado).
    private void ApplyVisual()
    {
        if (material == null || material.icon == null) return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = material.icon;
        spriteRenderer.color = Color.white; // quita el tinte azul/verde de prueba
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return; // GAME-46: evita recoger dos veces

        Inventory inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null) return; // solo el jugador (tiene Inventory)

        collected = true;
        inventory.Add(material, amount);
        Debug.Log($"Recogido: {material.displayName} x{amount} (total: {inventory.GetAmount(material.id)})");
        Destroy(gameObject);
    }
}