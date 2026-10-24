using System.Collections.Generic;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Arte extra: kit de UI (marcos 9-slice), muñeco de equipo con sus piezas, slime,
    // icono del Módulo de espada, círculo de invocación y texturas de partículas.
    public static partial class PlaceholderArtGenerator
    {
        // Paleta de la UI: ciruela oscuro, oro y carmesí (los colores de la espada)
        private static readonly Color32 Ink = H("0f0a17");
        private static readonly Color32 PlumDark = H("150f22");
        private static readonly Color32 Plum = H("1f1730");
        private static readonly Color32 PlumLight = H("35294f");
        private static readonly Color32 Line = H("4b3b6b");
        private static readonly Color32 GoldDark = H("8a6526");
        private static readonly Color32 Gold = H("c9a24a");
        private static readonly Color32 GoldLight = H("f0d27a");
        private static readonly Color32 CrimsonDark = H("5c1022");
        private static readonly Color32 Crimson = H("a3203a");
        private static readonly Color32 CrimsonLight = H("d8435e");

        private static IEnumerable<Entry> ExtraEntries()
        {
            var center = new Vector2(0.5f, 0.5f);
            return new List<Entry>
            {
                new Entry { name = "ui_frame", pivot = center, border = new Vector4(10, 10, 10, 10), draw = UiFrame },
                new Entry { name = "ui_panel", pivot = center, border = new Vector4(4, 4, 4, 4), draw = UiPanel },
                new Entry { name = "ui_slot", pivot = center, border = new Vector4(6, 6, 6, 6), draw = () => UiSlot(false) },
                new Entry { name = "ui_slot_selected", pivot = center, border = new Vector4(6, 6, 6, 6), draw = () => UiSlot(true) },
                new Entry { name = "ui_tab", pivot = center, border = new Vector4(8, 4, 8, 8), draw = () => UiTab(false) },
                new Entry { name = "ui_tab_active", pivot = center, border = new Vector4(8, 4, 8, 8), draw = () => UiTab(true) },
                new Entry { name = "ui_button", pivot = center, border = new Vector4(6, 6, 6, 6), draw = UiButton },
                new Entry { name = "ui_row", pivot = center, border = new Vector4(6, 6, 6, 6), draw = () => UiRow(false) },
                new Entry { name = "ui_row_selected", pivot = center, border = new Vector4(6, 6, 6, 6), draw = () => UiRow(true) },
                new Entry { name = "ui_badge_equipped", pivot = center, draw = EquippedBadge },

                new Entry { name = "ui_doll", pivot = center, draw = Doll },
                new Entry { name = "ui_doll_helmet", pivot = center, draw = DollHelmet },
                new Entry { name = "ui_doll_chest", pivot = center, draw = DollChest },
                new Entry { name = "ui_doll_boots", pivot = center, draw = DollBoots },
                new Entry { name = "ui_empty_head", pivot = center, draw = () => Ghost(DollHelmet()) },
                new Entry { name = "ui_empty_chest", pivot = center, draw = () => Ghost(DollChestTorso()) },
                new Entry { name = "ui_empty_feet", pivot = center, draw = () => Ghost(DollBoots()) },

                new Entry { name = "placeholder_icon_sword_module", pivot = center, draw = SwordModuleIcon },
                new Entry { name = "placeholder_enemy_slime", pivot = Feet(22, 1), draw = Slime },
                new Entry { name = "placeholder_summon_circle", pivot = center, draw = SummonCircle },
                new Entry { name = "placeholder_door_barrier", pivot = Feet(60, 1), draw = DoorBarrier },

                new Entry { name = "vfx_fire", pivot = center, draw = FireParticle },
                new Entry { name = "vfx_dust", pivot = center, draw = DustParticle },
                new Entry { name = "vfx_rock", pivot = center, draw = RockParticle },
                new Entry { name = "vfx_spark", pivot = center, draw = SparkParticle },
            };
        }

        // ---------------------------------------------------------------- kit de UI

        // Anillo de 1 pixel a "inset" pixeles del borde.
        private static void Ring(PixelCanvas c, int inset, Color32 color)
        {
            for (int x = inset; x < c.Width - inset; x++) { c.Set(x, inset, color); c.Set(x, c.Height - 1 - inset, color); }
            for (int y = inset; y < c.Height - inset; y++) { c.Set(inset, y, color); c.Set(c.Width - 1 - inset, y, color); }
        }

        // Anillo con luz arriba/izquierda y sombra abajo/derecha.
        private static void Bevel(PixelCanvas c, int inset, Color32 light, Color32 dark)
        {
            for (int x = inset; x < c.Width - inset; x++) { c.Set(x, inset, light); c.Set(x, c.Height - 1 - inset, dark); }
            for (int y = inset; y < c.Height - inset; y++) { c.Set(inset, y, light); c.Set(c.Width - 1 - inset, y, dark); }
        }

        private static void RoundCorners(PixelCanvas c)
        {
            var clear = new Color32(0, 0, 0, 0);
            c.Set(0, 0, clear); c.Set(c.Width - 1, 0, clear); c.Set(0, c.Height - 1, clear); c.Set(c.Width - 1, c.Height - 1, clear);
        }

        private static PixelCanvas UiFrame()
        {
            var c = new PixelCanvas(32, 32);
            c.Rect(0, 0, 32, 32, PlumDark);
            Ring(c, 0, Ink);
            Bevel(c, 1, GoldLight, GoldDark);
            Ring(c, 2, Gold);
            Ring(c, 3, Ink);
            Bevel(c, 4, PlumLight, PlumDark);
            // Remaches de las esquinas: oro con centro carmesí
            foreach (var (x, y) in new[] { (0, 0), (26, 0), (0, 26), (26, 26) })
            {
                c.Rect(x, y, 6, 6, Ink);
                c.Rect(x + 1, y + 1, 4, 4, Gold);
                c.Set(x + 1, y + 1, GoldLight);
                c.Rect(x + 2, y + 2, 2, 2, Crimson);
                c.Set(x + 2, y + 2, CrimsonLight);
            }
            return c;
        }

        private static PixelCanvas UiPanel()
        {
            var c = new PixelCanvas(16, 16);
            c.Rect(0, 0, 16, 16, Plum);
            Ring(c, 0, Ink);
            Bevel(c, 1, PlumDark, Line);
            RoundCorners(c);
            return c;
        }

        private static PixelCanvas UiSlot(bool selected)
        {
            var c = new PixelCanvas(24, 24);
            c.Rect(0, 0, 24, 24, selected ? H("2b1832") : H("1b1429"));
            if (selected)
            {
                Ring(c, 0, Gold);
                Bevel(c, 1, GoldLight, CrimsonDark);
                Ring(c, 2, CrimsonLight);
                Bevel(c, 3, Crimson, H("3a1426"));
                c.Set(1, 1, GoldLight); c.Set(22, 1, GoldLight); c.Set(1, 22, GoldLight); c.Set(22, 22, GoldLight);
            }
            else
            {
                Ring(c, 0, Ink);
                Bevel(c, 1, H("0b0812"), PlumLight);
                Bevel(c, 2, H("130e1e"), H("2a2140"));
            }
            RoundCorners(c);
            return c;
        }

        private static PixelCanvas UiTab(bool active)
        {
            var c = new PixelCanvas(32, 20);
            c.Rect(0, 0, 32, 20, active ? Crimson : Plum);
            // Sin borde inferior: la pestaña se une al panel
            for (int x = 0; x < 32; x++) c.Set(x, 0, active ? Gold : Ink);
            for (int y = 0; y < 20; y++) { c.Set(0, y, active ? Gold : Ink); c.Set(31, y, active ? Gold : Ink); }
            for (int x = 1; x < 31; x++) c.Set(x, 1, active ? GoldLight : Line);
            for (int y = 1; y < 20; y++) c.Set(1, y, active ? CrimsonLight : PlumLight);
            if (active) for (int x = 2; x < 30; x++) c.Set(x, 2, CrimsonLight);
            var clear = new Color32(0, 0, 0, 0);
            c.Set(0, 0, clear); c.Set(31, 0, clear);
            return c;
        }

        private static PixelCanvas UiButton()
        {
            var c = new PixelCanvas(32, 16);
            c.Rect(0, 0, 32, 16, Crimson);
            c.Rect(0, 2, 32, 2, CrimsonLight);
            c.Rect(0, 12, 32, 2, CrimsonDark);
            Ring(c, 0, Ink);
            Bevel(c, 1, GoldLight, GoldDark);
            RoundCorners(c);
            return c;
        }

        private static PixelCanvas UiRow(bool selected)
        {
            var c = new PixelCanvas(24, 16);
            c.Rect(0, 0, 24, 16, selected ? H("3a1530") : H("1b1429"));
            Ring(c, 0, selected ? Gold : Ink);
            Bevel(c, 1, selected ? CrimsonLight : H("0b0812"), selected ? CrimsonDark : PlumLight);
            RoundCorners(c);
            return c;
        }

        // Marca "equipado": escudo dorado pequeño
        private static PixelCanvas EquippedBadge()
        {
            var c = new PixelCanvas(9, 10);
            c.Poly(Gold, new Vector2(0, 0), new Vector2(9, 0), new Vector2(9, 5), new Vector2(4.5f, 10), new Vector2(0, 5));
            c.Rect(2, 2, 2, 3, GoldLight);
            c.Outline(Ink);
            return c;
        }

        // ---------------------------------------------------------------- muñeco de equipo
        // Maniquí de madera de 48x80; las piezas usan el mismo lienzo para encajar encima.

        private static readonly Color32 WoodLight = H("e2c29a");
        private static readonly Color32 Wood = H("c9a27a");
        private static readonly Color32 WoodShade = H("9c7650");
        private static readonly Color32 WoodLine = H("3a2614");

        private static PixelCanvas Doll()
        {
            var c = new PixelCanvas(48, 80);
            // Base y poste
            c.Rect(10, 74, 28, 5, WoodShade); c.Rect(10, 74, 28, 1, Wood);
            c.Rect(22, 66, 4, 8, WoodShade);
            // Piernas
            c.Rect(15, 47, 7, 26, Wood); c.Rect(26, 47, 7, 26, Wood);
            c.Rect(20, 47, 2, 26, WoodShade); c.Rect(31, 47, 2, 26, WoodShade);
            c.Rect(15, 59, 7, 1, WoodShade); c.Rect(26, 59, 7, 1, WoodShade); // rodillas
            // Torso (hombros más anchos)
            c.Poly(Wood, new Vector2(12, 23), new Vector2(36, 23), new Vector2(33, 48), new Vector2(15, 48));
            c.Rect(29, 25, 4, 22, WoodShade);
            c.Rect(16, 26, 2, 2, WoodLight);
            // Brazos
            c.Rect(7, 25, 5, 22, Wood); c.Rect(36, 25, 5, 22, Wood);
            c.Rect(7, 25, 1, 22, WoodLight); c.Rect(40, 25, 1, 22, WoodShade);
            // Cuello y cabeza
            c.Rect(21, 18, 6, 6, WoodShade);
            c.Ellipse(24, 10, 8, 9, Wood);
            c.Ellipse(21, 7, 3, 3, WoodLight);
            c.Rect(28, 6, 2, 9, WoodShade);
            c.Outline(WoodLine);
            return c;
        }

        private static readonly Color32 Leather = H("8a5a32");
        private static readonly Color32 LeatherLight = H("b07a44");
        private static readonly Color32 LeatherDark = H("5e3a1e");
        private static readonly Color32 Stitch = H("e2bd82");
        private static readonly Color32 LeatherLine = H("2e1c0e");

        private static PixelCanvas DollHelmet()
        {
            var c = new PixelCanvas(48, 80);
            c.Ellipse(24, 9, 10, 9, Leather);
            c.Rect(13, 9, 22, 12, new Color32(0, 0, 0, 0)); // deja la cara libre
            c.Rect(14, 9, 20, 3, LeatherDark);              // ala del casco
            c.Rect(14, 9, 3, 7, Leather); c.Rect(31, 9, 3, 7, Leather); // orejeras
            c.Ellipse(20, 4, 4, 2, LeatherLight);
            for (int x = 16; x <= 32; x += 3) c.Set(x, 10, Stitch);
            c.Rect(23, 0, 2, 9, LeatherDark); // costura central
            c.Outline(LeatherLine);
            return c;
        }

        // Pechera completa en el muñeco: torso, mangas y pantalón de cuero (las botas van encima).
        private static PixelCanvas DollChest()
        {
            var c = new PixelCanvas(48, 80);
            // Mangas hasta la muñeca; las manos de madera quedan libres
            c.Rect(6, 27, 7, 15, Leather); c.Rect(35, 27, 7, 15, Leather);
            c.Rect(6, 27, 1, 15, LeatherLight); c.Rect(41, 27, 1, 15, LeatherDark);
            c.Rect(6, 39, 7, 2, LeatherDark); c.Rect(35, 39, 7, 2, LeatherDark); // puños
            // Pantalón: cada pierna hasta el tobillo, unidas en la cadera
            c.Rect(14, 46, 9, 27, Leather); c.Rect(25, 46, 9, 27, Leather);
            c.Rect(14, 46, 20, 5, Leather);
            c.Rect(20, 51, 3, 22, LeatherDark); c.Rect(31, 51, 3, 22, LeatherDark);
            for (int y = 53; y <= 69; y += 4) { c.Set(17, y, Stitch); c.Set(28, y, Stitch); }
            c.Outline(LeatherLine);

            var torso = DollChestTorso();
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                    if (torso.IsFilled(x, y)) c.Set(x, y, torso.Get(x, y));
            return c;
        }

        // Solo el torso: también es la silueta de la ranura vacía.
        private static PixelCanvas DollChestTorso()
        {
            var c = new PixelCanvas(48, 80);
            c.Poly(Leather, new Vector2(11, 22), new Vector2(37, 22), new Vector2(34, 46), new Vector2(14, 46));
            // Hombreras
            c.Ellipse(10, 26, 5, 4, LeatherDark); c.Ellipse(38, 26, 5, 4, LeatherDark);
            c.Ellipse(9, 25, 2, 1, LeatherLight); c.Ellipse(37, 25, 2, 1, LeatherLight);
            c.Rect(21, 22, 6, 3, new Color32(0, 0, 0, 0)); // cuello
            c.Rect(29, 25, 4, 18, LeatherDark);
            c.Rect(16, 26, 3, 8, LeatherLight);
            // Correas cruzadas y cinturón con hebilla
            for (int i = 0; i < 18; i++) c.Set(15 + i, 25 + i, Stitch);
            c.Rect(14, 42, 20, 4, LeatherDark);
            c.Rect(22, 42, 4, 4, Gold); c.Rect(23, 43, 2, 2, LeatherDark);
            c.Outline(LeatherLine);
            return c;
        }

        private static PixelCanvas DollBoots()
        {
            var c = new PixelCanvas(48, 80);
            c.Rect(14, 61, 9, 12, Leather); c.Rect(25, 61, 9, 12, Leather);
            c.Rect(13, 70, 10, 3, LeatherDark); c.Rect(25, 70, 10, 3, LeatherDark); // suela
            c.Rect(14, 61, 9, 2, LeatherLight); c.Rect(25, 61, 9, 2, LeatherLight); // puño
            for (int y = 64; y <= 68; y += 2) { c.Set(18, y, Stitch); c.Set(29, y, Stitch); }
            c.Rect(21, 63, 2, 7, LeatherDark); c.Rect(32, 63, 2, 7, LeatherDark);
            c.Outline(LeatherLine);
            return c;
        }

        // Silueta tenue de una pieza (ranura vacía), recortada a su caja.
        private static PixelCanvas Ghost(PixelCanvas piece)
        {
            int minX = piece.Width, minY = piece.Height, maxX = 0, maxY = 0;
            for (int y = 0; y < piece.Height; y++)
                for (int x = 0; x < piece.Width; x++)
                    if (piece.IsFilled(x, y)) { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
            int size = Mathf.Max(maxX - minX, maxY - minY) + 3;
            var c = new PixelCanvas(size, size);
            int ox = (size - (maxX - minX + 1)) / 2 - minX, oy = (size - (maxY - minY + 1)) / 2 - minY;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (piece.IsFilled(x, y)) c.Set(x + ox, y + oy, Line);
            return c;
        }

        // ---------------------------------------------------------------- iconos y personajes

        private static PixelCanvas SwordModuleIcon()
        {
            var c = new PixelCanvas(24, 24);
            // Chip hexagonal carmesí con circuitos dorados
            c.Poly(Crimson, new Vector2(6, 2), new Vector2(18, 2), new Vector2(23, 12), new Vector2(18, 22), new Vector2(6, 22), new Vector2(1, 12));
            c.Poly(CrimsonDark, new Vector2(12, 12), new Vector2(23, 12), new Vector2(18, 22), new Vector2(6, 22));
            c.Ellipse(12, 12, 4, 4, Gold);
            c.Ellipse(12, 12, 2, 2, GoldLight);
            c.Line(4, 12, 8, 12, Gold); c.Line(16, 12, 20, 12, Gold);
            c.Line(12, 4, 12, 8, Gold); c.Line(12, 16, 12, 20, Gold);
            c.Set(4, 11, GoldLight); c.Set(20, 13, GoldLight);
            c.Rect(7, 4, 2, 1, CrimsonLight);
            c.Outline(H("2a0c14"));
            return c;
        }

        private static PixelCanvas Slime()
        {
            var c = new PixelCanvas(26, 22);
            var body = H("4fa3e0"); var light = H("8fd0ff"); var dark = H("2b6aa8");
            c.Ellipse(13, 13, 11, 8, body);
            c.Rect(3, 13, 21, 7, body);
            c.Ellipse(13, 19, 11, 2.5f, dark);
            c.Ellipse(9, 9, 3, 2, light);
            c.Set(7, 8, H("e8f7ff"));
            c.Rect(9, 12, 2, 3, H("10243a")); c.Rect(15, 12, 2, 3, H("10243a"));
            c.Set(9, 12, H("ffffff")); c.Set(15, 12, H("ffffff"));
            c.Rect(12, 16, 3, 1, dark);
            c.Outline(H("12304f"));
            return c;
        }

        // Círculo de invocación (elipse isométrica 2:1) cian, para la prueba del santuario
        private static PixelCanvas SummonCircle()
        {
            var c = new PixelCanvas(64, 32);
            var cyan = H("5fe3ff"); var deep = H("1c7fa6");
            c.Ellipse(32, 16, 31, 15.5f, cyan);
            c.Ellipse(32, 16, 29, 14, new Color32(0, 0, 0, 0));
            c.Ellipse(32, 16, 22, 11, deep);
            c.Ellipse(32, 16, 21, 10, new Color32(0, 0, 0, 0));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                int x = 32 + Mathf.RoundToInt(Mathf.Cos(a) * 25.5f), y = 16 + Mathf.RoundToInt(Mathf.Sin(a) * 12.5f);
                c.Rect(x - 1, y - 1, 3, 2, cyan);
            }
            c.Line(14, 16, 50, 16, PixelCanvas.Alpha(cyan, 160));
            c.Line(32, 7, 32, 25, PixelCanvas.Alpha(cyan, 160));
            return c;
        }

        // Barrera de piedra azul de la puerta del santuario (se desvanece al ofrecer el alma)
        private static PixelCanvas DoorBarrier()
        {
            var c = new PixelCanvas(40, 60);
            var glow = H("6fd6ff"); var mid = H("2f8fc0");
            c.Rect(2, 2, 36, 57, PixelCanvas.Alpha(mid, 170));
            for (int y = 4; y < 58; y += 6) c.Rect(4, y, 32, 1, PixelCanvas.Alpha(glow, 200));
            c.Ellipse(20, 26, 6, 7, PixelCanvas.Alpha(H("dff7ff"), 230)); // hueco con forma de alma
            c.Ellipse(20, 26, 3, 4, PixelCanvas.Alpha(glow, 230));
            c.Outline(H("103b55"));
            return c;
        }

        // ---------------------------------------------------------------- partículas (blancas: se tiñen)

        private static PixelCanvas FireParticle()
        {
            var c = new PixelCanvas(8, 10);
            c.Poly(PixelCanvas.Alpha(H("ffffff"), 220), new Vector2(4, 0), new Vector2(7.5f, 6), new Vector2(6, 10), new Vector2(2, 10), new Vector2(0.5f, 6));
            c.Ellipse(4, 7, 2, 2.5f, H("ffffff"));
            return c;
        }

        private static PixelCanvas DustParticle()
        {
            var c = new PixelCanvas(8, 8);
            c.Ellipse(4, 4, 4, 4, PixelCanvas.Alpha(H("ffffff"), 150));
            c.Ellipse(4, 4, 2.6f, 2.6f, PixelCanvas.Alpha(H("ffffff"), 230));
            return c;
        }

        private static PixelCanvas RockParticle()
        {
            var c = new PixelCanvas(6, 5);
            c.Rect(1, 1, 4, 3, H("8a8f9e"));
            c.Rect(1, 1, 2, 1, H("b9c1d3"));
            c.Outline(H("3a3e4a"));
            return c;
        }

        private static PixelCanvas SparkParticle()
        {
            var c = new PixelCanvas(3, 3);
            c.Set(1, 0, H("ffffff")); c.Set(0, 1, H("ffffff")); c.Set(1, 1, H("ffffff")); c.Set(2, 1, H("ffffff")); c.Set(1, 2, H("ffffff"));
            return c;
        }
    }
}
