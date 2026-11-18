using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
namespace EcoSphere.Editor
{
    public static class FontTools
    {
        public const string FontPath = "Assets/EcoSphere/Fonts/Outfit-Medium.ttf";
        public const string NumberFontPath = "Assets/EcoSphere/Fonts/Outfit-SemiBold.ttf";
        public const string FontAssetPath = "Assets/EcoSphere/Fonts/Outfit-Medium SDF.asset";
        public const string NumberAssetPath = "Assets/EcoSphere/Fonts/Outfit-SemiBold SDF.asset";
        static TMP_FontAsset Create(string source, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(source);
            if (!font) throw new Exception("Missing font " + source);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?/+-=()[]<>%·–—→←↑↓×", out string missing);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }
        [MenuItem("EcoSphere/Apply Outfit TextMesh Pro")]
        public static void Apply()
        {
            if (!Directory.Exists("Assets/TextMesh Pro/Resources"))
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
                AssetDatabase.Refresh();
            }
            var font = Create(FontPath, FontAssetPath);
            var numbers = Create(NumberFontPath, NumberAssetPath);
            AssetDatabase.SaveAssets();
            foreach (var path in new[] { TestLevelTools.ScenePath, SystemSceneTools.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var legacy in root.GetComponentsInChildren<Text>(true))
                    {
                        var go = legacy.gameObject;
                        string value = legacy.text;
                        float size = legacy.fontSize;
                        Color color = legacy.color;
                        bool raycast = legacy.raycastTarget;
                        foreach (var effect in go.GetComponents<BaseMeshEffect>()) UnityEngine.Object.DestroyImmediate(effect);
                        UnityEngine.Object.DestroyImmediate(legacy);
                        var text = go.AddComponent<TextMeshProUGUI>();
                        text.font = go.name == "Sphere Count" ? numbers : font;
                        text.text = value; text.fontSize = size; text.color = color;
                        text.alignment = TextAlignmentOptions.Center;
                        text.raycastTarget = raycast;
                        if (go.name == "Sphere Count") { text.outlineColor = new Color32(0,0,6,255); text.outlineWidth = .15f; }
                    }
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font = text.name == "Sphere Count" ? numbers : font;
                    foreach (var game in root.GetComponentsInChildren<EcoSpherePrototype>(true)) game.uiFont = font;
                    foreach (var system in root.GetComponentsInChildren<SystemView>(true))
                        if (!system.selection) system.selection = root.GetComponentInChildren<TMP_Text>(true);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentsInChildren<Text>(true).Length != 0 || root.GetComponentsInChildren<TextMesh>(true).Length != 0) throw new Exception("Legacy text remains");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_TMP_MIGRATION_PASSED");
            SystemSceneTools.ValidateAndRender();
        }
    }
}
