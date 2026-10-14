#!/usr/bin/env bash
# =====================================================================
#  Paso 2 de la práctica: protege main y dev.
#   - Solo se entra por pull request con 1 aprobación (y revisión de CODEOWNERS)
#   - Checks obligatorios en verde (nombres de job de los workflows de CI)
#   - Historial lineal (squash), sin push forzado ni borrado
#  Uso:   infra/scripts/setup/protect-branches.sh <owner>/<repo>
#  Requisito: en repos PRIVADOS la protección de ramas exige GitHub Pro/Team
#  (Pro es gratis para el dueño del repo con GitHub Student Developer Pack) o hacer el repo público.
# =====================================================================
set -euo pipefail
REPO="${1:?Uso: protect-branches.sh <owner>/<repo>}"

CHECKS='["game-tests"]'

protect() {
  local branch="$1" approvals="$2"
  echo "▶ Protegiendo $branch"
  gh api -X PUT "repos/$REPO/branches/$branch/protection" --input - <<JSON
{
  "required_status_checks": { "strict": true, "contexts": $CHECKS },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": $approvals,
    "require_code_owner_reviews": true,
    "dismiss_stale_reviews": true
  },
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_conversation_resolution": true
}
JSON
}

protect dev 1
protect main 1
echo "✔ Ramas protegidas. Los checks se vuelven seleccionables después de su primera ejecución."
