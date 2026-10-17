using UnityEngine;

// Punto de aparición identificado por texto. Las transiciones indican a qué id llegar.
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    public void SetId(string value) => id = value;

    public static SpawnPoint Find(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return null;
        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include))
            if (sp.id == spawnId) return sp;
        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.06f);
    }
}
