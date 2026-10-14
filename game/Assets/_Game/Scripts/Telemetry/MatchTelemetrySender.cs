// Envía la partida terminada al backend: POST /api/v1/matches con el header X-Telemetry-Key.
// Colócalo en un GameObject persistente y llama Send(payload) al terminar la partida.
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TDIMP.Telemetry
{
    public class MatchTelemetrySender : MonoBehaviour
    {
        [SerializeField] private int timeoutSeconds = 10;
        private TelemetryConfig config;

        private void Awake()
        {
            config = TelemetryConfig.Load();
        }

        public void Send(MatchTelemetryPayload payload)
        {
            var errors = payload.Validate();
            if (errors.Count > 0)
            {
                Debug.LogWarning("[Telemetry] Partida no enviada: " + string.Join("; ", errors));
                return;
            }
            StartCoroutine(Post(payload.ToJson()));
        }

        private IEnumerator Post(string json)
        {
            using var request = new UnityWebRequest(config.MatchesEndpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Telemetry-Key", config.apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Telemetry] Partida registrada");
            }
            else if (request.responseCode == 409)
            {
                Debug.Log("[Telemetry] La partida ya estaba registrada (reintento)");
            }
            else
            {
                Debug.LogWarning($"[Telemetry] Error {request.responseCode}: {request.error}");
            }
        }
    }
}
