using System;
using System.IO;
using UnityEngine;

// GAME-102 / SCRUM-86 (102.3): guarda SaveData como JSON en persistentDataPath/save_slot{n}.json.
// Escribe a un temporal y luego reemplaza, para no corromper el guardado bueno.
// Si el archivo falta, está dañado o es de otra versión, Load devuelve null (SRS-HEX-04).
public class LocalSaveRepository : ISaveRepository
{
    public string FilePath { get; }

    public LocalSaveRepository(int slot = 1)
        : this(Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json")) { }

    // Permite usar otra ruta (útil para pruebas).
    public LocalSaveRepository(string filePath)
    {
        FilePath = filePath;
    }

    public void Save(SaveData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        string dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        string tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(data, true));

        if (File.Exists(FilePath))
            File.Replace(tmp, FilePath, null);
        else
            File.Move(tmp, FilePath);
    }

    public SaveData Load()
    {
        if (!File.Exists(FilePath)) return null;

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || !data.IsValid)
            {
                Debug.LogWarning("Guardado inválido o de otra versión; se ignora.");
                return null;
            }
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Guardado dañado; se ignora. ({e.Message})");
            return null;
        }
    }

    public bool Exists() => File.Exists(FilePath);

    public void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        string tmp = FilePath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }
}