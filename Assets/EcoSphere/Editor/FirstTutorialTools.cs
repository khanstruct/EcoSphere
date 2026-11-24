using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class FirstTutorialTools
    {
        public const string ScenePath = "Assets/EcoSphere/Scenes/Tutorial1.unity";

        public static void FollowupSmoke()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying=true;
        }
        [MenuItem("EcoSphere/Setup Tutorial 1")]
        public static void Build()
        {
            if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(TestLevelTools.ScenePath, ScenePath))
                throw new Exception("Could not create Tutorial 1 from the gameplay scene");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<EcoSpherePrototype>();
            game.gameObject.name = "EcoSphere Genesis Alpha";
            game.levelLayout = EcoSpherePrototype.LevelLayout.FirstTutorial;
            var rules=WorldCatalog.Current.Find("genesis-alpha").rules;
            game.resourceGoal=rules.inclusionGoal;game.abundanceGoal=rules.abundanceGoal;
            EditorUtility.SetDirty(game); EditorSceneManager.SaveScene(scene);
            var existing = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            int system = existing.FindIndex(s => s.path == SystemSceneTools.ScenePath);
            existing.Insert(system >= 0 ? system + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_TUTORIAL_ONE_SETUP_PASSED");
        }

        public static void Smoke()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
