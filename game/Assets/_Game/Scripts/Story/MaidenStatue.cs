using System.Collections;
using UnityEngine;

// Estatua de la primera doncella. Primero pone a prueba la espada (SanctuaryTrial: oleadas
// de enemigos); al superarla despierta (una sola vez) y deja un espíritu con una pista corta.
public class MaidenStatue : Interactable
{
    public const string Flag = "maiden_awakened";

    [SerializeField] private DialogueData awakenDialogue;
    [SerializeField] private DialogueData hintDialogue;
    [SerializeField] private GameObject spirit;
    [SerializeField] private SpriteRenderer statueRenderer;
    [SerializeField] private SanctuaryTrial trial;
    [SerializeField] private DialogueData trialDialogue;

    public void Setup(DialogueData awaken, DialogueData hint, GameObject spiritObject, SpriteRenderer statue)
    {
        awakenDialogue = awaken;
        hintDialogue = hint;
        spirit = spiritObject;
        statueRenderer = statue;
    }

    public void SetTrial(SanctuaryTrial sanctuaryTrial, DialogueData intro)
    {
        trial = sanctuaryTrial;
        trialDialogue = intro;
    }

    public override string Prompt => StoryFlags.Has(Flag) ? "Hablar con Aoi" : "Tocar la estatua";
    public override bool CanInteract => trial == null || !trial.Running;

    private void Start()
    {
        if (spirit != null) spirit.SetActive(StoryFlags.Has(Flag));
        if (StoryFlags.Has(Flag)) Dim();
    }

    public override void Interact()
    {
        if (StoryFlags.Has(Flag))
        {
            DialogueUI.Play(hintDialogue);
            return;
        }
        if (trial != null && !trial.Done)
        {
            DialogueUI.Play(trialDialogue, () => trial.Begin(() => StartCoroutine(Awaken())));
            return;
        }
        StartCoroutine(Awaken());
    }

    private IEnumerator Awaken()
    {
        GameInput.Lock(this);
        // Brillo de la estatua antes de que aparezca el espíritu
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (statueRenderer != null) statueRenderer.color = Color.Lerp(Color.white, new Color(0.6f, 0.95f, 1f), Mathf.PingPong(t * 4f, 1f));
            yield return null;
        }
        Dim();
        if (spirit != null) spirit.SetActive(true);
        GameInput.Unlock(this);
        // El despertar queda guardado aunque el jugador salga al menú durante el diálogo
        StoryFlags.Set(Flag);
        if (GameFlow.Instance != null) GameFlow.Instance.SaveProgress();
        DialogueUI.Play(awakenDialogue);
    }

    private void Dim()
    {
        if (statueRenderer != null) statueRenderer.color = new Color(0.75f, 0.8f, 0.9f);
    }
}
