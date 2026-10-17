using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Lienzo de pixel art para generar placeholders por código.
    // Origen arriba a la izquierda (y hacia abajo), como en un editor de pixel art.
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color32[] pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Color32[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public Color32 Get(int x, int y) => InBounds(x, y) ? pixels[y * Width + x] : new Color32(0, 0, 0, 0);

        public void Set(int x, int y, Color32 c)
        {
            if (InBounds(x, y)) pixels[y * Width + x] = c;
        }

        public bool IsFilled(int x, int y) => Get(x, y).a > 0;

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        // Rellena un polígono (vértices en coordenadas de pixel, y hacia abajo).
        public void Poly(Color32 c, params Vector2[] pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                    if (Inside(x + 0.5f, y + 0.5f, pts)) Set(x, y, c);
        }

        private static bool Inside(float x, float y, Vector2[] pts)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                if ((pts[i].y > y) != (pts[j].y > y) &&
                    x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            }
            return inside;
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // Recolorea los pixeles ya pintados que cumplan la condición.
        public void Recolor(System.Func<int, int, Color32, bool> where, Color32 c)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var p = Get(x, y);
                    if (p.a > 0 && where(x, y, p)) Set(x, y, c);
                }
        }

        // Contorno de 1 pixel alrededor de todo lo pintado (estilo del arte del juego).
        public void Outline(Color32 c)
        {
            var add = new List<Vector2Int>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (IsFilled(x, y)) continue;
                    if (IsFilled(x + 1, y) || IsFilled(x - 1, y) || IsFilled(x, y + 1) || IsFilled(x, y - 1))
                        add.Add(new Vector2Int(x, y));
                }
            foreach (var p in add) Set(p.x, p.y, c);
        }

        // Pinta un pixel con probabilidad dada (ruido determinista por semilla).
        public void Speckle(System.Random rng, float chance, Color32 c, System.Func<int, int, Color32, bool> where)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var p = Get(x, y);
                    if (p.a > 0 && where(x, y, p) && rng.NextDouble() < chance) Set(x, y, c);
                }
        }

        public void SavePng(string assetPath)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var flipped = new Color32[pixels.Length];
            for (int y = 0; y < Height; y++)
                System.Array.Copy(pixels, y * Width, flipped, (Height - 1 - y) * Width, Width);
            tex.SetPixels32(flipped);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        public static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        public static Color32 Alpha(Color32 c, byte a) => new Color32(c.r, c.g, c.b, a);
    }
}
