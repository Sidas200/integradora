using UnityEngine;

// Correa de los enemigos comunes: recuerdan dónde aparecieron y, si pierden a Tsuki (se alejó,
// entró a una zona segura o el enemigo se apartó demasiado de su lugar), vuelven caminando.
// Mientras regresan la ignoran, así no se le pegan de nuevo al salir del campamento.
public class EnemyLeash
{
    public const float LeashRange = 2.5f;   // distancia máxima a su lugar antes de rendirse
    private const float ArriveDistance = 0.06f;
    private const float GiveUpTime = 6f;     // atorado en una pared: se queda donde está

    private Vector2 home;
    private float returnTimer;

    public bool Returning { get; private set; }

    public EnemyLeash(Vector2 homePosition) => home = homePosition;

    public void Reset() => Returning = false;

    // ¿Puede perseguir a Tsuki? Si no, empieza a volver a su lugar.
    public bool Track(bool seesPlayer, Vector2 self, Vector2 player)
    {
        if (Returning) return false;
        if (seesPlayer && !SafeZone.Contains(player) && Vector2.Distance(self, home) <= LeashRange) return true;
        if (Vector2.Distance(self, home) > ArriveDistance)
        {
            Returning = true;
            returnTimer = GiveUpTime;
        }
        return false;
    }

    // Velocidad para volver a su lugar (cero si ya llegó o no está regresando)
    public Vector2 ReturnVelocity(Vector2 self, float speed, float dt)
    {
        if (!Returning) return Vector2.zero;
        returnTimer -= dt;
        Vector2 toHome = home - self;
        if (toHome.magnitude <= ArriveDistance || returnTimer <= 0f)
        {
            if (returnTimer <= 0f) home = self;
            Returning = false;
            return Vector2.zero;
        }
        return toHome.normalized * speed;
    }
}
