using System;
using System.Collections.Generic;
using UnityEngine;

// Capítulo 1: botín de un enemigo (HEX-04).
// - Cada material sale con probabilidad dropChance y en cantidad de 1 a su máximo, para que no
//   siempre suelten todo. Los objetos clave y los enemigos que no reaparecen sí sueltan completo
//   (de ellos depende avanzar en la historia).
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
    [Tooltip("Probabilidad de que salga cada material (los objetos clave siempre salen)")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.45f;

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
        // El primer botín se sortea con una semilla fija por enemigo: al volver a la escena,
        // lo que quedó en el suelo es lo mismo que salió (no se puede volver a tirar el dado).
        var rng = firstLoot ? new System.Random(StableHash(pid.Id)) : new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        for (int i = 0; i < drops.Count; i++)
        {
            var material = drops[i].material;
            if (material == null) continue;
            string dropId = $"{pid.Id}:drop{i}";
            bool guaranteed = !respawns || ItemUsage.IsKeyItem(material.id);
            bool lucky = rng.NextDouble() < dropChance;
            int amount = guaranteed ? drops[i].amount : rng.Next(1, drops[i].amount + 1);
            if (firstLoot ? WorldState.IsCollected(dropId) : ItemUsage.IsKeyItem(material.id)) continue;
            if (!guaranteed && !lucky) continue;

            float angle = i * 2.4f;
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f) * scatter;
            var pickup = Instantiate(pickupPrefab, at + offset, Quaternion.identity);
            if (firstLoot) pickup.gameObject.AddComponent<PersistentId>().SetId(dropId);
            pickup.Setup(material, amount);
        }
    }

    // string.GetHashCode cambia entre ejecuciones; esta no.
    private static int StableHash(string s)
    {
        unchecked
        {
            int h = 23;
            foreach (char c in s) h = h * 31 + c;
            return h;
        }
    }
}
