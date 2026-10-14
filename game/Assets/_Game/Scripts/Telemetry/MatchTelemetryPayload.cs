// Datos de una partida terminada, tal como los define shared/contracts/openapi.yaml (MatchRequest).
// JsonUtility serializa los campos públicos con su nombre exacto, por eso usan camelCase:
// renombrar un campo aquí rompe el contrato, y la prueba de EditMode lo detecta.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TDIMP.Telemetry
{
    public enum MatchOutcome
    {
        COMPLETED,
        FAILED,
        ABANDONED
    }

    [Serializable]
    public class MatchTelemetryPayload
    {
        public string sessionId;
        public string playerId;
        public int score;
        public int durationSeconds;
        public int levelReached;
        public int progressPercent;
        public string outcome;
        public int materialsCollected;
        public int itemsCrafted;
        public string finishedAt;
        public string gameVersion;

        public static MatchTelemetryPayload Create(
            string playerId, int score, float durationSeconds, int levelReached, int progressPercent,
            MatchOutcome outcome, int materialsCollected, int itemsCrafted, DateTime finishedAtUtc)
        {
            return new MatchTelemetryPayload
            {
                sessionId = Guid.NewGuid().ToString(),
                playerId = playerId,
                score = score,
                durationSeconds = Mathf.Max(1, Mathf.RoundToInt(durationSeconds)),
                levelReached = levelReached,
                progressPercent = Mathf.Clamp(progressPercent, 0, 100),
                outcome = outcome.ToString(),
                materialsCollected = materialsCollected,
                itemsCrafted = itemsCrafted,
                finishedAt = finishedAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                gameVersion = Application.version
            };
        }

        /// <summary>Aplica las mismas reglas que el backend; si hay errores no vale la pena enviar.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (!Guid.TryParse(sessionId, out _)) errors.Add("sessionId debe ser un UUID");
            if (string.IsNullOrWhiteSpace(playerId) || playerId.Length > 64) errors.Add("playerId es obligatorio (máx. 64)");
            if (score < 0) errors.Add("score no puede ser negativo");
            if (durationSeconds < 1) errors.Add("durationSeconds debe ser ≥ 1");
            if (levelReached < 1) errors.Add("levelReached debe ser ≥ 1");
            if (progressPercent < 0 || progressPercent > 100) errors.Add("progressPercent debe estar entre 0 y 100");
            if (!Enum.TryParse(outcome, out MatchOutcome _)) errors.Add("outcome inválido");
            if (materialsCollected < 0 || itemsCrafted < 0) errors.Add("contadores no pueden ser negativos");
            if (string.IsNullOrEmpty(gameVersion) || gameVersion.Length > 20) errors.Add("gameVersion es obligatorio (máx. 20)");
            return errors;
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }
}
