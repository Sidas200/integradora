using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MenuBuilder
{
    const string Root = "Assets/UI/MainMenu";
    const string RootFx = Root + "/Efectos";
    const string ScenePath = "Assets/_Game/Scenes/Testing/MainMenu_Test.unity";

    enum Corner { TL, TR, BL, BR }

    class Fx
    {
        public string pref; public Corner c; public float x, y, w, rot; public MenuIntro.Side side; public float delay;
        public Fx(string pref, Corner c, float x, float y, float w, MenuIntro.Side side, float delay, float rot = 0f)
        { this.pref = pref; this.c = c; this.x = x; this.y = y; this.w = w; this.side = side; this.delay = delay; this.rot = rot; }
    }

    [MenuItem("Window/Crear Menú Principal")]
    public static void Build()
    {
        // 1) Imágenes generadas (fondo y luna) + configuración de todas las texturas
        if (!AssetDatabase.IsValidFolder(Root + "/Generado")) AssetDatabase.CreateFolder(Root, "Generado");
        Generate("Fondo_Gradiente.png", 4, 256, (u, v) =>
            Color.Lerp(new Color(0.10f, 0.02f, 0.17f), new Color(0.36f, 0.08f, 0.50f), v));
        Generate("Luna.png", 512, 512, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
            float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.44f, 0.5f, d));
            return new Color(0.95f, 0.30f, 0.85f, a * 0.85f);
        });
        ConfigurarTexturas();

        Sprite sFondo = FindSprite(Root + "/Generado", "Fondo_Gradiente");
        Sprite sLuna = FindSprite(Root + "/Generado", "Luna");
        Sprite sPj = FindSprite(Root, "01_Personaje");
        Sprite sTit = FindSprite(Root, "02_Titulo");
        Sprite[] normal = { FindSprite(Root, "03_"), FindSprite(Root, "04_"), FindSprite(Root, "05_") };
        Sprite[] sel = { FindSprite(Root, "06_"), FindSprite(Root, "07_"), FindSprite(Root, "08_") };

        if (sPj == null || sTit == null || normal[0] == null || normal[1] == null || normal[2] == null ||
            sel[0] == null || sel[1] == null || sel[2] == null)
        {
            EditorUtility.DisplayDialog("Falta algo", "No encontré los sprites 01 a 08 en " + Root, "OK");
            return;
        }

        // 2) Escena nueva (no toca Prototype_Isometric)
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; }

        // 3) EventSystem
        var es = new GameObject("EventSystem", typeof(EventSystem));
        var inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        es.AddComponent(inputModule != null ? inputModule : typeof(StandaloneInputModule));

        // 4) Canvas
        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform cv = canvasGO.transform;

        var intro = new List<MenuIntro.Elemento>();
        var botones = new List<Button>();

        // 5) Fondo y luna
        var fondo = NewImage("Fondo", cv, sFondo);
        fondo.GetComponent<Image>().preserveAspect = false;
        Stretch(fondo);
        AddIntro(intro, fondo, MenuIntro.Side.SoloFade, 0f, 0.6f, false);

        var luna = NewImage("Luna", cv, sLuna);
        Place(luna, Corner.TR, 560, 300, 560, 0);
        AddIntro(intro, luna, MenuIntro.Side.Arriba, 0.1f, 0.9f, false);

        // 6) Efectos decorativos
        var efectos = NewContainer("Efectos", cv);
        var L = MenuIntro.Side.Izquierda; var R = MenuIntro.Side.Derecha;
        var U = MenuIntro.Side.Arriba; var D = MenuIntro.Side.Abajo;
        var lista = new List<Fx>
        {
            new Fx("13_", Corner.BR, 560, 100, 1095, D, 0.30f),
            new Fx("09_", Corner.BL, 520, 220, 1005, L, 0.25f),
            new Fx("03_", Corner.TL, 560, 600, 880, L, 0.20f),
            new Fx("02_", Corner.TR, 700, 250, 890, R, 0.15f),
            new Fx("01_", Corner.TL, 470, 125, 835, L, 0.10f),
            new Fx("05_", Corner.TR, 230, 260, 400, R, 0.30f),
            new Fx("08_", Corner.TR, 170, 600, 300, R, 0.35f),
            new Fx("06_", Corner.TL, 1000, 720, 380, D, 0.40f),
            new Fx("10_", Corner.BL, 940, 170, 590, D, 0.45f),
            new Fx("11_", Corner.BR, 700, 230, 415, R, 0.50f),
            new Fx("16_", Corner.BR, 460, 60, 395, R, 0.45f),
            new Fx("12_", Corner.BR, 240, 200, 285, R, 0.55f),
            new Fx("14_", Corner.BR, 120, 330, 330, R, 0.60f),
            new Fx("15_", Corner.BL, 1230, 200, 225, D, 0.60f),
            new Fx("07_", Corner.TR, 610, 470, 180, R, 0.40f),
            new Fx("04_", Corner.TR, 330, 170, 420, U, 0.35f),
        };
        foreach (var f in lista)
        {
            Sprite s = FindSprite(RootFx, f.pref);
            if (s == null) { Debug.LogWarning("MenuBuilder: no encontré el efecto " + f.pref + " en " + RootFx); continue; }
            var rt = NewImage("Fx_" + s.name, efectos, s);
            Place(rt, f.c, f.x, f.y, f.w, f.rot);
            AddIntro(intro, rt, f.side, f.delay, 0.7f, false);
        }

        // 7) Personaje y título
        var pj = NewImage("Personaje", cv, sPj);
        Place(pj, Corner.BR, 390, 465, 840, 0);
        AddIntro(intro, pj, MenuIntro.Side.Derecha, 0.30f, 0.8f, true);

        var tit = NewImage("Titulo", cv, sTit);
        Place(tit, Corner.TL, 520, 290, 1080, 0);
        AddIntro(intro, tit, MenuIntro.Side.Izquierda, 0.50f, 0.7f, true);

        // 8) Botones
        var menu = NewContainer("Menu", cv);
        string[] nombres = { "BtnInventario", "BtnOpciones", "BtnSalir" };
        float[] bx = { 585, 594, 605 };
        float[] by = { 735, 855, 955 };
        float[] bw = { 560, 500, 450 };
        MenuIntro.Side[] bs = { MenuIntro.Side.Izquierda, MenuIntro.Side.Derecha, MenuIntro.Side.Izquierda };
        float[] bd = { 0.90f, 1.05f, 1.20f };
        for (int i = 0; i < 3; i++)
        {
            var rt = NewImage(nombres[i], menu, normal[i]);
            var img = rt.GetComponent<Image>();
            img.raycastTarget = true;
            Place(rt, Corner.TL, bx[i], by[i], bw[i], 0);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            var st = new SpriteState();
            st.highlightedSprite = sel[i];
            st.selectedSprite = sel[i];
            st.pressedSprite = sel[i];
            st.disabledSprite = normal[i];
            btn.spriteState = st;
            botones.Add(btn);
            AddIntro(intro, rt, bs[i], bd[i], 0.5f, true);
        }

        // 9) Script de animación
        var mi = canvasGO.AddComponent<MenuIntro>();
        mi.elementos = intro.ToArray();
        mi.botones = botones.ToArray();

        // 10) Guardar escena
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        string path = AssetDatabase.GenerateUniqueAssetPath(ScenePath);
        EditorSceneManager.SaveScene(scene, path);
        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog("Listo", "Menú creado y guardado en:\n" + path + "\n\nDale Play para ver la animación.", "OK");
    }

    // ---------- Ayudas ----------

    static void AddIntro(List<MenuIntro.Elemento> l, RectTransform rt, MenuIntro.Side side, float delay, float dur, bool bounce)
    {
        var e = new MenuIntro.Elemento();
        e.objeto = rt; e.entraDesde = side; e.retraso = delay; e.duracion = dur; e.rebote = bounce;
        l.Add(e);
    }

    static RectTransform NewImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    static RectTransform NewContainer(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        Stretch(rt);
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // x,y = distancia desde la esquina hasta el CENTRO del elemento (en el lienzo de 1920x1080)
    static void Place(RectTransform rt, Corner c, float x, float y, float width, float rot)
    {
        float ax = (c == Corner.TL || c == Corner.BL) ? 0f : 1f;
        float ay = (c == Corner.BL || c == Corner.BR) ? 0f : 1f;
        float sx = ax == 0f ? 1f : -1f;
        float sy = ay == 0f ? 1f : -1f;
        var s = rt.GetComponent<Image>().sprite;
        float h = width * s.rect.height / s.rect.width;
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, h);
        rt.anchoredPosition = new Vector2(sx * x, sy * y);
        rt.localEulerAngles = new Vector3(0, 0, rot);
    }

    static Sprite FindSprite(string folder, string prefix)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(p).Replace('\\', '/') != folder) continue;
            if (Path.GetFileName(p).StartsWith(prefix)) return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        return null;
    }

    static void Generate(string fileName, int w, int h, System.Func<float, float, Color> f)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, f((x + 0.5f) / w, (y + 0.5f) / h));
        tex.Apply();
        string path = Root + "/Generado/" + fileName;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    static void ConfigurarTexturas()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 4096;

            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(st);

            ti.SaveAndReimport();
        }
    }
}