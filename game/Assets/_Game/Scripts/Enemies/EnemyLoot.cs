using System;
using System.Collections.Generic;
using UnityEngine;

// Capítulo 1: botín garantizado de un enemigo (HEX-04).
// - La primera vez que muere queda marcado como vencido y suelta sus pickups, cada uno con su
//   propio id: si no los recoges, vuelven a aparecer al regresar a la escena.
// - Los enemigos de campo reaparecen cada vez que vuelves a la zona, para seguir juntando
//   materiales. Esas muertes sueltan botín normal (sin id) y nunca un objeto clave.
// - Los de la prueba del Santuario no reaparecen: SanctuaryTrial los marca con KeepDefeated.
[RequireComponent(typeof(EnemyHealth), typeof(PersistentId))]
public class EnemyLoot : MonoBehaviour
{
    [Serializable]
    public struct Drop
    {
        public MaterialData material;
        [Min(1)] public int amount;
    }

    [SerializeField] private MaterialPickup pickupPrefab;
    [SerializeField] private List<Drop> drops = new List<Drop>();
    [SerializeField] private float scatter = 0.12f;

    private EnemyHealth health;
    private PersistentId pid;
    private bool respawns = true;
    private bool respawned;

    public void Setup(MaterialPickup prefab, List<Drop> list)
    {
        pickupPrefab = prefab;
        drops = list;
    }

    // Para enemigos de un evento que solo ocurre una vez (la prueba del Santuario).
    public void KeepDefeated() => respawns = false;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        pid = GetComponent<PersistentId>();
        health.Died += HandleDied;
    }

    private void Start()
    {
        if (!WorldState.IsDefeated(pid.Id)) return;
        SpawnDrops(transform.position, true); // los del primer botín que quedaron en el suelo
        if (respawns) respawned = true;
        else gameObject.SetActive(false);
    }

    private void HandleDied()
    {
        if (respawned)
        {
            SpawnDrops(transform.position, false);
            return;
        }
        WorldState.MarkDefeated(pid.Id);
        SpawnDrops(transform.position, true);
    }

    private void SpawnDrops(Vector3 at, bool firstLoot)
    {
        if (pickupPrefab == null) return;
        for (int i = 0; i < drops.Count; i++)
        {
            var material = drops[i].material;
            if (material == null) continue;
            string dropId = $"{pid.Id}:drop{i}";
            if (firstLoot ? WorldState.IsCollected(dropId) : ItemUsage.IsKeyItem(material.id)) continue;

            float angle = i * 2.4f;
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f) * scatter;
            var pickup = Instantiate(pickupPrefab, at + offset, Quaternion.identity);
            if (firstLoot) pickup.gameObject.AddComponent<PersistentId>().SetId(dropId);
            pickup.Setup(material, drops[i].amount);
        }
    }
}
