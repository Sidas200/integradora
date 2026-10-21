using System;
using System.IO;
using UnityEngine;

// URL de la API. Se lee de StreamingAssets/telemetry.json, el mismo archivo que el pipeline escribe por
// ambiente (infra/scripts/game/write-telemetry-config.sh). La variable TDIMP_API_URL lo reemplaza,
// útil para probar en el editor contra un backend local.
[Serializable]
public class CloudConfig
{
    public const string FileName = "telemetry.json";
    public const string EnvVariable = "TDIMP_API_URL";
    // Puerto de la API en docker-compose de integradora-web.
    public const string DefaultBaseUrl = "http://localhost:18081";

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
