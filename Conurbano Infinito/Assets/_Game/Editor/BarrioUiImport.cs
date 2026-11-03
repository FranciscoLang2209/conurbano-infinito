using UnityEditor;
using UnityEngine;

public sealed class BarrioUiImport : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (!IsUiTexture()) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 48;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool sliced = name.StartsWith("button_") || name == "paper" || name == "enamel" || name == "slot";
        importer.spriteBorder = sliced ? new Vector4(6, 6, 6, 6) : Vector4.zero;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    private void OnPostprocessTexture(Texture2D texture)
    {
        if (!IsUiTexture() ||
            PlayerSettings.colorSpace != ColorSpace.Linear) return;
        // IMGUI samples GUIStyle textures without sRGB decoding; preserve the authored palette.
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = pixels[i].linear;
        texture.SetPixels(pixels);
        texture.Apply(false, false);
    }

    private bool IsUiTexture() => assetPath.StartsWith("Assets/_Game/Resources/BarrioUi/") ||
        assetPath.StartsWith("Assets/_Game/Resources/CharacterPortraits/") ||
        assetPath == "Assets/ThirdParty/NinjaAdventure/Resources/NinjaAdventure/focus.png";
}
