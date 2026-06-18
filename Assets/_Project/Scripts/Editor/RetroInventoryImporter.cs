using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-configures Retro Inventory PNG imports as pixel-art Sprites.
/// Runs automatically when assets are imported or when manually triggered.
/// </summary>
public class RetroInventoryImporter : AssetPostprocessor
{
    private const string RETRO_PATH = "Assets/_Project/Art/UI/RetroInventory";
    private const string RESOURCES_PATH = "Assets/_Project/Resources/RetroInventory";

    private void OnPreprocessTexture()
    {
        string path = assetPath;
        if (!path.StartsWith(RETRO_PATH) && !path.StartsWith(RESOURCES_PATH))
            return;
        if (!path.EndsWith(".png"))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        // 9-slice setup for the inventory background panel
        if (path.EndsWith("Inventory_9Slices.png"))
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureType = TextureImporterType.Sprite;
            // Border: 24px each side (8px original * 3x scale)
            importer.spriteBorder = new Vector4(24, 24, 24, 24);
        }

        Debug.Log($"RetroInventoryImporter: configured {path} as pixel-art sprite");
    }

    [MenuItem("Tools/Retro Inventory/Reimport All Sprites")]
    public static void ReimportAll()
    {
        ReimportFolder(RETRO_PATH);
        ReimportFolder(RESOURCES_PATH);
        AssetDatabase.Refresh();
        Debug.Log("RetroInventoryImporter: all sprites reimported");
    }

    private static void ReimportFolder(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
