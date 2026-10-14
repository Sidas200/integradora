#!/usr/bin/env bash
# =====================================================================
#  Script de pruebas — Videojuego (Unity) en la máquina del desarrollador.
#  Ejecuta las pruebas EditMode en modo batch, igual que GameCI en el pipeline.
#  Cierra Unity antes de correrlo (un proyecto no puede abrirse dos veces).
#
#  Uso (Git Bash en Windows):
#    UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe" infra/scripts/game/run-tests-local.sh
#  Uso (macOS):
#    UNITY="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity" infra/scripts/game/run-tests-local.sh
# =====================================================================
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
UNITY="${UNITY:?Define UNITY con la ruta al ejecutable del Editor (ver encabezado)}"
OUT="$ROOT/artifacts/game"
mkdir -p "$OUT"

echo "▶ Unity: pruebas EditMode"
"$UNITY" -batchmode -nographics \
  -projectPath "$ROOT/game" \
  -runTests -testPlatform EditMode \
  -testResults "$OUT/editmode-results.xml" \
  -logFile "$OUT/editmode.log"

echo "✔ Unity: pruebas aprobadas — resultados en artifacts/game/editmode-results.xml"
