using UnityEngine;

// Enemigos comunes que el 2do golpe del combo de Tsuki aturde un instante y empuja hacia atrás.
// El jefe no lo implementa.
public interface IStaggerable
{
    void Stagger(Vector2 from);
}
