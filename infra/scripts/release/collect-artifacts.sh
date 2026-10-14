#!/usr/bin/env bash
# =====================================================================
#  Reúne, SIN RECOMPILAR, el último ejecutable exitoso del juego en main
#  para adjuntarlo al GitHub Release de la versión.
#  Como el build solo corre cuando cambia game/, el último artefacto de main
#  ES la versión vigente del juego.
#  Requiere: gh CLI autenticado (GH_TOKEN) y jq.
#  Uso:   infra/scripts/release/collect-artifacts.sh <version>   ej. v1.0.0
# =====================================================================
set -euo pipefail
VERSION="${1:?Uso: collect-artifacts.sh <version>}"
OUT="artifacts/release"
rm -rf "$OUT" && mkdir -p "$OUT"

# workflow : nombre del artefacto : archivo final
MODULES=(
  "ci-game.yml:game-win64:tdimp-windows-${VERSION}.zip"
)

for entry in "${MODULES[@]}"; do
  IFS=: read -r wf artifact file <<<"$entry"
  echo "▶ Buscando '$artifact' en las ejecuciones exitosas de $wf sobre main"
  found=""
  for run in $(gh run list --workflow "$wf" --branch main --status success --limit 30 --json databaseId --jq '.[].databaseId'); do
    if gh api "repos/{owner}/{repo}/actions/runs/$run/artifacts" --jq '.artifacts[] | select(.expired == false) | .name' | grep -qx "$artifact"; then
      found="$run"; break
    fi
  done
  if [[ -z "$found" ]]; then
    echo "⚠ No hay artefacto vigente de $artifact en main; se omite del release"
    continue
  fi
  gh run download "$found" -n "$artifact" -D "$OUT/$artifact"
  # El ejecutable del juego se configura para producción antes de empaquetarlo
  if [[ "$artifact" == "game-win64" && -n "${PROD_API_URL:-}" && -n "${TELEMETRY_API_KEY_PROD:-}" ]]; then
    "$(dirname "$0")/../game/write-telemetry-config.sh" "$OUT/$artifact" "$PROD_API_URL" "$TELEMETRY_API_KEY_PROD"
  fi
  (cd "$OUT/$artifact" && zip -qr "../$file" .)
  echo "✔ $file (ejecución $found)"
done

ls -lh "$OUT"/*.zip 2>/dev/null || echo "⚠ No se reunió ningún artefacto"
