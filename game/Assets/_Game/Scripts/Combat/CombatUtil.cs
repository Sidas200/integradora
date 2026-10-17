using UnityEngine;

// Utilidades compartidas por enemigos y jefe.
public static class CombatUtil
{
    // Daño al jugador usando Health (respeta su invulnerabilidad) y su reacción de golpe.
    public static bool DamagePlayer(GameObject player, int damage, Vector2 from)
    {
        if (player == null) return false;
        var health = player.GetComponentInParent<Health>();
        if (health == null || health.IsDead) return false;

        int before = health.CurrentHealth;
        health.TakeDamage(damage);
        bool hit = health.CurrentHealth < before;
        if (hit && !health.IsDead)
        {
            var controller = player.GetComponentInParent<PlayerController>();
            if (controller != null) controller.ApplyHitReaction(from);
        }
        return hit;
    }

    // Prueba de pertenencia a una elipse isométrica (ancho = 2 * alto), como se dibujan los avisos.
    public static bool InsideIsoCircle(Vector2 point, Vector2 center, float radius)
    {
        var d = point - center;
        float x = d.x / radius;
        float y = d.y / (radius * 0.5f);
        return x * x + y * y <= 1f;
    }
}
