using System.Collections;
using UnityEngine;

// La investigadora cae del árbol frente a Tsuki la primera vez que se acerca.
// La cámara se acerca a las dos, Tsuki se sorprende y después hablan.
// Después de hablar se va hacia el campamento.
[RequireComponent(typeof(Collider2D))]
public class ResearcherFallEvent : MonoBehaviour
{
    public const string Flag = "met_researcher";

    [SerializeField] private GameObject researcher;
    [SerializeField] private Transform landPoint;
    [SerializeField] private float fallHeight = 0.75f;
    [SerializeField] private float fallTime = 0.45f;
    [SerializeField] private DialogueData dialogue;

    [Header("Sorpresa de Tsuki")]
    [SerializeField, Min(1)] private int surpriseFrames = 10;
    [SerializeField] private float surpriseFps = 8f;
    [SerializeField] private float holdAfterSurprise = 0.3f;

    [Header("Cámara")]
    [Tooltip("Lo más cerca que se acerca (tamaño respecto al normal); si están lejos se acerca menos para que se vean las dos.")]
    [SerializeField, Range(0.3f, 1f)] private float zoom = 0.6f;
    [SerializeField] private float zoomTime = 0.6f;

    private bool running;
    private PlayerController tsuki;

    public void Setup(GameObject npc, Transform land, DialogueData data)
    {
        researcher = npc;
        landPoint = land;
        dialogue = data;
    }

    private void Start()
    {
        if (researcher != null) researcher.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (running || StoryFlags.Has(Flag) || !StoryFlags.Has(IslandVanish.Flag)) return;
        tsuki = other.GetComponentInParent<PlayerController>();
        if (tsuki == null) return;
        running = true;
        StartCoroutine(Fall());
    }

    private IEnumerator Fall()
    {
        GameInput.Lock(this);
        tsuki.StopActions();
        var visual = researcher.transform.Find("Visual");
        var baseLocal = visual.localPosition;
        researcher.transform.position = landPoint.position;
        researcher.SetActive(true);
        FocusCamera();

        // Caída con aceleración y pequeño rebote
        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            float k = t / fallTime;
            visual.localPosition = baseLocal + Vector3.up * fallHeight * (1f - k * k);
            yield return null;
        }
        visual.localPosition = baseLocal;
        visual.localScale = new Vector3(1.15f, 0.85f, 1f);
        yield return new WaitForSeconds(0.12f);
        visual.localScale = Vector3.one;
        yield return new WaitForSeconds(0.25f);

        yield return Surprise();

        GameInput.Unlock(this);
        DialogueUI.Play(dialogue, () =>
        {
            StoryFlags.Set(Flag);
            CameraFocus.Release();
            StartCoroutine(Leave());
        });
    }

    // Tsuki se sorprende (con el "!") y queda mirando a la investigadora
    private IEnumerator Surprise()
    {
        var body = tsuki.GetComponentInChildren<SpriteRenderer>();
        var frames = TsukiSheet.Load(TsukiSheet.Surprise, surpriseFrames, body);
        if (frames != null)
        {
            float frameTime = 1f / Mathf.Max(1f, surpriseFps);
            foreach (var frame in frames)
            {
                tsuki.HoldPose(frame);
                yield return new WaitForSeconds(frameTime);
            }
            yield return new WaitForSeconds(holdAfterSurprise);
            tsuki.HoldPose(null);
        }
        tsuki.FaceTowards(researcher.transform.position - tsuki.transform.position);
    }

    // Enfoca el punto medio entre las dos, sin acercarse tanto que alguna quede fuera de cuadro
    private void FocusCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector2 a = tsuki.transform.position;
        Vector2 b = landPoint.position;
        Vector2 mid = (a + b) * 0.5f + Vector2.up * 0.2f;
        Vector2 half = new Vector2(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)) * 0.5f;
        // Margen para que se vean completas (los sprites miden ~0.5 de alto)
        float needed = Mathf.Max(half.y + 0.55f, (half.x + 0.45f) / Mathf.Max(0.1f, cam.aspect));
        float k = Mathf.Clamp(needed / Mathf.Max(0.01f, cam.orthographicSize), zoom, 1f);
        CameraFocus.Show(() => mid, k, zoomTime);
    }

    private IEnumerator Leave()
    {
        var sr = researcher.GetComponentInChildren<SpriteRenderer>();
        var start = researcher.transform.position;
        // Se aleja hacia el oeste (izquierda de la pantalla), donde está el campamento
        var end = start + new Vector3(-0.9f, 0.45f, 0f);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            float k = t / 1.2f;
            researcher.transform.position = Vector3.Lerp(start, end, k);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }
        researcher.SetActive(false);
    }
}
