using System;
using System.Collections.Generic;
using UnityEngine;

// Diálogo data-driven: una lista de líneas con quién habla y su retrato.
// Se crea con Create > Game > Dialogue o lo genera el builder del capítulo.
[CreateAssetMenu(menuName = "Game/Dialogue", fileName = "NewDialogue")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public struct Line
    {
        [Tooltip("Nombre en pantalla. Vacío = narración (sin nombre, en cursiva).")]
        public string speaker;
        public Sprite portrait;
        [TextArea(2, 4)] public string text;
        [Tooltip("La línea solo sale si se cumple (vacío = siempre). Sirve para que los personajes hablen " +
                 "distinto según lo que se eligió antes. Varios flags separados por coma.")]
        public FlagCondition condition;
    }

    // Una opción de la decisión al final del diálogo
    [Serializable]
    public struct Choice
    {
        [Tooltip("Lo que dice la opción en pantalla.")]
        [TextArea(1, 2)] public string text;
        [Tooltip("Lo que pasa después de elegirla (la respuesta del personaje). Puede tener su propia decisión. Vacío = termina la conversación.")]
        public DialogueData next;
        [Tooltip("Flag que se activa al elegirla (opcional). Sirve para que más adelante los diálogos cambien según lo que se eligió.")]
        public string setFlag;
    }

    [Tooltip("Si está activo, el builder del capítulo NO sobrescribe este diálogo (edición manual del equipo).")]
    public bool lockedByDesigner;

    public List<Line> lines = new List<Line>();

    [Header("Decisión (opcional)")]
    [Tooltip("Opciones que aparecen al terminar las líneas (2 o 3). Vacío = el diálogo termina normal.")]
    public List<Choice> choices = new List<Choice>();
    [Tooltip("De quién son los ojos que aparecen al decidir. Vacío = Tsuki.")]
    public string choiceSpeaker;
    [Tooltip("Si no hay decisión: al terminar sigue con este diálogo. Sirve para que todas las respuestas " +
             "de una decisión regresen al resto de la conversación.")]
    public DialogueData continueWith;

    public bool HasChoices => choices != null && choices.Count > 0;
}
