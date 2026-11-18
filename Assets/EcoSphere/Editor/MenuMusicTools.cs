using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class MenuMusicTools
    {
        public static void Configure()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/system-background.mp3");
            if (!clip) throw new Exception("Missing system-background music");
            foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (source.clip == clip) UnityEngine.Object.DestroyImmediate(source);
            var music = UnityEngine.Object.FindAnyObjectByType<MenuMusic>();
            if (!music) music = new GameObject("System and Prep Music").AddComponent<MenuMusic>();
            music.clip = clip;
            music.volume = .18f;
            EditorUtility.SetDirty(music);
        }

        [MenuItem("EcoSphere/Setup Continuous Menu Music")]
        public static void Setup()
        {
            foreach (string path in new[] { SystemSceneTools.ScenePath, LevelPrepTools.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                Configure();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_CONTINUOUS_MUSIC_CONFIGURED");
        }

        public static void Smoke()
        {
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
