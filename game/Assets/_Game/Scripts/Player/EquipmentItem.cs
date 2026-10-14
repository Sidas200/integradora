using UnityEngine;

[CreateAssetMenu(menuName = "Equipment/Equipment Item")]
public class EquipmentItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public EquipmentSlot slot;

    [Header("Bonuses")]
    public int bonusAttack;
    public int bonusDefense;
    public int bonusMaxHealth;
    public float bonusMoveSpeed;
}