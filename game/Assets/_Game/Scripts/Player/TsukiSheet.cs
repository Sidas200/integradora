using UnityEngine;

// Hojas de cinemática de Tsuki (Resources/Player): una fila de frames del mismo ancho,
// con los pies en la última fila. Se cortan en tiempo de ejecución con los pies a la misma
// altura que el sprite normal de Tsuki, para usarlas con PlayerController.HoldPose.
public static class TsukiSheet
{
    public const string Wake = "Player/tsuki_wake_sheet";
    public const string Surprise = "Player/tsuki_surprise_sheet";

    // body: el SpriteRenderer de Tsuki (su sprite actual da el tamaño de pixel y la altura de los pies).
    // feetPadding: pixeles vacíos debajo de los pies en la hoja.
    public static Sprite[] Load(string resource, int frameCount, SpriteRenderer body, int feetPadding = 0)
    {
        var tex = Resources.Load<Texture2D>(resource);
        if (tex == null || frameCount < 1 || body == null || body.sprite == null) return null;

        var current = body.sprite;
        int w = tex.width / frameCount;
        int h = tex.height;
        // Distancia en pixeles del pivote del sprite normal a sus pies
        var pivot = new Vector2(0.5f, (current.pivot.y + feetPadding) / h);

        string name = resource.Substring(resource.LastIndexOf('/') + 1);
        var frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = Sprite.Create(tex, new Rect(i * w, 0, w, h), pivot, current.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            frames[i].name = $"{name}_{i:00}";
        }
        return frames;
    }
}
