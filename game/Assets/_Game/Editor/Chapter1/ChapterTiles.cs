using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools.Chapter1
{
    // Acceso a los tiles del capítulo: los del tileset de Omar (Tilemaps/Tiles) y
    // los creados a partir de los placeholders (Tilemaps/Tiles/Chapter1).
    public static class ChapterTiles
    {
        private const string TilesFolder = "Assets/_Game/Tilemaps/Tiles";
        private const string ChapterFolder = "Assets/_Game/Tilemaps/Tiles/Chapter1";

        // Índices de tileset_grass_N (ver contacto en docs/claude/art-needed.md)
        private static readonly int[] GrassWeights = { 19, 19, 19, 19, 19, 19, 18, 18, 20, 20, 34 };
        public const int SandIndex = 7;
        private static readonly int[] FlowerIndices = { 1, 2, 3, 4, 5, 6, 7, 9, 10, 11 };

        public static TileBase Grass(int index) => Load($"{TilesFolder}/tileset_grass_{index}.asset");

        public static TileBase RandomGrass(System.Random rng) => Grass(GrassWeights[rng.Next(GrassWeights.Length)]);

        public static TileBase Sand => Grass(SandIndex);

        public static TileBase RandomFlower(System.Random rng) =>
            Load($"{TilesFolder}/tileset_grass_flowers_{FlowerIndices[rng.Next(FlowerIndices.Length)]}.asset");

        public static TileBase Bridge => Chapter("placeholder_tile_bridge");
        public static TileBase Stone => Chapter("placeholder_tile_stone");
        public static TileBase StoneRune => Chapter("placeholder_tile_stone_rune");
        public static TileBase Wood => Chapter("placeholder_tile_wood");
        public static TileBase Ruin => Chapter("placeholder_tile_ruin");

        // Celda de colisión: forma de la celda isométrica completa (invisible en juego).
        public static TileBase Collision
        {
            get
            {
                string path = $"{ChapterFolder}/collision_cell.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile != null) return tile;

                EnsureFolder();
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = AssetDatabase.LoadAllAssetsAtPath($"{TilesFolder}/collision_mask.png")
                    .OfTypeFirst<Sprite>();
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, path);
                return tile;
            }
        }

        private static readonly Dictionary<string, TileBase> cache = new Dictionary<string, TileBase>();

        public static void ClearCache() => cache.Clear();

        private static TileBase Load(string path)
        {
            if (cache.TryGetValue(path, out var t) && t != null) return t;
            t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (t == null) Debug.LogError($"[Chapter1] Falta el tile {path}");
            cache[path] = t;
            return t;
        }

        // Crea (o actualiza) un Tile a partir de un sprite placeholder.
        private static TileBase Chapter(string spriteName)
        {
            string path = $"{ChapterFolder}/{spriteName.Replace("placeholder_", "")}.asset";
            if (cache.TryGetValue(path, out var cached) && cached != null) return cached;

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                EnsureFolder();
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = PlaceholderArtGenerator.Load(spriteName);
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            cache[path] = tile;
            return tile;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(ChapterFolder))
                AssetDatabase.CreateFolder(TilesFolder, "Chapter1");
        }

        private static T OfTypeFirst<T>(this Object[] objects) where T : Object
        {
            foreach (var o in objects)
                if (o is T t) return t;
            return null;
        }
    }
}
