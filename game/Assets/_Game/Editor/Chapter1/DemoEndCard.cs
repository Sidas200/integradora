using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static Game.EditorTools.Chapter1.UiFactory;

namespace Game.EditorTools.Chapter1
{
    // Pantalla final (07_DemoEnd): fondo "Paisaje onírico" y tarjeta "Fin de la demo." con letra
    // serif (Libre Baskerville, OFL) y los adornos de UiArt (EndCardDecor los pone al mostrarse).
    public static class DemoEndCard
    {
        private const string Scene = "Assets/_Game/Scenes/Chapter1/07_DemoEnd.unity";
        private const string BackgroundPath = "Assets/_Game/Art/Intro/end_dream_landscape.png";
        private const string VignettePath = "Assets/_Game/Art/Intro/end_vignette.png";
        private const string ShadowMaterialPath = "Assets/_Game/Fonts/LibreBaskerville/LibreBaskerville Shadow.mat";
        private const string FontFolder = "Assets/_Game/Fonts/LibreBaskerville";

        private static readonly Color Cream = new Color(1f, 0.95f, 0.88f);
        private static readonly Color Pink = new Color(0.96f, 0.62f, 0.86f);

        // Letras que se usan (atlas estático: no cambia el asset al jugar)
        private const string Glyphs =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "¡¿áéíóúüñÁÉÍÓÚÜÑ·…“”‘’—–×";

        [MenuItem("Tools/Claude/Chapter 1/Demo End - Rebuild End Card")]
        public static void RebuildInScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != Scene)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(Scene);
            }
            var canvas = GameObject.Find("NarrativeCanvas");
            if (canvas == null) { Debug.LogError("[Chapter1] No se encontró NarrativeCanvas en " + Scene); return; }

            StyleBackground(canvas.transform);
            var card = canvas.transform.Find("EndCard") as RectTransform;
            if (card == null) { Debug.LogError("[Chapter1] No se encontró EndCard"); return; }
            BuildCard(card);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Chapter1] Tarjeta final reconstruida en " + scene.name);
        }

        // Fondo a pantalla completa (sin deformarse) y un velo muy suave
        public static void StyleBackground(Transform canvas)
        {
            var bg = canvas.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = Background();
                bg.color = Color.white;
                bg.type = UnityEngine.UI.Image.Type.Simple;
                var fit = bg.GetComponent<AspectRatioFitter>();
                if (fit == null) fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = bg.sprite != null ? bg.sprite.rect.width / bg.sprite.rect.height : 16f / 9f;
            }
            var dim = canvas.Find("Dim")?.GetComponent<Image>();
            if (dim != null) dim.color = new Color(0.03f, 0.01f, 0.08f, 0.18f);
        }

        // Rehace el contenido de la tarjeta; conserva el objeto (DialogueSequence lo muestra al final)
        public static void BuildCard(RectTransform card)
        {
            for (int i = card.childCount - 1; i >= 0; i--) Object.DestroyImmediate(card.GetChild(i).gameObject);
            var old = card.GetComponent<EndCardDecor>();
            if (old != null) Object.DestroyImmediate(old);

            var cardImg = card.GetComponent<Image>();
            if (cardImg != null) { cardImg.color = new Color(0f, 0f, 0f, 0f); cardImg.raycastTarget = true; }

            var font = Font(false);

            // Velo oscuro al centro para que el texto se lea sobre el paisaje
            var vignette = Box(card, "Vignette", new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1500, 760));
            Image(vignette, new Color(0.05f, 0.02f, 0.10f, 0.9f), Vignette());

            var titleShadow = Text(card, "TitleShadow", "Fin de la demo.", font, 104, new Vector2(4, 216), new Vector2(1100, 140),
                new Color(0.08f, 0.02f, 0.12f, 0.75f));
            titleShadow.fontStyle = FontStyles.Bold;
            var title = Text(card, "Title", "Fin de la demo.", font, 104, new Vector2(0, 220), new Vector2(1000, 140), Color.white);
            title.fontStyle = FontStyles.Bold;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(Cream, Cream, new Color(0.97f, 0.83f, 0.62f), new Color(0.97f, 0.83f, 0.62f));

            Text(card, "Chapter", "Capítulo 1 completado: obtuviste el permiso del primer guardián.", font, 32,
                new Vector2(0, 92), new Vector2(1300, 50), Cream);
            var div1 = Box(card, "Divider1", new Vector2(0.5f, 0.5f), new Vector2(0, 48), new Vector2(10, 10));
            Text(card, "Zones", "Las otras tres zonas, sus guardianes y el Gran Juzgado te esperan\nen la versión completa.", font, 32,
                new Vector2(0, -20), new Vector2(1300, 100), Cream).lineSpacing = 6;
            var div2 = Box(card, "Divider2", new Vector2(0.5f, 0.5f), new Vector2(0, -88), new Vector2(10, 10));
            Text(card, "Thanks", $"Gracias por jugar <i><color=#{ColorUtility.ToHtmlStringRGB(Pink)}>The Day I Made That Promise</color></i>.", font, 34,
                new Vector2(0, -140), new Vector2(1300, 56), Cream);

            // Aviso abajo sobre una placa (el texto lo cambia ClickToContinue según el dispositivo)
            var plate = Box(card, "HintPlate", new Vector2(0.5f, 0f), new Vector2(0, 92), new Vector2(760, 66));
            var hint = Text(plate, "Hint", "Pulsa cualquier tecla para volver al menú", font, 28, Vector2.zero, new Vector2(760, 66), Cream);
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.one;
            hint.rectTransform.sizeDelta = Vector2.zero;

            var decor = card.gameObject.AddComponent<EndCardDecor>();
            var so = new SerializedObject(decor);
            so.FindProperty("title").objectReferenceValue = title.rectTransform;
            var divs = so.FindProperty("dividersAt");
            divs.arraySize = 2;
            divs.GetArrayElementAtIndex(0).objectReferenceValue = div1;
            divs.GetArrayElementAtIndex(1).objectReferenceValue = div2;
            so.FindProperty("hintPlate").objectReferenceValue = plate;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Vector2 pos, Vector2 box, Color color)
        {
            var t = Label(parent, name, text, size, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), pos, box, color);
            if (font != null)
            {
                t.font = font;
                var shadow = ShadowMaterial(font);
                if (shadow != null && name != "TitleShadow") t.fontSharedMaterial = shadow;
            }
            t.richText = true;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        // ------------------------------------------------------------ recursos

        private static Sprite Background()
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        }

        // Material de la fuente con sombra suave (underlay) para leer sobre el paisaje
        private static Material ShadowMaterial(TMP_FontAsset font)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
            if (mat != null) return mat;
            mat = new Material(font.material) { name = "LibreBaskerville Shadow" };
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0.05f, 0.01f, 0.09f, 0.85f));
            mat.SetFloat("_UnderlayOffsetX", 0.35f);
            mat.SetFloat("_UnderlayOffsetY", -0.45f);
            mat.SetFloat("_UnderlayDilate", 0.25f);
            mat.SetFloat("_UnderlaySoftness", 0.5f);
            AssetDatabase.CreateAsset(mat, ShadowMaterialPath);
            return mat;
        }

        // Elipse con borde muy suave (alfa 1 al centro → 0 en la orilla)
        private static Sprite Vignette()
        {
            if (!File.Exists(VignettePath))
            {
                const int w = 256, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.5f, 1f, d));
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px);
                File.WriteAllBytes(VignettePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(VignettePath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(VignettePath);
                ti.textureType = TextureImporterType.Sprite;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
        }

        // Fuente SDF estática a partir del .ttf; la cursiva queda enlazada para <i>
        public static TMP_FontAsset Font(bool italic)
        {
            string name = italic ? "LibreBaskerville-Italic" : "LibreBaskerville";
            string path = $"{FontFolder}/{name} SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset == null)
            {
                var ttf = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{name}.ttf");
                if (ttf == null) { Debug.LogError("[Chapter1] Falta " + name + ".ttf"); return null; }
                asset = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                asset.name = name + " SDF";
                AssetDatabase.CreateAsset(asset, path);
                asset.atlasTextures[0].name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                asset.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                asset.TryAddCharacters(Glyphs, out string missing);
                if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[Chapter1] {name} sin glifos: {missing}");
                asset.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            if (!italic)
            {
                var it = Font(true);
                if (it != null && asset.fontWeightTable != null && asset.fontWeightTable.Length > 4 &&
                    asset.fontWeightTable[4].italicTypeface != it)
                {
                    asset.fontWeightTable[4].italicTypeface = it;
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                }
            }
            return asset;
        }
    }
}
