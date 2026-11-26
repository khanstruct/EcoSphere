using UnityEditor;

namespace EcoSphere.Editor
{
    // Keep future imports within the same runtime budget as the resized artwork.
    public sealed class GameArtImportRules : AssetPostprocessor
    {
        // Planet textures the shader repeats in both directions (by material tiling) rather than wrapping once around the globe.
        static readonly string[] TiledBothAxes = { "/city-lights-" };
        static bool IsTiledBothAxes(string path) { foreach (var marker in TiledBothAxes) if (path.Contains(marker)) return true; return false; }
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
                // Surface maps span pole to pole once, so they clamp vertically; tiled detail masks repeat on both axes.
                importer.wrapModeV = IsTiledBothAxes(assetPath) ? UnityEngine.TextureWrapMode.Repeat : UnityEngine.TextureWrapMode.Clamp;
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
