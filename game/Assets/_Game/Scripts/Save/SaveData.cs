using System;
using System.Collections.Generic;

// GAME-102 / SCRUM-84 (102.1): datos que se escriben al archivo de guardado (JSON).
// Clase C# simple (no MonoBehaviour): JsonUtility la convierte limpia y se prueba en EditMode.
[Serializable]
public class SaveData
{
    // Si cambia el formato, se sube este número y los guardados viejos se detectan.
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string savedAtUtc = "";

    // Jugador
    public int currentHealth = -1;
    public int swordLevel;
    public List<MaterialEntry> materials = new List<MaterialEntry>();

    // Punto seguro (HEX-08)
    public string safePointId = "";
    public string safePointScene = "";
    public float safePointX;
    public float safePointY;

    // Progreso (HEX-06)
    public bool sealOpened;
    public bool guardianPermission;

    // Un guardado es usable si es de esta versión y tiene datos coherentes.
    public bool IsValid =>
        version == CurrentVersion &&
        swordLevel >= 0 &&
        materials != null;

    // Toma la "foto" del estado actual de la sesión.
    public static SaveData FromSession(SessionState session)
    {
        var data = new SaveData
        {
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            currentHealth = session.currentHealth,
            swordLevel = session.swordLevel,
            safePointId = session.safePointId,
            safePointScene = session.safePointScene,
            safePointX = session.safePointX,
            safePointY = session.safePointY,
            sealOpened = session.sealOpened,
            guardianPermission = session.guardianPermission
        };

        // Copia cada entrada para que el guardado no cambie si el inventario cambia después.
        foreach (var m in session.materials)
            data.materials.Add(new MaterialEntry { id = m.id, amount = m.amount });

        return data;
    }

    // Carga la "foto" de vuelta a la sesión.
    public void ApplyTo(SessionState session)
    {
        session.currentHealth = currentHealth;
        session.swordLevel = swordLevel;
        session.safePointId = safePointId;
        session.safePointScene = safePointScene;
        session.safePointX = safePointX;
        session.safePointY = safePointY;
        session.sealOpened = sealOpened;
        session.guardianPermission = guardianPermission;

        session.materials.Clear();
        foreach (var m in materials)
            session.materials.Add(new MaterialEntry { id = m.id, amount = m.amount });
    }
}