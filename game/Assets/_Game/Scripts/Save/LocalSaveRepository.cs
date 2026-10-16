using System;
using System.IO;
using UnityEngine;

// GAME-102 / SCRUM-86 (102.3): guarda SaveData como JSON en persistentDataPath.
// Si el archivo falta, está dañado o es de otra versión, TryLoad devuelve false (SRS-HEX-04).
public class LocalSaveRepository : ISaveRepository
{
    public const string DefaultFileName = "save.json";

    public string FilePath { get; }

    public LocalSaveRepository()
        : this(Path.Combine(Application.persistentDataPath, DefaultFileName)) { }

    // Permite usar otra ruta (la prueba EditMode usa una carpeta temporal).
    public LocalSaveRepository(string filePath)
    {
        FilePath = filePath;
    }

    public void Save(SaveData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        string dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Primero a un temporal: si el juego se cierra a medio guardar, el bueno no se corrompe.
        string tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
        File.Copy(tmp, FilePath, true);
        File.Delete(tmp);
    }

    public bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(FilePath)) return false;

        try
        {
            string json = File.ReadAllText(FilePath);
            var loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded == null || !loaded.IsValid)
            {
                Debug.LogWarning("Guardado inválido o de otra versión; se ignora.");
                return false;
            }
            data = loaded;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Guardado dañado; se ignora. ({e.Message})");
            return false;
        }
    }

    public bool HasSave() => File.Exists(FilePath);

    public void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        string tmp = FilePath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }
}