using UnityEngine;

// La espada carmesí en la cabaña. Se toma una sola vez (flag has_sword).
public class SwordPickup : Interactable
{
    public const string Flag = "has_sword";

    [SerializeField] private DialogueData dialogue;
    [SerializeField] private SpriteRenderer rackRenderer;
    [SerializeField] private Sprite emptyRack;

    public void Setup(DialogueData data, SpriteRenderer rack, Sprite empty)
    {
        dialogue = data;
        rackRenderer = rack;
        emptyRack = empty;
    }

    private void Start()
    {
        if (StoryFlags.Has(Flag)) ShowEmpty();
    }

    public override bool CanInteract => !StoryFlags.Has(Flag);

    public override void Interact()
    {
        if (StoryFlags.Has(Flag)) return;
        DialogueUI.Play(dialogue, () =>
        {
            StoryFlags.Set(Flag);
            ShowEmpty();
        });
    }

    private void ShowEmpty()
    {
        if (rackRenderer != null && emptyRack != null) rackRenderer.sprite = emptyRack;
        SetHighlighted(false);
    }
}
