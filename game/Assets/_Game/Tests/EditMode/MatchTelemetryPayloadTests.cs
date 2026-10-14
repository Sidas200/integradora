// Pruebas EditMode (rápidas, sin escena): las corre GameCI en cada pull request.
// Verifican que lo que el juego envía respeta el contrato de la API.
using System;
using System.IO;
using NUnit.Framework;

namespace TDIMP.Telemetry.Tests
{
    public class MatchTelemetryPayloadTests
    {
        private static MatchTelemetryPayload Valid() =>
            MatchTelemetryPayload.Create("tsuki", 1500, 421.6f, 2, 35, MatchOutcome.COMPLETED, 6, 2,
                new DateTime(2026, 10, 6, 20, 15, 0, DateTimeKind.Utc));

        [Test]
        public void PartidaValidaNoTieneErrores()
        {
            Assert.IsEmpty(Valid().Validate());
        }

        [Test]
        public void JsonUsaLosNombresDeCampoDelContrato()
        {
            string json = Valid().ToJson();
            string[] contractFields =
            {
                "sessionId", "playerId", "score", "durationSeconds", "levelReached", "progressPercent",
                "outcome", "materialsCollected", "itemsCrafted", "finishedAt", "gameVersion"
            };
            foreach (string field in contractFields)
            {
                StringAssert.Contains($"\"{field}\":", json, $"Falta el campo {field} exigido por el contrato");
            }
        }

        [Test]
        public void FechaSeEnviaEnIso8601Utc()
        {
            Assert.AreEqual("2026-10-06T20:15:00Z", Valid().finishedAt);
        }

        [Test]
        public void DuracionSeRedondeaYNuncaEsCero()
        {
            Assert.AreEqual(422, Valid().durationSeconds);
            var instant = MatchTelemetryPayload.Create("tsuki", 0, 0.2f, 1, 0, MatchOutcome.ABANDONED, 0, 0, DateTime.UtcNow);
            Assert.AreEqual(1, instant.durationSeconds);
        }

        [Test]
        public void CadaPartidaTieneSesionUnica()
        {
            Assert.AreNotEqual(Valid().sessionId, Valid().sessionId);
        }

        [Test]
        public void PuntajeNegativoEsInvalido()
        {
            var p = Valid();
            p.score = -1;
            Assert.That(p.Validate(), Has.Some.Contains("score"));
        }

        [Test]
        public void ProgresoFueraDeRangoEsInvalido()
        {
            var p = Valid();
            p.progressPercent = 140;
            Assert.That(p.Validate(), Has.Some.Contains("progressPercent"));
        }

        [Test]
        public void ConfigSinArchivoUsaValoresLocales()
        {
            var config = TelemetryConfig.Load(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid()));
            Assert.AreEqual("http://localhost:8080/api/v1/matches", config.MatchesEndpoint);
        }

        [Test]
        public void ConfigLeeUrlYLlaveDelArchivo()
        {
            string path = Path.Combine(Path.GetTempPath(), "telemetry-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, "{\"baseUrl\":\"https://api-staging.example.com/\",\"apiKey\":\"abc\"}");
            try
            {
                var config = TelemetryConfig.Load(path);
                Assert.AreEqual("https://api-staging.example.com/api/v1/matches", config.MatchesEndpoint);
                Assert.AreEqual("abc", config.apiKey);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
