using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Flecha que guía al objetivo actual (ChapterObjectives.Where) para quien juega por primera vez:
// - si el destino está fuera de pantalla, gira alrededor de Tsuki apuntando hacia él;
// - si ya se ve, rebota encima del destino;
// - si el destino está en otra escena, señala la salida que lleva hacia allá.
// Lo crea GameHud. Va debajo del HUD (orden 41) y se oculta en diálogos, menús y cerca del destino.
public class ObjectiveGuide : MonoBehaviour
{
    private const float ArriveDistance = 0.3f;   // unidades del mundo: ya llegó
    private const float OrbitRadius = 0.12f;      // fracción del alto de pantalla
    private const float ArrowSize = 0.055f;
    private const float ScreenMargin = 0.08f;     // el destino cuenta como visible dentro de este margen
    private const float RefreshTime = 0.5f;

    private static readonly Color ArrowColor = new Color(0.965f, 0.835f, 0.557f); // dorado del HUD
    private static Sprite arrowSprite;

    private Image arrow;
    private Transform player;
    private Transform target;
    private string targetName;
    private Transform cabinDoor, cabinInside, cabinExit;
    private bool visible = true;
    private float refreshTimer;
    private float alpha;
    private float bob;

    public static ObjectiveGuide Create(Transform parent)
    {
        var go = new GameObject("ObjectiveGuide", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 41;

        var guide = go.AddComponent<ObjectiveGuide>();
        guide.arrow = new GameObject("Arrow", typeof(RectTransform)).AddComponent<Image>();
        guide.arrow.rectTransform.SetParent(go.transform, false);
        guide.arrow.rectTransform.anchorMin = guide.arrow.rectTransform.anchorMax = Vector2.zero;
        guide.arrow.sprite = ArrowSprite();
        guide.arrow.raycastTarget = false;
        guide.arrow.enabled = false;
        return guide;
    }

    public void Bind(GameObject playerObject)
    {
        player = playerObject != null ? playerObject.transform : null;
        target = null;
        targetName = null;
        cabinDoor = cabinInside = cabinExit = null;
        refreshTimer = 0f;
        alpha = 0f;
    }

    public void SetVisible(bool value) => visible = value;

    private void LateUpdate()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = RefreshTime;
            target = ResolveTarget();
        }

        var cam = Camera.main;
        bool show = visible && player != null && target != null && cam != null &&
                    !GameInput.GameplayBlocked && Time.timeScale > 0f &&
                    (DialogueUI.Instance == null || !DialogueUI.Instance.IsOpen) &&
                    Vector2.Distance(player.position, target.position) > ArriveDistance;

        // Aparece y desaparece suave para no distraer
        alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        arrow.enabled = alpha > 0.01f;
        if (!arrow.enabled || cam == null || player == null || target == null) return;

        bob += Time.unscaledDeltaTime;
        float h = Screen.height;
        float size = h * ArrowSize;
        var rt = arrow.rectTransform;
        rt.sizeDelta = new Vector2(size, size);

        Vector3 targetView = cam.WorldToViewportPoint(target.position);
        bool onScreen = targetView.z > 0f &&
                        targetView.x > ScreenMargin && targetView.x < 1f - ScreenMargin &&
                        targetView.y > ScreenMargin && targetView.y < 1f - ScreenMargin;

        if (onScreen)
        {
            // Encima del destino, apuntando hacia abajo y rebotando
            Vector2 p = cam.WorldToScreenPoint(target.position);
            rt.anchoredPosition = p + new Vector2(0f, size * (1.1f + 0.25f * Mathf.Sin(bob * 6f)));
            rt.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            // Alrededor de Tsuki, apuntando al destino; late hacia afuera
            Vector2 from = cam.WorldToScreenPoint(player.position + Vector3.up * 0.15f);
            Vector2 to = cam.WorldToScreenPoint(target.position);
            Vector2 dir = (to - from).normalized;
            float radius = h * OrbitRadius * (1f + 0.08f * Mathf.Sin(bob * 6f));
            rt.anchoredPosition = from + dir * radius;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        var c = ArrowColor;
        c.a = alpha * (0.8f + 0.2f * Mathf.Sin(bob * 3f));
        arrow.color = c;
    }

    // Objeto a señalar en esta escena: el destino si está aquí, o la salida hacia su escena
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
        if (cabinExit == null) cabinExit = Find("Exit_CabinInside");
        if (cabinDoor == null || cabinInside == null || player == null) return name;

        bool inside = Vector2.Distance(player.position, cabinInside.position) <
                      Vector2.Distance(player.position, cabinDoor.position);
        bool targetInside = name == "SwordRack";
        if (inside && !targetInside) return "Exit_CabinInside";
        if (!inside && targetInside) return "Door_Cabin";
        return name;
    }

    // Busca por nombre en la escena activa, incluidos objetos inactivos (p. ej. un enemigo vencido)
    private static Transform Find(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    // Flecha dorada con borde oscuro, apuntando hacia arriba
    private static Sprite ArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size - 0.5f;
            float v = (y + 0.5f) / size;
            float fill = Arrow(u, v, 0f);
            float edge = Arrow(u, v, 0.06f);
            byte a = (byte)(Mathf.Max(fill, edge) * 255f);
            byte shade = (byte)(Mathf.Lerp(40f, 255f, fill));
            pixels[y * size + x] = new Color32(shade, shade, shade, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return arrowSprite;
    }

    // 1 dentro de la flecha (punta arriba + cola), 0 fuera; grow la engorda para el borde
    private static float Arrow(float u, float v, float grow)
    {
        // Punta: triángulo de v=0.45 a v=0.95
        float headHalf = Mathf.Lerp(0.42f, 0f, Mathf.InverseLerp(0.45f, 0.95f, v));
        bool head = v >= 0.45f - grow && v <= 0.95f + grow && Mathf.Abs(u) <= headHalf + grow;
        // Cola: rectángulo de v=0.1 a v=0.47
        bool tail = v >= 0.1f - grow && v <= 0.47f && Mathf.Abs(u) <= 0.14f + grow;
        return head || tail ? 1f : 0f;
    }
}
