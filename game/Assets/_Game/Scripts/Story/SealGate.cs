using System.Collections;
using UnityEngine;

// HEX-06: sello del guardián. Rechaza al jugador si la espada no está mejorada;
// con la mejora se rompe una sola vez (flag seal_opened) y deja pasar.
public class SealGate : Interactable
{
    [SerializeField] private SpriteRenderer barrier;
    [SerializeField] private Collider2D blocker;
    [SerializeField] private DialogueData rejectDialogue;
    [SerializeField] private DialogueData openDialogue;
    [SerializeField] private float pushBack = 0.25f;

    public void Setup(SpriteRenderer barrierRenderer, Collider2D blockCollider, DialogueData reject, DialogueData open)
    {
        barrier = barrierRenderer;
        blocker = blockCollider;
        rejectDialogue = reject;
        openDialogue = open;
    }

    public override string Prompt => "Examinar el sello";
    public override bool CanInteract => !StoryFlags.Has(ChapterObjectives.SealOpened);

    private void Start()
    {
        if (StoryFlags.Has(ChapterObjectives.SealOpened)) SetOpen();
    }

    public override void Interact()
    {
        if (StoryFlags.Has(ChapterObjectives.SealOpened)) return;
        var player = SceneContext.FindPlayer();
        var stats = player != null ? player.GetComponent<PlayerStats>() : null;

        if (stats == null || stats.SwordLevel < 1)
        {
            // Rechazo: la espada rebota y Tsuki retrocede
            if (player != null)
            {
                var controller = player.GetComponent<PlayerController>();
                if (controller != null) controller.ApplyHitReaction(transform.position);
            }
            StartCoroutine(Flash(3));
            DialogueUI.Play(rejectDialogue);
            return;
        }
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        GameInput.Lock(this);
        yield return Flash(6);
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (barrier != null) barrier.color = new Color(1f, 1f, 1f, 1f - t / 0.8f);
            yield return null;
        }
        SetOpen();
        GameInput.Unlock(this);
        if (SessionState.Instance != null) SessionState.Instance.sealOpened = true;
        DialogueUI.Play(openDialogue, () => StoryFlags.Set(ChapterObjectives.SealOpened));
    }

    private IEnumerator Flash(int times)
    {
        if (barrier == null) yield break;
        for (int i = 0; i < times; i++)
        {
            barrier.color = new Color(1f, 1f, 1f, 0.4f);
            yield return new WaitForSeconds(0.06f);
            barrier.color = Color.white;
            yield return new WaitForSeconds(0.06f);
        }
    }

    private void SetOpen()
    {
        if (barrier != null) barrier.gameObject.SetActive(false);
        if (blocker != null) blocker.enabled = false;
        SetHighlighted(false);
    }
}
