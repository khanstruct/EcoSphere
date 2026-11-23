using UnityEditor;

namespace EcoSphere.Editor
{
    // Keep future imports within the same runtime budget as the resized artwork.
    public sealed class GameArtImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/EcoSphere/Art/") && !assetPath.StartsWith("Assets/suns/")) return;
            if (assetPath.Contains("/Backgrounds/starfield-")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 256;
            if (assetPath.StartsWith("Assets/EcoSphere/Art/Planets/") && assetPath.EndsWith(".png"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = !assetPath.Contains("heightmap") && !assetPath.Contains("mask");
                importer.mipmapEnabled = true;
                importer.wrapModeU = UnityEngine.TextureWrapMode.Repeat;
                importer.wrapModeV = UnityEngine.TextureWrapMode.Clamp;
                importer.filterMode = UnityEngine.FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
            foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                if (!settings.overridden || settings.maxTextureSize <= 256) continue;
                settings.maxTextureSize = 256;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
