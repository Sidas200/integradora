using UnityEngine;

// GAME-42 / GAME-43: define un tipo de material (Material A, Material B).
// Se crea desde el menú: clic derecho en Project > Create > Game > Material
[CreateAssetMenu(menuName = "Game/Material", fileName = "NewMaterial")]
public class MaterialData : ScriptableObject
{
    [Tooltip("Identificador único de texto. No lo cambies después de crearlo (se usará para guardar partida). Ej: material_a")]
    public string id;

    public string displayName;
    public Sprite icon;

    [TextArea] public string description;
}
