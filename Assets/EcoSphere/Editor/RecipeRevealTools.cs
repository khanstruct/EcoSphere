using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class RecipeRevealTools
    {
        [MenuItem("EcoSphere/Configure Recipe Reveal")]
        public static void Configure()
        {
            const string path="Assets/EcoSphere/Resources/RecipeRevealStyle.asset";
            var style=AssetDatabase.LoadAssetAtPath<RecipeRevealStyle>(path);
            if(!style) {style=ScriptableObject.CreateInstance<RecipeRevealStyle>(); AssetDatabase.CreateAsset(style,path);}
            style.sphereMaterial=Material("Discovery Sphere","Assets/EcoSphere/Art/UI/Visualizer/discovery-sphere-v1.mat");
            style.flareMaterial=Material("Discovery Flare","Assets/EcoSphere/Art/UI/Visualizer/discovery-flare-v1.mat");
            EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_RECIPE_SHADERS_PASSED: sphere whitening/glow and procedural collision flare");
        }
        static Material Material(string name,string path)
        {
            var shader=Shader.Find("EcoSphere/UI/"+name);
            if(!shader || !shader.isSupported)throw new Exception("Missing recipe shader: "+name);
            foreach(var message in ShaderUtil.GetShaderMessages(shader))
                if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(shader); AssetDatabase.CreateAsset(material,path);}
            material.shader=shader; EditorUtility.SetDirty(material); return material;
        }
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath); EditorApplication.isPlaying=true;
        }
    }
}
