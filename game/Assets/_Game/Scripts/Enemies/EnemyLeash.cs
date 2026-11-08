using UnityEngine;

// Correa de los enemigos comunes: recuerdan dónde aparecieron y, si pierden a Tsuki (se alejó,
// entró a una zona segura o el enemigo se apartó demasiado de su lugar), vuelven caminando.
// Mientras regresan solo vuelven a enfocarla si se les acerca mucho o si los golpea
// (nunca mientras ella está en la zona segura).
public class EnemyLeash
{
    public const float LeashRange = 2.5f;   // distancia máxima a su lugar antes de rendirse
    private const float ArriveDistance = 0.06f;
    private const float GiveUpTime = 6f;     // atorado en una pared: se queda donde está
    private const float CloseRange = 1f;     // tan cerca que la vuelve a ver aunque esté regresando
    private const float ProvokeTime = 4f;    // tras un golpe la persigue sin importar la correa

    private Vector2 home;
    private float returnTimer;
    private float provokedUntil = -1f;

    public bool Returning { get; private set; }

    public EnemyLeash(Vector2 homePosition) => home = homePosition;

    public void Reset()
    {
        Returning = false;
        provokedUntil = -1f;
    }

    // Lo golpearon: deja de regresar y la persigue un rato
    public void Provoke()
    {
        Returning = false;
        provokedUntil = Time.time + ProvokeTime;
    }

    // ¿Puede perseguir a Tsuki? Si no, empieza a volver a su lugar.
    public bool Track(bool seesPlayer, Vector2 self, Vector2 player)
    {
        if (SafeZone.Contains(player)) { StartReturn(self); return false; }

        bool close = Vector2.Distance(self, player) <= CloseRange;
        bool provoked = Time.time < provokedUntil;
        if (close || (provoked && seesPlayer))
        {
            Returning = false;
            return true;
        }

        if (Returning) return false;
        if (seesPlayer && Vector2.Distance(self, home) <= LeashRange) return true;
        StartReturn(self);
        return false;
    }

    private void StartReturn(Vector2 self)
    {
        if (Returning || Vector2.Distance(self, home) <= ArriveDistance) return;
        Returning = true;
        returnTimer = GiveUpTime;
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
