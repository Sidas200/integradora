using System.Collections;
using UnityEngine;

// Ofrenda de un item clave (p. ej. el Alma pequeña en la puerta del primer Santuario).
// Sin el item muestra una pista; con él lo consume una sola vez, activa el flag, oculta la
// barrera y guarda el progreso. El flag abre la salida (SceneTransition con ese flag).
public class ItemOffering : Interactable
{
    [SerializeField] private MaterialData item;
    [SerializeField] private string flag;
    [SerializeField] private string offerPrompt = "Ofrecer";
    [SerializeField] private DialogueData missingDialogue;
    [SerializeField] private DialogueData offeredDialogue;
    [SerializeField] private SpriteRenderer barrier;

    public void Setup(MaterialData requiredItem, string unlockFlag, string offerText, DialogueData missing, DialogueData offered, SpriteRenderer barrierRenderer)
    {
        item = requiredItem;
        flag = unlockFlag;
        offerPrompt = offerText;
        missingDialogue = missing;
        offeredDialogue = offered;
        barrier = barrierRenderer;
    }

    public override bool CanInteract => !StoryFlags.Has(flag);
    public override string Prompt => HasItem ? offerPrompt : prompt;

    private bool HasItem
    {
        get
        {
            var player = SceneContext.FindPlayer();
            var inventory = player != null ? player.GetComponent<Inventory>() : null;
            return inventory != null && item != null && inventory.GetAmount(item.id) > 0;
        }
    }

    private void Start()
    {
        if (StoryFlags.Has(flag) && barrier != null) barrier.gameObject.SetActive(false);
    }

    public override void Interact()
    {
        if (StoryFlags.Has(flag)) return;
        var inventory = SceneContext.FindPlayer().GetComponent<Inventory>();
        if (item == null || !inventory.TryConsume(item.id, 1))
        {
            DialogueUI.Play(missingDialogue);
            return;
        }
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        GameInput.Lock(this);
        if (barrier != null)
        {
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                barrier.color = new Color(1f, 1f, 1f, 1f - t / 0.9f);
                yield return null;
            }
            barrier.gameObject.SetActive(false);
        }
        GameInput.Unlock(this);
        StoryFlags.Set(flag);
        SetHighlighted(false);
        if (GameFlow.Instance != null) GameFlow.Instance.SaveProgress();
        DialogueUI.Play(offeredDialogue);
    }
}
