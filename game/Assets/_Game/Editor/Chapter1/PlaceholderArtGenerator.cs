using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Genera el arte placeholder del capítulo 1 (pixel art, PPU 100, Point, sin compresión).
    // Todo se dibuja por código para que sea reproducible. Los PNG se pueden reemplazar
    // por arte final con el MISMO nombre y pivote sin romper prefabs (ver docs/claude/art-needed.md).
    public static class PlaceholderArtGenerator
    {
        public const string Folder = "Assets/_Game/Art/Placeholders";
        public const int PixelsPerUnit = 100;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static Color32 H(string hex) => PixelCanvas.Hex(hex);

        // Pivote: (0.5, 0.5) para tiles; base del sprite para props y personajes.
        private struct Entry
        {
            public string name;
            public Vector2 pivot;
            public Func<PixelCanvas> draw;
        }

        public static string PathOf(string name) => $"{Folder}/{name}.png";

        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(name));

        [MenuItem("Tools/Claude/Chapter 1/2 - Generate Placeholder Art")]
        public static void GenerateAll()
        {
            var entries = BuildEntries();
            foreach (var e in entries)
                e.draw().SavePng(PathOf(e.name));

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var e in entries)
                ConfigureImporter(PathOf(e.name), e.pivot);

            Debug.Log($"[Chapter1] {entries.Count} placeholders generados en {Folder}");
        }

        private static void ConfigureImporter(string path, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Vector2 Feet(int height, int feetRow = 1) => new Vector2(0.5f, feetRow / (float)height);

        private static List<Entry> BuildEntries()
        {
            var tileCenter = new Vector2(0.5f, 0.5f);
            return new List<Entry>
            {
                // --- Tiles (mismo tamaño y silueta que tileset_grass) ---
                new Entry { name = "placeholder_tile_stone", pivot = tileCenter, draw = () => CubeTile(H("9aa3b8"), H("b9c1d3"), H("6f7891"), H("525a72"), 11, false) },
                new Entry { name = "placeholder_tile_stone_rune", pivot = tileCenter, draw = () => CubeTile(H("8d97b0"), H("b9c1d3"), H("6f7891"), H("525a72"), 12, true) },
                new Entry { name = "placeholder_tile_wood", pivot = tileCenter, draw = WoodTile },
                new Entry { name = "placeholder_tile_ruin", pivot = tileCenter, draw = () => CubeTile(H("a3b79a"), H("c3d4b4"), H("7d869e"), H("5b637a"), 13, false) },
                new Entry { name = "placeholder_tile_bridge", pivot = tileCenter, draw = BridgeTile },
                new Entry { name = "placeholder_exit_marker", pivot = tileCenter, draw = ExitMarker },

                // --- Naturaleza ---
                new Entry { name = "placeholder_tree_tall", pivot = Feet(92, 5), draw = () => Pine(38, 92, 6, false, Forest) },
                new Entry { name = "placeholder_tree_short", pivot = Feet(60, 5), draw = () => Pine(36, 60, 4, false, Forest) },
                new Entry { name = "placeholder_tree_dark", pivot = Feet(92, 5), draw = () => Pine(38, 92, 6, true, DarkForest) },
                new Entry { name = "placeholder_tree_dead", pivot = Feet(64, 4), draw = DeadTree },
                new Entry { name = "placeholder_bush", pivot = Feet(14, 2), draw = Bush },
                new Entry { name = "placeholder_rock_small", pivot = Feet(12, 2), draw = () => Rock(16, 12, 3) },
                new Entry { name = "placeholder_rock_large", pivot = Feet(26, 3), draw = () => Rock(32, 26, 7) },
                new Entry { name = "placeholder_crystal", pivot = Feet(24, 2), draw = CrystalCluster },
                new Entry { name = "placeholder_flower_glow", pivot = Feet(12, 1), draw = FlowerGlow },
                new Entry { name = "placeholder_cloud", pivot = tileCenter, draw = Cloud },

                // --- Estructuras y props ---
                new Entry { name = "placeholder_cabin", pivot = Feet(80, 4), draw = Cabin },
                new Entry { name = "placeholder_tent", pivot = Feet(40, 3), draw = Tent },
                new Entry { name = "placeholder_forge", pivot = Feet(44, 4), draw = Forge },
                new Entry { name = "placeholder_campfire", pivot = Feet(18, 3), draw = Campfire },
                new Entry { name = "placeholder_crate", pivot = Feet(18, 2), draw = () => IsoBox(18, 18, 7, 8, H("b07a44"), H("8c5a30"), H("6d4424"), true) },
                new Entry { name = "placeholder_barrel", pivot = Feet(18, 2), draw = Barrel },
                new Entry { name = "placeholder_desk", pivot = Feet(26, 3), draw = Desk },
                new Entry { name = "placeholder_bed", pivot = Feet(26, 3), draw = Bed },
                new Entry { name = "placeholder_table", pivot = Feet(20, 3), draw = () => IsoBox(24, 20, 10, 6, H("a0703f"), H("7d532c"), H("5e3d20"), false) },
                new Entry { name = "placeholder_wall_left_wood", pivot = new Vector2(1f, 0f), draw = () => Wall(true, H("8c5a30"), H("6d4424")) },
                new Entry { name = "placeholder_wall_right_wood", pivot = new Vector2(0f, 0f), draw = () => Wall(false, H("7d532c"), H("5e3d20")) },
                new Entry { name = "placeholder_wall_left_stone", pivot = new Vector2(1f, 0f), draw = () => Wall(true, H("6f7891"), H("525a72")) },
                new Entry { name = "placeholder_wall_right_stone", pivot = new Vector2(0f, 0f), draw = () => Wall(false, H("5d6680"), H("434a60")) },
                new Entry { name = "placeholder_sword_rack", pivot = Feet(38, 3), draw = SwordRack },
                new Entry { name = "placeholder_sword_rack_empty", pivot = Feet(38, 3), draw = SwordRackEmpty },
                new Entry { name = "placeholder_save_crystal", pivot = Feet(34, 3), draw = SaveCrystal },
                new Entry { name = "placeholder_pillar", pivot = Feet(50, 4), draw = () => IsoBox(16, 50, 7, 40, H("a9b1c4"), H("7d869e"), H("5b637a"), false) },
                new Entry { name = "placeholder_brazier", pivot = Feet(28, 2), draw = Brazier },
                new Entry { name = "placeholder_maiden_statue", pivot = Feet(62, 2), draw = MaidenStatue },
                new Entry { name = "placeholder_seal_gate", pivot = Feet(88, 4), draw = SealGate },
                new Entry { name = "placeholder_seal_barrier", pivot = Feet(60, 1), draw = SealBarrier },

                // --- Personajes ---
                new Entry { name = "placeholder_npc_researcher", pivot = Feet(50, 1), draw = Researcher },
                new Entry { name = "placeholder_maiden_spirit", pivot = Feet(52, 1), draw = MaidenSpirit },
                new Entry { name = "placeholder_hikari", pivot = Feet(50, 1), draw = Hikari },
                new Entry { name = "placeholder_enemy_ranged", pivot = Feet(34, 1), draw = RangedEnemy },
                new Entry { name = "placeholder_guardian", pivot = Feet(76, 2), draw = Guardian },

                // --- Efectos y UI ---
                new Entry { name = "placeholder_shadow_blob", pivot = tileCenter, draw = BlobShadow },
                new Entry { name = "placeholder_telegraph_circle", pivot = tileCenter, draw = TelegraphCircle },
                new Entry { name = "placeholder_telegraph_rect", pivot = new Vector2(0f, 0.5f), draw = TelegraphRect },
                new Entry { name = "placeholder_projectile", pivot = tileCenter, draw = Projectile },
                new Entry { name = "placeholder_interact_marker", pivot = Feet(12, 0), draw = InteractMarker },
                new Entry { name = "placeholder_icon_sword", pivot = tileCenter, draw = SwordIcon },
                new Entry { name = "placeholder_icon_permit", pivot = tileCenter, draw = PermitIcon },
                new Entry { name = "placeholder_portrait_tsuki", pivot = tileCenter, draw = () => Portrait(TsukiHead()) },
                new Entry { name = "placeholder_portrait_researcher", pivot = tileCenter, draw = () => Portrait(Researcher()) },
                new Entry { name = "placeholder_portrait_maiden", pivot = tileCenter, draw = () => Portrait(MaidenSpirit()) },
                new Entry { name = "placeholder_portrait_hikari", pivot = tileCenter, draw = () => Portrait(Hikari()) },
                new Entry { name = "placeholder_portrait_guardian", pivot = tileCenter, draw = GuardianPortrait },
                new Entry { name = "placeholder_intro_sky", pivot = tileCenter, draw = IntroSky },
            };
        }

        // ---------------------------------------------------------------- tiles

        // Silueta exacta del tile de pasto (cubo isométrico de 32x32).
        private static bool InCube(int x, int y)
        {
            float d = Mathf.Abs(x - 15.5f);
            if (y <= 7) return d < 2 + 2 * y;
            if (y <= 24) return true;
            return d < 2 + 2 * (31 - y);
        }

        private static bool InTopFace(int x, int y)
        {
            if (y > 15) return false;
            float d = Mathf.Abs(x - 15.5f);
            return y <= 7 ? d < 2 + 2 * y : d < 2 + 2 * (15 - y);
        }

        private static PixelCanvas CubeTile(Color32 top, Color32 topLight, Color32 left, Color32 right, int seed, bool rune)
        {
            var c = new PixelCanvas(32, 32);
            var rng = new System.Random(seed);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    if (!InCube(x, y)) continue;
                    if (InTopFace(x, y)) c.Set(x, y, top);
                    else c.Set(x, y, x < 16 ? left : right);
                }

            // Borde claro del lado superior y grietas
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                    if (InTopFace(x, y) && (!InTopFace(x, y - 1) || !InTopFace(x - 1, y) && x < 16)) c.Set(x, y, topLight);

            var dark = Shade(top, 0.82f);
            c.Speckle(rng, 0.08f, dark, (x, y, p) => InTopFace(x, y));
            c.Speckle(rng, 0.10f, Shade(left, 0.85f), (x, y, p) => !InTopFace(x, y) && x < 16);
            c.Speckle(rng, 0.10f, Shade(right, 0.85f), (x, y, p) => !InTopFace(x, y) && x >= 16);
            // Juntas de bloques en los lados
            for (int x = 0; x < 32; x++)
            {
                int yy = 20 + (x < 16 ? x / 2 : (31 - x) / 2);
                if (InCube(x, yy) && !InTopFace(x, yy)) c.Set(x, yy, Shade(x < 16 ? left : right, 0.75f));
            }

            if (rune)
            {
                var glow = H("ff4d6d");
                c.Ellipse(15.5f, 7.5f, 7, 3.5f, glow);
                c.Ellipse(15.5f, 7.5f, 5, 2.5f, top);
                c.Rect(15, 4, 2, 8, glow);
                c.Rect(10, 7, 12, 1, glow);
            }
            return c;
        }

        private static PixelCanvas WoodTile()
        {
            var c = CubeTile(H("b9844c"), H("d39c5c"), H("8c5a30"), H("6d4424"), 21, false);
            var seam = H("8f6136");
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                    if (InTopFace(x, y) && ((x / 2 + y) % 5 == 0)) c.Set(x, y, seam);
            return c;
        }

        private static PixelCanvas BridgeTile()
        {
            var c = new PixelCanvas(32, 32);
            var plank = H("b07a44");
            var plankDark = H("8c5a30");
            var rope = H("5e3d20");
            for (int y = 0; y < 21; y++)
                for (int x = 0; x < 32; x++)
                {
                    if (InTopFace(x, y)) c.Set(x, y, ((x / 2 + y) % 4 == 0) ? plankDark : plank);
                    else if (InTopFace(x, y - 4) && y > 8) c.Set(x, y, x < 16 ? plankDark : rope);
                }
            return c;
        }

        private static PixelCanvas ExitMarker()
        {
            var c = new PixelCanvas(32, 32);
            var glow = PixelCanvas.Alpha(H("ffe9a8"), 150);
            var rim = PixelCanvas.Alpha(H("fff6d6"), 230);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                    if (InTopFace(x, y)) c.Set(x, y, (!InTopFace(x, y - 1) || !InTopFace(x, y + 1)) ? rim : glow);
            return c;
        }

        // ---------------------------------------------------------------- naturaleza

        private struct Palette { public Color32 light, mid, dark, deep, outline, trunk, trunkDark; }

        private static readonly Palette Forest = new Palette
        {
            light = H("8ee060"), mid = H("4fbf55"), dark = H("2f9a52"), deep = H("247a4a"),
            outline = H("1b5a3c"), trunk = H("9a5b2c"), trunkDark = H("6e3d1e")
        };

        private static readonly Palette DarkForest = new Palette
        {
            light = H("5fa88a"), mid = H("3b7f6e"), dark = H("2b5f58"), deep = H("204748"),
            outline = H("152f33"), trunk = H("6b4a3a"), trunkDark = H("47302a")
        };

        // Pino escalonado como el de la referencia: copas en capas, sombra a la derecha.
        private static PixelCanvas Pine(int w, int h, int tiers, bool sparse, Palette pal)
        {
            var c = new PixelCanvas(w, h);
            int cx = w / 2;
            int trunkTop = h - (tiers >= 6 ? 34 : 20);
            int trunkW = tiers >= 6 ? 6 : 6;

            // Tronco con raíces
            c.Rect(cx - trunkW / 2, trunkTop, trunkW, h - 4 - trunkTop, pal.trunk);
            c.Rect(cx + 1, trunkTop, trunkW / 2 - 1, h - 4 - trunkTop, pal.trunkDark);
            c.Poly(pal.trunk, new Vector2(cx - 7, h - 3), new Vector2(cx - 3, h - 9), new Vector2(cx + 3, h - 9), new Vector2(cx + 8, h - 3));
            c.Poly(pal.trunkDark, new Vector2(cx + 1, h - 3), new Vector2(cx + 2, h - 9), new Vector2(cx + 3, h - 9), new Vector2(cx + 8, h - 3));
            if (tiers >= 6)
            {
                c.Line(cx - 3, trunkTop + 8, cx - 10, trunkTop + 3, pal.trunk);
                c.Line(cx + 3, trunkTop + 14, cx + 9, trunkTop + 9, pal.trunk);
            }

            // Copa por capas (de abajo hacia arriba)
            int canopyBottom = trunkTop + (tiers >= 6 ? 2 : 4);
            int top = 2;
            float span = canopyBottom - top;
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)(tiers - 1);
                float halfW = Mathf.Lerp(w / 2f - 1f, 5f, t);
                int bottom = Mathf.RoundToInt(canopyBottom - i * span / tiers);
                float tierH = span / tiers + 5f;
                c.Ellipse(cx, bottom - tierH * 0.45f, halfW, tierH * 0.6f, pal.mid);
                // Banda inferior oscura (separa las capas)
                for (int x = Mathf.FloorToInt(cx - halfW); x <= cx + halfW; x++)
                {
                    int yb = bottom - 1 - ((x + i) % 3 == 0 ? 1 : 0);
                    if (c.IsFilled(x, yb)) c.Set(x, yb, pal.dark);
                }
            }

            // Sombra lado derecho y luz lado izquierdo/superior
            c.Recolor((x, y, p) => p.Equals(pal.mid) && x > cx + 2, pal.dark);
            c.Recolor((x, y, p) => p.Equals(pal.mid) && x < cx - 3 && (x + y) % 7 != 0, pal.light);
            c.Recolor((x, y, p) => p.Equals(pal.dark) && x > cx + 6 && (x + y) % 5 == 0, pal.deep);
            if (sparse)
            {
                var rng = new System.Random(w * h);
                c.Speckle(rng, 0.06f, pal.deep, (x, y, p) => y < canopyBottom);
            }

            // Pasto en la base
            c.Rect(cx - 8, h - 3, 3, 2, pal.light);
            c.Rect(cx + 5, h - 3, 3, 2, pal.mid);
            c.Outline(pal.outline);
            return c;
        }

        private static PixelCanvas DeadTree()
        {
            var c = new PixelCanvas(32, 64);
            var bark = H("7a6a5c");
            var barkDark = H("564a40");
            c.Rect(13, 18, 6, 42, bark);
            c.Rect(16, 18, 3, 42, barkDark);
            c.Poly(bark, new Vector2(6, 61), new Vector2(12, 54), new Vector2(20, 54), new Vector2(26, 61));
            c.Line(13, 30, 5, 18, bark); c.Line(12, 30, 4, 18, bark);
            c.Line(18, 24, 26, 12, bark); c.Line(19, 24, 27, 12, barkDark);
            c.Line(16, 18, 14, 6, bark); c.Line(17, 18, 15, 6, barkDark);
            c.Line(5, 18, 2, 14, bark);
            // Ramas rotas con brillo carmesí (mundo dañado)
            c.Set(26, 12, H("c23b4f")); c.Set(14, 6, H("c23b4f"));
            c.Outline(H("2e2622"));
            return c;
        }

        private static PixelCanvas Bush()
        {
            var c = new PixelCanvas(22, 14);
            c.Ellipse(7, 8, 6, 5, Forest.mid);
            c.Ellipse(14, 7, 7, 6, Forest.mid);
            c.Ellipse(11, 10, 8, 3.5f, Forest.dark);
            c.Recolor((x, y, p) => p.Equals(Forest.mid) && y < 6 && x < 12, Forest.light);
            c.Outline(Forest.outline);
            return c;
        }

        private static PixelCanvas Rock(int w, int h, int seed)
        {
            var c = new PixelCanvas(w, h);
            var light = H("b8bcc8"); var mid = H("8b90a0"); var dark = H("626778");
            c.Ellipse(w / 2f, h * 0.58f, w / 2f - 1, h * 0.42f, mid);
            c.Recolor((x, y, p) => y < h * 0.45f && x < w * 0.6f, light);
            c.Recolor((x, y, p) => x > w * 0.65f || y > h * 0.8f, dark);
            c.Speckle(new System.Random(seed), 0.08f, dark, (x, y, p) => p.Equals(mid) || p.Equals(light));
            c.Outline(H("3c3f4d"));
            return c;
        }

        private static PixelCanvas CrystalCluster()
        {
            var c = new PixelCanvas(16, 24);
            var light = H("c9f6ff"); var mid = H("62d6f2"); var dark = H("2a8fc0");
            c.Poly(mid, new Vector2(6, 22), new Vector2(4, 8), new Vector2(7, 1), new Vector2(10, 8), new Vector2(9, 22));
            c.Poly(dark, new Vector2(9, 22), new Vector2(10, 12), new Vector2(14, 7), new Vector2(15, 14), new Vector2(12, 22));
            c.Poly(mid, new Vector2(1, 22), new Vector2(1, 14), new Vector2(4, 11), new Vector2(6, 22));
            c.Line(6, 3, 6, 18, light);
            c.Outline(H("1b4f73"));
            return c;
        }

        private static PixelCanvas FlowerGlow()
        {
            var c = new PixelCanvas(12, 12);
            c.Line(6, 11, 6, 5, Forest.dark);
            c.Ellipse(6, 4, 3.5f, 3.5f, H("ffe36e"));
            c.Set(6, 4, H("fff9d9")); c.Set(5, 4, H("fff9d9"));
            c.Rect(3, 8, 2, 1, Forest.mid);
            c.Outline(H("8a6b12"));
            return c;
        }

        private static PixelCanvas Cloud()
        {
            var c = new PixelCanvas(72, 28);
            var white = PixelCanvas.Alpha(H("f4f8ff"), 235);
            var shade = PixelCanvas.Alpha(H("c9d6ee"), 235);
            c.Ellipse(20, 16, 14, 9, white);
            c.Ellipse(38, 12, 18, 11, white);
            c.Ellipse(56, 17, 13, 8, white);
            c.Recolor((x, y, p) => y > 19, shade);
            return c;
        }

        // ---------------------------------------------------------------- estructuras

        // Caja isométrica genérica: base con vértice inferior en (cx, h-1-feet).
        private static PixelCanvas IsoBox(int w, int h, int halfW, int height, Color32 top, Color32 left, Color32 right, bool planks)
        {
            var c = new PixelCanvas(w, h);
            float cx = w / 2f;
            float by = h - 2;
            float halfH = halfW / 2f;
            var bottom = new Vector2(cx, by);
            var l = new Vector2(cx - halfW, by - halfH);
            var r = new Vector2(cx + halfW, by - halfH);
            var up = new Vector2(0, -height);
            c.Poly(left, l, bottom, bottom + up, l + up);
            c.Poly(right, bottom, r, r + up, bottom + up);
            c.Poly(top, l + up, bottom + up, r + up, new Vector2(cx, by - 2 * halfH) + up);
            if (planks)
            {
                c.Line((int)(cx - halfW), (int)(by - halfH - height / 2), (int)cx, (int)(by - height / 2), Shade(left, 0.8f));
                c.Line((int)cx, (int)(by - height / 2), (int)(cx + halfW), (int)(by - halfH - height / 2), Shade(right, 0.8f));
            }
            c.Outline(Shade(right, 0.55f));
            return c;
        }

        private static PixelCanvas Cabin()
        {
            var c = new PixelCanvas(84, 80);
            var wallL = H("a8763f"); var wallR = H("7d532c"); var beam = H("5e3d20");
            var roofL = H("8e3b3b"); var roofR = H("6b2b30"); var roofEdge = H("4a1d22");
            float cx = 42, by = 76;
            var bottom = new Vector2(cx, by); var l = new Vector2(cx - 34, by - 17); var r = new Vector2(cx + 34, by - 17);
            var up = new Vector2(0, -24);
            c.Poly(wallL, l, bottom, bottom + up, l + up);
            c.Poly(wallR, bottom, r, r + up, bottom + up);
            // Vigas de madera
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                var a = Vector2.Lerp(l, bottom, t); c.Line((int)a.x, (int)a.y, (int)a.x, (int)(a.y - 24), beam);
                var b = Vector2.Lerp(bottom, r, t); c.Line((int)b.x, (int)b.y, (int)b.x, (int)(b.y - 24), beam);
            }
            // Puerta (cara izquierda) y ventana (cara derecha)
            var d0 = Vector2.Lerp(l, bottom, 0.45f); var d1 = Vector2.Lerp(l, bottom, 0.7f);
            c.Poly(H("3a2414"), d0, d1, d1 + new Vector2(0, -17), d0 + new Vector2(0, -17));
            c.Set((int)d1.x - 2, (int)d1.y - 8, H("e0b04a"));
            var w0 = Vector2.Lerp(bottom, r, 0.35f) + new Vector2(0, -9); var w1 = Vector2.Lerp(bottom, r, 0.6f) + new Vector2(0, -9);
            c.Poly(H("ffd27a"), w0, w1, w1 + new Vector2(0, -8), w0 + new Vector2(0, -8));
            // Techo a cuatro aguas
            var apex = new Vector2(cx, 4);
            var rl = l + up + new Vector2(-5, 3); var rb = bottom + up + new Vector2(0, 5); var rr = r + up + new Vector2(5, 3);
            c.Poly(roofL, rl, rb, apex);
            c.Poly(roofR, rb, rr, apex);
            for (int y = 10; y < 60; y += 6)
                for (int x = 0; x < 84; x++)
                {
                    var p = c.Get(x, y);
                    if (p.Equals(roofL) || p.Equals(roofR)) c.Set(x, y, roofEdge);
                }
            // Chimenea
            c.Rect(56, 14, 6, 14, H("7a7f8e")); c.Rect(59, 14, 3, 14, H("5b5f6e"));
            c.Outline(H("2f1d12"));
            return c;
        }

        private static PixelCanvas Tent()
        {
            var c = new PixelCanvas(48, 40);
            var canvasL = H("e2d3a8"); var canvasR = H("b9a77c"); var dark = H("3a2f22");
            c.Poly(canvasL, new Vector2(3, 36), new Vector2(24, 4), new Vector2(24, 38));
            c.Poly(canvasR, new Vector2(24, 4), new Vector2(45, 32), new Vector2(24, 38));
            c.Poly(dark, new Vector2(16, 37), new Vector2(23, 17), new Vector2(24, 37));
            c.Line(24, 1, 24, 6, H("6d4424"));
            c.Rect(26, 2, 6, 3, H("c23b4f"));
            c.Outline(H("4a3d2a"));
            return c;
        }

        private static PixelCanvas Forge()
        {
            var c = IsoBox(44, 44, 18, 16, H("8b90a0"), H("6b7080"), H("4f5362"), false);
            // Chimenea trasera
            c.Rect(24, 2, 9, 22, H("5b5f6e")); c.Rect(29, 2, 4, 22, H("434756"));
            // Boca de la forja encendida
            c.Poly(H("2a1a14"), new Vector2(8, 34), new Vector2(18, 39), new Vector2(18, 31), new Vector2(8, 26));
            c.Poly(H("ff8a2a"), new Vector2(10, 33), new Vector2(16, 36), new Vector2(16, 31), new Vector2(10, 28));
            c.Set(12, 31, H("ffe36e")); c.Set(13, 32, H("ffe36e"));
            // Yunque encima
            c.Rect(12, 12, 12, 3, H("3d404c")); c.Rect(15, 15, 6, 5, H("3d404c"));
            c.Outline(H("262833"));
            return c;
        }

        private static PixelCanvas Campfire()
        {
            var c = new PixelCanvas(18, 18);
            c.Line(2, 15, 15, 12, H("6d4424")); c.Line(2, 12, 15, 15, H("8c5a30"));
            c.Poly(H("ff6a2a"), new Vector2(4, 13), new Vector2(9, 1), new Vector2(14, 13));
            c.Poly(H("ffd24a"), new Vector2(6, 13), new Vector2(9, 5), new Vector2(12, 13));
            c.Outline(H("3a2414"));
            return c;
        }

        private static PixelCanvas Barrel()
        {
            var c = new PixelCanvas(14, 18);
            c.Ellipse(7, 9, 6, 8, H("9a6638"));
            c.Recolor((x, y, p) => x > 8, H("7a4d28"));
            c.Rect(1, 5, 12, 1, H("4f5362")); c.Rect(1, 12, 12, 1, H("4f5362"));
            c.Ellipse(7, 3, 5, 2, H("b07a44"));
            c.Outline(H("3a2414"));
            return c;
        }

        private static PixelCanvas Desk()
        {
            var c = IsoBox(34, 26, 14, 8, H("a0703f"), H("7d532c"), H("5e3d20"), false);
            c.Rect(10, 6, 6, 3, H("f2ecd8")); c.Rect(17, 8, 5, 3, H("e6dcc0"));
            c.Rect(22, 4, 3, 5, H("4a6fa5"));
            c.Set(12, 7, H("3a3a3a")); c.Set(13, 7, H("3a3a3a"));
            return c;
        }

        private static PixelCanvas Bed()
        {
            var c = IsoBox(34, 26, 14, 6, H("d9d2c4"), H("7d532c"), H("5e3d20"), false);
            c.Ellipse(10, 9, 4, 2, H("ffffff"));
            c.Poly(H("8e3b3b"), new Vector2(14, 14), new Vector2(26, 8), new Vector2(31, 11), new Vector2(19, 17));
            return c;
        }

        // Muro interior que sigue un borde de celda (izquierdo sube hacia la derecha).
        private static PixelCanvas Wall(bool left, Color32 face, Color32 dark)
        {
            var c = new PixelCanvas(16, 44);
            if (left) c.Poly(face, new Vector2(0, 8), new Vector2(16, 0), new Vector2(16, 36), new Vector2(0, 44));
            else c.Poly(face, new Vector2(0, 0), new Vector2(16, 8), new Vector2(16, 44), new Vector2(0, 36));
            c.Recolor((x, y, p) => (y + (left ? x / 2 : -x / 2)) % 9 == 0, dark);
            c.Recolor((x, y, p) => left ? (y < 10 - x / 2 + 2) : (y < x / 2 + 2), Shade(face, 1.2f));
            return c;
        }

        private static PixelCanvas SwordRack()
        {
            var c = new PixelCanvas(16, 38);
            DrawCrimsonSword(c, 8, 2, 28);
            c.Poly(H("7d869e"), new Vector2(1, 36), new Vector2(3, 29), new Vector2(13, 29), new Vector2(15, 36));
            c.Rect(4, 29, 8, 2, H("a9b1c4"));
            c.Outline(H("2a1218"));
            return c;
        }

        private static PixelCanvas SwordRackEmpty()
        {
            var c = new PixelCanvas(16, 38);
            c.Poly(H("7d869e"), new Vector2(1, 36), new Vector2(3, 29), new Vector2(13, 29), new Vector2(15, 36));
            c.Rect(4, 29, 8, 2, H("a9b1c4"));
            c.Rect(7, 29, 2, 1, H("3c3f4d")); // ranura vacía
            c.Outline(H("2a1218"));
            return c;
        }

        // Espada carmesí sin filo (vertical, punta abajo clavada).
        private static void DrawCrimsonSword(PixelCanvas c, int cx, int top, int length)
        {
            var blade = H("b5283f"); var edge = H("e0566e"); var dark = H("7a1528");
            var gold = H("e0b04a"); var grip = H("3a2414");
            c.Rect(cx - 1, top, 2, 6, grip);
            c.Rect(cx - 4, top + 6, 8, 2, gold);
            c.Rect(cx - 2, top + 8, 4, length - 10, blade);
            c.Rect(cx - 2, top + 8, 1, length - 10, edge);
            c.Rect(cx + 1, top + 8, 1, length - 10, dark);
            c.Set(cx, top - 1, gold);
        }

        private static PixelCanvas SaveCrystal()
        {
            var c = new PixelCanvas(18, 34);
            c.Poly(H("7d869e"), new Vector2(2, 32), new Vector2(4, 25), new Vector2(14, 25), new Vector2(16, 32));
            c.Rect(5, 23, 8, 2, H("a9b1c4"));
            c.Poly(H("7ce0ff"), new Vector2(9, 1), new Vector2(15, 10), new Vector2(9, 20), new Vector2(3, 10));
            c.Poly(H("3fa7e0"), new Vector2(9, 1), new Vector2(15, 10), new Vector2(9, 20));
            c.Line(7, 5, 6, 10, H("e8fbff"));
            c.Outline(H("1b3f63"));
            return c;
        }

        private static PixelCanvas Brazier()
        {
            var c = new PixelCanvas(14, 28);
            c.Rect(6, 14, 3, 12, H("5b5f6e"));
            c.Rect(3, 25, 9, 2, H("434756"));
            c.Poly(H("7a7f8e"), new Vector2(1, 11), new Vector2(13, 11), new Vector2(10, 16), new Vector2(4, 16));
            c.Poly(H("ff6a2a"), new Vector2(2, 11), new Vector2(7, 0), new Vector2(12, 11));
            c.Poly(H("ffd24a"), new Vector2(4, 11), new Vector2(7, 4), new Vector2(10, 11));
            c.Outline(H("262833"));
            return c;
        }

        private static PixelCanvas SealGate()
        {
            var c = new PixelCanvas(76, 88);
            var stone = H("9aa3b8"); var stoneDark = H("6f7891"); var stoneDeep = H("525a72");
            // Pilares
            c.Rect(4, 18, 12, 66, stone); c.Rect(11, 18, 5, 66, stoneDark);
            c.Rect(60, 18, 12, 66, stone); c.Rect(67, 18, 5, 66, stoneDark);
            c.Rect(2, 80, 16, 6, stoneDeep); c.Rect(58, 80, 16, 6, stoneDeep);
            // Arco superior
            c.Rect(2, 8, 72, 12, stone);
            c.Rect(2, 16, 72, 4, stoneDark);
            c.Poly(stone, new Vector2(20, 8), new Vector2(38, 0), new Vector2(56, 8));
            // Runa central
            c.Ellipse(38, 9, 5, 4, H("ff4d6d"));
            c.Ellipse(38, 9, 3, 2, H("ffd1da"));
            c.Rect(6, 30, 2, 8, H("ff4d6d")); c.Rect(62, 30, 2, 8, H("ff4d6d"));
            c.Outline(H("2c3042"));
            return c;
        }

        private static PixelCanvas SealBarrier()
        {
            var c = new PixelCanvas(44, 60);
            var field = PixelCanvas.Alpha(H("e0304f"), 120);
            var bright = PixelCanvas.Alpha(H("ff8fa3"), 210);
            c.Rect(0, 0, 44, 60, field);
            for (int y = 0; y < 60; y++)
                for (int x = 0; x < 44; x++)
                    if ((x + y * 2) % 11 == 0) c.Set(x, y, bright);
            c.Ellipse(22, 26, 12, 12, bright);
            c.Ellipse(22, 26, 10, 10, field);
            c.Rect(21, 14, 2, 24, bright); c.Rect(10, 25, 24, 2, bright);
            return c;
        }

        private static PixelCanvas MaidenStatue()
        {
            var c = new PixelCanvas(28, 62);
            var stone = new HumanStyle
            {
                skin = H("b9c1d3"), hair = H("8d97b0"), hairDark = H("6f7891"), coat = H("a9b1c4"), coatDark = H("7d869e"),
                pants = H("a9b1c4"), boots = H("7d869e"), eyes = H("6f7891"), longHair = true, dress = true, outline = H("3c4256")
            };
            var figure = Human(stone);
            Paste(c, figure, 4, 0);
            // Manos juntas (rezando) y pedestal
            c.Rect(12, 24, 4, 4, stone.skin);
            c.Poly(H("7d869e"), new Vector2(1, 61), new Vector2(3, 50), new Vector2(25, 50), new Vector2(27, 61));
            c.Rect(3, 49, 22, 2, H("c9d0de"));
            c.Set(14, 4, H("ff4d6d"));
            c.Outline(H("2c3042"));
            return c;
        }

        // ---------------------------------------------------------------- personajes

        private struct HumanStyle
        {
            public Color32 skin, hair, hairDark, coat, coatDark, pants, boots, eyes, outline;
            public bool longHair, glasses, dress, mark;
        }

        // Figura humana de 20x50 con proporciones de Tsuki.
        private static PixelCanvas Human(HumanStyle s)
        {
            var c = new PixelCanvas(20, 50);
            if (s.longHair) c.Rect(4, 8, 12, 18, s.hairDark);

            // Piernas y botas
            if (!s.dress)
            {
                c.Rect(6, 36, 3, 11, s.pants); c.Rect(11, 36, 3, 11, s.pants);
                c.Rect(5, 45, 4, 3, s.boots); c.Rect(11, 45, 4, 3, s.boots);
            }
            else
            {
                c.Poly(s.coat, new Vector2(4, 30), new Vector2(16, 30), new Vector2(18, 47), new Vector2(2, 47));
                c.Poly(s.coatDark, new Vector2(11, 30), new Vector2(16, 30), new Vector2(18, 47), new Vector2(12, 47));
                c.Rect(6, 47, 3, 2, s.boots); c.Rect(11, 47, 3, 2, s.boots);
            }

            // Torso y abrigo
            c.Poly(s.coat, new Vector2(5, 18), new Vector2(15, 18), new Vector2(16, s.dress ? 31 : 39), new Vector2(4, s.dress ? 31 : 39));
            c.Rect(11, 18, 5, s.dress ? 13 : 21, s.coatDark);
            c.Rect(3, 19, 2, 13, s.coat); c.Rect(15, 19, 2, 13, s.coatDark);
            c.Rect(3, 32, 2, 2, s.skin); c.Rect(15, 32, 2, 2, s.skin);

            // Cabeza
            c.Ellipse(10, 11, 6.5f, 7, s.skin);
            c.Ellipse(10, 7, 7, 5, s.hair);
            c.Rect(3, 7, 3, 8, s.hair); c.Rect(14, 7, 3, 7, s.hairDark);
            c.Rect(6, 6, 9, 3, s.hair);
            c.Set(5, 9, s.hairDark); c.Set(9, 9, s.hairDark); c.Set(12, 9, s.hairDark);
            c.Set(8, 12, s.eyes); c.Set(12, 12, s.eyes);
            if (s.glasses)
            {
                var frame = H("3a3a48");
                c.Rect(7, 11, 3, 1, frame); c.Rect(11, 11, 3, 1, frame); c.Set(10, 11, frame);
            }
            if (s.mark) c.Set(10, 9, H("ff2e4f"));
            c.Outline(s.outline);
            return c;
        }

        private static PixelCanvas Researcher() => Human(new HumanStyle
        {
            skin = H("f1c9a5"), hair = H("6b3f2a"), hairDark = H("4a2a1c"), coat = H("e8ecef"), coatDark = H("b8c2cc"),
            pants = H("3f4a5c"), boots = H("4a2a1c"), eyes = H("2a2a3a"), outline = H("2a2230"), longHair = true, glasses = true
        });

        private static PixelCanvas Hikari() => Human(new HumanStyle
        {
            skin = H("f7d9c4"), hair = H("e8e4f2"), hairDark = H("b9b2d1"), coat = H("ffffff"), coatDark = H("d6d9ea"),
            pants = H("d6d9ea"), boots = H("8e93b0"), eyes = H("c23b4f"), outline = H("3a3550"), longHair = true, dress = true, mark = true
        });

        private static PixelCanvas MaidenSpirit()
        {
            var c = new PixelCanvas(22, 52);
            var figure = Human(new HumanStyle
            {
                skin = PixelCanvas.Alpha(H("d9fbff"), 220), hair = PixelCanvas.Alpha(H("7fe3f5"), 220), hairDark = PixelCanvas.Alpha(H("4fb8d9"), 220),
                coat = PixelCanvas.Alpha(H("bff3ff"), 200), coatDark = PixelCanvas.Alpha(H("86d8ef"), 200), pants = PixelCanvas.Alpha(H("bff3ff"), 200),
                boots = PixelCanvas.Alpha(H("86d8ef"), 150), eyes = H("1b5a7a"), outline = PixelCanvas.Alpha(H("2f9fd0"), 230), longHair = true, dress = true
            });
            Paste(c, figure, 1, 0);
            c.Outline(PixelCanvas.Alpha(H("e8fdff"), 110));
            return c;
        }

        private static PixelCanvas TsukiHead()
        {
            // Retrato a partir del sprite real de Tsuki (protagonista.png)
            var c = new PixelCanvas(20, 50);
            string path = "Assets/player/protagonista.png";
            if (!File.Exists(path)) return c;
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            for (int y = 0; y < Mathf.Min(50, tex.height); y++)
                for (int x = 0; x < Mathf.Min(20, tex.width); x++)
                    c.Set(x, y, tex.GetPixel(x, tex.height - 1 - y));
            UnityEngine.Object.DestroyImmediate(tex);
            return c;
        }

        private static PixelCanvas RangedEnemy()
        {
            var c = new PixelCanvas(26, 34);
            var shell = H("5b4a6e"); var shellDark = H("3e3150"); var core = H("ff3b5c"); var coreLight = H("ffc2cc");
            // Cuerpo flotante (las filas de abajo quedan vacías = altura de vuelo)
            c.Poly(shell, new Vector2(13, 1), new Vector2(23, 10), new Vector2(13, 22), new Vector2(3, 10));
            c.Poly(shellDark, new Vector2(13, 1), new Vector2(23, 10), new Vector2(13, 22));
            c.Ellipse(13, 11, 4.5f, 4.5f, core);
            c.Ellipse(12, 10, 1.5f, 1.5f, coreLight);
            // Fragmentos orbitando
            c.Poly(shell, new Vector2(0, 15), new Vector2(3, 13), new Vector2(4, 17), new Vector2(1, 19));
            c.Poly(shellDark, new Vector2(25, 15), new Vector2(22, 13), new Vector2(21, 17), new Vector2(24, 19));
            c.Outline(H("1e1628"));
            return c;
        }

        private static PixelCanvas Guardian()
        {
            var c = new PixelCanvas(60, 76);
            var armor = H("e9e4d4"); var armorDark = H("b5ae98"); var gold = H("e0b04a"); var goldDark = H("a87a26");
            var tabard = H("3b5ba5"); var tabardDark = H("2a4180"); var eye = H("fff3a0"); var halo = H("ffd75e");
            // Halo detrás de la cabeza
            c.Ellipse(30, 12, 13, 5, halo); c.Ellipse(30, 12, 10, 3.2f, Clear);
            // Piernas
            c.Rect(20, 52, 8, 20, armorDark); c.Rect(33, 52, 8, 20, armorDark);
            c.Rect(18, 69, 11, 5, goldDark); c.Rect(32, 69, 11, 5, goldDark);
            // Torso y tabardo
            c.Poly(armor, new Vector2(16, 24), new Vector2(44, 24), new Vector2(41, 54), new Vector2(19, 54));
            c.Poly(armorDark, new Vector2(31, 24), new Vector2(44, 24), new Vector2(41, 54), new Vector2(31, 54));
            c.Poly(tabard, new Vector2(24, 36), new Vector2(36, 36), new Vector2(38, 62), new Vector2(22, 62));
            c.Poly(tabardDark, new Vector2(31, 36), new Vector2(36, 36), new Vector2(38, 62), new Vector2(31, 62));
            c.Rect(18, 34, 24, 3, gold);
            // Balanza (justicia) bordada en el tabardo
            c.Rect(29, 42, 2, 9, gold); c.Rect(25, 43, 10, 1, gold); c.Set(25, 45, gold); c.Set(34, 45, gold);
            // Hombreras
            c.Ellipse(14, 27, 8, 6, armor); c.Ellipse(46, 27, 8, 6, armorDark);
            c.Rect(7, 28, 14, 2, gold); c.Rect(39, 28, 14, 2, goldDark);
            // Brazos
            c.Rect(8, 31, 6, 16, armorDark); c.Rect(46, 31, 6, 16, armorDark);
            // Yelmo
            c.Rect(22, 6, 16, 18, armor); c.Rect(31, 6, 7, 18, armorDark);
            c.Poly(gold, new Vector2(28, 0), new Vector2(32, 0), new Vector2(31, 7), new Vector2(29, 7));
            c.Rect(24, 14, 12, 2, H("2b2b38"));
            c.Rect(26, 14, 2, 2, eye); c.Rect(32, 14, 2, 2, eye);
            // Espada larga
            c.Rect(53, 6, 3, 46, H("d8dde8")); c.Rect(55, 6, 1, 46, H("9aa3b8"));
            c.Rect(50, 46, 9, 2, gold); c.Rect(53, 48, 3, 8, H("5e3d20"));
            c.Outline(H("2c2a36"));
            return c;
        }

        private static PixelCanvas GuardianPortrait()
        {
            var g = Guardian();
            var c = new PixelCanvas(48, 48);
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                {
                    var p = g.Get(18 + x, y + 2);
                    c.Rect(x * 2, y * 2, 2, 2, p);
                }
            return c;
        }

        private static PixelCanvas Portrait(PixelCanvas body)
        {
            var c = new PixelCanvas(48, 48);
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 20; x++)
                    c.Rect(4 + x * 2, y * 2, 2, 2, body.Get(x, y));
            return c;
        }

        // ---------------------------------------------------------------- efectos y UI

        private static PixelCanvas BlobShadow()
        {
            var c = new PixelCanvas(24, 10);
            c.Ellipse(12, 5, 11.5f, 4.5f, new Color32(10, 10, 30, 110));
            c.Ellipse(12, 5, 8, 3, new Color32(10, 10, 30, 140));
            return c;
        }

        private static PixelCanvas TelegraphCircle()
        {
            var c = new PixelCanvas(64, 32);
            c.Ellipse(32, 16, 31.5f, 15.5f, Color.white);
            c.Ellipse(32, 16, 29.5f, 13.5f, new Color32(255, 255, 255, 80));
            return c;
        }

        private static PixelCanvas TelegraphRect()
        {
            var c = new PixelCanvas(16, 16);
            c.Rect(0, 0, 16, 16, new Color32(255, 255, 255, 80));
            c.Rect(0, 0, 16, 2, Color.white); c.Rect(0, 14, 16, 2, Color.white);
            return c;
        }

        private static PixelCanvas Projectile()
        {
            var c = new PixelCanvas(10, 10);
            c.Ellipse(5, 5, 4.5f, 4.5f, H("ff3b5c"));
            c.Ellipse(4.5f, 4.5f, 2, 2, H("ffd1da"));
            return c;
        }

        private static PixelCanvas InteractMarker()
        {
            var c = new PixelCanvas(10, 13);
            c.Rect(1, 0, 8, 9, Color.white);
            c.Poly(Color.white, new Vector2(3, 9), new Vector2(7, 9), new Vector2(5, 12));
            c.Rect(4, 2, 2, 4, H("2a2230")); c.Rect(4, 7, 2, 1, H("2a2230"));
            c.Outline(H("2a2230"));
            return c;
        }

        private static PixelCanvas SwordIcon()
        {
            var c = new PixelCanvas(24, 24);
            for (int i = 0; i < 16; i++)
            {
                c.Rect(4 + i, 18 - i, 2, 2, H("b5283f"));
                c.Set(4 + i, 18 - i, H("e0566e"));
            }
            c.Rect(3, 16, 6, 2, H("e0b04a")); c.Rect(5, 14, 2, 6, H("e0b04a"));
            c.Rect(1, 20, 3, 3, H("3a2414"));
            c.Outline(H("2a1218"));
            return c;
        }

        private static PixelCanvas PermitIcon()
        {
            var c = new PixelCanvas(24, 24);
            c.Ellipse(12, 12, 10, 10, H("e0b04a"));
            c.Ellipse(12, 12, 8, 8, H("f5d27a"));
            c.Rect(11, 6, 2, 11, H("a87a26")); c.Rect(6, 8, 12, 1, H("a87a26"));
            c.Rect(5, 10, 4, 2, H("a87a26")); c.Rect(15, 10, 4, 2, H("a87a26"));
            c.Rect(8, 17, 8, 2, H("a87a26"));
            c.Outline(H("5a3d10"));
            return c;
        }

        private static PixelCanvas IntroSky()
        {
            var c = new PixelCanvas(240, 135);
            var top = H("0b1026"); var bottom = H("2a2f5e");
            for (int y = 0; y < 135; y++)
            {
                var col = Color32.Lerp(top, bottom, y / 134f);
                c.Rect(0, y, 240, 1, col);
            }
            var rng = new System.Random(7);
            for (int i = 0; i < 90; i++) c.Set(rng.Next(240), rng.Next(100), H(rng.NextDouble() < 0.2 ? "ffe9a8" : "c9d6ee"));
            // Estela de luz cayendo
            for (int i = 0; i < 60; i++)
            {
                byte a = (byte)(255 - i * 4);
                c.Set(150 - i, 20 + i, PixelCanvas.Alpha(H("ffd1da"), a));
                c.Set(151 - i, 20 + i, PixelCanvas.Alpha(H("ff8fa3"), a));
            }
            c.Ellipse(150, 20, 3, 3, H("ffffff"));
            // Islas flotando abajo
            c.Ellipse(60, 120, 30, 6, H("3e7a4a")); c.Poly(H("5a3f2a"), new Vector2(32, 121), new Vector2(88, 121), new Vector2(60, 134));
            c.Ellipse(190, 112, 22, 5, H("3e7a4a")); c.Poly(H("5a3f2a"), new Vector2(169, 113), new Vector2(211, 113), new Vector2(190, 126));
            return c;
        }

        // ---------------------------------------------------------------- utilidades

        private static void Paste(PixelCanvas dst, PixelCanvas src, int ox, int oy)
        {
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    var p = src.Get(x, y);
                    if (p.a > 0) dst.Set(ox + x, oy + y, p);
                }
        }

        private static Color32 Shade(Color32 c, float f) =>
            new Color32((byte)Mathf.Clamp(c.r * f, 0, 255), (byte)Mathf.Clamp(c.g * f, 0, 255), (byte)Mathf.Clamp(c.b * f, 0, 255), c.a);
    }
}
