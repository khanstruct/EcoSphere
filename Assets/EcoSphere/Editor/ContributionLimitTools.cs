using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class ContributionLimitTools
    {
        const string NoticeMaterialPath = "Assets/EcoSphere/Fonts/Shortage notice.mat";

        // Bakes the shortage notice's outline + shadow look (from VisualizerStyle) into a saved material on the HUD font,
        // so builds include that text shader variant instead of only creating it at runtime.
        public static void Configure()
        {
            var style = Resources.Load<VisualizerStyle>("VisualizerStyle");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(NoticeMaterialPath);
            if (!material) { material = new Material(font.material) { name = "Shortage notice" }; AssetDatabase.CreateAsset(material, NoticeMaterialPath); }
            material.CopyPropertiesFromMaterial(font.material);
            style.ApplyShortageLook(material);
            EditorUtility.SetDirty(material);
            style.shortageMaterial = material; EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            Validate();
        }


        public static void Validate()
        {
            var style = Resources.Load<VisualizerStyle>("VisualizerStyle");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            var material = style.shortageMaterial;
            if (!material || !EditorUtility.IsPersistent(material) || material.mainTexture != font.atlasTexture ||
                !material.IsKeywordEnabled(ShaderUtilities.Keyword_Outline) || !material.IsKeywordEnabled(ShaderUtilities.Keyword_Underlay) ||
                !Mathf.Approximately(material.GetFloat(ShaderUtilities.ID_OutlineWidth), style.shortageOutline))
                throw new Exception("Shortage notice material must be a saved HUD-font material with the style's outline and shadow");
            Debug.Log("ECOSPHERE_SHORTAGE_MATERIAL_PASSED: saved outline + shadow material on the HUD font atlas");
        }

        public static void Smoke(){Configure();SphereCatalogTools.Validate();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
