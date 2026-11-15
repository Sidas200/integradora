using UnityEngine;

// Interacción con el guardián dormido: ofrecerle la Gota de Amrita (o pulsar G cerca).
// La gota solo se gasta aquí; GuardianArena escucha ItemUser.AmritaUsed y lo despierta.
public class AmritaAltar : Interactable
{
    [SerializeField] private GuardianArena arena;
    [SerializeField] private DialogueData missingDialogue;

    public void Setup(GuardianArena guardianArena, DialogueData missing)
    {
        arena = guardianArena;
        missingDialogue = missing;
    }

    public override bool CanInteract => arena != null && arena.AwaitingAmrita;
    public override string Prompt => "Ofrecer la Gota de Amrita";

    public override void Interact()
    {
        var player = SceneContext.FindPlayer();
        var user = player != null ? player.GetComponent<ItemUser>() : null;
        if (user == null || !user.UseAmrita()) DialogueUI.Play(missingDialogue);
    }
}
