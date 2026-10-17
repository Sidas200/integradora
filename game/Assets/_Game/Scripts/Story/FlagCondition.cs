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

    public bool IsMet =>
        (string.IsNullOrEmpty(requires) || StoryFlags.Has(requires)) &&
        (string.IsNullOrEmpty(forbids) || !StoryFlags.Has(forbids));
}
