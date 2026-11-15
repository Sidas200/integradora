using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Campo floreado: siembra flores animadas (se mecen con el viento) sobre el pasto de la escena.
// Se instala solo en las escenas de FieldScenes, sin tocar la escena: lee el Tilemap "Ground" y
// evita colisiones, props, decoración, bordes de la isla y los puntos de entrada/salida.
// Las flores forman manchas (ruido Perlin), cada mancha casi de un solo tipo, y se mueven más
// rápido un momento cuando Tsuki pasa entre ellas.
// El sprite "Environment/flowers" es una hoja de 5 filas (rosa, margarita, campanilla, amarilla,
// lila) con 8-9 cuadros cada una; el pivote de cada cuadro está en la base del tallo.
public class FlowerField : MonoBehaviour
{
    private const string SheetResource = "Environment/flowers";
    private const float SheetHeight = 793f;

    private static readonly HashSet<string> FieldScenes = new HashSet<string> { "01_FlowerIsland" };

    // Baldosas donde crecen: las que ya tienen flores pintadas (más densas) y el pasto liso
    private const string FlowerTilePrefix = "tileset_grass_flowers_";
    private static readonly HashSet<string> GrassTiles = new HashSet<string> { "tileset_grass_19", "tileset_grass_18", "tileset_grass_20" };

    [Header("Siembra")]
    [Tooltip("Probabilidad de flor en una baldosa que ya tiene flores pintadas.")]
    [SerializeField, Range(0f, 1f)] private float flowerTileChance = 0.95f;
    [Tooltip("Probabilidad máxima en pasto liso (dentro de una mancha).")]
    [SerializeField, Range(0f, 1f)] private float grassChance = 0.8f;
    [Tooltip("Tamaño de las manchas de flores (más chico = manchas más grandes).")]
    [SerializeField] private float patchScale = 0.25f;
    [SerializeField] private Vector2Int perCell = new Vector2Int(1, 3);
    [SerializeField] private Vector2 height = new Vector2(0.14f, 0.19f);
    [Tooltip("Distancia mínima a entradas, salidas y objetos con los que se interactúa.")]
    [SerializeField] private float clearRadius = 0.35f;

    [Header("Animación")]
    [Tooltip("Cuadros por segundo (mínimo, máximo): un vaivén completo dura unos 6 s, como brisa muy suave.")]
    [SerializeField] private Vector2 fps = new Vector2(1.2f, 1.8f);
    [Tooltip("Al pasar Tsuki cerca, la flor se agita más rápido un momento.")]
    [SerializeField] private float rustleRadius = 0.14f;
    [SerializeField] private float rustleSpeed = 1.8f;
    [SerializeField] private float rustleTime = 0.6f;

    private class Flower
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float phase;
        public float fps;
        public float rustle;
        public Vector2 pos;
    }

    private readonly List<Flower> flowers = new List<Flower>();
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!FieldScenes.Contains(scene.name)) return;
        var go = new GameObject("FlowerField");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<FlowerField>();
    }

    // x, y (desde abajo), ancho, alto en píxeles de la hoja original; pivote normalizado (base del tallo)
    private struct FrameDef { public float x, y, w, h, px, py; }
    private static FrameDef F(float x, float y, float w, float h, float px, float py) =>
        new FrameDef { x = x, y = y, w = w, h = h, px = px, py = py };

    private static readonly FrameDef[][] Sheet =
    {
        new[] {F(17, 645, 145, 140, 0.496f, 0.036f), F(219, 645, 153, 140, 0.397f, 0.036f), F(442, 645, 158, 140, 0.398f, 0.036f), F(672, 645, 182, 140, 0.413f, 0.036f), F(928, 645, 168, 140, 0.369f, 0.036f), F(1155, 645, 162, 140, 0.391f, 0.036f), F(1384, 645, 145, 140, 0.551f, 0.036f), F(1604, 645, 153, 140, 0.501f, 0.036f), F(1803, 645, 149, 140, 0.345f, 0.036f)},
        new[] {F(17, 481, 138, 140, 0.426f, 0.043f), F(219, 481, 144, 140, 0.423f, 0.043f), F(437, 481, 124, 140, 0.426f, 0.071f), F(645, 481, 140, 140, 0.375f, 0.043f), F(875, 481, 142, 140, 0.43f, 0.036f), F(1106, 481, 140, 140, 0.494f, 0.036f), F(1332, 481, 156, 140, 0.523f, 0.036f), F(1574, 481, 132, 140, 0.508f, 0.064f), F(1803, 481, 140, 140, 0.426f, 0.036f)},
        new[] {F(18, 347, 163, 133, 0.315f, 0.083f), F(235, 347, 159, 133, 0.343f, 0.045f), F(452, 347, 175, 133, 0.289f, 0.068f), F(715, 347, 169, 133, 0.292f, 0.045f), F(982, 347, 170, 133, 0.35f, 0.045f), F(1238, 347, 154, 133, 0.452f, 0.045f), F(1479, 347, 167, 133, 0.296f, 0.045f), F(1743, 347, 168, 133, 0.341f, 0.038f)},
        new[] {F(17, 191, 165, 146, 0.415f, 0.068f), F(235, 191, 195, 146, 0.454f, 0.034f), F(511, 191, 155, 146, 0.377f, 0.034f), F(742, 191, 177, 146, 0.304f, 0.034f), F(960, 191, 180, 146, 0.316f, 0.034f), F(1190, 191, 163, 146, 0.309f, 0.034f), F(1392, 191, 162, 146, 0.429f, 0.034f), F(1593, 191, 157, 146, 0.379f, 0.034f), F(1803, 191, 164, 146, 0.381f, 0.034f)},
        new[] {F(17, 17, 162, 157, 0.568f, 0.032f), F(235, 17, 203, 157, 0.444f, 0.032f), F(488, 17, 215, 157, 0.414f, 0.038f), F(752, 17, 209, 157, 0.428f, 0.064f), F(1010, 17, 227, 157, 0.349f, 0.038f), F(1273, 17, 214, 157, 0.516f, 0.064f), F(1531, 17, 168, 157, 0.49f, 0.057f), F(1748, 17, 214, 157, 0.396f, 0.064f)},
    };

    // Alto de referencia de un cuadro (px) para escalar todas las flores parecido
    private const float FrameHeight = 140f;

    private static Sprite[][] LoadSheet()
    {
        var tex = Resources.Load<Texture2D>(SheetResource);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        float k = tex.height / SheetHeight; // por si se importa a otro tamaño
        var result = new Sprite[Sheet.Length][];
        for (int r = 0; r < Sheet.Length; r++)
        {
            result[r] = new Sprite[Sheet[r].Length];
            for (int i = 0; i < Sheet[r].Length; i++)
            {
                var f = Sheet[r][i];
                var s = Sprite.Create(tex, new Rect(f.x * k, f.y * k, f.w * k, f.h * k), new Vector2(f.px, f.py), 100f * k);
                s.name = $"flower_{r}_{i}";
                result[r][i] = s;
            }
        }
        return result;
    }

    private void Start()
    {
        var sheet = LoadSheet();
        if (sheet == null) { enabled = false; return; }

        Tilemap ground = null, collisions = null, props = null, decoration = null, elevations = null;
        foreach (var map in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
        {
            if (map.gameObject.scene != gameObject.scene) continue;
            switch (map.name)
            {
                case "Ground": ground = map; break;
                case "Collisions": collisions = map; break;
                case "Props": props = map; break;
                case "Decoration": decoration = map; break;
                case "Elevations": elevations = map; break;
            }
        }
        if (ground == null) { enabled = false; return; }

        // Puntos a dejar libres: entradas/salidas, objetos con los que se interactúa y props sueltos
        var keepClear = new List<Vector2>();
        var keepClearSmall = new List<Vector2>();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.gameObject.scene != gameObject.scene) continue;
            if (t.name.StartsWith("Spawn_") || t.name.StartsWith("Exit_") || t.GetComponent<Interactable>() != null)
                keepClear.Add(t.position);
            else if (t.parent != null && t.parent.name == "Props" && t.GetComponent<SpriteRenderer>() != null)
                keepClearSmall.Add(t.position);
        }
        var sword = GameObject.Find("sword_in_stone");
        if (sword != null) keepClear.Add(sword.transform.position);

        // Mismo material que los props (iluminado por las luces 2D de la escena)
        Material material = null;
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
            if (sr.transform.parent != null && sr.transform.parent.name == "Props") { material = sr.sharedMaterial; break; }

        var rng = new System.Random(gameObject.scene.name.GetHashCode());
        float seedX = (float)rng.NextDouble() * 100f, seedY = (float)rng.NextDouble() * 100f;
        var root = transform;
        var b = ground.cellBounds;

        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var cell = new Vector2Int(x, y);
                string top = TopTile(ground, cell, out int z);
                if (top == null || z != 0) continue;

                bool flowerTile = top.StartsWith(FlowerTilePrefix);
                if (!flowerTile && !GrassTiles.Contains(top)) continue;
                if (HasAny(collisions, cell) || HasAny(props, cell) || HasAny(decoration, cell) || HasAny(elevations, cell)) continue;
                // Bordes de la isla: todas las vecinas deben tener suelo
                if (TopTile(ground, cell + Vector2Int.up, out _) == null || TopTile(ground, cell + Vector2Int.down, out _) == null ||
                    TopTile(ground, cell + Vector2Int.left, out _) == null || TopTile(ground, cell + Vector2Int.right, out _) == null) continue;

                float noise = Mathf.PerlinNoise(seedX + x * patchScale, seedY + y * patchScale);
                float chance = flowerTile ? flowerTileChance : grassChance * Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0f, 1f, (noise - 0.35f) / 0.3f));
                if (rng.NextDouble() >= chance) continue;

                Vector3 center = ground.GetCellCenterWorld(new Vector3Int(x, y, 0));
                if (Near(center, keepClear, clearRadius) || Near(center, keepClearSmall, 0.18f)) continue;

                // Tipo según la mancha (otro ruido), con algo de mezcla
                float typeNoise = Mathf.PerlinNoise(seedY + x * patchScale * 0.7f, seedX + y * patchScale * 0.7f);
                int baseType = Mathf.Clamp((int)(typeNoise * Sheet.Length * 1.3f - 0.15f * Sheet.Length), 0, Sheet.Length - 1);

                int count = rng.Next(perCell.x, perCell.y + 1);
                for (int i = 0; i < count; i++)
                {
                    int type = rng.NextDouble() < 0.8 ? baseType : rng.Next(Sheet.Length);
                    // Dentro del rombo de la baldosa (0.32 x 0.16), un poco hacia adentro
                    float u = (float)rng.NextDouble() - 0.5f, v = (float)rng.NextDouble() - 0.5f;
                    Vector2 offset = new Vector2((u - v) * 0.5f * ground.cellSize.x, (u + v) * 0.5f * ground.cellSize.y) * 0.8f;
                    Vector2 pos = (Vector2)center + offset;
                    flowers.Add(NewFlower(root, sheet[type], pos, material, rng));
                }
            }
    }

    private Flower NewFlower(Transform root, Sprite[] frames, Vector2 pos, Material material, System.Random rng)
    {
        var go = new GameObject("Flower");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        float h = Mathf.Lerp(height.x, height.y, (float)rng.NextDouble());
        float scale = h / (FrameHeight / 100f);
        go.transform.localScale = new Vector3(rng.NextDouble() < 0.5 ? -scale : scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sortingLayerName = "Sprites";
        sr.spriteSortPoint = SpriteSortPoint.Pivot; // Tsuki pasa delante o detrás según su altura
        var f = new Flower
        {
            sr = sr,
            frames = frames,
            phase = (float)rng.NextDouble() * frames.Length,
            fps = Mathf.Lerp(fps.x, fps.y, (float)rng.NextDouble()),
            pos = pos,
        };
        sr.sprite = frames[(int)f.phase % frames.Length];
        return f;
    }

    private void Update()
    {
        if (player == null)
        {
            var go = SceneContext.FindPlayer();
            if (go != null) player = go.transform;
        }
        Vector2 p = player != null ? (Vector2)player.position : new Vector2(float.MaxValue, float.MaxValue);
        float r2 = rustleRadius * rustleRadius;
        float dt = Time.deltaTime;

        for (int i = 0; i < flowers.Count; i++)
        {
            var f = flowers[i];
            if ((f.pos - p).sqrMagnitude < r2) f.rustle = rustleTime;
            float speed = f.fps;
            if (f.rustle > 0f)
            {
                f.rustle -= dt;
                speed *= Mathf.Lerp(1f, rustleSpeed, f.rustle / rustleTime);
            }
            f.phase = (f.phase + speed * dt) % f.frames.Length;
            var s = f.frames[(int)f.phase];
            if (f.sr.sprite != s) f.sr.sprite = s;
        }
    }

    private static string TopTile(Tilemap map, Vector2Int c, out int z)
    {
        for (z = map.cellBounds.zMax - 1; z >= map.cellBounds.zMin; z--)
        {
            var t = map.GetTile(new Vector3Int(c.x, c.y, z));
            if (t != null) return t.name;
        }
        z = 0;
        return null;
    }

    private static bool HasAny(Tilemap map, Vector2Int c)
    {
        if (map == null) return false;
        for (int z = map.cellBounds.zMin; z < map.cellBounds.zMax; z++)
            if (map.HasTile(new Vector3Int(c.x, c.y, z))) return true;
        return false;
    }

    private static bool Near(Vector2 p, List<Vector2> points, float radius)
    {
        float r2 = radius * radius;
        foreach (var q in points)
            if ((p - q).sqrMagnitude < r2) return true;
        return false;
    }
}
