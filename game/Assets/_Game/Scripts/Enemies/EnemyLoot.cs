using System;
using System.Collections.Generic;
using UnityEngine;

// Capítulo 1: botín garantizado y persistente de un enemigo (HEX-04).
// - Al morir queda marcado como vencido y suelta sus pickups, cada uno con su propio id.
// - Al volver a la escena: si ya estaba vencido no reaparece, y solo vuelven los drops
//   que el jugador no alcanzó a recoger. Así cada fuente se recoge una sola vez.
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

    public void Setup(MaterialPickup prefab, List<Drop> list)
    {
        pickupPrefab = prefab;
        drops = list;
    }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        pid = GetComponent<PersistentId>();
        health.Died += HandleDied;
    }

    private void Start()
    {
        if (!WorldState.IsDefeated(pid.Id)) return;
        SpawnDrops(transform.position);
        gameObject.SetActive(false);
    }

    private void HandleDied()
    {
        WorldState.MarkDefeated(pid.Id);
        SpawnDrops(transform.position);
    }

    private void SpawnDrops(Vector3 at)
    {
        if (pickupPrefab == null) return;
        for (int i = 0; i < drops.Count; i++)
        {
            string dropId = $"{pid.Id}:drop{i}";
            if (drops[i].material == null || WorldState.IsCollected(dropId)) continue;

            float angle = i * 2.4f;
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f) * scatter;
            var pickup = Instantiate(pickupPrefab, at + offset, Quaternion.identity);
            pickup.gameObject.AddComponent<PersistentId>().SetId(dropId);
            pickup.Setup(drops[i].material, drops[i].amount);
        }
    }
}
