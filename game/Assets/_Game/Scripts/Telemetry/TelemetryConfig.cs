// Configuración de telemetría leída en tiempo de ejecución desde StreamingAssets/telemetry.json.
// El pipeline compila el juego UNA vez y luego escribe ese archivo por ambiente
// (infra/scripts/game/write-telemetry-config.sh), igual que config.json en la PWA.
using System;
using System.IO;
using UnityEngine;

namespace TDIMP.Telemetry
{
    [Serializable]
    public class TelemetryConfig
    {
        public const string FileName = "telemetry.json";

        public string baseUrl = "http://localhost:8080";
        public string apiKey = "dev-telemetry-key";

        public string MatchesEndpoint => baseUrl.TrimEnd('/') + "/api/v1/matches";

        public static TelemetryConfig Load() => Load(Path.Combine(Application.streamingAssetsPath, FileName));

        public static TelemetryConfig Load(string path)
        {
            var config = new TelemetryConfig();
            if (!File.Exists(path))
            {
                return config;
            }
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
            return config;
        }
    }
}
