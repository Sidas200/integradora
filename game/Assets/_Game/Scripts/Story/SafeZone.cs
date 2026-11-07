using System.Collections.Generic;
using UnityEngine;

// Zona segura (el campamento de Kaede): mientras Tsuki está dentro, los enemigos pierden su
// rastro y vuelven a su lugar (EnemyLeash). Área en elipse isométrica, como los avisos del suelo.
public class SafeZone : MonoBehaviour
{
    private static readonly List<SafeZone> zones = new List<SafeZone>();

    [Tooltip("Radio horizontal de la elipse (el vertical es la mitad).")]
    [SerializeField, Min(0.1f)] private float radius = 1.7f;

    public void Setup(float zoneRadius) => radius = zoneRadius;

    public static bool Contains(Vector2 point)
    {
        foreach (var z in zones)
            if (CombatUtil.InsideIsoCircle(point, z.transform.position, z.radius)) return true;
        return false;
    }

    private void OnEnable() => zones.Add(this);

    private void OnDisable() => zones.Remove(this);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.8f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1f, 0.5f, 1f));
        Gizmos.DrawWireSphere(Vector3.zero, radius);
    }
}
