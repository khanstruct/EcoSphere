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
            game.laserSurface = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "laser-surface-v1.png");
            game.laserIcon = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "laser-icon-v1.png");
            if (!game.laserSurface || !game.laserIcon) throw new Exception("Laser artwork missing");
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
            var scene = EditorSceneManager.OpenScene(TestLevelTools.ScenePath);
            var audio = UnityEngine.Object.FindAnyObjectByType<LevelAudio>();
            audio.laser = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/laser-sphere.mp3");
            if (!audio.laser) throw new Exception("Missing laser-sphere sound");
            EditorUtility.SetDirty(audio);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ECOSPHERE_LASER_AUDIO_ASSIGNED");
        }
    }
}
