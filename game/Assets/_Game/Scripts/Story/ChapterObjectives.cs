// Objetivo actual del capítulo según los flags de historia y el inventario. Se muestra en el HUD.
// El orden importa: gana el primer objetivo que todavía no se cumple.
public static class ChapterObjectives
{
    public const string CampBriefing = "camp_briefing";
    public const string SanctuaryUnlocked = "sanctuary_unlocked";
    public const string SwordUpgraded = "sword_upgraded";
    public const string SealOpened = "seal_opened";
    public const string PermitObtained = "permit_obtained";

    // Recetas del equipo (Receta_ModuloEspada y Receta_GotaAmrita)
    private const int ModuleSouls = 10, ModuleCircuits = 5;
    private const int AmritaFlowers = 5, AmritaSouls = 3;

    // Cantidad en el inventario del jugador; la asigna el HUD.
    public static System.Func<string, int> AmountProvider;

    public static string Current()
    {
        if (!StoryFlags.Has(SwordPickup.Flag)) return "Busca algo útil en la cabaña";
        if (!StoryFlags.Has(IslandVanish.Flag) && !StoryFlags.Has(CombatTutorial.DoneFlag))
            return "Entrena con el muñeco de práctica (en la explanada de abajo)";
        if (!StoryFlags.Has(IslandVanish.Flag)) return "Cruza el puente";
        if (!StoryFlags.Has(ResearcherFallEvent.Flag)) return "Busca a alguien que te explique dónde estás";
        if (!StoryFlags.Has(CampBriefing)) return "Ve al campamento de Kaede (oeste)";
        if (!StoryFlags.Has(SanctuaryUnlocked))
            return Amount("alma_pequena") > 0
                ? "Ofrece el Alma pequeña en la puerta del Santuario (norte)"
                : "Consigue un Alma pequeña: la guardan los Custodios de ruinas";
        if (!StoryFlags.Has(MaidenStatue.Flag)) return "Supera la prueba de la espada en el Santuario";
        if (!StoryFlags.Has(SwordUpgraded) && !StoryFlags.Has(SealOpened))
            return "Forja el Módulo de espada: " + Progress(("alma_antigua", "Alma antigua", ModuleSouls), ("circuito_antiguo", "Circuito antiguo", ModuleCircuits));
        if (!StoryFlags.Has(SealOpened)) return "Rompe el sello del Bosque Quebrado (este)";
        if (!StoryFlags.Has(GuardianArena.AwakenedFlag) && Amount("gota_amrita") <= 0)
            return "Forja una Gota de Amrita para despertar a Varn: " + Progress(("flor_destello", "Flor destello", AmritaFlowers), ("alma_antigua", "Alma antigua", AmritaSouls));
        if (!StoryFlags.Has(GuardianArena.AwakenedFlag)) return "Despierta a Varn con la Gota de Amrita (más allá del sello)";
        if (!StoryFlags.Has(PermitObtained)) return "Vence al guardián";
        return "Obtuviste el permiso del guardián";
    }

    // A dónde ir para el objetivo actual: escena y objeto que marca el minimapa (Minimap).
    // Mismo orden que Current(). Sin destino (null) cuando hay que juntar materiales por la zona.
    public static (string scene, string target) Where()
    {
        if (!StoryFlags.Has(SwordPickup.Flag)) return (FlowerIsland, "SwordRack");
        if (!StoryFlags.Has(IslandVanish.Flag) && !StoryFlags.Has(CombatTutorial.DoneFlag)) return (FlowerIsland, "Tutorial_Spawn");
        if (!StoryFlags.Has(IslandVanish.Flag)) return (FlowerIsland, "Exit_Bridge");
        if (!StoryFlags.Has(ResearcherFallEvent.Flag)) return (OpenField, "Researcher_Land");
        if (!StoryFlags.Has(CampBriefing)) return (OpenField, "NPC_Researcher");
        if (!StoryFlags.Has(SanctuaryUnlocked))
            return Amount("alma_pequena") > 0 ? (OpenField, "Sanctuary_Door") : (OpenField, "Enemy_Melee_Soul_A");
        if (!StoryFlags.Has(MaidenStatue.Flag)) return (Sanctuary, "Maiden_Statue");
        if (!StoryFlags.Has(SwordUpgraded) && !StoryFlags.Has(SealOpened))
            return Ready(("alma_antigua", ModuleSouls), ("circuito_antiguo", ModuleCircuits)) ? (OpenField, "Forge_Front") : (null, null);
        if (!StoryFlags.Has(SealOpened)) return (CombatZone, "Seal_Front");
        if (!StoryFlags.Has(GuardianArena.AwakenedFlag) && Amount("gota_amrita") <= 0)
            return Ready(("flor_destello", AmritaFlowers), ("alma_antigua", AmritaSouls)) ? (OpenField, "Forge_Front") : (null, null);
        if (!StoryFlags.Has(GuardianArena.AwakenedFlag)) return (GuardianArenaScene, "Guardian");
        // Durante la pelea no se señala nada: el jefe ya está a la vista
        if (!StoryFlags.Has(PermitObtained)) return (null, null);
        return (GuardianArenaScene, "Exit_Victory");
    }

    // Escenas del capítulo (ChapterScenes)
    public const string FlowerIsland = "01_FlowerIsland";
    public const string OpenField = "02_OpenField";
    public const string Sanctuary = "04_Sanctuary";
    public const string CombatZone = "05_CombatZone";
    public const string GuardianArenaScene = "06_GuardianArena";

    private static bool Ready(params (string id, int goal)[] needs)
    {
        foreach (var n in needs)
            if (Amount(n.id) < n.goal) return false;
        return true;
    }

    private static int Amount(string id) => AmountProvider != null ? AmountProvider(id) : 0;

    // "Alma antigua 4/10 · Circuito antiguo 2/5" y, si ya alcanza, el aviso de volver a la forja.
    private static string Progress(params (string id, string name, int goal)[] needs)
    {
        var parts = new System.Collections.Generic.List<string>();
        bool ready = true;
        foreach (var n in needs)
        {
            int have = Amount(n.id);
            if (have < n.goal) ready = false;
            parts.Add($"{n.name} {System.Math.Min(have, n.goal)}/{n.goal}");
        }
        string text = string.Join(" · ", parts);
        return ready ? text + "\n¡Listo! Vuelve a la forja del campamento" : text;
    }
}
