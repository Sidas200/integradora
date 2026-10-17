using UnityEngine;

// HEX-06: permiso del guardián. Se obtiene una sola vez (flag permit_obtained + SessionState),
// se guarda la partida y se pasa al cierre de la demo.
public class PermitPickup : Interactable
{
    [SerializeField] private DialogueData obtainedDialogue;

    public void Setup(DialogueData dialogue) => obtainedDialogue = dialogue;

    public override bool CanInteract => !StoryFlags.Has(ChapterObjectives.PermitObtained);

    public override void Interact()
    {
        if (!StoryFlags.Set(ChapterObjectives.PermitObtained)) return; // ya obtenido

        if (SessionState.Instance != null) SessionState.Instance.guardianPermission = true;
        if (GameHud.Instance != null) GameHud.Instance.ShowToast("Obtuviste: Permiso del Guardián");
        gameObject.SetActive(false);

        // HEX-08: el permiso queda guardado antes de ir al cierre
        if (GameFlow.Instance != null) GameFlow.Instance.SaveAtSafePoint("guardian_defeated", transform.position);
        DialogueUI.Play(obtainedDialogue, () =>
        {
            if (GameFlow.Instance != null) GameFlow.Instance.GoToDemoEnd();
        });
    }
}
