# The Day I Made That Promise — Videojuego

Proyecto integrador: videojuego 2D isométrico en **Unity 6000.6.2f1** (URP 2D), con integración y entrega continua en GitHub Actions.

| Carpeta | Contenido |
|---|---|
| `game/` | Proyecto de Unity (ábrelo en Unity Hub con *Add › carpeta `game/`*, nunca la raíz del repo) |
| `game/ArtSource/` | Archivos fuente de arte que Unity no importa (Aseprite, etc.) |
| `infra/scripts/` | Pruebas locales, configuración del build, empaquetado del release y configuración del repo |
| `.github/workflows/` | `ci-game.yml` (pruebas y build) y `release.yml` (versiones) |
| `docs/` | Guía de implementación de CI/CD |

## Pruebas locales

```bash
# macOS
UNITY="/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity" infra/scripts/game/run-tests-local.sh
```

También desde el editor: *Window › General › Test Runner › EditMode › Run All*.

## Pipelines

| Workflow | Se dispara | Produce |
|---|---|---|
| `ci-game.yml` | PR y push a `dev`/`main` que toquen `game/`; noches de lunes a viernes | `game-test-results` (pruebas EditMode) y, fuera de PR, `game-win64` (ejecutable de Windows) |
| `release.yml` | tag `vX.Y.Z` sobre `main` | GitHub Release con `tdimp-windows-vX.Y.Z.zip` |

Secretos que usa: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` y, opcionalmente, `TELEMETRY_API_KEY_STAGING` / `TELEMETRY_API_KEY_PROD` con las variables `API_BASE_URL_STAGING` / `API_BASE_URL_PROD`.

## Flujo de trabajo

`feature/<descripcion>` desde `dev` → PR con plantilla → `game-tests` en verde + 1 aprobación → merge commit a `dev` → al cierre del sprint, PR `dev → main` → tag `vX.Y.Z` → Release.

> Hasta el 9 de octubre de 2026 este repositorio fue un monorepo con backend (Spring Boot), PWA (React) y app móvil (Flutter). Ese estado se conserva en el tag `monorepo-final`.
