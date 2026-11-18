using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class ButtonAudioTools
    {
        public static void Setup()
        {
            const string clipPath = "Assets/EcoSphere/Audio/button-click.mp3";
            var importer = (AudioImporter)AssetImporter.GetAtPath(clipPath);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (!clip) throw new Exception("button-click.mp3 is missing");
            Directory.CreateDirectory("Assets/EcoSphere/Resources");
            AssetDatabase.Refresh();
            var root = new GameObject("UI click audio", typeof(AudioSource), typeof(UIClickAudio));
            var source = root.GetComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.volume = .65f;
            source.priority = 32;
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/EcoSphere/Resources/UIAudio.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_BUTTON_AUDIO_SETUP_PASSED");
        }
    }
}
