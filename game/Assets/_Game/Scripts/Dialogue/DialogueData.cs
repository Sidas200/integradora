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
    }

    [Tooltip("Si está activo, el builder del capítulo NO sobrescribe este diálogo (edición manual del equipo).")]
    public bool lockedByDesigner;

    public List<Line> lines = new List<Line>();
}
