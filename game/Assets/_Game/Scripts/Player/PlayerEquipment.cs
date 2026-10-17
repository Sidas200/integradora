using System;
using System.Collections.Generic;
using UnityEngine;

// Capítulo 1: armadura de Tsuki (casco, pechera y botas de cuero). Las piezas son items del
// inventario con su id estable (casco_cuero, ...); equiparlas no las saca del inventario.
// Lo equipado se guarda por id en ProgressData.equipped y sube la defensa vía PlayerStats.
// Corre después de SessionSync para que el inventario ya esté restaurado.
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(PlayerStats), typeof(Inventory))]
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private List<EquipmentItem> catalog = new List<EquipmentItem>();

    public event Action Changed;

    private PlayerStats stats;
    private Inventory inventory;

    public IReadOnlyList<EquipmentItem> Catalog => catalog;

    public void Setup(List<EquipmentItem> items) => catalog = items;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        var session = SessionState.Instance;
        if (session == null) return;
        foreach (var id in session.equipped) Equip(id, false);
        session.BeforeSceneChange += CopyToSession;
    }

    private void OnDestroy()
    {
        if (SessionState.Instance != null) SessionState.Instance.BeforeSceneChange -= CopyToSession;
    }

    public EquipmentItem ForItem(string itemId) =>
        catalog.Find(e => e != null && e.item != null && e.item.id == itemId);

    public EquipmentItem Equipped(EquipmentSlot slot) => stats.GetEquipped(slot);

    public bool IsEquipped(string itemId)
    {
        var piece = ForItem(itemId);
        return piece != null && stats.GetEquipped(piece.slot) == piece;
    }

    public bool Equip(string itemId) => Equip(itemId, true);

    private bool Equip(string itemId, bool notify)
    {
        var piece = ForItem(itemId);
        if (piece == null || inventory.GetAmount(itemId) <= 0) return false;
        stats.EquipItem(piece);
        if (notify) Changed?.Invoke();
        return true;
    }

    public void Unequip(EquipmentSlot slot)
    {
        if (stats.GetEquipped(slot) == null) return;
        stats.UnequipSlot(slot);
        Changed?.Invoke();
    }

    public void CopyToSession()
    {
        var session = SessionState.Instance;
        if (session == null) return;
        session.equipped.Clear();
        foreach (var piece in catalog)
            if (piece != null && piece.item != null && stats.GetEquipped(piece.slot) == piece)
                session.equipped.Add(piece.item.id);
    }
}
