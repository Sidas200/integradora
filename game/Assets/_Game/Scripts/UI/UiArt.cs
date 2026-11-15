using System;
using System.Collections.Generic;
using UnityEngine;

// Arte pixel de la UI del juego (pausa, diálogos, objetivo, derrota, avisos y vida), generado por código
// para que todo comparta la misma paleta: fondo ciruela, marcos dorado/rosa con esquinas en escalón,
// listones carmesí, sakura, torii, nubes moradas y lirios rojos (higanbana).
// Cada pieza se dibuja en una grilla chica y la UI la muestra escalada sin suavizar (pixel perfecto).
public static class UiArt
{
    // ------------------------------------------------------------------ paleta

    public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    public static readonly Color32 GoldLight = new Color32(250, 222, 150, 255);
    public static readonly Color32 Gold = new Color32(222, 168, 92, 255);
    public static readonly Color32 GoldDark = new Color32(140, 86, 48, 255);
    public static readonly Color32 Rose = new Color32(196, 96, 104, 255);
    public static readonly Color32 RoseDark = new Color32(112, 40, 58, 255);
    public static readonly Color32 CrimsonLight = new Color32(222, 62, 92, 255);
    public static readonly Color32 Crimson = new Color32(176, 28, 62, 255);
    public static readonly Color32 CrimsonDark = new Color32(104, 14, 40, 255);
    public static readonly Color32 Plum = new Color32(40, 16, 38, 255);
    public static readonly Color32 PlumDark = new Color32(22, 9, 24, 255);
    public static readonly Color32 PlumLight = new Color32(66, 28, 60, 255);
    public static readonly Color32 Ink = new Color32(14, 6, 14, 255);

    // Colores de texto que combinan con el arte
    public static readonly Color TextCream = new Color(0.96f, 0.91f, 0.84f, 1f);
    public static readonly Color TextGold = new Color(0.97f, 0.84f, 0.55f, 1f);
    public static readonly Color TextMuted = new Color(0.80f, 0.70f, 0.74f, 1f);

    // ------------------------------------------------------------------ lienzo

    // Lienzo de pixeles con el origen arriba a la izquierda (y hacia abajo), como se dibuja a mano.
    public class Canvas
    {
        public readonly int W, H;
        public readonly Color32[] Px;

        public Canvas(int w, int h)
        {
            W = w; H = h;
            Px = new Color32[w * h];
        }

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public Color32 Get(int x, int y) => In(x, y) ? Px[y * W + x] : Clear;

        public void Set(int x, int y, Color32 c)
        {
            if (In(x, y)) Px[y * W + x] = c;
        }

        // Mezcla con alfa (para sombras y brillos suaves)
        public void Blend(int x, int y, Color32 c)
        {
            if (!In(x, y) || c.a == 0) return;
            var d = Px[y * W + x];
            float a = c.a / 255f, da = d.a / 255f;
            float oa = a + da * (1f - a);
            if (oa <= 0f) return;
            Px[y * W + x] = new Color32(
                (byte)((c.r * a + d.r * da * (1f - a)) / oa),
                (byte)((c.g * a + d.g * da * (1f - a)) / oa),
                (byte)((c.b * a + d.b * da * (1f - a)) / oa),
                (byte)(oa * 255f));
        }

        public void Fill(int x0, int y0, int w, int h, Color32 c)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                    Set(x, y, c);
        }

        public void HLine(int x0, int x1, int y, Color32 c) { for (int x = x0; x <= x1; x++) Set(x, y, c); }
        public void VLine(int x, int y0, int y1, Color32 c) { for (int y = y0; y <= y1; y++) Set(x, y, c); }

        public void RectOutline(int x0, int y0, int x1, int y1, Color32 c)
        {
            HLine(x0, x1, y0, c); HLine(x0, x1, y1, c);
            VLine(x0, y0, y1, c); VLine(x1, y0, y1, c);
        }

        public void Line(float x0, float y0, float x1, float y1, Color32 c)
        {
            int steps = (int)Math.Ceiling(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0))) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Set((int)Math.Round(x0 + (x1 - x0) * t), (int)Math.Round(y0 + (y1 - y0) * t), c);
            }
        }

        // Copia otro lienzo encima (los pixeles transparentes no tapan)
        public void Stamp(Canvas src, int ox, int oy, bool flipX = false)
        {
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var c = src.Px[y * src.W + (flipX ? src.W - 1 - x : x)];
                    if (c.a == 255) Set(ox + x, oy + y, c);
                    else if (c.a > 0) Blend(ox + x, oy + y, c);
                }
        }

        // Contorno de 1 pixel alrededor de lo dibujado (para que las piezas se lean sobre cualquier fondo)
        public void Outline(Color32 c)
        {
            var copy = (Color32[])Px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a > 0) continue;
                    bool near = false;
                    for (int k = 0; k < 4 && !near; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx >= 0 && ny >= 0 && nx < W && ny < H && copy[ny * W + nx].a > 128) near = true;
                    }
                    if (near) Px[y * W + x] = c;
                }
        }
    }

    // Dibuja un mapa de texto: cada carácter es un color de la tabla ('.' o ' ' = vacío).
    public static Canvas FromMap(string[] rows, Dictionary<char, Color32> colors)
    {
        int w = 0;
        foreach (var r in rows) w = Math.Max(w, r.Length);
        var cv = new Canvas(w, rows.Length);
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (colors.TryGetValue(rows[y][x], out var c)) cv.Set(x, y, c);
        return cv;
    }

    private static Color32 Lerp(Color32 a, Color32 b, float t)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t),
            (byte)(a.b + (b.b - a.b) * t), (byte)(a.a + (b.a - a.a) * t));
    }

    private static Color32 WithAlpha(Color32 c, byte a) => new Color32(c.r, c.g, c.b, a);

    // ------------------------------------------------------------------ marcos

    public const int FrameSize = 40;
    public const int FrameBorder = 14;
    public const int FrameInset = 3; // donde empieza el relleno

    // Marco ornamentado (9-slice): línea dorada por fuera, línea rosa por dentro y esquinas en escalón
    // con un rombo. El centro va transparente: el relleno con degradado es otra imagen.
    public static Canvas Frame()
    {
        int n = FrameSize;
        var c = new Canvas(n, n);
        // sombra interior para dar profundidad al borde
        for (int i = 0; i < 2; i++)
            c.RectOutline(FrameInset + 1 + i, FrameInset + 1 + i, n - FrameInset - 2 - i, n - FrameInset - 2 - i, new Color32(10, 3, 10, (byte)(120 - i * 50)));
        // línea exterior dorada (con brillo arriba y oscuro abajo)
        c.RectOutline(FrameInset, FrameInset, n - 1 - FrameInset, n - 1 - FrameInset, Gold);
        c.HLine(FrameInset, n - 1 - FrameInset, FrameInset, GoldLight);
        c.HLine(FrameInset, n - 1 - FrameInset, n - 1 - FrameInset, GoldDark);
        // línea interior rosa
        int r = FrameInset + 4;
        c.RectOutline(r, r, n - 1 - r, n - 1 - r, Rose);

        // esquinas: escalón que sale del marco con un rombo dentro
        foreach (var (fx, fy) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            void P(int x, int y, Color32 col) => c.Set(fx ? n - 1 - x : x, fy ? n - 1 - y : y, col);
            // cuadro de la esquina
            for (int i = 0; i <= 10; i++)
            {
                P(i, 0, Gold); P(0, i, Gold);
            }
            for (int i = 0; i <= 10; i++) { P(i, 10, i < 4 ? Gold : Clear); P(10, i, i < 4 ? Gold : Clear); }
            // escalones de unión con la línea principal
            P(10, 1, Gold); P(10, 2, Gold); P(1, 10, Gold); P(2, 10, Gold);
            for (int i = 1; i <= 9; i++) for (int j = 1; j <= 9; j++)
                    if (!(i == 10 || j == 10)) P(i, j, Clear);
            // marco del cuadro
            for (int i = 0; i <= 9; i++) { P(i, 0, i == 0 ? GoldLight : Gold); P(0, i, Gold); }
            for (int i = 0; i <= 6; i++) { P(i, 6, GoldDark); P(6, i, GoldDark); }
            P(6, 6, Gold);
            // rombo
            P(3, 2, GoldLight); P(2, 3, GoldLight); P(4, 3, Gold); P(3, 4, Gold); P(3, 3, Rose);
            // fondo oscuro del cuadro para que se lea
            for (int i = 1; i <= 5; i++) for (int j = 1; j <= 5; j++)
                    if (c.Get(fx ? n - 1 - i : i, fy ? n - 1 - j : j).a == 0) P(i, j, PlumDark);
            // la línea principal sale del cuadro
            for (int i = 7; i <= 9; i++) { P(i, FrameInset, Gold); P(FrameInset, i, Gold); }
        }
        // reponer brillos de la línea superior e inferior tocados por las esquinas
        return c;
    }

    // Placa de botón (9-slice): fondo ciruela, borde rosa dorado con esquinas cortadas.
    public const int PlateSize = 16, PlateBorder = 5;
    public static Canvas ButtonPlate()
    {
        int n = PlateSize;
        var c = new Canvas(n, n);
        var fill = new Color32(44, 18, 42, 235);
        c.Fill(2, 2, n - 4, n - 4, fill);
        c.HLine(2, n - 3, 1, Rose); c.HLine(2, n - 3, n - 2, Rose);
        c.VLine(1, 2, n - 3, Rose); c.VLine(n - 2, 2, n - 3, Rose);
        c.Set(1, 1, GoldDark); c.Set(n - 2, 1, GoldDark); c.Set(1, n - 2, GoldDark); c.Set(n - 2, n - 2, GoldDark);
        c.HLine(2, n - 3, 2, PlumLight);
        // contorno oscuro exterior
        c.HLine(2, n - 3, 0, Ink); c.HLine(2, n - 3, n - 1, Ink);
        c.VLine(0, 2, n - 3, Ink); c.VLine(n - 1, 2, n - 3, Ink);
        return c;
    }

    // Panel interior (detalle de la forja, páginas del inventario): ciruela oscuro, borde rosa fino
    // y puntitos dorados en las esquinas. 9-slice con borde PlateBorder.
    public static Canvas InsetPanel()
    {
        int n = PlateSize;
        var c = new Canvas(n, n);
        c.Fill(1, 1, n - 2, n - 2, new Color32(30, 12, 30, 225));
        c.RectOutline(1, 1, n - 2, n - 2, RoseDark);
        c.HLine(2, n - 3, 1, Rose);
        foreach (var (x, y) in new[] { (1, 1), (n - 2, 1), (1, n - 2), (n - 2, n - 2) }) c.Set(x, y, Gold);
        c.HLine(2, n - 3, 0, Ink); c.HLine(2, n - 3, n - 1, Ink);
        c.VLine(0, 2, n - 3, Ink); c.VLine(n - 1, 2, n - 3, Ink);
        return c;
    }

    // Casilla de objeto: fondo hundido, borde dorado oscuro; elegida, borde dorado claro con esquinas.
    public static Canvas Slot(bool selected)
    {
        int n = PlateSize;
        var c = new Canvas(n, n);
        c.Fill(2, 2, n - 4, n - 4, selected ? new Color32(70, 22, 46, 240) : new Color32(20, 8, 20, 235));
        c.HLine(2, n - 3, 2, selected ? new Color32(110, 36, 70, 255) : new Color32(12, 4, 12, 255));
        var border = selected ? GoldLight : GoldDark;
        c.RectOutline(1, 1, n - 2, n - 2, border);
        c.Set(1, 1, Clear); c.Set(n - 2, 1, Clear); c.Set(1, n - 2, Clear); c.Set(n - 2, n - 2, Clear);
        if (selected)
        {
            c.RectOutline(0, 0, n - 1, n - 1, new Color32(255, 214, 140, 90));
            foreach (var (x, y) in new[] { (2, 2), (n - 3, 2), (2, n - 3), (n - 3, n - 3) }) c.Set(x, y, Gold);
        }
        return c;
    }

    // Listón carmesí con puntas (botón elegido, nombre del que habla, "Objetivo").
    // Se estira a lo ancho (9-slice); el alto debe ser h * escala.
    public static Canvas Ribbon(int h, bool pointLeft = true)
    {
        int tip = h / 2;
        int w = tip * 2 + 10;
        var c = new Canvas(w, h);
        float mid = (h - 1) / 2f;
        for (int y = 0; y < h; y++)
        {
            int inset = (int)Math.Round(Math.Abs(y - mid) * (tip / Math.Max(1f, mid)));
            int left = pointLeft ? tip - (tip - inset) : 0;
            int right = w - 1 - (tip - (tip - inset));
            for (int x = left; x <= right; x++)
            {
                float t = y / (float)(h - 1);
                var col = t < 0.35f ? Lerp(CrimsonLight, Crimson, t / 0.35f) : Lerp(Crimson, CrimsonDark, (t - 0.35f) / 0.65f);
                c.Set(x, y, col);
            }
            // borde dorado en las diagonales y arriba/abajo
            c.Set(left, y, Gold); c.Set(right, y, Gold);
            if (inset > 0)
            {
                c.Set(left + 1, y, Gold); c.Set(right - 1, y, Gold);
            }
        }
        c.HLine(pointLeft ? tip : 0, w - 1 - tip, 0, GoldLight);
        c.HLine(pointLeft ? tip : 0, w - 1 - tip, h - 1, GoldDark);
        // línea interior fina
        c.HLine(pointLeft ? tip + 1 : 2, w - 2 - tip, 2, new Color32(240, 120, 140, 160));
        return c;
    }

    public static Vector4 RibbonBorder(int h)
    {
        int tip = h / 2;
        return new Vector4(tip + 2, 0, tip + 2, 0);
    }

    // Placa con el marco del diamante (retrato de la vida): cuadro dorado doble.
    public static Canvas SquareFrame(int n = 24)
    {
        var c = new Canvas(n, n);
        c.RectOutline(0, 0, n - 1, n - 1, Ink);
        c.RectOutline(1, 1, n - 2, n - 2, Gold);
        c.HLine(1, n - 2, 1, GoldLight);
        c.RectOutline(2, 2, n - 3, n - 3, GoldDark);
        c.RectOutline(4, 4, n - 5, n - 5, Rose);
        return c;
    }

    // Relleno con degradado vertical (arriba más oscuro, abajo con un toque carmesí).
    public static Canvas Gradient(Color32 top, Color32 bottom, int h = 64)
    {
        var c = new Canvas(1, h);
        for (int y = 0; y < h; y++) c.Set(0, y, Lerp(top, bottom, y / (float)(h - 1)));
        return c;
    }

    // Patrón de olas japonesas (seigaiha) que se repite (Image tipo Tiled).
    public static Canvas Seigaiha(Color32 line)
    {
        const int W = 16, H = 8, R = 8;
        var c = new Canvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                // círculo visible: el de centro más abajo que cubre el pixel (las filas de abajo tapan)
                float best = -1f, bestD = 0f;
                for (int j = -2; j <= 4; j++)
                    for (int i = -2; i <= 2; i++)
                    {
                        float cx = i * W + ((j & 1) != 0 ? W / 2f : 0f);
                        float cy = j * 4f;
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d < R && dy <= 0.5f && cy > best) { best = cy; bestD = d; }
                    }
                if (best < -50f) continue;
                int ring = (int)Math.Floor(bestD);
                if (ring % 3 == 0 || ring == R - 1) c.Set(x, y, line);
            }
        return c;
    }

    // ------------------------------------------------------------------ adornos chicos

    public static readonly Dictionary<char, Color32> IconColors = new Dictionary<char, Color32>
    {
        { '#', GoldLight }, { '+', Gold }, { '-', GoldDark }, { 'r', Crimson },
    };

    public static Canvas Diamond()
    {
        return FromMap(new[]
        {
            "....#....",
            "...#-#...",
            "..#-.-#..",
            ".#-.#.-#.",
            "#-.###.-#",
            ".#-.#.-#.",
            "..#-.-#..",
            "...#-#...",
            "....#....",
        }, IconColors);
    }

    public static Canvas Icon(string name)
    {
        string[] map;
        switch (name)
        {
            case "torii":
                map = new[]
                {
                    "#.........#",
                    "###########",
                    ".+++++++++.",
                    "..#..#..#..",
                    ".#########.",
                    "..#.....#..",
                    "..#.....#..",
                    "..#.....#..",
                    "..#.....#..",
                    ".###...###.",
                };
                break;
            case "home":
                map = new[]
                {
                    ".....#.....",
                    "....###....",
                    "...##+##...",
                    "..##+++##..",
                    ".##+++++##.",
                    "###########",
                    ".#+++++++#.",
                    ".#++###++#.",
                    ".#++#.#++#.",
                    ".#++#.#++#.",
                    ".####.####.",
                };
                break;
            case "exit":
                map = new[]
                {
                    "######.....",
                    "#++++#.....",
                    "#++++#..#..",
                    "#++++#..##.",
                    "#+#++######",
                    "#++++#..##.",
                    "#++++#..#..",
                    "#++++#.....",
                    "######.....",
                };
                break;
            case "save":
                map = new[]
                {
                    "##########.",
                    "#+######+##",
                    "#+######++#",
                    "#+######++#",
                    "#+++++++++#",
                    "#+#######+#",
                    "#+#-----#+#",
                    "#+#-----#+#",
                    "#+#---#-#+#",
                    "#+#-----#+#",
                    "###########",
                };
                break;
            case "gear":
                map = new[]
                {
                    "....#.#....",
                    ".#.#####.#.",
                    "..#######..",
                    ".####.####.",
                    "####...####",
                    ".###...###.",
                    "####...####",
                    ".####.####.",
                    "..#######..",
                    ".#.#####.#.",
                    "....#.#....",
                };
                break;
            case "talk":
                map = new[]
                {
                    ".#########.",
                    "#+++++++++#",
                    "#+#+#+#+++#",
                    "#+++++++++#",
                    ".####++###.",
                    "....#+#....",
                    ".....##....",
                };
                break;
            default:
                return Diamond();
        }
        return FromMap(map, IconColors);
    }

    // Flecha de "continuar" (triángulo dorado hacia abajo)
    public static Canvas DownArrow()
    {
        return FromMap(new[]
        {
            "#########",
            ".#+++++#.",
            "..#+++#..",
            "...#+#...",
            "....#....",
        }, IconColors);
    }

    // Línea con rombo al centro y puntas de flecha (títulos). Mitad derecha; la izquierda es la misma volteada.
    public static Canvas TitleFlourish(int len = 56)
    {
        var c = new Canvas(len, 7);
        // trazo doble que se adelgaza
        c.HLine(0, len - 8, 3, Gold);
        c.HLine(4, len - 14, 2, GoldDark);
        c.HLine(4, len - 14, 4, GoldDark);
        // punta en flecha
        int t = len - 8;
        c.Set(t + 1, 2, Gold); c.Set(t + 1, 4, Gold); c.Set(t + 2, 1, Gold); c.Set(t + 2, 5, Gold);
        c.HLine(t + 1, t + 4, 3, GoldLight);
        c.Set(t + 5, 3, Gold);
        // rombo al inicio
        c.Set(0, 3, GoldLight); c.Set(1, 2, Gold); c.Set(1, 4, Gold); c.Set(2, 3, Gold);
        return c;
    }

    // Divisor horizontal completo: líneas que se desvanecen con un rombo al centro.
    public static Canvas Divider(int len = 120)
    {
        var c = new Canvas(len, 7);
        int mid = len / 2;
        for (int x = 0; x < len; x++)
        {
            float d = Math.Abs(x - mid) / (float)mid;
            byte a = (byte)(255 * Math.Max(0f, 1f - d * d));
            if (Math.Abs(x - mid) > 4) c.Set(x, 3, WithAlpha(Gold, a));
        }
        c.Set(mid, 0, GoldLight); c.Set(mid - 1, 1, Gold); c.Set(mid + 1, 1, Gold);
        c.Set(mid - 2, 2, Gold); c.Set(mid + 2, 2, Gold); c.Set(mid - 3, 3, Gold); c.Set(mid + 3, 3, Gold);
        c.Set(mid - 2, 4, Gold); c.Set(mid + 2, 4, Gold); c.Set(mid - 1, 5, Gold); c.Set(mid + 1, 5, Gold);
        c.Set(mid, 6, GoldDark); c.Set(mid, 3, CrimsonLight);
        return c;
    }

    // ------------------------------------------------------------------ naturaleza

    // Flor de sakura de cinco pétalos con muesca, centro rosado y estambres.
    public static Canvas Sakura(int size, bool red = false, float rotation = 0f)
    {
        var c = new Canvas(size, size);
        float cx = (size - 1) / 2f, cy = (size - 1) / 2f, R = size / 2f - 0.6f;
        Color32 light = red ? new Color32(246, 92, 104, 255) : new Color32(255, 214, 228, 255);
        Color32 mid = red ? new Color32(214, 34, 60, 255) : new Color32(248, 158, 190, 255);
        Color32 edge = red ? new Color32(132, 14, 36, 255) : new Color32(212, 96, 140, 255);
        Color32 core = red ? new Color32(255, 196, 120, 255) : new Color32(222, 70, 120, 255);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float r = (float)Math.Sqrt(dx * dx + dy * dy);
                float a = (float)Math.Atan2(dy, dx) + rotation;
                // cinco lóbulos y una muesca en la punta de cada pétalo
                float lobe = (float)Math.Abs(Math.Cos(a * 2.5f));
                float notch = (float)Math.Pow(Math.Max(0, Math.Cos(a * 5f)), 12) * 0.22f;
                float edgeR = R * (0.55f + 0.45f * (float)Math.Pow(lobe, 0.6f) - notch);
                if (r > edgeR) continue;
                float k = r / Math.Max(0.01f, edgeR);
                Color32 col = k > 0.82f ? edge : k > 0.45f ? light : mid;
                if (r < R * 0.22f) col = core;
                c.Set(x, y, col);
            }
        // estambres
        if (size >= 9)
        {
            c.Set((int)cx, (int)cy, red ? GoldLight : new Color32(255, 226, 140, 255));
            for (int i = 0; i < 5; i++)
            {
                double a = i * Math.PI * 2 / 5 - rotation + 0.6;
                int sx = (int)Math.Round(cx + Math.Cos(a) * R * 0.34f), sy = (int)Math.Round(cy + Math.Sin(a) * R * 0.34f);
                c.Set(sx, sy, new Color32(255, 230, 150, 255));
            }
        }
        return c;
    }

    // Capullo de sakura
    private static Canvas Bud()
    {
        return FromMap(new[] { ".pp.", "pPPp", "pPPp", ".gg." },
            new Dictionary<char, Color32> {
                { 'p', new Color32(212, 96, 140, 255) }, { 'P', new Color32(248, 158, 190, 255) }, { 'g', new Color32(96, 52, 40, 255) } });
    }

    private static Canvas Petal(int variant)
    {
        var map = variant % 2 == 0 ? new[] { ".PP", "PPp", "Pp." } : new[] { "PP.", "PPp" };
        return FromMap(map, new Dictionary<char, Color32> {
            { 'p', new Color32(212, 96, 140, 255) }, { 'P', new Color32(250, 176, 204, 255) } });
    }

    public static Canvas PetalSprite(int variant) => Petal(variant);

    // Rama de sakura en diagonal con flores y capullos. La rama nace en la esquina de arriba a la izquierda.
    public static Canvas SakuraBranch(int w, int h, int seed)
    {
        var rnd = new System.Random(seed);
        var c = new Canvas(w, h);
        var bark = new Color32(86, 40, 40, 255);
        var barkLight = new Color32(130, 66, 58, 255);
        // rama principal: curva suave desde (0,2) hacia (w*0.85, h*0.75)
        var pts = new List<(float x, float y)>();
        for (int i = 0; i <= 40; i++)
        {
            float t = i / 40f;
            float x = t * w * 0.82f;
            float y = 3 + t * h * 0.55f + (float)Math.Sin(t * 3.4f) * h * 0.08f;
            pts.Add((x, y));
        }
        for (int i = 0; i < pts.Count; i++)
        {
            float t = i / (float)(pts.Count - 1);
            int thick = t < 0.35f ? 3 : t < 0.7f ? 2 : 1;
            for (int k = 0; k < thick; k++) c.Set((int)pts[i].x, (int)pts[i].y + k, k == 0 ? barkLight : bark);
        }
        // ramitas
        var twigEnds = new List<(int x, int y)>();
        foreach (float t in new[] { 0.3f, 0.52f, 0.72f })
        {
            var p = pts[(int)(t * 40)];
            float dir = rnd.Next(2) == 0 ? -1f : 1f;
            float len = h * (0.22f + (float)rnd.NextDouble() * 0.15f);
            float ex = p.x + len * 0.8f, ey = p.y + dir * len * 0.6f;
            c.Line(p.x, p.y, ex, ey, bark);
            twigEnds.Add(((int)ex, (int)ey));
        }
        twigEnds.Add(((int)pts[40].x, (int)pts[40].y));
        // flores en racimos sobre la rama y en las puntas
        var spots = new List<(int x, int y, int s)>();
        foreach (var e in twigEnds) spots.Add((e.x, e.y, 11));
        for (int i = 8; i < 40; i += 6) spots.Add(((int)pts[i].x, (int)pts[i].y + rnd.Next(-3, 3), rnd.Next(2) == 0 ? 9 : 11));
        foreach (var s in spots)
        {
            var f = Sakura(s.s, false, (float)rnd.NextDouble() * 6f);
            c.Stamp(f, s.x - s.s / 2 + rnd.Next(-2, 3), s.y - s.s / 2 + rnd.Next(-2, 3));
            if (rnd.Next(3) == 0) c.Stamp(Sakura(7, false, (float)rnd.NextDouble() * 6f), s.x + rnd.Next(3, 7), s.y + rnd.Next(-6, 2));
            if (rnd.Next(2) == 0) c.Stamp(Bud(), s.x + rnd.Next(-8, 8), s.y + rnd.Next(-8, 6));
        }
        c.Outline(new Color32(30, 8, 22, 200));
        return c;
    }

    // Racimo compacto de flores (extremos de avisos y de la vida)
    public static Canvas SakuraCluster(int seed)
    {
        var rnd = new System.Random(seed);
        var c = new Canvas(30, 26);
        var bark = new Color32(86, 40, 40, 255);
        c.Line(2, 24, 14, 13, bark); c.Line(14, 13, 26, 6, bark);
        c.Stamp(Sakura(13, false, 0.3f), 8, 6);
        c.Stamp(Sakura(11, false, 1.4f), 16, 11);
        c.Stamp(Sakura(9, false, 2.2f), 3, 14);
        c.Stamp(Sakura(7, false, 0.8f), 20, 2);
        c.Stamp(Bud(), 25, 6);
        c.Stamp(Bud(), 1, 21);
        c.Outline(new Color32(30, 8, 22, 200));
        return c;
    }

    // Nube estilizada de tormenta (silueta morada con borde claro arriba y una voluta)
    public static Canvas Cloud(int w, int h, int seed)
    {
        var rnd = new System.Random(seed);
        var c = new Canvas(w, h);
        var body = new Color32(76, 32, 74, 235);
        var top = new Color32(136, 68, 124, 245);
        var dark = new Color32(42, 16, 42, 230);
        // base ancha y aplanada + lomos que se enciman (silueta de nube japonesa)
        var circles = new List<(float x, float y, float r)>();
        circles.Add((w * 0.5f, h * 0.78f, -1f)); // base (elipse)
        int n = Math.Max(2, w / 26);
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0.5f : 0.22f + 0.56f * i / (n - 1);
            float r = h * (0.44f + (float)rnd.NextDouble() * 0.16f) * (1f - Math.Abs(t - 0.5f) * 0.6f);
            circles.Add((t * w + rnd.Next(-2, 3), h * 0.74f - r * 0.4f, r));
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inside = false;
                foreach (var ci in circles)
                {
                    if (ci.r < 0)
                    {
                        float ex = (x - ci.x) / (w * 0.49f), ey = (y - ci.y) / (h * 0.24f);
                        if (ex * ex + ey * ey < 1f) { inside = true; break; }
                        continue;
                    }
                    float dx = x - ci.x, dy = (y - ci.y) * 1.1f;
                    if (dx * dx + dy * dy < ci.r * ci.r) { inside = true; break; }
                }
                if (inside) c.Set(x, y, y > h * 0.86f ? dark : body);
            }
        // borde claro arriba
        var copy = (Color32[])c.Px.Clone();
        for (int y = 1; y < h; y++)
            for (int x = 0; x < w; x++)
                if (copy[y * w + x].a > 0 && copy[(y - 1) * w + x].a == 0)
                {
                    c.Set(x, y, top);
                    if (y + 1 < h && copy[(y + 1) * w + x].a > 0 && rnd.Next(3) == 0) c.Set(x, y + 1, top);
                }
        // voluta: espiral pequeña de línea clara
        for (int k = 1; k < circles.Count; k += 2)
        {
            var cc = circles[k];
            int turns = (int)(cc.r * 1.6f);
            for (int i = 0; i < turns; i++)
            {
                double a = i * 0.5, rr = 0.5 + i * 0.16;
                c.Set((int)Math.Round(cc.x + Math.Cos(a) * rr), (int)Math.Round(cc.y + cc.r * 0.25f + Math.Sin(a) * rr * 0.7), top);
            }
        }
        return c;
    }

    // Torii rojo con nubes en la base (decoración del objetivo)
    public static Canvas Torii()
    {
        return FromMap(new[]
        {
            "kk....................kk",
            "kkkkkkkkkkkkkkkkkkkkkkkk",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
            "..RRRRRRRRRRRRRRRRRRRR..",
            "....Rr.....Rr.....Rr....",
            "..RRRRRRRRRRRRRRRRRRRR..",
            "..ddddddddddddddddddddd.",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "....Rr............Rr....",
            "...kkkk..........kkkk...",
        }, new Dictionary<char, Color32>
        {
            { 'k', new Color32(54, 16, 28, 255) }, { 'R', new Color32(222, 44, 56, 255) },
            { 'r', new Color32(150, 20, 38, 255) }, { 'd', new Color32(150, 20, 38, 255) },
        });
    }

    // Lirio araña rojo (higanbana): pétalos finos curvados hacia atrás y estambres largos.
    private static void Lily(Canvas c, float cx, float cy, float R, System.Random rnd)
    {
        var stem = new Color32(46, 70, 40, 255);
        var stemDark = new Color32(28, 44, 28, 255);
        var red = new Color32(222, 34, 44, 255);
        var redLight = new Color32(255, 92, 96, 255);
        var redDark = new Color32(140, 12, 30, 255);
        // tallo
        for (int y = (int)cy; y < c.H; y++) { c.Set((int)cx, y, stem); c.Set((int)cx + 1, y, stemDark); }
        // pétalos: salen del centro y se enroscan hacia arriba
        int petals = 7;
        for (int p = 0; p < petals; p++)
        {
            double a0 = Math.PI + p * Math.PI / (petals - 1) + (rnd.NextDouble() - 0.5) * 0.25; // de izquierda a derecha por arriba
            double curl = (Math.Cos(a0) < 0 ? -1 : 1) * 1.6;
            for (int i = 0; i <= 14; i++)
            {
                double t = i / 14.0;
                double a = a0 + curl * t * t;
                double rr = R * 0.75 * t;
                int x = (int)Math.Round(cx + Math.Cos(a) * rr);
                int y = (int)Math.Round(cy + Math.Sin(a) * rr * 0.8 - t * R * 0.15);
                c.Set(x, y, t > 0.8 ? redDark : t < 0.4 ? redLight : red);
                if (t < 0.6) c.Set(x, y + 1, red);
            }
        }
        // estambres: largos, finos, se arquean hacia arriba y afuera
        for (int s = 0; s < 6; s++)
        {
            double a0 = -Math.PI / 2 + (s - 2.5) * 0.42;
            double bend = (s - 2.5) * 0.18;
            int lx = 0, ly = 0;
            for (int i = 0; i <= 20; i++)
            {
                double t = i / 20.0;
                double a = a0 + bend * t * 3;
                double rr = R * 1.25 * t;
                lx = (int)Math.Round(cx + Math.Cos(a) * rr);
                ly = (int)Math.Round(cy + Math.Sin(a) * rr);
                c.Set(lx, ly, t < 0.3 ? red : new Color32(240, 70, 80, 255));
            }
            c.Set(lx, ly - 1, new Color32(255, 200, 120, 255));
        }
        c.Set((int)cx, (int)cy, redDark);
    }

    // Grupo de lirios rojos con hojas (esquinas inferiores de la pantalla de derrota)
    public static Canvas LilyField(int w, int h, int seed)
    {
        var rnd = new System.Random(seed);
        var c = new Canvas(w, h);
        // hojas: briznas oscuras desde abajo
        var leaf = new Color32(36, 54, 34, 255);
        var leafLight = new Color32(62, 86, 48, 255);
        for (int i = 0; i < w / 3; i++)
        {
            float x0 = (float)rnd.NextDouble() * w;
            float len = h * (0.15f + (float)rnd.NextDouble() * 0.25f);
            float lean = ((float)rnd.NextDouble() - 0.5f) * len;
            c.Line(x0, h - 1, x0 + lean, h - 1 - len, rnd.Next(2) == 0 ? leaf : leafLight);
        }
        int count = Math.Max(2, w / 22);
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.5f) / count;
            float R = h * (0.2f + (float)rnd.NextDouble() * 0.1f);
            float cy = h * (0.3f + (float)rnd.NextDouble() * 0.35f);
            Lily(c, t * w, cy, R, rnd);
        }
        c.Outline(new Color32(30, 4, 14, 170));
        return c;
    }

    // Estandarte colgante carmesí con flor (esquina de la pausa)
    public static Canvas Banner()
    {
        int w = 15, h = 38;
        var c = new Canvas(w, h);
        for (int y = 0; y < h; y++)
        {
            int cut = y > h - 8 ? (y - (h - 8)) : 0; // cola de golondrina
            for (int x = 0; x < w; x++)
            {
                bool notch = cut > 0 && Math.Abs(x - w / 2) < cut;
                if (notch) continue;
                bool border = x == 0 || x == w - 1 || y == 0 || (cut > 0 && Math.Abs(x - w / 2) == cut);
                c.Set(x, y, border ? Gold : (x == 1 || y == 1 ? CrimsonLight : Crimson));
            }
        }
        c.Fill(-1, 0, w + 2, 2, GoldDark);
        c.HLine(-1, w, 0, Gold);
        c.Stamp(Sakura(11, true, 0.3f), 2, 6);
        return c;
    }

#if !UI_ART_PREVIEW
    // ------------------------------------------------------------------ Unity

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // Convierte el lienzo en sprite (filtro de puntos). border en pixeles del lienzo (izq, abajo, der, arriba).
    public static Sprite ToSprite(string key, Canvas c, Vector4 border = default, bool smooth = false)
    {
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false)
        {
            filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "UiArt_" + key,
        };
        // Unity guarda las filas de abajo hacia arriba
        var flipped = new Color32[c.Px.Length];
        for (int y = 0; y < c.H; y++)
            Array.Copy(c.Px, y * c.W, flipped, (c.H - 1 - y) * c.W, c.W);
        tex.SetPixels32(flipped);
        tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, c.W, c.H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.name = key;
        cache[key] = sprite;
        return sprite;
    }

    public static Sprite Get(string key, Func<Canvas> draw, Vector4 border = default, bool smooth = false)
    {
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        return ToSprite(key, draw(), border, smooth);
    }
#endif
}
