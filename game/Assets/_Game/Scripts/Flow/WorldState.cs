// Qué fuentes del mundo ya se usaron (pickups recogidos, enemigos vencidos).
// Vive en SessionState y se guarda con la partida, así una recompensa nunca se da dos veces.
public static class WorldState
{
    public static bool IsCollected(string id) => Has(id, true);
    public static bool IsDefeated(string id) => Has(id, false);

    public static void MarkCollected(string id) => Add(id, true);
    public static void MarkDefeated(string id) => Add(id, false);

    private static bool Has(string id, bool collected)
    {
        var s = SessionState.Instance;
        if (s == null || string.IsNullOrEmpty(id)) return false;
        return (collected ? s.collected : s.defeated).Contains(id);
    }

    private static void Add(string id, bool collected)
    {
        var s = SessionState.Instance;
        if (s == null || string.IsNullOrEmpty(id)) return;
        var list = collected ? s.collected : s.defeated;
        if (!list.Contains(id)) list.Add(id);
    }
}
