using UnityEngine;
using UnityEditor;

public static class PixelArtTextureSetup
{
    [MenuItem("Tools/Pixel Art/Configure All Sprites")]
    public static void ConfigureAllSprites()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { "Assets" }
        );

        int modified = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            bool changed = false;

            // ============================
            // SPRITE
            // ============================

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType =
                    TextureImporterType.Sprite;

                changed = true;
            }

            // ============================
            // FILTER MODE
            // ============================

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode =
                    FilterMode.Point;

                changed = true;
            }

            // ============================
            // COMPRESSION
            // ============================

            if (importer.textureCompression !=
                TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                changed = true;
            }

            // ============================
            // MIP MAPS
            // ============================

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            // ============================
            // ALPHA
            // ============================

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            // ============================
            // WRAP MODE
            // ============================

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode =
                    TextureWrapMode.Clamp;

                changed = true;
            }

            // ============================
            // PIXELS PER UNIT
            // ============================

            if (importer.spritePixelsPerUnit != 100)
            {
                importer.spritePixelsPerUnit = 100;
                changed = true;
            }

            // ============================
            // APPLY
            // ============================

            if (changed)
            {
                importer.SaveAndReimport();
                modified++;
            }
        }

        AssetDatabase.Refresh();

        Debug.Log(
            $"Pixel Art configurado. Sprites modificados: {modified}"
        );
    }
}