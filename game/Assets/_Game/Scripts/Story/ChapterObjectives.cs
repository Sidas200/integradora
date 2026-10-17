// Objetivo actual del capítulo según los flags de historia. Se muestra en el HUD.
// El orden importa: gana el primer objetivo que todavía no se cumple.
public static class ChapterObjectives
{
    public const string CampBriefing = "camp_briefing";
    public const string SwordUpgraded = "sword_upgraded";
    public const string SealOpened = "seal_opened";
    public const string PermitObtained = "permit_obtained";

    public static string Current()
    {
        if (!StoryFlags.Has(SwordPickup.Flag)) return "Busca algo útil en la cabaña";
        if (!StoryFlags.Has(IslandVanish.Flag)) return "Cruza el puente";
        if (!StoryFlags.Has(ResearcherFallEvent.Flag)) return "Busca a alguien que te explique dónde estás";
        if (!StoryFlags.Has(CampBriefing)) return "Ve al campamento de Kaede (oeste)";
        if (!StoryFlags.Has(SwordUpgraded) && !StoryFlags.Has(SealOpened))
        {
            string text = "Mejora la espada en la forja: " + MaterialProgress();
            if (!StoryFlags.Has(MaidenStatue.Flag)) text += "\n(Opcional: visita el santuario del norte)";
            return text;
        }
        if (!StoryFlags.Has(SealOpened)) return "Rompe el sello del Bosque Quebrado (este)";
        if (!StoryFlags.Has(PermitObtained)) return "Vence al guardián";
        return "Obtuviste el permiso del guardián";
    }

    // Lo completa la fase de materiales; por ahora muestra los nombres.
    public static System.Func<string> MaterialProgressProvider;

    private static string MaterialProgress() =>
        MaterialProgressProvider != null ? MaterialProgressProvider() : "Cuero antiguo y Flor destello";
}
