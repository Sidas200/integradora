using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools.Chapter1
{
    // Cerezos de la Isla de las Flores: convierte las dos ilustraciones (fondo negro, alta resolución)
    // en sprites pixel art del mismo alto que cada pino del tileset, con el tronco en el mismo punto,
    // y reemplaza los pinos del tilemap Props alternando los dos cerezos.
    public static class CherryTrees
    {
        private const string Folder = "Assets/_Game/Art/Props/CherryTrees";
        private const string TilesFolder = "Assets/_Game/Tilemaps/Tiles";
        private const string Scene = "Assets/_Game/Scenes/Chapter1/01_FlowerIsland.unity";
        private const float Ppu = 100f;            // igual que tileset_nature
        private const int TrunkOffsetX = 3;        // en los pinos el pivote queda 3 px a la derecha del tronco
        private const int BackgroundMax = 38;      // canal máximo para considerar un pixel fondo negro

        private static readonly string[] Variants = { "a", "b" };

        // Pinos del tileset → alto del sprite y alto del pivote (px)
        private struct Pine { public string tile; public int height; public int pivotY; }

        private static readonly Pine[] Pines =
        {
            new Pine { tile = "tileset_nature_tree_big_long", height = 83, pivotY = 6 },
            new Pine { tile = "tileset_nature_tree_small_long", height = 84, pivotY = 6 },
            new Pine { tile = "tileset_nature_tree_small_med", height = 68, pivotY = 6 },
            new Pine { tile = "tileset_nature_tree_big_short", height = 52, pivotY = 6 },
        };

        [MenuItem("Tools/Claude/Chapter 1/Cherry Trees - Replace Island Trees")]
        public static void ReplaceIslandTrees()
        {
            var tiles = BuildTiles();
            if (tiles == null) return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != Scene)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(Scene);
            }

            var grid = GameObject.Find("Grid_01_FlowerIsland");
            var props = grid != null ? grid.transform.Find("Props")?.GetComponent<Tilemap>() : null;
            if (props == null) { Debug.LogError("[Chapter1] No se encontró el tilemap Props en " + Scene); return; }

            Undo.RegisterCompleteObjectUndo(props, "Cerezos");
            int replaced = 0;
            foreach (var cell in props.cellBounds.allPositionsWithin)
            {
                var tile = props.GetTile(cell);
                if (tile == null || !tiles.TryGetValue(tile.name, out var pair)) continue;
                // Alternar: vecinos en fila o columna quedan con cerezos distintos
                int v = ((cell.x + cell.y) & 1) == 0 ? 0 : 1;
                var m = props.GetTransformMatrix(cell);
                var color = props.GetColor(cell);
                props.SetTile(cell, pair[v]);
                props.SetTransformMatrix(cell, m);
                props.SetColor(cell, color);
                replaced++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Chapter1] {replaced} pinos reemplazados por cerezos en {scene.name}.");
        }

        // Crea (o actualiza) los sprites y tiles de los cerezos. Devuelve pino → [cerezo a, cerezo b].
        public static Dictionary<string, Tile[]> BuildTiles()
        {
            var sources = new List<Color32[]>();
            var sizes = new List<Vector2Int>();
            foreach (var v in Variants)
            {
                var path = $"{Folder}/Source/cherry_{v}_source.png";
                if (!File.Exists(path)) { Debug.LogError("[Chapter1] Falta " + path); return null; }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(path));
                var cut = Cutout(tex, out var size);
                Object.DestroyImmediate(tex);
                sources.Add(cut);
                sizes.Add(size);
            }

            var result = new Dictionary<string, Tile[]>();
            var byHeight = new Dictionary<int, Tile[]>();
            foreach (var pine in Pines)
            {
                if (!byHeight.TryGetValue(pine.height, out var pair))
                {
                    pair = new Tile[Variants.Length];
                    for (int i = 0; i < Variants.Length; i++)
                        pair[i] = MakeTile($"cherry_{Variants[i]}_{pine.height}", sources[i], sizes[i], pine.height, pine.pivotY);
                    byHeight[pine.height] = pair;
                }
                result[pine.tile] = pair;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        // Quita el fondo negro y recorta al contorno del árbol.
        private static Color32[] Cutout(Texture2D tex, out Vector2Int size)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                bool bg = c.r <= BackgroundMax && c.g <= BackgroundMax && c.b <= BackgroundMax;
                px[i].a = bg ? (byte)0 : (byte)255;
                if (bg) continue;
                int x = i % w, y = i / w;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            var cut = new Color32[cw * ch];
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    cut[y * cw + x] = px[(y + minY) * w + x + minX];
            size = new Vector2Int(cw, ch);
            return cut;
        }

        // Reduce por promedio de área (solo pixeles opacos) a alto = height; alfa 0/1 para que quede nítido.
        private static Color32[] Downscale(Color32[] src, Vector2Int size, int height, out int width)
        {
            float scale = (float)size.y / height;
            width = Mathf.Max(1, Mathf.RoundToInt(size.x / scale));
            var dst = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int y0 = Mathf.FloorToInt(y * scale), y1 = Mathf.Min(size.y, Mathf.CeilToInt((y + 1) * scale));
                for (int x = 0; x < width; x++)
                {
                    int x0 = Mathf.FloorToInt(x * scale), x1 = Mathf.Min(size.x, Mathf.CeilToInt((x + 1) * scale));
                    long r = 0, g = 0, b = 0; int opaque = 0, total = 0;
                    for (int sy = y0; sy < y1; sy++)
                        for (int sx = x0; sx < x1; sx++)
                        {
                            var c = src[sy * size.x + sx];
                            total++;
                            if (c.a == 0) continue;
                            opaque++; r += c.r; g += c.g; b += c.b;
                        }
                    dst[y * width + x] = opaque * 2 >= total && opaque > 0
                        ? new Color32((byte)(r / opaque), (byte)(g / opaque), (byte)(b / opaque), 255)
                        : new Color32(0, 0, 0, 0);
                }
            }
            return dst;
        }

        private static Tile MakeTile(string name, Color32[] src, Vector2Int size, int height, int pivotY)
        {
            var px = Downscale(src, size, height, out int width);

            // Tronco: columnas cafés en la parte baja del árbol
            int trunkSum = 0, trunkCount = 0, trunkBottom = -1;
            for (int y = 0; y < height / 3; y++)
                for (int x = 0; x < width; x++)
                {
                    var c = px[y * width + x];
                    if (c.a == 0 || !(c.r > c.g && c.g >= c.b && c.r < 200)) continue;
                    trunkSum += x; trunkCount++;
                    if (trunkBottom < 0) trunkBottom = y;
                }
            float trunkX = trunkCount > 0 ? (float)trunkSum / trunkCount : width * 0.5f;
            float baseY = trunkBottom >= 0 ? Mathf.Max(trunkBottom, pivotY * 0.5f) : pivotY;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            var path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = Ppu;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.spritePivot = new Vector2((trunkX + 0.5f + TrunkOffsetX) / width, baseY / height);
            ti.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var tilePath = $"{TilesFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }
            return tile;
        }
    }
}
