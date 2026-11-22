using UnityEngine;
using UnityEngine.UI;

// Figura de UI en forma de hoja inclinada con puntas (no rectangular). Puede llevar una imagen recortada
// (uvRect) que se ajusta a la figura; sin imagen es de un solo color (marcos, sombras, fondos de opciones).
// Se usa en la decisión de diálogo para la franja con los ojos del personaje.
[RequireComponent(typeof(CanvasRenderer))]
public class SlantedShape : MaskableGraphic
{
    [SerializeField] private Texture texture;
    [Tooltip("Parte de la imagen que se ve (0-1).")]
    [SerializeField] private Rect uvRect = new Rect(0f, 0f, 1f, 1f);
    [Tooltip("Cuánto se inclina (fracción del ancho).")]
    [SerializeField, Range(0f, 0.3f)] private float slant = 0.08f;
    [SerializeField] private bool flipU;

    public override Texture mainTexture => texture != null ? texture : s_WhiteTexture;

    public void SetImage(Texture tex, Rect uv, bool flip)
    {
        texture = tex;
        uvRect = uv;
        flipU = flip;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    public Rect UvRect
    {
        get => uvRect;
        set { uvRect = value; SetVerticesDirty(); }
    }

    public float Slant
    {
        get => slant;
        set { slant = value; SetVerticesDirty(); }
    }

    // Contorno en coordenadas 0-1 del rectángulo: arriba se corre a la derecha, abajo a la izquierda
    // y en los costados terminan en punta.
    private Vector2[] Outline() => new[]
    {
        new Vector2(slant, 1f),
        new Vector2(1f - slant * 0.3f, 1f),
        new Vector2(1f, 0.45f),
        new Vector2(1f - slant, 0f),
        new Vector2(slant * 0.3f, 0f),
        new Vector2(0f, 0.55f),
    };

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Color32 c = color;

        // Abanico desde el centro
        AddVertex(vh, r, new Vector2(0.5f, 0.5f), c);
        var pts = Outline();
        foreach (var p in pts) AddVertex(vh, r, p, c);
        for (int i = 0; i < pts.Length; i++)
            vh.AddTriangle(0, 1 + i, 1 + (i + 1) % pts.Length);
    }

    private void AddVertex(VertexHelper vh, Rect r, Vector2 n, Color32 c)
    {
        var pos = new Vector3(r.xMin + n.x * r.width, r.yMin + n.y * r.height);
        float u = flipU ? 1f - n.x : n.x;
        var uv = new Vector2(uvRect.x + u * uvRect.width, uvRect.y + n.y * uvRect.height);
        vh.AddVert(pos, c, uv);
    }

    // Los clics solo cuentan dentro de la figura, no en las esquinas vacías del rectángulo
    public override bool Raycast(Vector2 sp, Camera eventCamera)
    {
        if (!base.Raycast(sp, eventCamera)) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out var local))
            return false;
        Rect r = rectTransform.rect;
        var n = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        return Contains(Outline(), n);
    }

    public bool ContainsScreenPoint(Vector2 sp, Camera cam)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, cam, out var local))
            return false;
        Rect r = rectTransform.rect;
        return Contains(Outline(), new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height));
    }

    private static bool Contains(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }
}
