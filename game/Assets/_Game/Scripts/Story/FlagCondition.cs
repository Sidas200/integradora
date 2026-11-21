using System;

// Condición simple sobre flags de historia: requiere uno y/o prohíbe otro.
[Serializable]
public struct FlagCondition
{
    public string requires;
    public string forbids;

    public FlagCondition(string requires, string forbids)
    {
        this.requires = requires;
        this.forbids = forbids;
    }

    // Se pueden poner varios separados por coma: "requires" pide todos, "forbids" prohíbe cualquiera.
    public bool IsMet => HasAll(requires) && HasNone(forbids);

    private static bool HasAll(string flags)
    {
        if (string.IsNullOrEmpty(flags)) return true;
        foreach (var f in flags.Split(','))
            if (f.Trim().Length > 0 && !StoryFlags.Has(f.Trim())) return false;
        return true;
    }

    private static bool HasNone(string flags)
    {
        if (string.IsNullOrEmpty(flags)) return true;
        foreach (var f in flags.Split(','))
            if (f.Trim().Length > 0 && StoryFlags.Has(f.Trim())) return false;
        return true;
    }
}
