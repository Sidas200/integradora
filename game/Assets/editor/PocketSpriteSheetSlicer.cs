using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PocketSpriteSheetSlicer
{
    // Ajusta esta ruta si tu carpeta se llama distinto.
    private const string SPRITESHEET_NAME = "SpriteSheet.png";

    [MenuItem("Tools/Pocket Inventory/Slice Original SpriteSheet")]
    public static void Slice()
    {
        string[] guids = AssetDatabase.FindAssets(
            "SpriteSheet t:Texture2D",
            new[] { "Assets" }
        );

        string path = null;

        foreach (string guid in guids)
        {
            string candidate =
                AssetDatabase.GUIDToAssetPath(guid);

            if (
                candidate.Contains("PNG SpriteSheet") &&
                candidate.EndsWith(SPRITESHEET_NAME)
            )
            {
                path = candidate;
                break;
            }
        }

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError(
                "No encontré PNG SpriteSheet/SpriteSheet.png"
            );

            return;
        }

        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError("No se pudo obtener TextureImporter.");
            return;
        }

        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Multiple;

        importer.filterMode =
            FilterMode.Point;

        importer.textureCompression =
            TextureImporterCompression.Uncompressed;

        importer.mipmapEnabled =
            false;

        importer.alphaIsTransparency =
            true;

        importer.wrapMode =
            TextureWrapMode.Clamp;

        List<SpriteMetaData> sprites =
            new List<SpriteMetaData>();

        // =====================================================
        // MENÚS COMPLETOS DEL PACK
        // Coordenadas convertidas al sistema de Unity.
        // SpriteSheet: 4720 x 2960
        // =====================================================

        AddSprite(
            sprites,
            "Menu_Profile",
            182,
            2443,
            444,
            331
        );

        AddSprite(
            sprites,
            "Menu_Inventory",
            822,
            2443,
            444,
            331
        );

        AddSprite(
            sprites,
            "Menu_WorldMap",
            1462,
            2443,
            444,
            331
        );

        AddSprite(
            sprites,
            "Menu_SaveLoad",
            2102,
            2443,
            444,
            331
        );

        AddSprite(
            sprites,
            "Menu_Options",
            2742,
            2443,
            444,
            331
        );

        // =====================================================
        // FRAMES DE CIERRE
        // Marrón superior.
        //
        // Los nombro en orden de más abierto a más cerrado.
        // =====================================================

        AddSprite(
            sprites,
            "Close_00",
            2216,
            1897,
            299,
            51
        );

        AddSprite(
            sprites,
            "Close_01",
            2856,
            1945,
            299,
            67
        );

        AddSprite(
            sprites,
            "Close_02",
            3496,
            2009,
            299,
            99
        );

        AddSprite(
            sprites,
            "Close_03",
            4136,
            2073,
            299,
            131
        );

        AddSprite(
            sprites,
            "Close_04",
            2856,
            1587,
            299,
            45
        );

        AddSprite(
            sprites,
            "Close_05",
            2216,
            1587,
            299,
            48
        );

        AddSprite(
            sprites,
            "Close_06",
            1576,
            1587,
            299,
            66
        );

        AddSprite(
            sprites,
            "Close_07",
            936,
            1587,
            299,
            98
        );

        AddSprite(
            sprites,
            "Close_08",
            296,
            1561,
            299,
            131
        );

        importer.spritesheet =
            sprites.ToArray();

        importer.SaveAndReimport();

        Debug.Log(
            $"SpriteSheet cortado correctamente: {sprites.Count} sprites creados."
        );
    }

    private static void AddSprite(
        List<SpriteMetaData> list,
        string name,
        float x,
        float y,
        float width,
        float height
    )
    {
        SpriteMetaData data =
            new SpriteMetaData();

        data.name = name;

        data.rect =
            new Rect(
                x,
                y,
                width,
                height
            );

        data.alignment =
            (int)SpriteAlignment.Center;

        data.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        list.Add(data);
    }
}