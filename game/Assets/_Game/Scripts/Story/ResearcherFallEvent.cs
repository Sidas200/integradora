using System.Collections;
using UnityEngine;

// La investigadora cae del árbol frente a Tsuki la primera vez que se acerca.
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

    private bool running;

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
        if (other.GetComponentInParent<PlayerController>() == null) return;
        running = true;
        StartCoroutine(Fall());
    }

    private IEnumerator Fall()
    {
        GameInput.Lock(this);
        var visual = researcher.transform.Find("Visual");
        var baseLocal = visual.localPosition;
        researcher.transform.position = landPoint.position;
        researcher.SetActive(true);

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

        GameInput.Unlock(this);
        DialogueUI.Play(dialogue, () =>
        {
            StoryFlags.Set(Flag);
            StartCoroutine(Leave());
        });
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
