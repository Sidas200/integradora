# Guía de implementación paso a paso — CI/CD en `byfelc/integradora`

Proyecto integrador **The Day I Made That Promise** · Grupo IDGS101N · Gestión del Proceso de Desarrollo de Software, Unidad 1

Esta guía aplica la configuración de CI/CD sobre el repositorio real, partiendo de la rama `dev` (commit `3be9ff9`, "chore: estructura monorepo y mueve Unity a game/"). Cada fase dice **quién** la hace, **qué** se ejecuta y **qué evidencia** se captura.

> Los comandos se ejecutan desde la raíz del repo en Git Bash (Windows) o una terminal (macOS/Linux).

> **Nota (9 de octubre de 2026):** el equipo decidió que este repositorio contenga solo el videojuego. Las fases de backend, PWA, app móvil, contrato de API y despliegues a Render/Netlify/Supabase describen el estado anterior (monorepo), que se conserva en el tag `monorepo-final`. Del repositorio actual siguen vigentes las fases de Unity, licencia, protección de ramas y release.

## Estado del repositorio al iniciar (revisado el 7 de octubre de 2026)

| Hallazgo | Impacto | Se resuelve en |
|---|---|---|
| Repo **privado** en cuenta personal (plan Free) | Sin protección de ramas, sin secretos por ambiente, sin revisores en ambientes | Fase 0 |
| Existen `dev` y `DEV`; `DEV` ya está contenida completa en `dev` | En Windows (sin distinción de mayúsculas) dos ramas así chocan en el clon local | Fase 1 |
| `main` tiene `.github/workflows/game-ci.yml` apuntando a la raíz y a `DEV` | Fallará en cuanto `game/` llegue a main | Fase 2 |
| Unity en modo *Unity Version Control* | El editor intenta integrarse con Plastic en lugar de Git | Fase 1 |
| 2 archivos `.aseprite` fuera de Git LFS | Binarios en el historial normal | Fase 2, paso 5 |
| `backend/`, `web/`, `mobile/`, `shared/`, `infra/` solo con `.gitkeep` | No hay código que compilar ni probar | Fase 2 (esqueletos con pruebas) |

---

## Fase 0 · Decidir el plan del repositorio (Felc, 10 min)

Un repo privado en GitHub Free no permite proteger ramas ni separar secretos por ambiente. Hay dos caminos:

| Opción | Cómo | Qué se obtiene |
|---|---|---|
| **A. GitHub Pro con Student Developer Pack** (recomendado) | Felc solicita el pack en <https://education.github.com/pack> con su correo de la UTCH; Pro se activa sin costo | Protección de ramas, ambientes y secretos por ambiente en el repo privado, 3,000 min/mes de Actions |
| B. Hacer público el repo | *Settings › General › Danger Zone › Change visibility* | Todo lo anterior y además revisores requeridos en ambientes, minutos ilimitados |

La aprobación a producción funciona igual en ambos casos: el workflow abre un issue que Regina o Arturo aprueban comentando **aprobar**.

---

## Fase 1 · Limpiar y configurar el repositorio (Felc, 20 min)

```bash
git switch dev && git pull
infra/scripts/setup/configure-repo.sh byfelc/integradora            # diagnóstico + dev por defecto
infra/scripts/setup/configure-repo.sh byfelc/integradora --borrar-DEV   # tras avisar al equipo
```

El script revisa LFS, el modo de control de versiones de Unity, la rama `DEV` (solo la borra si todo su contenido está en `dev`) y el workflow viejo de `main`; al final deja `dev` como rama por defecto y solo *squash merge*.

**En Unity (Omar):** *Edit › Project Settings › Version Control › Mode* → **Visible Meta Files**. Confirmar *Editor › Asset Serialization* = **Force Text** (ya está así).

Cada integrante, después de borrar `DEV`:
```bash
git fetch --prune && git switch dev
```

📸 **Evidencia 1:** salida del script y la página del repo con `dev` como rama por defecto.

---

## Fase 2 · Integrar la configuración de CI/CD (Felc + revisión del equipo, 30 min)

La rama `feature/ci-cd-config` (sale de `dev`) contiene todo: `.github/`, `infra/`, `shared/contracts/`, los esqueletos de `backend/`, `web/`, `mobile/` y los scripts de CI dentro de `game/Assets/_Game/`.

1. Abrir el PR `feature/ci-cd-config → dev` con la plantilla.
2. **Omar abre el proyecto en Unity** con esa rama para que genere los `.meta` de los archivos nuevos de `game/Assets/_Game/{Editor/CI, Scripts/Telemetry, Tests/EditMode}` y los sube a la misma rama. Sin esos `.meta`, cada integrante generaría GUID distintos y habría conflictos.
3. En Unity: *Window › General › Test Runner › EditMode › Run All* → deben pasar 9 pruebas. Menú *Build › CI › Windows 64* → debe generar el ejecutable.
4. **Llevar `main` a `dev` y retirar el workflow viejo** (en la misma rama):
   ```bash
   git merge origin/main
   git rm .github/workflows/game-ci.yml
   git commit -m "ci: reemplaza game-ci.yml por ci-game.yml"
   ```
5. **Archivos en Git LFS, desde la PC de Felc** (el entorno que preparó la rama no pudo subir objetos LFS):
   ```bash
   git switch feature/ci-cd-config && git pull
   # Íconos de la PWA (vienen en tdimp-cicd.zip, carpeta web/public/icons/)
   cp <ruta-del-zip>/web/public/icons/*.png web/public/icons/
   # Migrar las fuentes de Aseprite a LFS
   echo '*.aseprite              lfs' >> .gitattributes
   git add .gitattributes web/public/icons
   git add --renormalize game/Assets/Resources/Aseprite
   git lfs ls-files | grep -E "aseprite|icons"     # deben aparecer los 10
   git commit -m "chore: íconos de la PWA y fuentes Aseprite en Git LFS"
   git push
   ```
6. App móvil (Emmanuel):
   ```bash
   cd mobile
   flutter create --org mx.utch.tdimp --project-name tdimp_mobile --platforms android .
   dart format lib test
   cd .. && infra/scripts/mobile/test.sh
   ```
   Ajustar `environment.flutter` en `mobile/pubspec.yaml` a la salida de `flutter --version`.
7. Verificación local de backend y web (Yahir):
   ```bash
   infra/scripts/backend/test.sh      # requiere Docker abierto (Testcontainers)
   infra/scripts/web/test.sh && infra/scripts/web/build.sh
   ```

📸 **Evidencia 2:** el PR con la lista de archivos y los scripts locales en verde.

---

## Fase 3 · Servicios externos (Yahir, 45 min)

- **Supabase** (`tdimp-staging`, `tdimp-prod`): *Connect › Session pooler* (IPv4). URL JDBC `jdbc:postgresql://aws-0-<region>.pooler.supabase.com:5432/postgres?sslmode=require`, usuario `postgres.<project-ref>`.
- **Render**: tras el primer push a `dev`, el CI publica `ghcr.io/byfelc/tdimp-backend`. En *Packages* hacerlo público o registrar en Render una credencial con token `read:packages`. Crear un Web Service por ambiente desde la imagen, *Health check path* `/actuator/health`, y copiar el *Deploy Hook*.
- **Netlify**: dos sitios vacíos (staging, producción); copiar cada *Site ID* y crear un *Personal access token*.

## Fase 4 · Licencia de Unity (Omar, 15 min)

Activar la licencia Personal en Unity Hub y localizar `Unity_lic.ulf` (Windows: `C:\ProgramData\Unity\Unity_lic.ulf`; macOS: `/Library/Application Support/Unity/Unity_lic.ulf`). El archivo nunca se sube (`*.ulf` está en `.gitignore`).
```bash
gh secret set UNITY_LICENSE  --repo byfelc/integradora < Unity_lic.ulf
gh secret set UNITY_EMAIL    --repo byfelc/integradora
gh secret set UNITY_PASSWORD --repo byfelc/integradora
```
`main` ya usaba estos tres nombres en `game-ci.yml`; si ya existen, no hay que volver a cargarlos.

## Fase 5 · Ambientes, secretos y variables (Felc, 20 min)

```bash
infra/scripts/setup/create-environments.sh byfelc/integradora
infra/scripts/setup/load-secrets.sh        byfelc/integradora
```

`load-secrets.sh` lee archivos locales que no se versionan:

**`secrets.repo.env`**
```
NETLIFY_AUTH_TOKEN=nfp_xxx
TELEMETRY_API_KEY_STAGING=<llave del Render de staging>
TELEMETRY_API_KEY_PROD=<llave del Render de producción>
VAR_API_BASE_URL_STAGING=https://tdimp-api-staging.onrender.com
VAR_API_BASE_URL_PROD=https://tdimp-api.onrender.com
VAR_PROD_APPROVERS=Sidas200,<usuario-de-Regina>
```

**`secrets.staging.env`** y **`secrets.production.env`**
```
DB_URL=jdbc:postgresql://aws-0-<region>.pooler.supabase.com:5432/postgres?sslmode=require
DB_USER=postgres.<project-ref>
DB_PASSWORD=********
RENDER_DEPLOY_HOOK=https://api.render.com/deploy/srv-xxxx?key=yyyy
NETLIFY_SITE_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
VAR_API_URL=https://tdimp-api-staging.onrender.com
VAR_WEB_URL=https://tdimp-staging.netlify.app
```

📸 **Evidencia 3:** *Settings › Secrets and variables › Actions* con los **nombres** (nunca los valores) y *Settings › Environments*.

---

## Fase 6 · Primera ejecución (todo el equipo, 30 min)

1. Con el PR de la Fase 2 abierto, revisar *Checks*: corren los 4 CI; los módulos sin cambios aparecen como *Skipped*.
2. Un compañero aprueba y se fusiona con *Squash and merge*.
3. Para correr un pipeline completo: *Actions › CI Game › Run workflow* (rama `dev`).

📸 **Evidencia 4:** el PR con los checks en verde.

## Fase 7 · Proteger `dev` y `main` (Felc, 5 min)

Después de la Fase 6, porque GitHub solo deja exigir checks que ya se ejecutaron una vez:
```bash
infra/scripts/setup/protect-branches.sh byfelc/integradora
```
Checks obligatorios: `backend-verify`, `backend-contract`, `web-verify`, `mobile-verify`, `game-tests`.

📸 **Evidencia 5:** *Settings › Branches* y un `git push origin dev` directo rechazado.

## Fase 8 · Demostrar que el pipeline protege la calidad (Yahir + Santiago, 20 min)

1. Rama `fix/demo-rojo`: en `backend/.../MatchService.java` cambiar `MAX_LIMIT = 50` por `5`. Abrir PR → `backend-verify` en rojo y *Merge* bloqueado. Revertir → verde.
2. Rama `fix/demo-contrato`: borrar `GET /api/v1/players/{playerId}/stats` de `shared/contracts/openapi.yaml` → `backend-contract` falla. Cerrar sin fusionar.
3. Tras fusionar a `dev`: *Actions › CD Staging* (Flyway → Render → humo → Netlify → Cypress). Abrir la URL de staging e **Instalar app**.

📸 **Evidencia 6:** PR rojo bloqueado y luego verde; *CD Staging* completo; la PWA instalada.

## Fase 9 · Primera versión (Regina + Arturo, 20 min)

1. PR `dev → main` titulado `release: v0.1.0`; al fusionar, los CI de `main` guardan artefactos 90 días.
2. Tag:
   ```bash
   git switch main && git pull
   git tag -a v0.1.0 -m "Sprint 1: ciclo de juego y endpoint de partidas"
   git push origin v0.1.0
   ```
3. *Release* crea el GitHub Release con `tdimp-pwa-v0.1.0.zip`, `tdimp-android-v0.1.0.zip`, `tdimp-windows-v0.1.0.zip` y lanza *CD Production*.
4. *CD Production* abre el issue "Desplegar v0.1.0 a producción"; Arturo comenta **aprobar**.

📸 **Evidencia 7:** el Release con sus artefactos y el issue de aprobación.

---

## Lista de verificación

- [ ] Fase 0: Pro (Student Pack) activo o repo público
- [ ] `DEV` eliminada, `dev` por defecto, solo squash merge
- [ ] Unity en *Visible Meta Files*; `.meta` de los archivos nuevos subidos
- [ ] `game-ci.yml` retirado de main
- [ ] Usuarios reales en `.github/CODEOWNERS` (faltan Yahir, Santiago, Omar, Regina)
- [ ] Secretos y variables cargados; `PROD_APPROVERS` con Arturo y Regina
- [ ] 7 workflows ejecutados al menos una vez
- [ ] `dev` y `main` protegidas
- [ ] PR rojo bloqueado y luego verde
- [ ] Despliegue a staging verificado en el navegador
- [ ] Release v0.1.0 aprobado a producción

## Problemas frecuentes

| Síntoma | Solución |
|---|---|
| `protect-branches.sh` responde "Upgrade to GitHub Pro" | Falta la Fase 0 |
| PR en "Expected — Waiting for status" | El check nunca corrió: *Run workflow* una vez |
| GameCI: `No valid Unity license` | `.ulf` completo + `UNITY_EMAIL` y `UNITY_PASSWORD` |
| Unity marca conflictos de GUID en `.meta` | Los `.meta` de archivos nuevos no se subieron (Fase 2, paso 2) |
| Flyway: `Connection refused` desde Actions | Usar el *Session pooler* de Supabase (IPv4) |
| Render no encuentra la imagen | Hacer público el paquete de GHCR o registrar la credencial |
| `mobile-verify` falla en formato | `dart format lib test` y volver a subir |
| CD Staging no se dispara | `dev` debe ser la rama por defecto |
| El issue de aprobación no acepta el comentario | El usuario debe estar en `PROD_APPROVERS` y tener acceso al repo |
