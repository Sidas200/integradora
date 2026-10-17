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

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float a = 1f - t / duration;
            var c = new Color(1f, 1f, 1f, a);
            foreach (var p in cells) ground.SetColor(p, c);
            if (elevations != null) elevations.color = c;
            foreach (var r in renderers) r.color = c;
            yield return null;
        }
        foreach (var p in cells) ground.SetTile(p, null);
        if (elevations != null) elevations.gameObject.SetActive(false);
        if (props != null) props.gameObject.SetActive(false);
        if (backBlocker != null) backBlocker.SetActive(true);
        if (alsoHide != null) foreach (var go in alsoHide) if (go != null) go.SetActive(false);

        GameInput.Unlock(this);
        DialogueUI.Play(dialogue, () => StoryFlags.Set(Flag));
    }
}
