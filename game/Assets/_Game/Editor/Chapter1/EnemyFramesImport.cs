using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Importa los frames de los enemigos con animación en 8 direcciones como pixel art: punto, sin
    // compresión y pivote en el suelo. Clips "<Acción>_<Dirección>" en <carpeta>/Direcciones:
    // Idle, Walk, Alert, Windup, Attack, Hurt y Death (los recortó el script de las hojas originales).
    public class EnemyFramesImport : AssetPostprocessor
    {
        // Lobo del Velo: 8 hojas 3x3 recortadas a 80x80, patas en la fila 6 desde abajo
        public const string Lobo = "Assets/_Game/Art/enemies/Lobo_del_Velo";
        // Fragmento Errante: PNG de 512 reducidos a 64x64; el suelo (centro de su onda) en la fila 5
        public const string Fragmento = "Assets/_Game/Art/enemies/Fragmento_Errante";

        private static readonly (string folder, float frameSize, float groundRow)[] Sets =
        {
            (Lobo, 80f, 6f),
            (Fragmento, 64f, 5f),
        };

        private void OnPreprocessTexture()
        {
            var set = Sets.FirstOrDefault(s => assetPath.StartsWith(s.folder + "/"));
            if (set.folder == null) return;

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
            settings.spritePivot = new Vector2(0.5f, set.groundRow / set.frameSize);
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
        }

        // Un clip por carpeta (Direcciones/<acción>_<dirección>), frames por nombre
        public static GuardianVarnAnimator.Clip[] LoadClips(string folder)
        {
            return AssetDatabase.GetSubFolders($"{folder}/Direcciones")
                .Select(dir => new GuardianVarnAnimator.Clip
                {
                    name = Path.GetFileName(dir),
                    frames = Directory.GetFiles(dir, "*.png").OrderBy(f => f)
                        .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'))).ToArray(),
                })
                .ToArray();
        }
    }
}
