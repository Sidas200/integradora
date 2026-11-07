using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Minimapa redondo (arriba a la derecha, bajo el objetivo) para quien juega por primera vez.
// - El mapa se dibuja una vez por escena a partir de los tilemaps de la zona (Ground y Elevations).
// - Tsuki es el punto del centro; el mapa se mueve con ella.
// - El objetivo actual (ChapterObjectives.Where) es un rombo que late; si queda fuera del mapa,
//   una flecha en el borde indica hacia dónde ir. Si el destino está en otra escena, se marca
//   la salida que lleva hacia allá.
// Lo crea GameHud dentro de su canvas (unidades de la resolución de referencia 1920x1080).
public class Minimap : MonoBehaviour
{
    private const float Size = 230f;
    private const float ViewRadius = 3.2f;       // unidades del mundo del centro al borde
    private const int PixelsPerUnit = 24;        // resolución del mapa dibujado
    private const float ArriveDistance = 0.3f;   // ya llegó: se deja de marcar el destino
    private const float RefreshTime = 0.5f;

    private static readonly Color Background = new Color(0.08f, 0.07f, 0.12f, 0.88f);
    private static readonly Color Gold = new Color(0.965f, 0.835f, 0.557f);
    private static readonly Color GoalColor = new Color(1f, 0.42f, 0.38f);

    private static readonly Color Grass = new Color(0.36f, 0.55f, 0.33f);
    private static readonly Color Flowers = new Color(0.52f, 0.6f, 0.4f);
    private static readonly Color Sand = new Color(0.78f, 0.68f, 0.47f);
    private static readonly Color Stone = new Color(0.56f, 0.56f, 0.6f);
    private static readonly Color Ruin = new Color(0.5f, 0.47f, 0.52f);
    private static readonly Color Wood = new Color(0.58f, 0.42f, 0.26f);

    private static Sprite circleSprite, ringSprite, arrowSprite, diamondSprite;

    private CanvasGroup group;
    private RawImage map;
    private Image goal;
    private Texture2D texture;
    private Vector2 worldMin;
    private Vector2 worldSize;

    private Transform player;
    private Transform target;
    private string targetName;
    private Transform cabinDoor, cabinInside;
    private bool visible = true;
    private float refreshTimer;
    private float pulse;

    public static Minimap Create(Transform parent)
    {
        var root = new GameObject("Minimap", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        // Bajo el panel de objetivo (que termina en y = -200)
        rt.anchoredPosition = new Vector2(-32f, -216f);
        rt.sizeDelta = new Vector2(Size, Size);

        var mm = root.AddComponent<Minimap>();
        mm.group = root.GetComponent<CanvasGroup>();
        mm.group.blocksRaycasts = false;
        mm.group.interactable = false;
        mm.group.alpha = 0f;

        // Fondo redondo que además recorta el mapa
        var mask = NewImage("Mask", rt, CircleSprite(), Background, Vector2.zero, Size - 8f);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        mm.map = new GameObject("Map", typeof(RectTransform)).AddComponent<RawImage>();
        var mapRt = mm.map.rectTransform;
        mapRt.SetParent(mask.rectTransform, false);
        mapRt.anchorMin = mapRt.anchorMax = new Vector2(0.5f, 0.5f);
        mapRt.pivot = Vector2.zero;
        mm.map.raycastTarget = false;
        mm.map.enabled = false;

        NewImage("Ring", rt, RingSprite(), Gold, Vector2.zero, Size);
        // Tsuki: punto dorado con borde oscuro
        NewImage("PlayerBorder", rt, CircleSprite(), new Color(0.1f, 0.06f, 0.1f), Vector2.zero, 18f);
        NewImage("Player", rt, CircleSprite(), Gold, Vector2.zero, 12f);
        mm.goal = NewImage("Goal", rt, DiamondSprite(), GoalColor, Vector2.zero, 22f);
        mm.goal.enabled = false;
        return mm;
    }

    public void Bind(GameObject playerObject)
    {
        player = playerObject != null ? playerObject.transform : null;
        target = null;
        targetName = null;
        cabinDoor = cabinInside = null;
        refreshTimer = 0f;
        BuildMap();
    }

    public void SetVisible(bool value) => visible = value;

    private void OnDestroy()
    {
        if (texture != null) Destroy(texture);
    }

    private void LateUpdate()
    {
        bool show = visible && player != null && texture != null;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        if (!show) return;

        float uiPerUnit = (Size * 0.5f - 6f) / ViewRadius;
        Vector2 here = player.position;
        var mapRt = map.rectTransform;
        mapRt.sizeDelta = worldSize * uiPerUnit;
        mapRt.anchoredPosition = (worldMin - here) * uiPerUnit;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = RefreshTime;
            target = ResolveTarget();
        }

        bool hasGoal = target != null && Vector2.Distance(here, target.position) > ArriveDistance;
        goal.enabled = hasGoal;
        if (!hasGoal) return;

        pulse += Time.unscaledDeltaTime;
        Vector2 offset = ((Vector2)target.position - here) * uiPerUnit;
        float edge = Size * 0.5f - 16f;
        var grt = goal.rectTransform;
        if (offset.magnitude > edge)
        {
            // Fuera del mapa: flecha en el borde apuntando hacia el destino
            goal.sprite = ArrowSprite();
            grt.sizeDelta = Vector2.one * 26f;
            grt.anchoredPosition = offset.normalized * edge;
            grt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90f);
        }
        else
        {
            // Dentro: rombo que late sobre el destino
            goal.sprite = DiamondSprite();
            grt.sizeDelta = Vector2.one * (20f + 4f * Mathf.Sin(pulse * 5f));
            grt.anchoredPosition = offset;
            grt.localRotation = Quaternion.identity;
        }
    }

    // ---------------------------------------------------------------- mapa

    // Dibuja la zona vista desde arriba: cada pixel toma el color del tile que hay debajo
    private void BuildMap()
    {
        if (texture != null) Destroy(texture);
        texture = null;
        map.enabled = false;
        if (player == null) return;

        Tilemap ground = null, elevations = null;
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.gameObject.scene != SceneManager.GetActiveScene()) continue;
            if (tm.name == "Ground") ground = tm;
            else if (tm.name == "Elevations") elevations = tm;
        }
        if (ground == null) return;

        ground.CompressBounds();
        var cells = ground.cellBounds;
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (var c in new[]
        {
            new Vector3Int(cells.xMin, cells.yMin, 0), new Vector3Int(cells.xMax, cells.yMin, 0),
            new Vector3Int(cells.xMin, cells.yMax, 0), new Vector3Int(cells.xMax, cells.yMax, 0),
        })
        {
            Vector2 w = ground.CellToWorld(c);
            min = Vector2.Min(min, w);
            max = Vector2.Max(max, w);
        }
        min -= Vector2.one * 0.3f;
        max += Vector2.one * 0.3f;

        int width = Mathf.Clamp(Mathf.CeilToInt((max.x - min.x) * PixelsPerUnit), 1, 2048);
        int height = Mathf.Clamp(Mathf.CeilToInt((max.y - min.y) * PixelsPerUnit), 1, 2048);
        var colors = new Color32[width * height];
        var solid = new bool[width * height];
        var cache = new Dictionary<TileBase, Color>();

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var world = new Vector3(min.x + (x + 0.5f) / PixelsPerUnit, min.y + (y + 0.5f) / PixelsPerUnit, 0f);
            var tile = ground.GetTile(ground.WorldToCell(world));
            if (tile == null) continue;
            if (!cache.TryGetValue(tile, out var color)) cache[tile] = color = ColorOf(tile.name);
            // Las elevaciones van desplazadas en z: se compensa para leer su celda
            if (elevations != null && elevations.HasTile(elevations.WorldToCell(new Vector3(world.x, world.y, elevations.transform.position.z))))
                color *= 0.72f;
            color.a = 1f;
            colors[y * width + x] = color;
            solid[y * width + x] = true;
        }

        // Orilla oscura donde el suelo termina, para que se lea la forma de la zona
        var outlined = (Color32[])colors.Clone();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            if (!solid[i]) continue;
            bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1 ||
                        !solid[i - 1] || !solid[i + 1] || !solid[i - width] || !solid[i + width];
            if (edge) outlined[i] = Color.Lerp(colors[i], Color.black, 0.55f);
        }

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(outlined);
        texture.Apply();
        map.texture = texture;
        map.enabled = true;
        worldMin = min;
        worldSize = max - min;
    }

    // Tiles del capítulo (ChapterTiles): tileset_grass_7 es la arena y los placeholders tienen nombre
    private static Color ColorOf(string tile)
    {
        string n = tile.ToLowerInvariant();
        if (n.Contains("bridge") || n.Contains("wood")) return Wood;
        if (n.Contains("ruin")) return Ruin;
        if (n.Contains("stone") || n.Contains("rune")) return Stone;
        if (n == "tileset_grass_7" || n.Contains("sand")) return Sand;
        if (n.Contains("flower")) return Flowers;
        return Grass;
    }

    // ---------------------------------------------------------------- destino

    // Objeto a marcar en esta escena: el destino si está aquí, o la salida hacia su escena
    private Transform ResolveTarget()
    {
        var (scene, name) = ChapterObjectives.Where();
        if (string.IsNullOrEmpty(scene)) return null;

        string current = SceneManager.GetActiveScene().name;
        if (scene != current) name = ExitToward(current, scene);
        if (string.IsNullOrEmpty(name)) return null;

        if (current == ChapterObjectives.FlowerIsland) name = CabinWaypoint(name);
        if (name == targetName && target != null) return target;
        targetName = name;
        return Find(name);
    }

    // Salida de la escena actual que acerca a la escena destino
    private static string ExitToward(string from, string to)
    {
        switch (from)
        {
            case ChapterObjectives.FlowerIsland: return "Exit_Bridge";
            case ChapterObjectives.OpenField:
                if (to == ChapterObjectives.Sanctuary) return "Exit_ToSanctuary";
                if (to == ChapterObjectives.CombatZone || to == ChapterObjectives.GuardianArenaScene) return "Exit_ToCombat";
                return null;
            case ChapterObjectives.Sanctuary: return "Exit_ToField";
            case ChapterObjectives.CombatZone:
                return to == ChapterObjectives.GuardianArenaScene ? "Exit_ToArena" : "Exit_ToField";
            case ChapterObjectives.GuardianArenaScene: return "Exit_ToZone2";
            default: return null;
        }
    }

    // El interior de la cabaña está aparte en el mapa: se entra y se sale por sus puertas
    private string CabinWaypoint(string name)
    {
        if (cabinDoor == null) cabinDoor = Find("Door_Cabin");
        if (cabinInside == null) cabinInside = Find("Spawn_CabinInside");
        if (cabinDoor == null || cabinInside == null || player == null) return name;

        bool inside = Vector2.Distance(player.position, cabinInside.position) <
                      Vector2.Distance(player.position, cabinDoor.position);
        bool targetInside = name == "SwordRack";
        if (inside && !targetInside) return "Exit_CabinInside";
        if (!inside && targetInside) return "Door_Cabin";
        return name;
    }

    // Busca por nombre en la escena activa, incluidos objetos inactivos (p. ej. un enemigo vencido).
    // Un enemigo se llama igual que su marcador: se prefiere el objeto real y el marcador queda de respaldo.
    private static Transform Find(string name)
    {
        Transform marker = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                if (t.parent == null || t.parent.name != "Markers") return t;
                if (marker == null) marker = t;
            }
        return marker;
    }

    // ---------------------------------------------------------------- sprites

    private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color, Vector2 pos, float size)
    {
        var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        var rt = image.rectTransform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.one * size;
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite CircleSprite() => circleSprite != null ? circleSprite
        : circleSprite = Shape(128, (u, v) => Edge(1f - Mathf.Sqrt(u * u + v * v), 128));

    private static Sprite RingSprite() => ringSprite != null ? ringSprite
        : ringSprite = Shape(256, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            return Mathf.Min(Edge(1f - d, 256), Edge(d - 0.93f, 256));
        });

    private static Sprite DiamondSprite() => diamondSprite != null ? diamondSprite
        : diamondSprite = Shape(64, (u, v) => Edge(1f - (Mathf.Abs(u) + Mathf.Abs(v)), 64));

    // Flecha hacia arriba: punta triangular y cola
    private static Sprite ArrowSprite() => arrowSprite != null ? arrowSprite
        : arrowSprite = Shape(64, (u, v) =>
        {
            float y = (v + 1f) * 0.5f;
            float headHalf = Mathf.Lerp(0.85f, 0f, Mathf.InverseLerp(0.4f, 0.95f, y));
            bool head = y >= 0.4f && y <= 0.95f && Mathf.Abs(u) <= headHalf;
            bool tail = y >= 0.08f && y < 0.42f && Mathf.Abs(u) <= 0.3f;
            return head || tail ? 1f : 0f;
        });

    // Borde suave de un pixel: d es la distancia (en coordenadas -1..1) al borde, positiva adentro
    private static float Edge(float d, int size) => Mathf.Clamp01(d * size * 0.5f + 0.5f);

    private static Sprite Shape(int size, System.Func<float, float, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size * 2f - 1f;
            float v = (y + 0.5f) / size * 2f - 1f;
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(u, v)) * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
