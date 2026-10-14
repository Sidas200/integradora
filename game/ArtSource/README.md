# Archivos fuente de arte

Archivos de trabajo (Aseprite, etc.) que **no** importa Unity: están fuera de `game/Assets/`.

- `Aseprite/Pixel Map.aseprite`: 433 cuadros de 608×512. Importarlo en Unity genera texturas de
  varios GB y tumba el editor en CI (SIGBUS al importar). Si el juego necesita la imagen, expórtala
  desde Aseprite como PNG u hoja de sprites reducida y colócala en `game/Assets/Resources/Sprites/`.
