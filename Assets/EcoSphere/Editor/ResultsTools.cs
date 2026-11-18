using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class ResultsTools
    {
        public const string ScenePath = "Assets/EcoSphere/Scenes/Results.unity";
        const string Art = "Assets/EcoSphere/Art/";
        static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
        static DiscoveryArtwork Element(int type, string name, string folder) => new DiscoveryArtwork {
            type = type, displayName = name,
            surface = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Spheres/" + folder + "/" + folder.ToLowerInvariant() + "-surface-v1.png"),
            icon = Sprite("Spheres/" + folder + "/" + folder.ToLowerInvariant() + "-icon-v1.png")
        };

        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var results = new GameObject("World Results").AddComponent<ResultsView>();
            results.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            results.frame = Sprite("UI/Frames/panel-frame-v1.png");
            // Heading filenames are shared with LevelPrep's existing serialized assignments.
            var prepScene = EditorSceneManager.OpenScene(LevelPrepTools.ScenePath, OpenSceneMode.Additive);
            var prep = UnityEngine.Object.FindAnyObjectByType<LevelPrepView>();
            results.line = prep.line; results.arch = prep.arch; results.starfield = prep.starfield;
            results.buttonPrefab = prep.buttonPrefab;
            EditorSceneManager.CloseScene(prepScene, true);
            results.elementDiscovered = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/Voice/element-discovered.mp3");
            results.elements = new[] { Element(0, "Ice", "Ice"), Element(1, "Sunlight", "Sunlight"), Element(3, "Stone", "Stone") };
            if (!results.elementDiscovered || !results.buttonPrefab || results.elements.Any(e => !e.surface || !e.icon))
                throw new Exception("Missing results artwork or discovery voice");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var systemScene = EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            var system = UnityEngine.Object.FindAnyObjectByType<SystemView>();
            system.completedWorldMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Planets/barren-planet-v1.mat");
            EditorUtility.SetDirty(system); EditorSceneManager.SaveScene(systemScene);
            var levelScene = EditorSceneManager.OpenScene(TestLevelTools.ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<EcoSpherePrototype>();
            game.resourceGoal = 1000000; game.abundanceGoal = 3000000;
            EditorUtility.SetDirty(game); EditorSceneManager.SaveScene(levelScene);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_RESULTS_SETUP_PASSED");
        }

        public static void Smoke()
        {
            EditorSceneManager.OpenScene(TestLevelTools.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
