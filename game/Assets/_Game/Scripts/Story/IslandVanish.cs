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

    private void Start() => BlockIslandEdge();

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
        if (done || !StoryFlags.Has(SwordPickup.Flag)) return;
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
