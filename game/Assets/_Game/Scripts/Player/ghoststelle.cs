using UnityEngine;

public class DashGhost : MonoBehaviour
{
    private SpriteRenderer sr;
    private float fadeSpeed;

    public void Initialize(Sprite sprite, Color color, Vector3 scale, float lifetime, Material material, string sortingLayer, int sortingOrder)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.material = material;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder - 1; // un poco detrás del jugador
        transform.localScale = scale;
        fadeSpeed = 1f / lifetime;
    }

    private void Update()
    {
        Color c = sr.color;
        c.a -= fadeSpeed * Time.deltaTime;
        sr.color = c;

        if (c.a <= 0f)
            Destroy(gameObject);
    }
}