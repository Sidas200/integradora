using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LibroMenu : MonoBehaviour
{
    [Header("Opciones")]
    public bool iniciarVisible = false;
    public bool pausarJuego = true;
    public float duracionGiro = 0.7f;
    [Range(1, 5)] public int numeroMesa = 4;           // Book Desk 1..5
    public Font fuente;                                  // opcional: pon aqui una fuente pixel
    public Color colorTexto = new Color(0.58f, 0.35f, 0.31f);

    const string R = "Libro/Sprites/";
    const float S = 2f;                                  // 1 pixel del arte = 2 unidades
    const float BW = 661f, BH = 416f;                    // tamaño del libro en pixeles del arte

    static readonly string[] nombres = { "Estado", "Mapa", "Equipo", "Inventario", "Opciones", "Salir" };
    static readonly string[] iconosTab =
    {
        "Content/1 Items/13", "Content/2 Icons/7", "Content/8 Equipment/8",
        "Content/1 Items/27", "Content/2 Icons/1", "Content/2 Icons/11"
    };

    Canvas canvas; CanvasGroup grupo; RectTransform escena, libro;
    RectTransform contI, contD, hojaI, hojaD;
    Image imgHojaI, imgHojaD;
    RectTransform[] tabs; Vector2[] tabBase;
    int baseIndex;
    int espacio = 0, seleccion = 0;
    bool girando, animando, abierto;

    // ---------------------------------------------------------- INICIO
    void Awake()
    {
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AsegurarEventSystem();
        Construir();
        abierto = iniciarVisible;
        canvas.gameObject.SetActive(iniciarVisible);
        if (iniciarVisible && pausarJuego) Time.timeScale = 0f;
    }

    void OnDestroy() { if (pausarJuego) Time.timeScale = 1f; }

    void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
        var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) go.AddComponent(t); else go.AddComponent<StandaloneInputModule>();
    }

    Sprite Sp(string ruta)
    {
        var s = Resources.Load<Sprite>(R + ruta);
        if (s == null) Debug.LogWarning("Libro: no encuentro el sprite " + R + ruta);
        return s;
    }

    Sprite SpL(string nombre)
    {
        var s = Resources.Load<Sprite>("Libro/" + nombre);
        if (s == null) Debug.LogWarning("Libro: no encuentro Resources/Libro/" + nombre);
        return s;
    }

    // coordenadas del arte (pixeles, origen arriba-izquierda del libro) -> unidades centradas en el libro
    Vector2 V(float nx, float ny) { return new Vector2((nx - BW / 2f) * S, (BH / 2f - ny) * S); }

    // ---------------------------------------------------------- CONSTRUCCION
    void Construir()
    {
        var cgo = new GameObject("LibroCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();
        grupo = cgo.AddComponent<CanvasGroup>();

        escena = Hijo(cgo.transform, "Escena", 10, 10, Vector2.zero);

        var relleno = Img(escena, null, 1, Vector2.zero, new Color(0.18f, 0.15f, 0.14f), 4000, 3000);
        relleno.raycastTarget = true;

        // mesa (768x560 del arte)
        Img(escena, Sp("Book Desk/" + numeroMesa), S, Vector2.zero);

        // libro
        libro = Hijo(escena, "Libro", BW * S, BH * S, new Vector2(-25f, 28f));

        // pestañas (detras del libro)
        tabs = new RectTransform[nombres.Length];
        tabBase = new Vector2[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            int idx = i;
            var im = Img(libro, SpL("pestana"), S, V(676f, 79f + i * 38f));
            im.raycastTarget = true;
            var b = im.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => IrASeccion(idx));
            Img(im.transform, Sp(iconosTab[i]), S, Vector2.zero);
            tabs[i] = im.rectTransform;
            tabBase[i] = tabs[i].anchoredPosition;
        }

        // base del libro abierto
        var bas = Img(libro, SpL("libro_abierto"), S, Vector2.zero);
        if (bas.sprite == null) bas.color = new Color(0.9f, 0.85f, 0.7f);
        baseIndex = bas.transform.GetSiblingIndex();

        // contenido de las dos hojas
        contI = CrearContenido(espacio * 2, libro);
        contD = CrearContenido(espacio * 2 + 1, libro);

        // hojas que giran (arriba de todo)
        imgHojaD = Img(libro, SpL("hoja_der"), S, Vector2.zero);
        hojaD = imgHojaD.rectTransform; hojaD.pivot = new Vector2(0f, 0.5f); hojaD.anchoredPosition = new Vector2(1f, 0f);
        imgHojaI = Img(libro, SpL("hoja_izq"), S, Vector2.zero);
        hojaI = imgHojaI.rectTransform; hojaI.pivot = new Vector2(1f, 0.5f); hojaI.anchoredPosition = new Vector2(-1f, 0f);
        hojaD.gameObject.SetActive(false); hojaI.gameObject.SetActive(false);

        var ht = Txt(cgo.transform, "← →  /  A D  /  Q E : cambiar página      Esc / M : cerrar", 26,
                     TextAnchor.MiddleCenter, 700, new Vector2(0, -500));
        ht.color = new Color(1, 1, 1, 0.85f);
    }

    // ---------------------------------------------------------- CONTENIDO DE CADA PAGINA
    RectTransform CrearContenido(int seccion, Transform padre)
    {
        var c = Hijo(padre, "Contenido_" + nombres[seccion], 10, 10, Vector2.zero);
        bool izq = (seccion % 2 == 0);
        float cx = izq ? 166f : 496f;                       // centro de la pagina

        Txt(c, nombres[seccion].ToUpper(), 46, TextAnchor.MiddleCenter, 300, V(cx, 42f));

        switch (seccion)
        {
            case 0: // ESTADO
                Txt(c, "JUGADOR", 22, TextAnchor.MiddleCenter, 200, V(164f, 79f));
                Img(c, Sp("Content/5 Holders/2"), S, V(164f, 145f));
                Img(c, Sp("Content/2 Icons/17"), S, V(164f, 145f));
                for (int i = 0; i < 22; i++)
                {
                    float a = 2f * Mathf.PI * i / 22f + Mathf.PI / 2f;
                    Img(c, Sp("Content/6 Highlighter/7"), S, V(164f + Mathf.Cos(a) * 68f, 145f - Mathf.Sin(a) * 72f));
                }
                Img(c, Sp("Content/6 Highlighter/7"), S * 2f, V(164f, 218f));
                TxtI(c, "EQUIPO", 20, 51f, 229f, 150f);
                for (int i = 0; i < 7; i++)
                {
                    float x = 51f + 16f + i * 38f;
                    Img(c, Sp("Content/5 Holders/4"), S, V(x, 257f));
                    Img(c, Sp("Content/8 Equipment/" + (i < 6 ? 7 + i : 12)), S, V(x, 257f));
                }
                Img(c, Sp("Content/6 Highlighter/5"), S * 2f, V(51f + 16f + 3 * 38f, 257f));
                Txt(c, "RELOJ", 20, TextAnchor.MiddleCenter, 150, V(164f, 289f));
                Img(c, Sp("Content/7 Day & Night Cycle/7"), S, V(164f, 335f));
                TxtI(c, "02:14:55", 22, 51f, 310f, 110f);
                TxtI(c, "DOMINGO", 22, 51f, 334f, 110f);
                TxtI(c, "120$", 22, 51f, 358f, 110f);
                TxtI(c, "AÑO: 2", 22, 236f, 310f, 110f);
                TxtI(c, "SOLEADO", 22, 236f, 334f, 110f);
                TxtI(c, "5/100", 22, 236f, 358f, 110f);
                break;

            case 1: // MAPA
                TxtI(c, "NIVEL: 1", 22, 385f, 93f, 120f);
                TxtD(c, "ZONA CIUDAD", 22, 612f, 93f, 160f);
                Img(c, Sp("Content/5 Holders/1"), S, V(496f, 190f));
                float[,] salas = { { -36, -34, 24, 24 }, { 2, -52, 16, 14 }, { 26, -30, 22, 46 }, { -6, -12, 16, 18 },
                                   { -52, 12, 14, 14 }, { -20, 30, 20, 20 }, { 22, 32, 26, 22 } };
                for (int i = 0; i < salas.GetLength(0); i++)
                    Marco(c, 496f + salas[i, 0], 190f + salas[i, 1], 496f + salas[i, 0] + salas[i, 2], 190f + salas[i, 1] + salas[i, 3], colorTexto);
                Marco(c, 372f, 304f, 624f, 365f, colorTexto);
                Img(c, Sp("Content/5 Holders/4"), S, V(396f, 334f));
                Img(c, Sp("Content/2 Icons/7"), S, V(396f, 334f));
                TxtI(c, "¡HOGAR!", 22, 420f, 320f, 190f);
                TxtI(c, "AQUÍ ESTÁS AHORA.", 20, 420f, 342f, 200f);
                break;

            case 2: // EQUIPO
                {
                    string[] et = { "Casco", "Armadura", "Pantalón", "Guantes", "Cinturón", "Anillos" };
                    for (int k = 0; k < 6; k++)
                    {
                        float x = 76f + (k % 3) * 90f;
                        float y = 130f + (k / 3) * 120f;
                        Img(c, Sp("Content/5 Holders/3"), S, V(x, y));
                        Img(c, Sp("Content/8 Equipment/" + (k + 1)), S * 2f, V(x, y));
                        Txt(c, et[k], 20, TextAnchor.MiddleCenter, 100, V(x, y + 52f));
                    }
                    break;
                }

            case 3: // INVENTARIO
                {
                    for (int k = 0; k < 12; k++)
                    {
                        float x = 496f + ((k % 4) - 1.5f) * 72f;
                        float y = 110f + (k / 4) * 72f;
                        Img(c, Sp("Content/5 Holders/5"), S * 2f, V(x, y));
                        Img(c, Sp("Content/1 Items/" + (k + 1)), S * 2f, V(x, y));
                    }
                    Img(c, Sp("Content/1 Items/14"), S * 2f, V(400f, 330f));
                    TxtI(c, "MONEDAS: 120", 24, 430f, 330f, 190f);
                    break;
                }

            case 4: // OPCIONES
                BarraOpcion(c, "MÚSICA", 0.7f, 120f);
                BarraOpcion(c, "EFECTOS", 0.5f, 170f);
                Img(c, Sp("Content/2 Icons/10"), S * 2f, V(70f, 230f));
                TxtI(c, "PANTALLA COMPLETA", 22, 96f, 230f, 220f);
                Txt(c, "(pendiente de conectar)", 20, TextAnchor.MiddleCenter, 250, V(166f, 310f));
                break;

            case 5: // SALIR
                {
                    Txt(c, "¿SEGURO QUE QUIERES SALIR?", 28, TextAnchor.MiddleCenter, 320, V(496f, 130f));
                    var btn = Img(c, Sp("Content/4 Buttons/0"), S * 2f, V(496f, 215f));
                    btn.raycastTarget = true;
                    var b = btn.gameObject.AddComponent<Button>();
                    b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(Salir);
                    var t = Txt(btn.transform, "SALIR", 40, TextAnchor.MiddleCenter, 100, new Vector2(-10f, 0f));
                    Txt(c, "ENTER para confirmar", 20, TextAnchor.MiddleCenter, 250, V(496f, 300f));
                    break;
                }
        }
        return c;
    }

    void BarraOpcion(Transform p, string etiqueta, float valor, float ny)
    {
        TxtI(p, etiqueta, 22, 51f, ny, 100f);
        Caja(p, new Color(0.35f, 0.25f, 0.2f, 0.45f), 140f, 12f, 150f + 70f, ny);
        Caja(p, new Color(0.58f, 0.35f, 0.31f), 140f * valor, 12f, 150f + 70f * valor, ny);
    }

    // ---------------------------------------------------------- AYUDAS UI
    RectTransform Hijo(Transform p, string n, float w, float h, Vector2 pos)
    {
        var go = new GameObject(n, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(p, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = pos;
        return rt;
    }

    // imagen: tamaño = sprite * escala
    Image Img(Transform p, Sprite s, float escala, Vector2 pos, Color? color = null, float w = 0, float h = 0)
    {
        float ww = w > 0 ? w : (s != null ? s.rect.width * escala : 40f);
        float hh = h > 0 ? h : (s != null ? s.rect.height * escala : 40f);
        var rt = Hijo(p, "Img", ww, hh, pos);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = s;
        im.color = color ?? (s == null ? new Color(1, 1, 1, 0.15f) : Color.white);
        im.raycastTarget = false;
        return im;
    }

    // rectangulo de color con medidas en pixeles del arte
    Image Caja(Transform p, Color c, float wNat, float hNat, float nx, float ny)
    {
        return Img(p, null, 1, V(nx, ny), c, wNat * S, hNat * S);
    }

    void Marco(Transform p, float x0, float y0, float x1, float y1, Color c)
    {
        float w = x1 - x0, h = y1 - y0, cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
        Caja(p, c, w, 1f, cx, y0); Caja(p, c, w, 1f, cx, y1);
        Caja(p, c, 1f, h, x0, cy); Caja(p, c, 1f, h, x1, cy);
    }

    Text Txt(Transform p, string s, int size, TextAnchor al, float wNat, Vector2 pos)
    {
        var rt = Hijo(p, "Texto", wNat * S, size * 1.6f, pos);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fuente; t.fontSize = size; t.alignment = al; t.color = colorTexto; t.text = s;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // texto alineado a la izquierda empezando en nxIzq
    Text TxtI(Transform p, string s, int size, float nxIzq, float ny, float wNat)
    {
        return Txt(p, s, size, TextAnchor.MiddleLeft, wNat, V(nxIzq + wNat / 2f, ny));
    }

    // texto alineado a la derecha terminando en nxDer
    Text TxtD(Transform p, string s, int size, float nxDer, float ny, float wNat)
    {
        return Txt(p, s, size, TextAnchor.MiddleRight, wNat, V(nxDer - wNat / 2f, ny));
    }

    // ---------------------------------------------------------- INPUT
    bool Tecla(int cual)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; if (k == null) return false;
        switch (cual)
        {
            case 0: return k.escapeKey.wasPressedThisFrame || k.mKey.wasPressedThisFrame;
            case 1: return k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame;
            case 2: return k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
            default: return k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
        }
#else
        switch (cual)
        {
            case 0: return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M);
            case 1: return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q);
            case 2: return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.E);
            default: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }
#endif
    }

    // ---------------------------------------------------------- UPDATE
    void Update()
    {
        if (Tecla(0) && !animando && !girando) StartCoroutine(Animar(!abierto));
        if (!abierto) return;

        if (Tecla(1) && espacio > 0) { seleccion = (espacio - 1) * 2; IrAEspacio(espacio - 1); }
        if (Tecla(2) && espacio < 2) { seleccion = (espacio + 1) * 2; IrAEspacio(espacio + 1); }
        if (Tecla(3) && espacio == 2) Salir();

        for (int i = 0; i < tabs.Length; i++)
        {
            bool activa = (i / 2 == espacio);
            Vector2 objetivo = tabBase[i] + (activa ? new Vector2(i == seleccion ? 28f : 14f, 0) : Vector2.zero);
            tabs[i].anchoredPosition = Vector2.Lerp(tabs[i].anchoredPosition, objetivo, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }
    }

    void Salir()
    {
        Debug.Log("Salir del juego");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------- ABRIR / CERRAR
    IEnumerator Animar(bool abrir)
    {
        animando = true;
        if (abrir) { canvas.gameObject.SetActive(true); if (pausarJuego) Time.timeScale = 0f; }
        float a = abrir ? 0f : 1f, b = abrir ? 1f : 0f, t = 0f, d = 0.25f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Lerp(a, b, Mathf.Clamp01(t / d));
            grupo.alpha = p;
            escena.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, p);
            yield return null;
        }
        grupo.alpha = b;
        if (!abrir) { canvas.gameObject.SetActive(false); if (pausarJuego) Time.timeScale = 1f; }
        abierto = abrir;
        animando = false;
    }

    // ---------------------------------------------------------- GIRO DE HOJA
    public void IrASeccion(int s)
    {
        if (girando) return;
        seleccion = Mathf.Clamp(s, 0, nombres.Length - 1);
        IrAEspacio(seleccion / 2);
    }

    void IrAEspacio(int destino)
    {
        if (girando || destino == espacio) return;
        StartCoroutine(Girar(destino));
    }

    IEnumerator Girar(int destino)
    {
        girando = true;
        bool sig = destino > espacio;

        var viejaI = contI; var viejaD = contD;
        var nuevaI = CrearContenido(destino * 2, libro);
        var nuevaD = CrearContenido(destino * 2 + 1, libro);

        RectTransform vuela, aparece, contVuela, contAparece, contQuedaNueva, contQuedaVieja;
        Image imgV, imgA;
        if (sig)
        {   // la hoja derecha se levanta hacia la izquierda
            vuela = hojaD; imgV = imgHojaD; contVuela = viejaD;
            aparece = hojaI; imgA = imgHojaI; contAparece = nuevaI;
            contQuedaNueva = nuevaD; contQuedaVieja = viejaI;
        }
        else
        {   // la hoja izquierda se levanta hacia la derecha
            vuela = hojaI; imgV = imgHojaI; contVuela = viejaI;
            aparece = hojaD; imgA = imgHojaD; contAparece = nuevaD;
            contQuedaNueva = nuevaI; contQuedaVieja = viejaD;
        }

        // lo que se queda debajo de las hojas
        contQuedaNueva.SetSiblingIndex(baseIndex + 1);

        vuela.localScale = Vector3.one; vuela.localRotation = Quaternion.identity;
        aparece.localScale = Vector3.one; aparece.localRotation = Quaternion.identity;
        vuela.gameObject.SetActive(true); aparece.gameObject.SetActive(true);
        contVuela.SetParent(vuela, true);            // el contenido viejo viaja con la hoja que se levanta
        contAparece.SetParent(aparece, true);        // el contenido nuevo llega con la hoja que cae
        aparece.localScale = new Vector3(0.001f, 1, 1);

        float signo = sig ? 1f : -1f;
        Color gris = new Color(0.72f, 0.68f, 0.62f, 1f);
        float t = 0f;
        while (t < duracionGiro)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracionGiro);
            if (p < 0.5f)
            {
                float q = Ease(p * 2f);
                vuela.localScale = new Vector3(Mathf.Max(0.001f, 1f - q), 1, 1);
                vuela.localRotation = Quaternion.Euler(0, 0, signo * 4f * Mathf.Sin(q * Mathf.PI * 0.5f));
                imgV.color = Color.Lerp(Color.white, gris, q);
            }
            else
            {
                if (vuela.gameObject.activeSelf) vuela.gameObject.SetActive(false);
                float q = Ease((p - 0.5f) * 2f);
                aparece.localScale = new Vector3(Mathf.Max(0.001f, q), 1, 1);
                aparece.localRotation = Quaternion.Euler(0, 0, -signo * 4f * Mathf.Cos(q * Mathf.PI * 0.5f));
                imgA.color = Color.Lerp(gris, Color.white, q);
            }
            yield return null;
        }

        // ordenar: el contenido nuevo de la hoja que cayo pasa a la base del libro
        aparece.localScale = Vector3.one; aparece.localRotation = Quaternion.identity;
        contAparece.SetParent(libro, true);
        contAparece.SetSiblingIndex(baseIndex + 1);
        Destroy(contVuela.gameObject);
        Destroy(contQuedaVieja.gameObject);

        aparece.gameObject.SetActive(false);
        vuela.gameObject.SetActive(false);
        vuela.localScale = Vector3.one; vuela.localRotation = Quaternion.identity;
        imgHojaD.color = Color.white; imgHojaI.color = Color.white;
        hojaD.SetAsLastSibling(); hojaI.SetAsLastSibling();

        contI = nuevaI; contD = nuevaD;
        espacio = destino;
        girando = false;
    }

    static float Ease(float x) { return x * x * (3f - 2f * x); }
}