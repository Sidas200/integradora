using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.EditorTools.Chapter1
{
    // Importa los frames de Varn como pixel art: punto, sin compresión y pivote en los pies.
    // Los frames ya vienen reescalados a 100 PPU, el mismo de la Pixel Perfect Camera, para que cada
    // pixel de la imagen caiga en uno del juego: fases (vista sur) en 150x150 y 8 direcciones en
    // 124x124 (se dibujaron a distinta escala; así las dos miden lo mismo en el mundo).
    public class GuardianVarnImport : AssetPostprocessor
    {
        public const string Folder = "Assets/_Game/Art/enemies/Guardian_Varn";
        private const float PhaseFrameSize = 150f;
        private const float DirectionalFrameSize = 124f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder + "/")) return;
            bool directional = assetPath.Contains("/Direcciones/");

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = 100f;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // Fila de los pies contada desde abajo (las direccionales traen sombra pintada debajo)
            settings.spritePivot = directional
                ? new Vector2(0.5f, 10f / DirectionalFrameSize)
                : new Vector2(0.5f, 19f / PhaseFrameSize);
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
        }

        // Un clip por carpeta (Fases/<animación>, Direcciones/<acción>_<dirección>), frames por nombre
        public static GuardianVarnAnimator.Clip[] LoadClips()
        {
            return new[] { "Fases", "Direcciones" }
                .SelectMany(group => AssetDatabase.GetSubFolders($"{Folder}/{group}"))
                .Select(dir => new GuardianVarnAnimator.Clip
                {
                    name = Path.GetFileName(dir),
                    frames = Directory.GetFiles(dir, "*.png").OrderBy(f => f)
                        .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'))).ToArray(),
                })
                .ToArray();
        }

        // Atlas con empaque ajustado: los frames son casi todo transparencia
        public static void EnsureAtlas()
        {
            string path = $"{Folder}/Guardian_Varn.spriteatlasv2";
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder) });
                SpriteAtlasAsset.Save(atlas, path);
                AssetDatabase.Refresh();
            }
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                enableTightPacking = true,
                enableRotation = false,
                padding = 2,
                blockOffset = 1,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Point,
                generateMipMaps = false,
                sRGB = true,
                readable = false,
            };
            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformSettings(platform);
            importer.includeInBuild = true;
            importer.SaveAndReimport();
        }
    }
}
