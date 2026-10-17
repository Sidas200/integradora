using System;
using System.Collections.Generic;

// GAME-102 / SCRUM-84 (102.1): DTOs del guardado. Cada uno corresponde a Supabase:
//   MatchData      -> partidas (slot, personaje_activo, capitulo_actual, vida)
//   CheckpointData -> partidas (escena_actual, pos_x, pos_y)
//   ProgressData   -> partidas.estado_extra (jsonb)
//   InventoryEntry -> inventario (item_id, cantidad)
// Listas, no diccionarios (JsonUtility no serializa Dictionary).
[Serializable]
public class SaveData
{
    // v2: formato con sub-DTOs. Un guardado v1 (formato plano) se ignora.
    public const int CurrentVersion = 2;

    public int version = CurrentVersion;
    public string savedAtUtc = "";

    public MatchData match = new MatchData();
    public CheckpointData checkpoint = new CheckpointData();
    public ProgressData progress = new ProgressData();
    public List<InventoryEntry> inventory = new List<InventoryEntry>();

    public bool IsValid =>
        version == CurrentVersion &&
        match != null && checkpoint != null && progress != null && inventory != null &&
        match.slot >= 1 && match.slot <= 3 &&
        progress.swordLevel >= 0;

    // Toma la "foto" del estado actual de la sesión.
    public static SaveData FromSession(SessionState session, int slot = 1)
    {
        var data = new SaveData { savedAtUtc = DateTime.UtcNow.ToString("o") };

        data.match.slot = slot;
        data.match.health = session.currentHealth;

        data.checkpoint.scene = session.safePointScene;
        data.checkpoint.posX = session.safePointX;
        data.checkpoint.posY = session.safePointY;

        data.progress.safePointId = session.safePointId;
        data.progress.sealOpened = session.sealOpened;
        data.progress.guardianPermit = session.guardianPermission;
        data.progress.swordLevel = session.swordLevel;
        data.progress.flags = new List<string>(session.flags);
        data.progress.collected = new List<string>(session.collected);
        data.progress.defeated = new List<string>(session.defeated);
        data.progress.equipped = new List<string>(session.equipped);

        // inventario.cantidad debe ser > 0 en la base de datos.
        foreach (var m in session.materials)
        {
            if (m.amount > 0)
                data.inventory.Add(new InventoryEntry { materialId = m.id, amount = m.amount });
        }

        return data;
    }

    // Carga la "foto" de vuelta a la sesión.
    public void ApplyTo(SessionState session)
    {
        session.currentHealth = match.health;

        session.safePointScene = checkpoint.scene;
        session.safePointX = checkpoint.posX;
        session.safePointY = checkpoint.posY;

        session.safePointId = progress.safePointId;
        session.sealOpened = progress.sealOpened;
        session.guardianPermission = progress.guardianPermit;
        session.swordLevel = progress.swordLevel;
        session.flags.Clear();
        if (progress.flags != null) session.flags.AddRange(progress.flags);
        session.collected.Clear();
        if (progress.collected != null) session.collected.AddRange(progress.collected);
        session.defeated.Clear();
        if (progress.defeated != null) session.defeated.AddRange(progress.defeated);
        session.equipped.Clear();
        if (progress.equipped != null) session.equipped.AddRange(progress.equipped);

        session.materials.Clear();
        foreach (var e in inventory)
            session.materials.Add(new MaterialEntry { id = e.materialId, amount = e.amount });
    }
}

// partidas: slot, personaje_activo, capitulo_actual, vida
[Serializable]
public class MatchData
{
    public int slot = 1;                      // 1–3
    public string activeCharacter = "Tsuki";  // Tsuki | Hikari
    public int currentChapter = 1;
    public int health = -1;                   // -1 = sin dato (vida máxima)
}

// partidas: escena_actual, pos_x, pos_y
[Serializable]
public class CheckpointData
{
    public string scene = "";
    public float posX;
    public float posY;
}

// partidas.estado_extra (jsonb)
[Serializable]
public class ProgressData
{
    public string safePointId = "";   // "base" | "boss"
    public bool sealOpened;
    public bool guardianPermit;
    public int swordLevel;
    // Capítulo 1: eventos de historia. Campo nuevo: un guardado v2 sin él carga una lista vacía.
    public List<string> flags = new List<string>();
    // Capítulo 1: fuentes de recompensa ya usadas (HEX-04 / HEX-08: sin duplicados al cargar)
    public List<string> collected = new List<string>();
    public List<string> defeated = new List<string>();
    // Capítulo 1: piezas equipadas (ids de inventario). Las piezas siguen contando en el inventario.
    public List<string> equipped = new List<string>();
}

// inventario: item_id, cantidad
[Serializable]
public class InventoryEntry
{
    public string materialId;
    public int amount;
}