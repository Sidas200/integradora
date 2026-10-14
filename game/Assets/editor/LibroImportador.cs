using UnityEditor;
using UnityEngine;

public static class LibroImportador
{
    [MenuItem("Window/Libro/Configurar Sprites")]
    public static void Configurar()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Libro" });
        int n = 0;
        AssetDatabase.StartAssetEditing();
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = 16;
            imp.SaveAndReimport();
            n++;
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();
        Debug.Log("Libro: " + n + " sprites configurados.");
    }
}