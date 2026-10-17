using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Datos de juego del capítulo: receta de la mejora y botín de cada fuente.
    public static class ChapterData
    {
        public const string Folder = "Assets/_Game/Data/Chapter1";
        public const string SwordRecipePath = Folder + "/Receta_EspadaCarmesi.asset";
        public const int RecipeAmount = 3;

        // Receta nueva del capítulo (decisión 1): Cuero antiguo x3 + Flor destello x3.
        // Receta_ModuloEspada del equipo queda intacta.
        public static CraftingRecipe SwordRecipe()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(SwordRecipePath);
            if (recipe == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data")) AssetDatabase.CreateFolder("Assets/_Game", "Data");
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Data", "Chapter1");
                recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                AssetDatabase.CreateAsset(recipe, SwordRecipePath);
            }
            recipe.id = "espada_carmesi";
            recipe.displayName = "Despertar de la espada carmesí  (Daño 15 → 25)";
            recipe.ingredients = new List<CraftingRecipe.Ingredient>
            {
                new CraftingRecipe.Ingredient { material = ChapterPrefabs.LoadMaterial("CueroAntiguo"), amount = RecipeAmount },
                new CraftingRecipe.Ingredient { material = ChapterPrefabs.LoadMaterial("FlorDestello"), amount = RecipeAmount },
            };
            recipe.result = null;
            recipe.resultAmount = 1;
            recipe.upgradesSword = true;
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        // Material de un pickup según el nombre de su marcador (Pickup_Flor_*, Pickup_Cuero_*, Pickup_Pocion_*).
        public static MaterialData MaterialForMarker(string marker)
        {
            if (marker.Contains("Flor")) return ChapterPrefabs.LoadMaterial("FlorDestello");
            if (marker.Contains("Cuero")) return ChapterPrefabs.LoadMaterial("CueroAntiguo");
            if (marker.Contains("Pocion")) return ChapterPrefabs.LoadMaterial("Pocion");
            return null;
        }

        // Botín garantizado por arquetipo: el melee suelta cuero y el Centinela flor.
        public static MaterialData LootForEnemy(string marker) =>
            marker.StartsWith("Enemy_Ranged") ? ChapterPrefabs.LoadMaterial("FlorDestello") : ChapterPrefabs.LoadMaterial("CueroAntiguo");
    }
}
