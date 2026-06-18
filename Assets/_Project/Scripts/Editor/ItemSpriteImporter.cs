using UnityEditor;
using UnityEngine;

public class ItemSpriteImporter : AssetPostprocessor
{
    private static readonly string SPRITES_PATH = "Assets/_Project/UI/Sprites";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SPRITES_PATH))
            return;

        TextureImporter importer = assetImporter as TextureImporter;
        if (importer == null)
            return;

        // Set as Sprite
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;

        // Point filter for pixel art look
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // No mipmaps
        importer.mipmapEnabled = false;

        // Alpha
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;

        Debug.Log($"ItemSpriteImporter: configured {assetPath}");
    }
}
