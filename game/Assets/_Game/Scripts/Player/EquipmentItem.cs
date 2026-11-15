using UnityEngine;

[CreateAssetMenu(menuName = "Equipment/Equipment Item")]
public class EquipmentItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public EquipmentSlot slot;
    [Tooltip("Item del inventario que representa esta pieza (p. ej. casco_cuero). Se equipa desde el inventario.")]
    public MaterialData item;

    [Header("Bonuses")]
    public int bonusAttack;
    public int bonusDefense;
    public int bonusMaxHealth;
    public float bonusMoveSpeed;
}