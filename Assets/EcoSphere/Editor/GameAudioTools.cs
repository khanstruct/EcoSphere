using System.IO;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class GameAudioTools
    {
        public const string SettingsPath = "Assets/EcoSphere/Resources/GameAudioSettings.asset";

        [MenuItem("EcoSphere/Configure Shared Audio")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/EcoSphere/Resources"); AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<GameAudioSettings>(SettingsPath);
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<GameAudioSettings>();
                AssetDatabase.CreateAsset(settings,SettingsPath);
            }
            // Seed missing defaults only. All gameplay levels consume this one asset automatically.
            Assign(ref settings.music,"Deep Space Loop.mp3");
            Assign(ref settings.shoot,"shoot.ogg");
            Assign(ref settings.ricochet,"ricochet.ogg");
            Assign(ref settings.match,"match.wav");
            Assign(ref settings.breakaway,"breakaway.mp3");
            Assign(ref settings.miss,"miss.mp3");
            Assign(ref settings.laser,"laser-sphere.mp3");
            Assign(ref settings.worldComplete,"Voice/world-complete.mp3");
            Assign(ref settings.creationFailed,"Voice/creation-failed.mp3");
            Assign(ref settings.impressive,"Voice/impressive.mp3");
            Assign(ref settings.ecosphere,"Voice/ecosphere.mp3");
            Assign(ref settings.sphereCombine,"sphere-combine.mp3");
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); settings.Validate();
            Debug.Log("ECOSPHERE_SHARED_AUDIO_CONFIGURED: all common gameplay clips come from one asset");
        }
        static void Assign(ref AudioClip clip,string filename)
        {
            if (!clip) clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/"+filename);
        }

        public static void PlaybackSmoke()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
