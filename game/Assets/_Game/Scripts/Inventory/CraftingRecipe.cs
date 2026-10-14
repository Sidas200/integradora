using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public MaterialData material;
        [Min(1)] public int amount;
    }

    [Header("Datos")]
    public string id;
    public string displayName;

    [Header("Receta")]
    public List<Ingredient> ingredients = new List<Ingredient>();
    public MaterialData result;
    [Min(1)] public int resultAmount = 1;
}