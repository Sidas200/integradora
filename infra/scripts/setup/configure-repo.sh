#!/usr/bin/env bash
# =====================================================================
#  Paso 1 de la práctica: revisa y configura el repositorio existente
#  (byfelc/integradora). Ejecutar desde un clon actualizado, en la rama dev.
#   1. Binarios de game/ que quedaron FUERA de Git LFS.
#   2. Modo de control de versiones de Unity (debe ser "Visible Meta Files").
#   3. Rama DEV duplicada: solo la borra si todo su contenido ya está en dev.
#   4. Workflow viejo game-ci.yml de main (apunta a la raíz y a DEV).
#   5. dev como rama por defecto y solo squash merge.
#  Uso:   infra/scripts/setup/configure-repo.sh <owner>/<repo> [--borrar-DEV]
#  Requiere: git, git-lfs, gh (autenticado con `gh auth login`).
# =====================================================================
set -euo pipefail
REPO="${1:?Uso: configure-repo.sh <owner>/<repo> [--borrar-DEV]}"
BORRAR_DEV="${2:-}"
cd "$(dirname "$0")/../../.."
git lfs install >/dev/null
git fetch --prune origin

echo "▶ 1. Binarios del juego versionados FUERA de Git LFS (deberían ser 0):"
# Compara lo que REALMENTE está guardado como puntero LFS contra los binarios por extensión
in_lfs="$(git lfs ls-files -n)"
outside=$(git ls-files -- 'game/**' \
  | grep -iE '\.(png|jpe?g|psd|aseprite|ase|tga|wav|mp3|ogg|fbx|blend|ttf|otf|mp4)$' \
  | grep -vxF -f <(printf '%s\n' "$in_lfs") || true)
if [[ -n "$outside" ]]; then
  echo "$outside" | sed 's/^/  ✘ /'
  echo "  → Agrega el patrón a .gitattributes y ejecuta: git add --renormalize game && git commit"
else
  echo "  ✔ ninguno"
fi

echo "▶ 2. Modo de control de versiones de Unity"
mode=$(sed -n 's/^ *m_Mode: //p' game/ProjectSettings/VersionControlSettings.asset)
if [[ "$mode" == "Visible Meta Files" ]]; then
  echo "  ✔ Visible Meta Files"
else
  echo "  ✘ Está en \"$mode\". En Unity: Edit › Project Settings › Version Control › Mode = Visible Meta Files"
fi

echo "▶ 3. Ramas de integración duplicadas (dev / DEV)"
if git show-ref --verify --quiet refs/remotes/origin/DEV; then
  if git merge-base --is-ancestor origin/DEV origin/dev; then
    echo "  DEV no tiene nada que no esté en dev."
    if [[ "$BORRAR_DEV" == "--borrar-DEV" ]]; then
      git push origin --delete DEV
      echo "  ✔ DEV eliminada"
    else
      echo "  → Vuelve a ejecutar con --borrar-DEV para eliminarla (avisa al equipo primero)"
    fi
  else
    echo "  ✘ DEV tiene commits que no están en dev: fusiónalos antes de borrarla"
    git log --oneline origin/dev..origin/DEV | sed 's/^/    /'
  fi
else
  echo "  ✔ solo existe dev"
fi

echo "▶ 4. Workflow anterior en main"
if git cat-file -e origin/main:.github/workflows/game-ci.yml 2>/dev/null; then
  echo "  ✘ main tiene .github/workflows/game-ci.yml (proyecto en la raíz, rama DEV)."
  echo "    → En el PR dev → main elimínalo: lo reemplaza ci-game.yml"
else
  echo "  ✔ sin workflows anteriores"
fi

echo "▶ 5. Configuración del repositorio en GitHub"
# dev por defecto: los PR apuntan ahí y schedule/workflow_run leen sus workflows
gh repo edit "$REPO" --default-branch dev --delete-branch-on-merge \
  --enable-squash-merge --enable-merge-commit=false --enable-rebase-merge=false
echo "  ✔ dev por defecto, solo squash merge, ramas borradas al fusionar"
