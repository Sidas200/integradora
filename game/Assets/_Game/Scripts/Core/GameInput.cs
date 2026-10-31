using System.Collections.Generic;

// Bloqueo del input del jugador mientras hay algo encima (pausa, diálogo, forja, transición).
// Cada sistema bloquea con su propia "llave" y la libera al cerrarse; el jugador solo
// se mueve cuando no queda ninguna.
public static class GameInput
{
    private static readonly HashSet<object> owners = new HashSet<object>();

    public static bool Locked => owners.Count > 0;

    // Frame en que se liberó el último bloqueo. El botón que cerró un panel (A en un diálogo,
    // Start al omitirlo, E en la forja) no debe disparar también una acción del jugador o
    // abrir otro menú en ese mismo frame.
    public static int UnlockedFrame { get; private set; } = -1;

    // Bloqueado, o recién desbloqueado en este frame: lo usan el jugador y los menús que se abren.
    public static bool GameplayBlocked => Locked || UnlockedFrame == UnityEngine.Time.frameCount;

    // Frame en que un panel usó Esc para cerrarse: la pausa no debe abrirse en ese mismo frame.
    public static int EscapeConsumedFrame { get; private set; } = -1;

    public static void ConsumeEscape() => EscapeConsumedFrame = UnityEngine.Time.frameCount;

    public static void Lock(object owner)
    {
        if (owner != null) owners.Add(owner);
    }

    public static void Unlock(object owner)
    {
        if (owner != null && owners.Remove(owner) && owners.Count == 0)
            UnlockedFrame = UnityEngine.Time.frameCount;
    }

    public static bool IsLockedBy(object owner) => owner != null && owners.Contains(owner);

    // Al cambiar de escena se descartan los bloqueos de objetos que ya no existen.
    public static void ClearDestroyed()
    {
        if (owners.RemoveWhere(o => o == null || (o is UnityEngine.Object u && u == null)) > 0 && owners.Count == 0)
            UnlockedFrame = UnityEngine.Time.frameCount;
    }

    public static void ClearAll()
    {
        owners.Clear();
        UnlockedFrame = -1;
    }
}
