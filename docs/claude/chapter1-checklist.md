# Capítulo 1 (MVP) — guía, checklist y pendientes

Rama: `feature/claude` (sin push). Unity 6000.6.2f1 · URP 17.6 (2D Renderer) · Input System 1.20.

## Cómo reconstruir y jugar

1. Abrir el proyecto `game/` en Unity.
2. Menú **Tools › Claude › Build Chapter 1**. Hace todo en orden y es reproducible:
   1. *Verify Project Settings*: sorting layers y Custom Axis.
   2. *Generate Placeholder Art*: `Art/Placeholders`.
   3. *Build Prefabs*: jugador, enemigos y `Resources/Chapter1/GameSystems`.
   4. *Generate Dialogues*: `Data/Chapter1/Dialogues`.
   5. *Build Scenes*: `Scenes/Chapter1`.
   6. *Configure Build Settings*: el menú primero.
3. Jugar: abrir `Scenes/Chapter1/MainMenu` y dar Play (también se puede dar Play desde cualquier escena del capítulo).
4. Build de PC: el CI existente (`TDIMP.CI.BuildScript`) ya toma las escenas habilitadas, que empiezan en el menú. Desde el editor: **Build › CI › Windows 64** (requiere el módulo Windows Build Support).
5. Prueba de humo del ejecutable, sin Unity: `<ejecutable> -smoketest`. Recorre las 9 escenas, escribe `[SMOKE] … FPS` en el log del jugador y se cierra.

> ⚠️ Volver a ejecutar el builder **sobrescribe** las escenas de `Scenes/Chapter1`, el arte placeholder, los prefabs de `Prefabs/Chapter1` y `GameSystems`. Los cambios de layout se hacen en `Editor/Chapter1/ChapterScenes.cs`. Los diálogos marcados **Locked By Designer** no se sobrescriben.

## Controles

WASD/flechas: mover · Clic izquierdo: atacar (combo de 2) · Espacio: esquivar · E: interactuar/avanzar diálogo · Q: poción · Esc: pausa · F3: FPS.

## Recorrido del capítulo (20–30 min)

Menú → Intro (leyenda, Hikari, la promesa) → **Isla de las Flores** (despertar, cabaña, espada carmesí, la isla se desvanece al cruzar el puente) → **Llanura del Eco** (Kaede cae del árbol; 2 melee, 2 flores, 1 poción) → **Campamento oculto** dentro de la Llanura, al oeste entre los árboles (explicación de zonas, guardianes, santuarios y Gran Juzgado; forja; cristal de guardado) → *opcional* **Santuario** (despierta Aoi) → **Bosque Quebrado** (2 melee, 2 Centinelas, 1 cuero, 1 flor, 1 poción; cristal antes del jefe; sello) → regresar a la forja con 3/3 → romper el sello → **Umbral del Guardián** (Varn) → permiso → **Fin de la demo**.

## Checklist de requisitos

Leyenda: ✅ verificado en Play Mode o en el build · 🧪 cubierto por test EditMode · 👀 revisar a mano con teclado.

| Req. | Qué se pide | Cómo verificarlo | Estado |
|---|---|---|---|
| HEX-01 | Nueva partida, objetivo explicado | Menú → Nueva partida → intro → el HUD muestra el objetivo y Kaede lo explica en el campamento | ✅ |
| HEX-01 | Movimiento y ataque isométricos | WASD/clic; el ataque se apunta con el mouse en 4 direcciones; sin la espada no se ataca | ✅ 👀 |
| HEX-01 | Pausa y reintento | Esc → Pausa (tiempo detenido) → "Volver al punto seguro" | ✅ |
| HEX-01 | Colisiones y profundidad en todas las zonas | Bordes de isla con colisión (casts en 4 direcciones); Tsuki detrás o delante de bloques, árboles y pilares (Custom Axis 0,1,-0.26) | ✅ |
| HEX-02 | Base + zona 1 exterior/interior + zona 2 | Llanura del Eco (con el campamento integrado) ↔ Santuario (interior) ↔ Bosque Quebrado; cabaña interior | ✅ |
| HEX-02 | Volver a la base sin perder progreso | Ir y volver con materiales y vida: se conservan (SessionState persistente) | ✅ |
| HEX-03 | 2 arquetipos con aviso, salud y derrota | Enemy_Melee (círculo rojo 0.6 s) y Centinela (círculo en el suelo 0.9 s + orbe) | ✅ |
| HEX-03 | Daño una vez por golpe | Enemigo con 2 colliders: 40 → 25 con ataque 15 (deduplicado por golpe) | ✅ |
| HEX-04 | 2 materiales visibles en inventario | Tab abre el inventario: pestaña **Objetos** (espada, permiso, materiales y pociones con descripción) y **Estadísticas** (vida, ataque, defensa, velocidad, espada, permisos, doncellas). Pausa el juego; Tab/Esc cierran; 1/2 cambian de pestaña. El objetivo sigue mostrando Cuero x/3 y Flor x/3 | ✅ |
| HEX-04 | Cada fuente una sola vez; persiste entre zonas | Pickups y enemigos con PersistentId; al volver no reaparecen y el drop no recogido sigue | ✅ 🧪 |
| HEX-05 | Mejora en la forja con ambos materiales | Receta "Despertar de la espada carmesí": 3 + 3, daño 15 → 25 | ✅ |
| HEX-05 | Sin recursos no consume; con recursos consume una vez | Con 2/3: botón desactivado y nada se gasta. Con 3/3 dos veces: consume una vez y la receta desaparece | ✅ |
| HEX-06 | Sello que rechaza sin la mejora | E en el sello con espada nivel 0 → rechazo y retroceso. Con nivel 1 → se abre (una vez) | ✅ |
| HEX-06 | Guardián con 2 patrones telegrafiados | Embestida (franja) y golpe en área (círculo); vulnerable solo después de atacar; i-frames tras cada golpe | ✅ |
| HEX-06 | Permiso una sola vez | El segundo intento no hace nada; al continuar, el jefe y el permiso no reaparecen | ✅ |
| HEX-07 | Victoria/cierre solo tras vencer al jefe | El cierre solo se alcanza con el permiso (la salida de la arena exige `permit_obtained`) | ✅ |
| HEX-07 | Derrota con reintento coherente | Morir → "Tsuki ha caído" → Reintentar: último punto seguro, vida llena, estado del guardado | ✅ |
| HEX-08 | Guardado en base y antes del jefe | Cristales en el campamento (Llanura) y antes del sello (auto al acercarse y con E) | ✅ |
| HEX-08 | Cargar sin duplicar recompensas | Guardar → recoger → morir → reintentar: materiales del guardado; recogidos y vencidos se respetan | ✅ 🧪 |
| SRS | Build de PC jugable sin Unity | macOS: `-smoketest` recorre las 9 escenas sin excepciones. Windows: CI o módulo Windows | ✅ (macOS) 👀 (Windows) |
| SRS | Funciona sin red | No hay llamadas de red en el flujo; la telemetría existente no se invoca | ✅ |
| SRS | Guardado ausente o dañado no impide Nueva partida | Archivo corrupto → "Continuar" desactivado con aviso; "Nueva partida" funciona | ✅ 🧪 |
| SRS | HUD: vida, materiales, objetivo, prompt, pausa/derrota/victoria | Arriba izquierda: vida · arriba derecha: objetivo · debajo de la vida: «Tab: Inventario · Esc: Pausa» · abajo: "E: …" · paneles | ✅ |
| RNF | Legibilidad de amenazas e interactivos | Rojo reservado a peligro (las runas son cian), "!" y prompt en interactivos, etiquetas en salidas | ✅ |
| RNF | Avisos que permiten reaccionar | 0.6 s (melee), 0.9 s (Centinela), 1.0/1.2 s (jefe; 0.75–0.9 s furioso); el mundo se pausa en los diálogos | ✅ 👀 |
| RNF | ≥ 30 FPS | Build macOS 1920×1080: mínimo 119.6 FPS (tope del monitor); F3 muestra los FPS en cualquier PC | ✅ |
| RNF | Se entiende sin sonido | Todo evento tiene texto: diálogos, objetivos, avisos, estado del jefe | ✅ |
| Opcional | Login con la API sin bloquear | No implementado (decisión 5): el juego es 100 % offline | — |

Tests EditMode: `SaveRepositoryTests` (8) y telemetría (9). Todos pasan.

## Pendientes y supuestos

**Pendientes para el equipo**
- **Arte final**: lista y reglas de reemplazo en [`art-needed.md`](art-needed.md). `kratos.png` (Enemy_Melee) no es pixel art (PPU 800, Bilinear).
- **Nombres por aprobar**: [`pending-names.md`](pending-names.md) (Kaede, Aoi, Llanura del Eco, Bosque Quebrado, Varn, Centinela).
- **Audio**: no hay música ni efectos (el juego se entiende sin sonido, como pide el RNF).
- **Animaciones**: Kaede, Aoi, el Centinela y Varn son sprites estáticos con movimiento por código (caída, flotar, sacudidas).
- **Build de Windows**: verificar en el CI o instalar *Windows Build Support* y usar *Build › CI › Windows 64*.
- **Telemetría al terminar la demo**: el `MatchTelemetrySender` existente no se conectó. Se puede llamar desde `PermitPickup` sin bloquear.
- **Inventario del Pocket Map**: el capítulo usa su propia ventana con Tab (`InventoryScreen`); el menú Pocket del equipo no se integró.

**Supuestos tomados**
- Receta nueva (3 cueros + 3 flores) en lugar de `Receta_ModuloEspada`, que quedó intacta.
- Reintento sin punto seguro previo: vuelve a la entrada de la zona con el estado de ese momento.
- Los cristales guardan solos al acercarse (una vez por visita) y con E; siempre restauran la vida.
- Mientras hay un diálogo abierto, el mundo se pausa (`timeScale = 0`).
- El jugador tiene 0.6 s de invulnerabilidad tras recibir daño.
- Varn solo recibe daño en su ventana vulnerable y es inmune a la espada sin mejorar (coherente con "las armas normales no hieren a un dios").
- Los enemigos vencidos no reaparecen (se guardan); al recargar un guardado vuelven solo los vencidos después de ese guardado.

**Cambios en código o assets del equipo (mínimos y justificados)**
- `PlayerController`: busca el sprite en los hijos; bloqueo de input; `AttackEnabled`; un golpe daña una vez por objetivo.
- `EnemyHealth`: invulnerabilidad opcional y eventos (sin cambiar el comportamiento por defecto).
- `MaterialPickup`: respeta `PersistentId` si lo tiene. `CraftingPanel`, `CraftingStation`, `ItemUser`: bloqueo de input.
- `SessionState` y `SaveData`: flags, recogidos y vencidos (compatible con guardados v2); corregido el fallback de carga por nombre en el editor.
- `CameraFollow.SnapToTarget`. `Game.Gameplay.asmdef`: referencias a URP 2D.
- Prefabs `Enemy_Melee` y `Pickup_*`: solo pasan del sorting layer Default a Sprites.
- `TagManager`: `suelo` → `Objects` (mismo ID) y nuevos Background/Sprites/UI; `Renderer2D`: Custom Axis.
- Build Settings: escenas del capítulo primero; `Prototype_Isometric` queda en la lista pero deshabilitada.

## Cambios tras la primera prueba de juego

- **Campamento dentro de la Llanura del Eco**: se eliminó la escena `03_Camp` (el camino de 1 celda en escalera atoraba a Tsuki). Un bosque denso con un camino lo esconde; el paso se abre al conocer a Kaede (`FlagGate`) y al entrar aparece «Campamento oculto» (`AreaTitle`).
- **Escenarios más grandes**: Llanura ~1.4×, Bosque Quebrado ~1.35×, isla y arena más amplias, santuario con más espacio.
- **Caminos anchos y validación automática**: `ZoneBuilder.Path` ensancha el terreno a ≥3 celdas cuando un camino cruza el vacío. Al construir, cada escena comprueba que salidas, NPC, forja, cristales y objetos se alcancen caminando con holgura (2×2) y que ningún enemigo detecte un punto de aparición o de guardado; cualquier fallo sale como error en la consola.
- **Inventario con Tab** (ver HEX-04); se quitó el panel fijo del HUD.
- **Varn** ahora se limita a la elipse real de la arena (antes era un círculo y podía salirse arriba/abajo).
- Un guardado antiguo que apunte a `03_Camp` se ignora: «Continuar» no aparece y la nueva partida funciona.
