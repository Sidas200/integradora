using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Animación de muerte de Tsuki antes de la pantalla de derrota:
// el fondo se oscurece mientras cae (frames 1-9), se enciende un reflector de teatro
// que solo la ilumina a ella, termina de caer (frames 10-18) y se queda en el último frame
// hasta que se reintenta o se vuelve al menú.
public class DeathStage : MonoBehaviour
{
    private const string SheetResource = "Player/tsuki_death_sheet";

    [Header("Frames")]
    [Tooltip("Cuántos frames se animan antes de encender el reflector (el resto va después).")]
    [SerializeField, Min(1)] private int framesBeforeLight = 9;
    [SerializeField] private float fps = 10f;

    [Header("Oscuridad")]
    [SerializeField, Range(0f, 1f)] private float darkness = 0.93f;
    [Tooltip("Qué tan visible queda Tsuki en la oscuridad antes de que se encienda la luz (1 = normal).")]
    [SerializeField, Range(0f, 1f)] private float shadowTint = 0.4f;

    [Header("Zoom de cámara")]
    [Tooltip("Tamaño de la cámara respecto al normal (menor = más cerca). 1 = sin zoom.")]
    [SerializeField, Range(0.3f, 1f)] private float zoom = 0.6f;
    [Tooltip("Segundos en acercarse (se acerca mientras se oscurece el fondo).")]
    [SerializeField] private float zoomTime = 1f;

    [Header("Reflector")]
    [SerializeField] private Color lightColor = new Color(1f, 0.98f, 0.92f, 1f);
    [SerializeField] private float pauseAfterLight = 0.35f;
    [SerializeField] private float holdBeforeMenu = 1.2f;

    // Por encima del HUD (canvas "UI" = 50) mientras se anima; debajo cuando sale el menú de derrota.
    private const int SortingWhileAnimating = 60;
    private const int SortingUnderMenu = 49;

    private Sprite[] frames;
    private Canvas canvas;
    private Image dark;
    private Image cone;
    private Image floorGlow;
    private Image actor;
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private Vector3 feetWorld;
    private float spriteWorldHeight;
    private float spriteWorldWidth;
    private Coroutine routine;

    public bool IsShowing => canvas != null && canvas.gameObject.activeSelf;

    // Devuelve false si no hay frames (la derrota sigue funcionando como antes).
    public bool CanPlay()
    {
        if (frames == null)
            frames = Resources.LoadAll<Sprite>(SheetResource);
        return frames != null && frames.Length > 0 && Camera.main != null;
    }

    public IEnumerator Play(GameObject player)
    {
        if (!CanPlay() || player == null)
            yield break;

        Build();
        Anchor(player);
        HidePlayer(player);

        // Acerca la cámara a Tsuki; CameraFocus la suelta solo al cargar la siguiente escena
        if (zoom < 1f)
        {
            Vector2 focus = feetWorld + Vector3.up * (spriteWorldHeight * 0.4f);
            CameraFocus.Show(() => focus, zoom, zoomTime);
        }

        canvas.sortingOrder = SortingWhileAnimating;
        canvas.gameObject.SetActive(true);
        SetLight(0f);
        dark.color = new Color(0f, 0f, 0f, 0f);

        int before = Mathf.Clamp(framesBeforeLight, 1, frames.Length);
        float frameTime = 1f / Mathf.Max(1f, fps);

        // 1) Cae mientras el fondo se oscurece
        for (int i = 0; i < before; i++)
        {
            actor.sprite = frames[i];
            float t0 = (float)i / before;
            float t1 = (float)(i + 1) / before;
            for (float t = 0f; t < frameTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Lerp(t0, t1, t / frameTime);
                dark.color = new Color(0f, 0f, 0f, darkness * k);
                float tint = Mathf.Lerp(1f, shadowTint, k);
                actor.color = new Color(tint, tint, tint, 1f);
                Place();
                yield return null;
            }
        }
        dark.color = new Color(0f, 0f, 0f, darkness);

        // 2) Se enciende el reflector (con un pequeño parpadeo, como una lámpara de teatro)
        float[] flicker = { 0.07f, 0.09f, 0.05f, 0.14f };
        for (int i = 0; i < flicker.Length; i++)
        {
            SetLight(i % 2 == 0 ? 1f : 0f);
            yield return Wait(flicker[i]);
        }
        SetLight(1f);
        yield return Wait(pauseAfterLight);

        // 3) Termina de caer y se queda en el último frame
        for (int i = before; i < frames.Length; i++)
        {
            actor.sprite = frames[i];
            yield return Wait(frameTime);
        }

        yield return Wait(holdBeforeMenu);
    }

    // El menú de derrota se dibuja encima; la escena del reflector sigue detrás.
    public void SendBehindMenu()
    {
        if (canvas != null) canvas.sortingOrder = SortingUnderMenu;
    }

    public void Clear()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
        foreach (var r in hiddenRenderers)
            if (r != null) r.enabled = true;
        hiddenRenderers.Clear();
    }

    private IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            Place();
            yield return null;
        }
    }

    // ---------------------------------------------------------------- posición

    private void Anchor(GameObject player)
    {
        // El sprite de Tsuki va en un hijo ("Visual"); la sombra no cuenta
        SpriteRenderer body = null;
        foreach (var r in player.GetComponentsInChildren<SpriteRenderer>())
            if (r.enabled && r.sprite != null && (body == null || r.bounds.size.y > body.bounds.size.y))
                body = r;

        Vector3 scale = body != null ? body.transform.lossyScale : player.transform.lossyScale;
        if (body != null)
        {
            var b = body.bounds;
            feetWorld = new Vector3(b.center.x, b.min.y, b.center.z);
        }
        else
        {
            feetWorld = player.transform.position;
        }

        // Mismo tamaño de pixel que el sprite normal de Tsuki
        Rect r0 = frames[0].rect;
        spriteWorldWidth = r0.width / frames[0].pixelsPerUnit * Mathf.Abs(scale.x);
        spriteWorldHeight = r0.height / frames[0].pixelsPerUnit * Mathf.Abs(scale.y);
    }

    private void HidePlayer(GameObject player)
    {
        hiddenRenderers.Clear();
        foreach (var r in player.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!r.enabled) continue;
            r.enabled = false;
            hiddenRenderers.Add(r);
        }
    }

    // Recalcula la posición en pantalla (la cámara puede seguir moviéndose un poco)
    private void Place()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 feet = cam.WorldToScreenPoint(feetWorld);
        Vector3 top = cam.WorldToScreenPoint(feetWorld + Vector3.up * spriteWorldHeight);
        float h = Mathf.Abs(top.y - feet.y);
        float w = h * (spriteWorldWidth / Mathf.Max(0.0001f, spriteWorldHeight));
        float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

        Vector2 feetPx = new Vector2(Mathf.Round(feet.x), Mathf.Round(feet.y)) / scale;
        w /= scale;
        h /= scale;

        actor.rectTransform.anchoredPosition = feetPx;
        actor.rectTransform.sizeDelta = new Vector2(w, h);

        // El cono baja desde arriba de la pantalla hasta los pies
        float screenTop = Screen.height / scale;
        float coneWidth = w * 2.4f;
        cone.rectTransform.anchoredPosition = new Vector2(feetPx.x, feetPx.y - h * 0.12f);
        cone.rectTransform.sizeDelta = new Vector2(coneWidth, screenTop - feetPx.y + h * 0.12f);

        floorGlow.rectTransform.anchoredPosition = new Vector2(feetPx.x, feetPx.y);
        floorGlow.rectTransform.sizeDelta = new Vector2(coneWidth * 1.05f, h * 0.42f);
    }

    private void SetLight(float on)
    {
        cone.color = new Color(lightColor.r, lightColor.g, lightColor.b, on);
        floorGlow.color = new Color(lightColor.r, lightColor.g, lightColor.b, on);
        if (on > 0f) actor.color = Color.white;
        else actor.color = new Color(shadowTint, shadowTint, shadowTint, 1f);
    }

    // ---------------------------------------------------------------- construcción

    private void Build()
    {
        if (canvas != null) return;

        var go = new GameObject("DeathStage", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingWhileAnimating;
        canvas.pixelPerfect = true;

        dark = MakeImage("Dark", go.transform, null);
        var darkRt = dark.rectTransform;
        darkRt.anchorMin = Vector2.zero;
        darkRt.anchorMax = Vector2.one;
        darkRt.offsetMin = Vector2.zero;
        darkRt.offsetMax = Vector2.zero;
        dark.raycastTarget = true; // nada del juego recibe clics mientras dura

        cone = MakeImage("SpotCone", go.transform, MakeConeSprite());
        cone.rectTransform.pivot = new Vector2(0.5f, 0f);

        floorGlow = MakeImage("SpotFloor", go.transform, MakeFloorSprite());
        floorGlow.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        actor = MakeImage("Tsuki", go.transform, frames[0]);
        actor.rectTransform.pivot = new Vector2(0.5f, 0f);

        go.SetActive(false);
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // Cono de reflector en pixel art: angosto arriba, ancho abajo, bordes suaves en escalones.
    private static Sprite MakeConeSprite()
    {
        const int W = 64, H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            float v = (float)y / (H - 1);              // 0 = abajo (pies), 1 = arriba (lámpara)
            float half = Mathf.Lerp(0.5f, 0.1f, v);    // medio ancho normalizado
            float strength = Mathf.Lerp(0.30f, 0.14f, v);
            for (int x = 0; x < W; x++)
            {
                float d = Mathf.Abs((x + 0.5f) / W - 0.5f);
                float edge = Mathf.Clamp01((half - d) / (half * 0.35f));
                float a = Quantize(edge * strength, 0.04f);
                px[y * W + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100f);
    }

    // Círculo de luz en el piso
    private static Sprite MakeFloorSprite()
    {
        const int W = 64, H = 24;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = ((x + 0.5f) / W - 0.5f) * 2f;
                float dy = ((y + 0.5f) / H - 0.5f) * 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Quantize(Mathf.Clamp01(1f - r) * 0.55f, 0.12f);
                px[y * W + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Quantize(float value, float step) => Mathf.Floor(value / step) * step;
}
