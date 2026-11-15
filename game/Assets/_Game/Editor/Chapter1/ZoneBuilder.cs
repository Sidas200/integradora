using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace Game.EditorTools.Chapter1
{
    public enum Ground { Void, Grass, Flower, Sand, Bridge, Stone, Rune, Wood, Ruin }

    // Describe una zona en coordenadas de celda isométrica y la construye en la escena abierta.
    // Orientación en pantalla: +x = arriba-derecha, +y = arriba-izquierda.
    public class ZoneBuilder
    {
        // Grid de Omar: 32x16 px por celda con PPU 100.
        public static readonly Vector3 CellSize = new Vector3(0.32f, 0.16f, 1f);
        // El centro visible de la cara superior del tile está 8 px arriba del centro de la celda.
        public static readonly Vector3 SurfaceOffset = new Vector3(0f, 0.08f, 0f);
        // Z de la capa de elevaciones: hace que el bloque elevado se ordene como su celda de suelo
        // con el eje (0, 1, -0.26): 0.08 / 0.26.
        public const float ElevationZ = 0.3077f;

        public struct PropSpec
        {
            public string sprite;
            public Vector2Int cell;
            public bool blocks;
            public Vector2Int[] footprint;
            public string name;
            public int sortingOrder;
            public Vector2 offset;
        }

        public readonly string Name;
        private readonly System.Random rng;
        private readonly Dictionary<Vector2Int, Ground> ground = new Dictionary<Vector2Int, Ground>();
        private readonly Dictionary<Vector2Int, TileBase> groundOverride = new Dictionary<Vector2Int, TileBase>();
        private readonly Dictionary<Vector2Int, TileBase> elevation = new Dictionary<Vector2Int, TileBase>();
        private readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> keepClear = new HashSet<Vector2Int>();
        private readonly List<PropSpec> props = new List<PropSpec>();
        private readonly Dictionary<string, Vector2Int> markers = new Dictionary<string, Vector2Int>();
        private readonly List<(Vector2Int min, Vector2Int max, bool stone)> walls = new List<(Vector2Int, Vector2Int, bool)>();

        public Color GroundTint = Color.white;
        // Suelo con el que se rellenan los bordes de un camino que cruza el vacío
        public Ground Fill = Ground.Grass;

        public ZoneBuilder(string name, int seed)
        {
            Name = name;
            rng = new System.Random(seed);
        }

        public IReadOnlyDictionary<string, Vector2Int> Markers => markers;

        // ------------------------------------------------------------ forma del terreno

        public ZoneBuilder Blob(Vector2Int center, float rx, float ry, Ground kind = Ground.Grass, float noise = 0.18f)
        {
            int seedX = rng.Next(1000), seedY = rng.Next(1000);
            for (int y = Mathf.FloorToInt(-ry - 2); y <= ry + 2; y++)
                for (int x = Mathf.FloorToInt(-rx - 2); x <= rx + 2; x++)
                {
                    float d = (x * x) / (rx * rx) + (y * y) / (ry * ry);
                    float n = (Mathf.PerlinNoise((x + seedX) * 0.35f, (y + seedY) * 0.35f) - 0.5f) * 2f * noise;
                    if (d <= 1f + n) Set(center + new Vector2Int(x, y), kind);
                }
            return this;
        }

        public ZoneBuilder Rect(Vector2Int min, Vector2Int max, Ground kind)
        {
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                    Set(new Vector2Int(x, y), kind);
            return this;
        }

        // Cambia el tipo de suelo solo donde ya hay tierra.
        public ZoneBuilder Patch(Vector2Int center, float radius, Ground kind, float density = 1f)
        {
            for (int y = Mathf.FloorToInt(-radius); y <= radius; y++)
                for (int x = Mathf.FloorToInt(-radius); x <= radius; x++)
                {
                    var c = center + new Vector2Int(x, y);
                    if (!IsLand(c) || ground[c] == Ground.Bridge) continue;
                    float d = Mathf.Sqrt(x * x + y * y) / radius;
                    if (d <= 1f && rng.NextDouble() < density * (1.15f - d * 0.5f)) Set(c, kind);
                }
            return this;
        }

        // Camino que también crea tierra si pasa por el vacío. Con widen, el terreno alrededor
        // queda de al menos 3 celdas de ancho: un camino de 1 celda en escalera atora al jugador.
        public ZoneBuilder Path(Ground kind, int width, params Vector2Int[] points) => Path(kind, width, true, points);

        public ZoneBuilder Path(Ground kind, int width, bool widen, params Vector2Int[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
                foreach (var c in Line(points[i], points[i + 1]))
                    for (int w = 0; w < width; w++)
                    {
                        var cell = c + new Vector2Int(0, w);
                        Set(cell, kind);
                        for (int y = -1; y <= 1; y++)
                            for (int x = -1; x <= 1; x++)
                            {
                                var n = cell + new Vector2Int(x, y);
                                keepClear.Add(n);
                                if (widen && !IsLand(n)) Set(n, Fill);
                            }
                    }
            return this;
        }

        // Bosque denso en un área (p. ej. para esconder el campamento); respeta caminos y marcadores.
        public ZoneBuilder Forest(Vector2Int center, float rx, float ry, float density, params string[] kinds)
        {
            var used = new HashSet<Vector2Int>();
            for (int y = Mathf.FloorToInt(-ry); y <= ry; y++)
                for (int x = Mathf.FloorToInt(-rx); x <= rx; x++)
                {
                    if ((x * x) / (rx * rx) + (y * y) / (ry * ry) > 1f) continue;
                    var c = center + new Vector2Int(x, y);
                    if (!AcceptsProp(c) || keepClear.Contains(c) || blocked.Contains(c)) continue;
                    if (HasNeighbor(used, c) || rng.NextDouble() > density) continue;
                    used.Add(c);
                    Prop(kinds[rng.Next(kinds.Length)], c);
                }
            return this;
        }

        // ------------------------------------------------------------ validación

        // Celda transitable con holgura: forma parte de algún bloque 2x2 de celdas libres.
        // Así se detectan pasillos de 1 celda (en escalera) donde el jugador se atora.
        private bool Walkable(Vector2Int c) => IsLand(c) && !blocked.Contains(c);

        private bool Roomy(Vector2Int c)
        {
            if (!Walkable(c)) return false;
            for (int dy = -1; dy <= 0; dy++)
                for (int dx = -1; dx <= 0; dx++)
                {
                    bool ok = true;
                    for (int y = 0; y <= 1 && ok; y++)
                        for (int x = 0; x <= 1 && ok; x++)
                            ok = Walkable(c + new Vector2Int(dx + x, dy + y));
                    if (ok) return true;
                }
            return false;
        }

        // Devuelve los marcadores objetivo a los que NO se llega caminando desde los orígenes.
        public List<string> Unreachable(IEnumerable<string> sources, IEnumerable<string> targets)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var s in sources)
            {
                if (!markers.TryGetValue(s, out var start)) continue;
                foreach (var n in Around(start))
                    if (Roomy(n) && visited.Add(n)) queue.Enqueue(n);
            }
            var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (Roomy(n) && visited.Add(n)) queue.Enqueue(n);
                }
            }

            var missing = new List<string>();
            foreach (var t in targets)
            {
                if (!markers.TryGetValue(t, out var cell)) continue;
                bool reached = false;
                foreach (var n in Around(cell)) reached |= visited.Contains(n);
                if (!reached) missing.Add(t);
            }
            return missing;
        }

        private static IEnumerable<Vector2Int> Around(Vector2Int c)
        {
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    yield return c + new Vector2Int(x, y);
        }

        public ZoneBuilder Bridge(Vector2Int from, Vector2Int direction, int length, int width = 2)
        {
            var side = new Vector2Int(-direction.y, direction.x);
            for (int i = 0; i < length; i++)
                for (int w = 0; w < width; w++)
                {
                    var c = from + direction * i + side * w;
                    Set(c, Ground.Bridge);
                    keepClear.Add(c);
                }
            return this;
        }

        // Bloque elevado (obstáculo) sobre la celda de suelo indicada.
        public ZoneBuilder Raise(Vector2Int cell, TileBase tile)
        {
            if (!IsLand(cell)) Set(cell, Ground.Grass);
            elevation[cell + Vector2Int.one] = tile;
            blocked.Add(cell);
            return this;
        }

        public ZoneBuilder RaiseRect(Vector2Int min, Vector2Int max, TileBase tile)
        {
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                    Raise(new Vector2Int(x, y), tile);
            return this;
        }

        // Copia los tilemaps de un Grid de referencia (p. ej. la isla de Omar).
        public ZoneBuilder Stamp(GameObject gridPrefab, Vector2Int at)
        {
            foreach (var tm in gridPrefab.GetComponentsInChildren<Tilemap>(true))
            {
                bool isElevation = tm.name.Contains("Elevation");
                bool isGround = tm.name.Contains("Overworld");
                if (!isElevation && !isGround) continue;

                // El prefab se centró con un desplazamiento de celda completa en el transform.
                var interp = gridPrefab.GetComponent<Grid>().LocalToCellInterpolated(tm.transform.localPosition);
                var shift = new Vector2Int(Mathf.RoundToInt(interp.x), Mathf.RoundToInt(interp.y));
                foreach (var p in tm.cellBounds.allPositionsWithin)
                {
                    var tile = tm.GetTile(p);
                    if (tile == null) continue;
                    var c = new Vector2Int(p.x + shift.x, p.y + shift.y) + at;
                    if (isGround)
                    {
                        ground[c] = Ground.Grass;
                        groundOverride[c] = tile;
                    }
                    else
                    {
                        elevation[c] = tile;
                        var below = c - Vector2Int.one;
                        if (!IsLand(below)) ground[below] = Ground.Grass;
                        blocked.Add(below);
                    }
                }
            }
            return this;
        }

        public ZoneBuilder Walls(Vector2Int min, Vector2Int max, bool stone)
        {
            walls.Add((min, max, stone));
            return this;
        }

        // ------------------------------------------------------------ marcadores y props

        public ZoneBuilder Marker(string name, Vector2Int cell, int clearRadius = 1)
        {
            markers[name] = cell;
            if (!IsLand(cell)) Set(cell, Ground.Grass);
            Clear(cell, clearRadius);
            return this;
        }

        public ZoneBuilder Clear(Vector2Int center, int radius)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    keepClear.Add(center + new Vector2Int(x, y));
            return this;
        }

        public ZoneBuilder Prop(string sprite, Vector2Int cell, bool blocks = true, string name = null, Vector2Int[] footprint = null, int sortingOrder = 0, Vector2 offset = default)
        {
            props.Add(new PropSpec { sprite = sprite, cell = cell, blocks = blocks, footprint = footprint, name = name, sortingOrder = sortingOrder, offset = offset });
            if (blocks)
            {
                if (footprint == null) blocked.Add(cell);
                else foreach (var f in footprint) blocked.Add(cell + f);
            }
            keepClear.Add(cell);
            if (footprint != null) foreach (var f in footprint) keepClear.Add(cell + f);
            return this;
        }

        // Árboles en el borde de la isla (dejan libres caminos, puentes y marcadores).
        public ZoneBuilder TreeRing(float density, params string[] kinds)
        {
            var cells = new List<Vector2Int>(ground.Keys);
            cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            var used = new HashSet<Vector2Int>();
            foreach (var c in cells)
            {
                if (!AcceptsProp(c) || keepClear.Contains(c) || blocked.Contains(c)) continue;
                if (DistanceToVoid(c) > 2) continue;
                // Borde frontal (hacia la cámara): sin árboles para no tapar al jugador
                if (IsFrontEdge(c)) continue;
                if (rng.NextDouble() > density) continue;
                if (HasNeighbor(used, c)) continue;
                used.Add(c);
                Prop(kinds[rng.Next(kinds.Length)], c);
            }
            return this;
        }

        // Props sueltos en celdas libres, separados entre sí.
        public ZoneBuilder Scatter(int count, bool blocks, params string[] kinds)
        {
            var free = new List<Vector2Int>();
            foreach (var c in ground.Keys)
                if (AcceptsProp(c) && !keepClear.Contains(c) && !blocked.Contains(c) && DistanceToVoid(c) > 1)
                    free.Add(c);
            free.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            var placed = new HashSet<Vector2Int>();
            for (int i = 0; i < count && free.Count > 0; i++)
            {
                var c = free[rng.Next(free.Count)];
                free.Remove(c);
                if (HasNeighbor(placed, c, 2)) { i--; if (free.Count == 0) break; continue; }
                placed.Add(c);
                Prop(kinds[rng.Next(kinds.Length)], c, blocks);
            }
            return this;
        }

        // ------------------------------------------------------------ consultas

        public bool IsLand(Vector2Int c) => ground.TryGetValue(c, out var g) && g != Ground.Void;

        // Pasto liso de tileset_grass donde se ven bien árboles y rocas.
        private static readonly HashSet<string> PlainGrass = new HashSet<string>
            { "tileset_grass_18", "tileset_grass_19", "tileset_grass_20", "tileset_grass_34" };

        // Celdas donde puede ir un prop: nada sobre arena, caminos, puentes ni plazas copiadas.
        private bool AcceptsProp(Vector2Int c)
        {
            if (!ground.TryGetValue(c, out var g)) return false;
            if (g == Ground.Sand || g == Ground.Bridge || g == Ground.Void || g == Ground.Rune || g == Ground.Wood || g == Ground.Stone) return false;
            if (groundOverride.TryGetValue(c, out var tile)) return tile != null && PlainGrass.Contains(tile.name);
            return true;
        }

        public void Set(Vector2Int c, Ground kind)
        {
            ground[c] = kind;
            groundOverride.Remove(c);
        }

        public static Vector3 SurfaceWorld(Vector2Int cell, Grid grid)
        {
            var p = grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0)) + SurfaceOffset;
            p.z = 0f; // el centro de celda trae z = 0.5 por el tamaño de celda en z
            return p;
        }

        private bool IsFrontEdge(Vector2Int c)
        {
            for (int i = 1; i <= 3; i++)
                if (!IsLand(c - new Vector2Int(i, 0)) || !IsLand(c - new Vector2Int(0, i)) || !IsLand(c - new Vector2Int(i, i)))
                    return true;
            return false;
        }

        private int DistanceToVoid(Vector2Int c)
        {
            for (int r = 1; r <= 3; r++)
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) == r && !IsLand(c + new Vector2Int(x, y)))
                            return r;
            return 4;
        }

        private static bool HasNeighbor(HashSet<Vector2Int> set, Vector2Int c, int radius = 1)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if ((x != 0 || y != 0) && set.Contains(c + new Vector2Int(x, y))) return true;
            return false;
        }

        private static IEnumerable<Vector2Int> Line(Vector2Int a, Vector2Int b)
        {
            int x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y;
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                yield return new Vector2Int(x0, y0);
                if (x0 == x1 && y0 == y1) yield break;
                int e2 = 2 * err;
                // Pasos en "L" para que el camino quede continuo en isométrico
                if (e2 >= dy) { err += dy; x0 += sx; }
                else if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // ------------------------------------------------------------ construcción

        public GameObject Build(Transform parent = null)
        {
            var root = new GameObject("Grid_" + Name);
            if (parent != null) root.transform.SetParent(parent, false);
            var grid = root.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = CellSize;

            // Individual: el suelo mezcla texturas (pasto, flores, placeholders) y en modo Chunk
            // Unity las agrupa por textura, lo que rompe el orden isométrico entre tiles.
            var groundMap = CreateTilemap(root, "Ground", "Background", TilemapRenderer.Mode.Individual, Vector3.zero);
            groundMap.color = GroundTint;
            var elevMap = CreateTilemap(root, "Elevations", "Sprites", TilemapRenderer.Mode.Individual, new Vector3(0, 0, ElevationZ));
            elevMap.color = GroundTint;
            var collisionMap = CreateTilemap(root, "Collisions", "Background", TilemapRenderer.Mode.Chunk, SurfaceOffset);
            SetupCollision(collisionMap);

            foreach (var pair in ground)
            {
                if (pair.Value == Ground.Void) continue;
                var tile = groundOverride.TryGetValue(pair.Key, out var o) ? o : TileFor(pair.Value);
                groundMap.SetTile(new Vector3Int(pair.Key.x, pair.Key.y, 0), tile);
            }

            foreach (var pair in elevation)
                elevMap.SetTile(new Vector3Int(pair.Key.x, pair.Key.y, 0), pair.Value);

            // Colisiones: borde del vacío y celdas bloqueadas
            var collisionCells = new HashSet<Vector2Int>(blocked);
            foreach (var c in ground.Keys)
            {
                if (!IsLand(c)) continue;
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                    {
                        var n = c + new Vector2Int(x, y);
                        if (!IsLand(n)) collisionCells.Add(n);
                    }
            }
            var collisionTile = ChapterTiles.Collision;
            foreach (var c in collisionCells)
                collisionMap.SetTile(new Vector3Int(c.x, c.y, 0), collisionTile);
            // Genera la geometría ya en el editor para que se guarde con la escena
            collisionMap.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            collisionMap.GetComponent<CompositeCollider2D>().GenerateGeometry();

            BuildWalls(root.transform, grid);
            BuildProps(root.transform, grid);
            BuildMarkers(root.transform, grid);
            return root;
        }

        private TileBase TileFor(Ground kind)
        {
            switch (kind)
            {
                case Ground.Flower: return ChapterTiles.RandomFlower(rng);
                case Ground.Sand: return ChapterTiles.Sand;
                case Ground.Bridge: return ChapterTiles.Bridge;
                case Ground.Stone: return rng.NextDouble() < 0.1 ? ChapterTiles.Ruin : ChapterTiles.Stone;
                case Ground.Rune: return ChapterTiles.StoneRune;
                case Ground.Wood: return ChapterTiles.Wood;
                case Ground.Ruin: return rng.NextDouble() < 0.12 ? ChapterTiles.Ruin : ChapterTiles.RandomGrass(rng);
                default: return ChapterTiles.RandomGrass(rng);
            }
        }

        private static Tilemap CreateTilemap(GameObject gridGo, string name, string sortingLayer, TilemapRenderer.Mode mode, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(gridGo.transform, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<Tilemap>();
            tm.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingLayerName = sortingLayer;
            r.mode = mode;
            // Igual que la escena de Omar: los tiles del fondo se dibujan primero en isométrico
            r.sortOrder = TilemapRenderer.SortOrder.TopRight;
            return tm;
        }

        private static void SetupCollision(Tilemap map)
        {
            var go = map.gameObject;
            go.GetComponent<TilemapRenderer>().enabled = false;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var tc = go.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var comp = go.AddComponent<CompositeCollider2D>();
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        private void BuildProps(Transform parent, Grid grid)
        {
            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);
            foreach (var p in props)
            {
                var sprite = PlaceholderArtGenerator.Load(p.sprite);
                var go = new GameObject(p.name ?? p.sprite.Replace("placeholder_", "") + $"_{p.cell.x}_{p.cell.y}");
                go.transform.SetParent(root, false);
                var pos = SurfaceWorld(p.cell, grid);
                if (p.footprint != null && p.footprint.Length > 0)
                {
                    // Centro visual del área ocupada (estructuras de varias celdas)
                    var sum = Vector3.zero;
                    foreach (var f in p.footprint) sum += SurfaceWorld(p.cell + f, grid);
                    pos = sum / p.footprint.Length;
                    pos.y -= 0.08f;
                }
                go.transform.position = pos + (Vector3)p.offset;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Sprites";
                sr.sortingOrder = p.sortingOrder;
                sr.spriteSortPoint = SpriteSortPoint.Pivot;
            }
        }

        private void BuildWalls(Transform parent, Grid grid)
        {
            if (walls.Count == 0) return;
            var root = new GameObject("Walls").transform;
            root.SetParent(parent, false);
            foreach (var (min, max, stone) in walls)
            {
                string mat = stone ? "stone" : "wood";
                // Borde +y (arriba-izquierda): muro izquierdo
                for (int x = min.x; x <= max.x; x++)
                    AddWall(root, grid, new Vector2Int(x, max.y), $"placeholder_wall_left_{mat}");
                // Borde +x (arriba-derecha): muro derecho
                for (int y = min.y; y <= max.y; y++)
                    AddWall(root, grid, new Vector2Int(max.x, y), $"placeholder_wall_right_{mat}");
            }
        }

        private static void AddWall(Transform root, Grid grid, Vector2Int cell, string sprite)
        {
            var go = new GameObject($"Wall_{cell.x}_{cell.y}");
            go.transform.SetParent(root, false);
            go.transform.position = SurfaceWorld(cell, grid);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load(sprite);
            sr.sortingLayerName = "Sprites";
            sr.sortingOrder = -10; // siempre detrás de lo que hay en la sala
        }

        private void BuildMarkers(Transform parent, Grid grid)
        {
            var root = new GameObject("Markers").transform;
            root.SetParent(parent, false);
            foreach (var pair in markers)
            {
                var go = new GameObject(pair.Key);
                go.transform.SetParent(root, false);
                go.transform.position = SurfaceWorld(pair.Value, grid);
            }
        }
    }
}
