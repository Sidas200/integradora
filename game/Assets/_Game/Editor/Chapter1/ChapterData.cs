using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Datos de juego del capítulo: recetas de la forja (las del equipo), piezas de armadura
    // y botín de cada fuente. Los ids de items son los estables del inventario.
    public static class ChapterData
    {
        public const string Folder = "Assets/_Game/Data/Chapter1";
        public const string EquipmentFolder = Folder + "/Equipment";
        private const string TeamRecipes = "Assets/_Game/Scripts/ScriptableObjects/Recipes";

        // Recetas del equipo, en el orden en que aparecen en la forja. El Módulo de espada
        // (10 Almas antiguas + 5 Circuitos antiguos) es la mejora de la espada (upgradesSword).
        public static List<CraftingRecipe> ForgeRecipes()
        {
            var list = new List<CraftingRecipe>();
            foreach (var name in new[] { "Receta_ModuloEspada", "Receta_GotaAmrita", "Receta_Pocion", "Receta_CascoCuero", "Receta_PecheraCuero", "Receta_BotasCuero" })
            {
                var r = AssetDatabase.LoadAssetAtPath<CraftingRecipe>($"{TeamRecipes}/{name}.asset");
                if (r == null) Debug.LogError("[Chapter1] Falta la receta " + name);
                else list.Add(r);
            }
            return list;
        }

        // El Módulo de espada del equipo no tiene icono: se le asigna el placeholder.
        public static void FillMissingIcons()
        {
            var module = ChapterPrefabs.LoadMaterial("ModuloEspada");
            if (module != null && module.icon == null)
            {
                module.icon = PlaceholderArtGenerator.Load("placeholder_icon_sword_module");
                EditorUtility.SetDirty(module);
            }
        }

        // Set de armadura de cuero. La defensa resta al daño recibido (Health.TakeDamage):
        // con el set completo un golpe de Custodio (10) quita 5 y la embestida de Varn (20) quita 15.
        public static List<EquipmentItem> LeatherSet() => new List<EquipmentItem>
        {
            Piece("Equip_CascoCuero", "Casco de cuero", "CascoCuero", EquipmentSlot.Head, defense: 1, health: 0, speed: 0f),
            Piece("Equip_PecheraCuero", "Pechera de cuero", "PecheraCuero", EquipmentSlot.Chest, defense: 3, health: 10, speed: 0f),
            Piece("Equip_BotasCuero", "Botas de cuero", "BotasCuero", EquipmentSlot.Feet, defense: 1, health: 0, speed: 0.1f),
        };

        private static EquipmentItem Piece(string asset, string name, string material, EquipmentSlot slot, int defense, int health, float speed)
        {
            if (!AssetDatabase.IsValidFolder(EquipmentFolder)) AssetDatabase.CreateFolder(Folder, "Equipment");
            string path = $"{EquipmentFolder}/{asset}.asset";
            var piece = AssetDatabase.LoadAssetAtPath<EquipmentItem>(path);
            if (piece == null)
            {
                piece = ScriptableObject.CreateInstance<EquipmentItem>();
                AssetDatabase.CreateAsset(piece, path);
            }
            piece.itemName = name;
            piece.item = ChapterPrefabs.LoadMaterial(material);
            piece.icon = piece.item != null ? piece.item.icon : null;
            piece.slot = slot;
            piece.bonusDefense = defense;
            piece.bonusMaxHealth = health;
            piece.bonusMoveSpeed = speed;
            piece.bonusAttack = 0;
            EditorUtility.SetDirty(piece);
            return piece;
        }

        // Material de un pickup según el nombre de su marcador (Pickup_Flor_*, Pickup_Resto_*, ...).
        public static MaterialData MaterialForMarker(string marker)
        {
            if (marker.Contains("Flor")) return ChapterPrefabs.LoadMaterial("FlorDestello");
            if (marker.Contains("Cuero")) return ChapterPrefabs.LoadMaterial("CueroAntiguo");
            if (marker.Contains("Pocion")) return ChapterPrefabs.LoadMaterial("Pocion");
            if (marker.Contains("Concentrado")) return ChapterPrefabs.LoadMaterial("ConcentradoSlime");
            if (marker.Contains("Resto")) return ChapterPrefabs.LoadMaterial("RestoSlime");
            return null;
        }

        // Botín garantizado por tipo de enemigo (ver tabla de items del equipo):
        //  - Slime: Flor destello, Resto y Concentrado de slime.
        //  - Custodio de ruinas (golem antiguo): Cuero antiguo x2, Alma antigua x2, Circuito antiguo.
        //    El que lleva "Soul" en el marcador guarda además el Alma pequeña del Santuario.
        //  - Centinela: Alma antigua x2, Circuito antiguo.
        public static List<EnemyLoot.Drop> LootForEnemy(string marker)
        {
            var list = new List<EnemyLoot.Drop>();
            void Add(string material, int amount) =>
                list.Add(new EnemyLoot.Drop { material = ChapterPrefabs.LoadMaterial(material), amount = amount });

            if (marker.StartsWith("Enemy_Slime"))
            {
                Add("FlorDestello", 1);
                Add("RestoSlime", 1);
                Add("ConcentradoSlime", 1);
            }
            else if (marker.StartsWith("Enemy_Ranged"))
            {
                Add("AlmaAntigua", 2);
                Add("CircuitoAntiguo", 1);
            }
            else
            {
                Add("CueroAntiguo", 2);
                Add("AlmaAntigua", 2);
                Add("CircuitoAntiguo", 1);
                if (marker.Contains("Soul")) Add("AlmaPequena", 1);
            }
            return list;
        }
    }
}
