using System;
using System.Collections.Generic;
using UnityEngine;

// Tabla de drops reutilizable: un asset por tipo de enemigo.
[CreateAssetMenu(menuName = "Game/Drop Table")]
public class DropTable : ScriptableObject
{
    [Serializable]
    public struct Drop
    {
        public MaterialData material;
        [Min(1)] public int minAmount;
        [Min(1)] public int maxAmount;
        [Range(0f, 1f)] public float chance;
    }

    public List<Drop> drops = new List<Drop>();
}