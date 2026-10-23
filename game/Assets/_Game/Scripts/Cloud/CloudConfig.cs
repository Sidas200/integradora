using System;
using System.IO;
using UnityEngine;

// URL de la API. Se lee de StreamingAssets/telemetry.json, el mismo archivo que el pipeline escribe por
// ambiente (infra/scripts/game/write-telemetry-config.sh). La variable TDIMP_API_URL lo reemplaza,
// útil para probar en el editor contra un backend local (TDIMP_API_URL=http://localhost:18081, el puerto de
// docker-compose de integradora-web).
[Serializable]
public class CloudConfig
{
    public const string FileName = "telemetry.json";
    public const string EnvVariable = "TDIMP_API_URL";
    // Staging en Render: el editor y los builds sin telemetry.json guardan en la base de pruebas.
    public const string DefaultBaseUrl = "https://tdimp-api-staging.onrender.com";

    public string baseUrl = DefaultBaseUrl;

    public string ApiRoot => baseUrl.TrimEnd('/') + "/api/v1";

    public static CloudConfig Load() => Load(Path.Combine(Application.streamingAssetsPath, FileName),
        Environment.GetEnvironmentVariable(EnvVariable));

    public static CloudConfig Load(string path, string overrideUrl)
    {
        var config = new CloudConfig();
        if (File.Exists(path))
        {
            try { JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config); }
            catch (Exception e) { Debug.LogWarning($"[Cloud] {FileName} ilegible, se usa {DefaultBaseUrl}: {e.Message}"); }
        }
        if (!string.IsNullOrWhiteSpace(overrideUrl)) config.baseUrl = overrideUrl.Trim();
        if (string.IsNullOrWhiteSpace(config.baseUrl)) config.baseUrl = DefaultBaseUrl;
        return config;
    }
}
