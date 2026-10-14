#!/usr/bin/env bash
# =====================================================================
#  Configura un build YA COMPILADO del juego para un ambiente.
#  Escribe StreamingAssets/telemetry.json, que TelemetryConfig.Load() lee al iniciar.
#  Mismo principio que la PWA: se compila una vez y se configura por ambiente.
#  Uso:   infra/scripts/game/write-telemetry-config.sh <carpeta-build> <api-base-url> <telemetry-key>
# =====================================================================
set -euo pipefail
BUILD="${1:?Uso: write-telemetry-config.sh <carpeta-build> <api-base-url> <telemetry-key>}"
API="${2:?Falta la URL de la API}"
KEY="${3:?Falta la llave de telemetría}"

DATA_DIR="$(find "$BUILD" -maxdepth 1 -type d -name '*_Data' | head -n1)"
[[ -n "$DATA_DIR" ]] || { echo "✘ No se encontró la carpeta *_Data en $BUILD"; exit 1; }

mkdir -p "$DATA_DIR/StreamingAssets"
printf '{\n  "baseUrl": "%s",\n  "apiKey": "%s"\n}\n' "$API" "$KEY" > "$DATA_DIR/StreamingAssets/telemetry.json"
echo "✔ telemetry.json → $API"
