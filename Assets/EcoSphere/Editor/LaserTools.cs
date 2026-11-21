using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class LaserTools
    {
        public static void Setup()
        {
            SphereCatalogTools.Configure();
            const string folder = "Assets/EcoSphere/Art/Spheres/Laser/";
            foreach (string filename in new[] { "laser-surface-v1.png", "laser-icon-v1.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(folder + filename);
                bool surface = filename.Contains("surface");
                importer.textureType = surface ? TextureImporterType.Default : TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = !surface;
                importer.mipmapEnabled = surface;
                importer.wrapMode = surface ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
            }
            EditorSceneManager.OpenScene(TestLevelTools.ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<EcoSpherePrototype>();
            var definition = SphereCatalog.Current.Get(SphereIds.Laser);
            if (!definition.surface || !definition.icon) throw new Exception("Laser artwork missing");
            EditorUtility.SetDirty(game);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            TestLevelTools.Validate();
            Debug.Log("ECOSPHERE_LASER_SETUP_PASSED");
        }

        public static void RunSmoke()
        {
            EditorSceneManager.OpenScene(TestLevelTools.ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void SetupAudio()
        {
            GameAudioTools.Configure();
            Debug.Log("ECOSPHERE_LASER_AUDIO_ASSIGNED: shared audio settings, no scene assignments");
        }
    }
}
