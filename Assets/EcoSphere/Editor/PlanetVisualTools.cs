using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class PlanetVisualTools
    {
        public static void Configure()
        {
            AssetDatabase.Refresh();
            const string catalogPath="Assets/EcoSphere/Resources/PlanetStatCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<PlanetStatCatalog>(catalogPath);
            if(!catalog){catalog=ScriptableObject.CreateInstance<PlanetStatCatalog>();catalog.PopulateDefaults();AssetDatabase.CreateAsset(catalog,catalogPath);}
            foreach(string name in new[]{"EcoSphere/Barren Planet","EcoSphere/Planet Atmosphere","EcoSphere/UI/Planet Preview"})
            {
                var shader=Shader.Find(name);
                if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Planet shader failed: "+name);
            }
            const string materialPath="Assets/EcoSphere/Art/Planets/planet-atmosphere.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("EcoSphere/Planet Atmosphere"));AssetDatabase.CreateAsset(material,materialPath);}
            const string stylePath="Assets/EcoSphere/Resources/PlanetVisualStyle.asset";
            var style=AssetDatabase.LoadAssetAtPath<PlanetVisualStyle>(stylePath);
            if(!style){style=ScriptableObject.CreateInstance<PlanetVisualStyle>();AssetDatabase.CreateAsset(style,stylePath);}
            const string previewPath="Assets/EcoSphere/Art/Planets/planet-preview.mat";
            var preview=AssetDatabase.LoadAssetAtPath<Material>(previewPath);
            if(!preview){preview=new Material(Shader.Find("EcoSphere/UI/Planet Preview"));AssetDatabase.CreateAsset(preview,previewPath);}
            style.atmosphereMaterial=material;style.previewMaterial=preview;EditorUtility.SetDirty(style);AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_PLANET_VISUAL_SETUP_PASSED");
            PlanetStatChecks.Validate();
        }
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(TestLevelTools.ScenePath);EditorApplication.isPlaying=true;
        }
    }
}
