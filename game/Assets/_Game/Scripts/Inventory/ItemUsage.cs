// Capítulo 1: para qué sirve cada item (se muestra en el inventario y en la forja).
// Los ids son los estables del inventario (ver docs del equipo).
public static class ItemUsage
{
    public static string Hint(string id)
    {
        switch (id)
        {
            case "pocion": return "Úsala con Q o con el botón Usar: recupera 30 de vida.";
            case "gota_amrita": return "Ofrécela a un guardián dormido para despertarlo (E frente a él, o G).";
            case "alma_pequena": return "Ofrécela en la puerta del primer Santuario para abrirla.";
            case "casco_cuero":
            case "pechera_cuero":
            case "botas_cuero": return "Equípala en la pestaña Equipo para subir tu defensa.";
            case "flor_destello":
            case "concentrado_slime":
            case "resto_slime":
            case "cuero_antiguo":
            case "alma_antigua":
            case "circuito_antiguo": return "Material de forja. Llévalo a la forja del campamento.";
            default: return "";
        }
    }

    public static bool IsEquipment(string id) => id == "casco_cuero" || id == "pechera_cuero" || id == "botas_cuero";
    public static bool IsUsable(string id) => id == "pocion" || id == "gota_amrita";
}
