using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public interface ICloudTokenStore
{
    string Load();
    void Save(string refreshToken);
    void Clear();
}

// Guarda el refresh token cifrado (AES-256) con una llave derivada del id del dispositivo:
// copiar el archivo a otra máquina no sirve. No es el llavero del sistema, pero el token nunca
// queda en texto plano ni en PlayerPrefs.
public class FileCloudTokenStore : ICloudTokenStore
{
    private const string Salt = "tdimp-refresh-token-v1";

    private readonly string path;
    private readonly byte[] key;

    public FileCloudTokenStore()
        : this(Path.Combine(Application.persistentDataPath, "cloud_session.dat"), SystemInfo.deviceUniqueIdentifier) { }

    public FileCloudTokenStore(string path, string deviceId)
    {
        this.path = path;
        using var sha = SHA256.Create();
        key = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + "|" + deviceId));
    }

    public string Load()
    {
        if (!File.Exists(path)) return null;
        try
        {
            byte[] blob = File.ReadAllBytes(path);
            using var aes = Aes.Create();
            aes.Key = key;
            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(blob, 0, iv, 0, iv.Length);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(blob, iv.Length, blob.Length - iv.Length);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] Sesión guardada ilegible; hay que iniciar sesión otra vez. ({e.Message})");
            Clear();
            return null;
        }
    }

    public void Save(string refreshToken)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        byte[] plain = Encoding.UTF8.GetBytes(refreshToken);
        byte[] cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        var blob = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, blob, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, blob, aes.IV.Length, cipher.Length);

        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, blob);
    }

    public void Clear()
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
