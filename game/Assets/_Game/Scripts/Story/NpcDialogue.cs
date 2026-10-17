using System;
using System.Collections.Generic;
using UnityEngine;

// NPC que habla con E. Usa la primera entrada cuya condición se cumple
// (ordenar de la más específica a la más general) y opcionalmente activa un flag.
public class NpcDialogue : Interactable
{
    [Serializable]
    public struct Entry
    {
        public FlagCondition condition;
        public DialogueData dialogue;
        [Tooltip("Flag que se activa al terminar este diálogo (opcional).")]
        public string setFlag;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public void SetEntries(List<Entry> list) => entries = list;

    public override bool CanInteract => FindEntry(out _);

    public override void Interact()
    {
        if (!FindEntry(out var entry)) return;
        DialogueUI.Play(entry.dialogue, () => StoryFlags.Set(entry.setFlag));
    }

    private bool FindEntry(out Entry found)
    {
        foreach (var e in entries)
        {
            if (e.dialogue != null && e.condition.IsMet)
            {
                found = e;
                return true;
            }
        }
        found = default;
        return false;
    }
}
