using UnityEngine;

// Saca un hijo de su padre al iniciar (p. ej. el aviso de ataque del enemigo), para que se ordene
// como parte del suelo y no dentro del SortingGroup del personaje. Se oculta o destruye con su dueño.
public class DetachedChild : MonoBehaviour
{
    [Tooltip("Sorting layer y orden que toma al separarse")]
    [SerializeField] private string sortingLayer = "Background";
    [SerializeField] private int sortingOrder = 20;

    private Transform owner;

    private void Awake()
    {
        owner = transform.parent;
        if (owner == null) return;
        bool wasActive = gameObject.activeSelf;
        transform.SetParent(null, true);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
        }
        gameObject.SetActive(wasActive);
    }

    private void LateUpdate()
    {
        if (owner == null) Destroy(gameObject);
        else if (!owner.gameObject.activeInHierarchy) gameObject.SetActive(false);
    }
}
