# Arte placeholder del Capítulo 1

Todo el arte de esta lista lo genera `Tools/Claude/Chapter 1/2 - Generate Placeholder Art`
(`game/Assets/_Game/Editor/Chapter1/PlaceholderArtGenerator.cs`) en `game/Assets/_Game/Art/Placeholders/`.

## Cómo reemplazar un placeholder sin romper prefabs ni escenas

1. Exporta el PNG final con **el mismo nombre de archivo** y sobrescribe el placeholder (conserva el `.meta`).
2. Mantén **PPU 100**, Filter Mode **Point**, Compression **None** y el **pivote** indicado abajo.
3. Si el arte final cambia de tamaño, ajusta solo el pivote: el builder coloca cada sprite por su pivote.
4. Después de reemplazar arte, **no** vuelvas a ejecutar el generador (lo sobrescribiría). Si hace falta,
   quita la entrada de `BuildEntries()` en `PlaceholderArtGenerator.cs`.

Escala de referencia: Tsuki mide 19×50 px; un tile isométrico mide 32×32 px (cara superior de 32×16).

## Tiles (32×32, pivote centro 0.5/0.5, misma silueta que `tileset_grass`)

| Archivo | Uso | Notas para el arte final |
|---|---|---|
| `placeholder_tile_stone` | Suelo del santuario, arena del guardián, entrada al santuario | Piedra fría y limpia |
| `placeholder_tile_stone_rune` | Círculo de runas (santuario y arena) | Runa carmesí que brille un poco |
| `placeholder_tile_wood` | Piso de la cabaña | Tablones |
| `placeholder_tile_ruin` | Variación de suelo en la zona 2 (Bosque Quebrado) | Piedra con musgo |
| `placeholder_tile_bridge` | Puentes (isla de flores → campo abierto) | Tablones con cuerda; cara lateral delgada |
| `placeholder_exit_marker` | Marca de salida/transición en el suelo | Brillo suave, legible sin sonido |

## Naturaleza (pivote en la base del tronco o del objeto)

| Archivo | Tamaño | Uso |
|---|---|---|
| `placeholder_tree_tall` | 38×92 | Pino alto (como la referencia del equipo) |
| `placeholder_tree_short` | 36×60 | Pino bajo |
| `placeholder_tree_dark` | 38×92 | Pino oscuro del Bosque Quebrado |
| `placeholder_tree_dead` | 32×64 | Árbol seco del Bosque Quebrado |
| `placeholder_bush` | 22×14 | Arbusto (no bloquea) |
| `placeholder_rock_small` / `_large` | 16×12 / 32×26 | Rocas (bloquean) |
| `placeholder_crystal` | 16×24 | Cristal azul (fuente de luz en la fase 6) |
| `placeholder_flower_glow` | 12×12 | Flor destello en el mapa |
| `placeholder_cloud` | 72×28 | Nubes decorativas bajo las islas |

## Estructuras y props

| Archivo | Tamaño | Uso |
|---|---|---|
| `placeholder_cabin` | 84×80 | Cabaña abandonada (exterior, ocupa 2×2 celdas) |
| `placeholder_wall_left_*` / `_right_*` (`wood`, `stone`) | 16×44 | Muros de interiores, 1 por celda de borde |
| `placeholder_bed`, `placeholder_table`, `placeholder_barrel`, `placeholder_crate` | — | Interior de la cabaña y campamento |
| `placeholder_sword_rack` | 16×38 | Espada carmesí sin filo en su soporte |
| `placeholder_tent` | 48×40 | Tiendas del campamento |
| `placeholder_forge` | 44×44 | Forja (CraftingStation) |
| `placeholder_campfire` | 18×18 | Fogata del campamento |
| `placeholder_desk` | 34×26 | Mesa de la investigadora |
| `placeholder_save_crystal` | 18×34 | Punto de guardado (base y antes del jefe) |
| `placeholder_pillar`, `placeholder_brazier` | 16×50, 14×28 | Santuario y arena |
| `placeholder_maiden_statue` | 28×62 | Estatua de la primera doncella |
| `placeholder_seal_gate` / `placeholder_seal_barrier` | 76×88 / 44×60 | Sello del guardián (arco + barrera que desaparece) |

## Personajes (pivote en los pies)

| Archivo | Tamaño | Uso | Falta en el arte final |
|---|---|---|---|
| `placeholder_npc_researcher` | 20×50 | Investigadora | Animación idle y "caída del árbol" |
| `placeholder_maiden_spirit` | 22×52 | Primera doncella despierta | Idle flotando |
| `placeholder_hikari` | 20×50 | Hikari (intro) | — |
| `placeholder_enemy_ranged` | 26×34 | Enemigo a distancia (arquetipo 2) | Idle, carga de disparo, muerte |
| `placeholder_guardian` | 60×76 | Primer guardián | Idle, embestida, golpe en área, vulnerable, muerte |

Pendiente aparte: `Art/enemies/kratos.png` (Enemy_Melee) es arte de alta resolución escalado (PPU 800 y filtro Bilinear),
no pixel art. Hace falta un sprite de enemigo cuerpo a cuerpo a PPU 100.

## Efectos y UI

| Archivo | Uso |
|---|---|
| `placeholder_shadow_blob` | Sombra bajo personajes y enemigos |
| `placeholder_telegraph_circle` / `_rect` | Avisos de ataque en el suelo (se tiñen en código) |
| `placeholder_projectile` | Proyectil del enemigo a distancia |
| `placeholder_interact_marker` | Icono "!" sobre interactivos |
| `placeholder_icon_sword`, `placeholder_icon_permit` | HUD: espada mejorada y permiso del guardián |
| `placeholder_portrait_*` (tsuki, researcher, maiden, hikari, guardian) | Retratos de diálogo 48×48 |
| `placeholder_intro_sky` | Fondo de la intro (240×135) |

## Índices de `tileset_grass` usados por el builder

- Pasto liso: 18, 19, 20, 34 · Arena completa: 7 · Flores: `tileset_grass_flowers_1…11`
- La isla de Omar se copia tal cual desde `Scenes/Chapter1/_Source/FlowerIsland_Grid.prefab`.
