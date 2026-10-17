using System.Collections.Generic;

// Bloqueo del input del jugador mientras hay algo encima (pausa, diálogo, forja, transición).
// Cada sistema bloquea con su propia "llave" y la libera al cerrarse; el jugador solo
// se mueve cuando no queda ninguna.
public static class GameInput
{
    private static readonly HashSet<object> owners = new HashSet<object>();

    public static bool Locked => owners.Count > 0;

    // Frame en que un panel usó Esc para cerrarse: la pausa no debe abrirse en ese mismo frame.
    public static int EscapeConsumedFrame { get; private set; } = -1;

    public static void ConsumeEscape() => EscapeConsumedFrame = UnityEngine.Time.frameCount;

    public static void Lock(object owner)
    {
        if (owner != null) owners.Add(owner);
    }

    public static void Unlock(object owner)
    {
        if (owner != null) owners.Remove(owner);
    }

    public static bool IsLockedBy(object owner) => owner != null && owners.Contains(owner);

    // Al cambiar de escena se descartan los bloqueos de objetos que ya no existen.
    public static void ClearDestroyed()
    {
        owners.RemoveWhere(o => o == null || (o is UnityEngine.Object u && u == null));
    }

    public static void ClearAll() => owners.Clear();
}
