using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

// Al cruzar el puente, la isla inicial se desvanece detrás de Tsuki (todo menos el puente).
[RequireComponent(typeof(Collider2D))]
public class IslandVanish : MonoBehaviour
{
    public const string Flag = "island_left";

    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap elevations;
    [SerializeField] private Transform props;
    [SerializeField] private TileBase keepTile; // el puente
    [SerializeField] private DialogueData dialogue;
    [SerializeField] private float duration = 2.2f;
    [Tooltip("Collider que se activa al final para no volver a la isla que ya no existe.")]
    [SerializeField] private GameObject backBlocker;
    [Tooltip("Otros objetos de la isla que también desaparecen (marcas de salida de la cabaña).")]
    [SerializeField] private GameObject[] alsoHide;
    [Tooltip("Tilemap de colisiones: las celdas de la isla se vuelven vacío infranqueable al desaparecer.")]
    [SerializeField] private Tilemap collisions;
    [SerializeField] private TileBase collisionTile;
    [Tooltip("Dónde se coloca a Tsuki si al terminar no está sobre el puente.")]
    [SerializeField] private Transform safeSpot;

    private bool done;

    public void Setup(Tilemap groundMap, Tilemap elevationMap, Transform propsRoot, TileBase bridge, DialogueData data, GameObject blocker, GameObject[] hide)
    {
        alsoHide = hide;
        backBlocker = blocker;
        if (backBlocker != null) backBlocker.SetActive(false);
        ground = groundMap;
        elevations = elevationMap;
        props = propsRoot;
        keepTile = bridge;
        dialogue = data;
    }

    public void SetCollisions(Tilemap collisionMap, TileBase tile, Transform spot)
    {
        collisions = collisionMap;
        collisionTile = tile;
        safeSpot = spot;
    }

    [Tooltip("Qué tanto se aleja Tsuki del borde de abajo del puente (fracción de celda). Arriba choca antes con la cabeza del colisionador.")]
    [SerializeField, Range(0f, 0.45f)] private float bridgeMargin = 0.22f;
    [Tooltip("Cuánto más arriba se dibuja el puente que su celda (unidades del mundo): el sprite trae la tabla en la mitad de arriba.")]
    [SerializeField] private float bridgeVisualLift = 0.065f;

    // Arriba el barandal va un poco más allá del dibujo: lo que choca ahí es la parte alta del
    // colisionador (los pies quedan ~media celda antes), así los pies llegan cerca del borde de arriba.
    private const float UpperReach = 0.2f;

    private GameObject rails;

    private void Start()
    {
        BlockIslandEdge();
        ClearBridge();
        UnblockBridgeOverhang();
        BuildRails();
    }

    // Desfase del dibujo del puente expresado en celdas (hacia arriba en pantalla = +x y +y en isométrico)
    private Vector2 VisualShift()
    {
        Vector3 a = ground.LocalToCellInterpolated(Vector3.zero);
        Vector3 b = ground.LocalToCellInterpolated(new Vector3(0f, bridgeVisualLift, 0f));
        return new Vector2(b.x - a.x, b.y - a.y);
    }

    // Las celdas vacías del lado de arriba del puente se ven como parte de la tabla (por el desfase del
    // dibujo); si quedan bloqueadas, Tsuki no puede acercarse a ese borde. Ahí manda el barandal.
    private void UnblockBridgeOverhang()
    {
        var blocking = BlockingMap();
        if (keepTile == null || blocking == null) return;
        foreach (var p in ground.cellBounds.allPositionsWithin)
        {
            if (ground.GetTile(p) != keepTile) continue;
            foreach (var d in new[] { Vector3Int.right, Vector3Int.up, new Vector3Int(1, 1, 0) })
            {
                var n = p + d;
                if (ground.HasTile(n)) continue;
                var cell = blocking.WorldToCell(ground.GetCellCenterWorld(n));
                if (blocking.HasTile(cell)) blocking.SetTile(cell, null);
            }
        }
    }

    // Barandales invisibles en los bordes del puente que dan al vacío, alineados con el dibujo de la
    // tabla (no con la celda). El colisionador de Tsuki está en sus pies: abajo se deja un margen para
    // que no se pare en la orilla; arriba no hace falta porque el colisionador choca primero.
    private void BuildRails()
    {
        if (keepTile == null || ground == null) return;
        if (rails != null) Destroy(rails);
        rails = new GameObject("BridgeRails");
        rails.transform.SetParent(ground.transform, false);
        var blocking = BlockingMap();
        rails.layer = blocking != null ? blocking.gameObject.layer : ground.gameObject.layer;
        var shift = VisualShift();
        float m = bridgeMargin;
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (var p in ground.cellBounds.allPositionsWithin)
        {
            if (ground.GetTile(p) != keepTile) continue;
            foreach (var d in dirs)
            {
                var n = p + new Vector3Int(d.x, d.y, 0);
                if (ground.HasTile(n)) continue; // sigue el puente o la isla: abierto
                Vector2 a, b;
                if (d.x != 0)
                {
                    float x = d.x > 0 ? 1f + UpperReach : m;
                    a = new Vector2(x, -0.05f); b = new Vector2(x, 1.05f);
                }
                else
                {
                    float y = d.y > 0 ? 1f + UpperReach : m;
                    a = new Vector2(-0.05f, y); b = new Vector2(1.05f, y);
                }
                var edge = rails.AddComponent<EdgeCollider2D>();
                edge.points = new[] { CellPoint(p, a + shift), CellPoint(p, b + shift) };
            }
        }
    }

    // Punto dentro de la celda (0..1 en cada eje) en coordenadas locales del Tilemap
    private Vector2 CellPoint(Vector3Int cell, Vector2 f) =>
        ground.CellToLocalInterpolated(new Vector3(cell.x + f.x, cell.y + f.y, 0f));

    // El puente siempre se puede cruzar: se borran las colisiones pintadas encima de sus celdas
    // (al repintar la isla quedaron celdas bloqueadas sobre el puente y Tsuki se atoraba en un carril de 1 celda).
    private void ClearBridge()
    {
        if (keepTile == null) return;
        var maps = ground.layoutGrid.GetComponentsInChildren<TilemapCollider2D>(true);
        foreach (var p in ground.cellBounds.allPositionsWithin)
        {
            if (ground.GetTile(p) != keepTile) continue;
            foreach (var map in maps)
            {
                var tilemap = map.GetComponent<Tilemap>();
                // Las capas se alinean por posición en el mundo (Collisions puede tener otro desfase que Ground)
                var cell = tilemap.WorldToCell(ground.GetCellCenterWorld(p));
                if (tilemap.HasTile(cell)) tilemap.SetTile(cell, null);
            }
        }
    }

    // La isla no tiene muro: cada celda vacía que toca el suelo se vuelve infranqueable, así no se camina
    // por el cielo aunque cambie la forma del suelo. Las capas de colisiones no se dibujan (se verían en el vacío).
    private void BlockIslandEdge()
    {
        foreach (var map in ground.layoutGrid.GetComponentsInChildren<TilemapCollider2D>())
            if (map.TryGetComponent<TilemapRenderer>(out var r)) r.enabled = false;

        var blocking = BlockingMap();
        if (blocking == null || collisionTile == null) return;
        var bounds = ground.cellBounds;
        for (int x = bounds.xMin - 1; x <= bounds.xMax; x++)
            for (int y = bounds.yMin - 1; y <= bounds.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (ground.HasTile(cell) || !TouchesGround(cell)) continue;
                blocking.SetTile(cell, collisionTile);
            }
    }

    private bool TouchesGround(Vector3Int cell)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if ((dx != 0 || dy != 0) && ground.HasTile(cell + new Vector3Int(dx, dy, 0))) return true;
        return false;
    }

    // La capa de colisiones activa: la asignada, o la que esté encendida en el Grid si esa se apagó.
    private Tilemap BlockingMap()
    {
        if (collisions != null && collisions.gameObject.activeInHierarchy) return collisions;
        foreach (var map in ground.layoutGrid.GetComponentsInChildren<TilemapCollider2D>())
            return map.GetComponent<Tilemap>();
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Con la espada y el entrenamiento terminado (el puente también está cerrado hasta entonces)
        if (done || !StoryFlags.Has(SwordPickup.Flag) || !StoryFlags.Has(CombatTutorial.DoneFlag)) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        done = true;
        StartCoroutine(Vanish());
    }

    private IEnumerator Vanish()
    {
        GameInput.Lock(this);
        var cells = new System.Collections.Generic.List<Vector3Int>();
        foreach (var p in ground.cellBounds.allPositionsWithin)
        {
            var tile = ground.GetTile(p);
            if (tile == null || tile == keepTile) continue;
            ground.SetTileFlags(p, TileFlags.None);
            cells.Add(p);
        }
        var renderers = new System.Collections.Generic.List<SpriteRenderer>();
        if (props != null) renderers.AddRange(props.GetComponentsInChildren<SpriteRenderer>());
        if (alsoHide != null)
            foreach (var go in alsoHide)
                if (go != null) go.SetActive(false); // las marcas desaparecen al instante

        // Campo de flores animadas (FlowerField se crea al cargar la escena, fuera del Grid)
        var flowers = FindAnyObjectByType<FlowerField>();
        if (flowers != null) renderers.AddRange(flowers.GetComponentsInChildren<SpriteRenderer>());

        // Capas pintadas a mano (Decoration, Props con árboles y rocas): se desvanecen con la isla.
        // Las de colisiones ya están ocultas desde Start (BlockIslandEdge).
        var layers = new System.Collections.Generic.List<Tilemap>();
        var blocking = BlockingMap();
        foreach (var map in ground.layoutGrid.GetComponentsInChildren<Tilemap>())
        {
            if (map == ground || map == elevations || map.GetComponent<TilemapCollider2D>() != null) continue;
            layers.Add(map);
        }
        // Objetos sueltos de la isla en el Grid (cabaña, muros); Markers no, porque ahí está el puente.
        var objects = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in ground.layoutGrid.transform)
        {
            if (child.GetComponent<Tilemap>() != null || child == props || transform.IsChildOf(child)) continue;
            objects.Add(child.gameObject);
            renderers.AddRange(child.GetComponentsInChildren<SpriteRenderer>());
        }

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float a = 1f - t / duration;
            var c = new Color(1f, 1f, 1f, a);
            foreach (var p in cells) ground.SetColor(p, c);
            if (elevations != null) elevations.color = c;
            foreach (var map in layers) map.color = c;
            foreach (var r in renderers) r.color = c;
            yield return null;
        }
        foreach (var p in cells) ground.SetTile(p, null);
        // La isla ya no existe: todas sus celdas bloquean, no solo la entrada del puente
        if (blocking != null && collisionTile != null)
            foreach (var p in cells) blocking.SetTile(p, collisionTile);
        KeepPlayerOnBridge();
        foreach (var map in layers) map.gameObject.SetActive(false);
        foreach (var go in objects) go.SetActive(false);
        if (elevations != null) elevations.gameObject.SetActive(false);
        if (props != null) props.gameObject.SetActive(false);
        if (flowers != null) flowers.gameObject.SetActive(false);
        UnblockBridgeOverhang();
        BuildRails(); // la isla ya no está: ahora el borde del lado de la isla también da al vacío
        if (backBlocker != null) backBlocker.SetActive(true);
        if (alsoHide != null) foreach (var go in alsoHide) if (go != null) go.SetActive(false);

        GameInput.Unlock(this);
        DialogueUI.Play(dialogue, () => StoryFlags.Set(Flag));
    }

    // Si Tsuki quedó fuera del puente (sobre la isla que desapareció), vuelve a su inicio.
    private void KeepPlayerOnBridge()
    {
        var player = SceneContext.FindPlayer();
        if (player == null || safeSpot == null) return;
        var cell = ground.WorldToCell(player.transform.position - new Vector3(0f, 0.08f, 0f));
        if (ground.GetTile(cell) == keepTile) return;
        player.transform.position = safeSpot.position;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = safeSpot.position;
            rb.linearVelocity = Vector2.zero;
        }
    }
}
