using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class CloudTools
    {
        public static void Configure()
        {
            const string texturePath="Assets/EcoSphere/Art/Planets/cloud-mask-v1.png";
            AssetDatabase.Refresh();var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.sRGBTexture=false;importer.textureType=TextureImporterType.Default;importer.maxTextureSize=256;
            importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.isReadable=true;   // lightning picks flash spots where the mask shows cloud
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            const string materialPath="Assets/EcoSphere/Art/Planets/planet-clouds.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("EcoSphere/Planet Clouds"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_CloudMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(material);
            var style=Resources.Load<PlanetVisualStyle>("PlanetVisualStyle");style.cloudMaterial=material;EditorUtility.SetDirty(style);
            var catalog=PlanetStatCatalog.Current;
            Set(catalog,ElementId.Air,PlanetStat.CloudCover,.75f);Set(catalog,ElementId.Water,PlanetStat.CloudCover,.2f);Set(catalog,ElementId.Water,PlanetStat.CloudThickness,.6f);Set(catalog,ElementId.Fire,PlanetStat.CloudDarkness,.6f);
            catalog.NotifyChanged();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Validate();
        }
        static void Set(PlanetStatCatalog catalog,ElementId element,PlanetStat stat,float amount)
        {
            var profile=catalog.elements.Find(p=>p.element==element);profile.effects.RemoveAll(e=>e.stat==stat);profile.effects.Add(new PlanetStatEffect(stat,amount));
        }
        public static void Validate()
        {
            PlanetStats Sample(ElementId element){var totals=new double[ElementComposition.Capacity];totals[(int)element]=1000;return PlanetStatCalculator.Evaluate(totals);}
            var fire=Sample(ElementId.Fire);var air=Sample(ElementId.Air);var water=Sample(ElementId.Water);
            if(fire[PlanetStat.CloudCover]!=0 || fire[PlanetStat.CloudThickness]!=0 || fire[PlanetStat.CloudDarkness]<=0 || air[PlanetStat.CloudCover]<=0 || air[PlanetStat.CloudThickness]!=0 || water[PlanetStat.CloudCover]<=0 || water[PlanetStat.CloudCover]>=air[PlanetStat.CloudCover] || water[PlanetStat.CloudThickness]<=0)
                throw new Exception("Cloud coverage, thickness and darkness must remain independent");
            var saved=JsonUtility.FromJson<PlanetStats>(JsonUtility.ToJson(fire));if(saved[PlanetStat.CloudDarkness]!=fire[PlanetStat.CloudDarkness])throw new Exception("Cloud snapshot lost darkness");
            var material=Resources.Load<PlanetVisualStyle>("PlanetVisualStyle").cloudMaterial;
            if(!material || ShaderUtil.ShaderHasError(material.shader))throw new Exception("Cloud shader/material failed");
            var cloudMask=material.GetTexture("_CloudMap") as Texture2D;if(!cloudMask || !cloudMask.isReadable)throw new Exception("Cloud mask must be readable so lightning can find clouds");
            Debug.Log("ECOSPHERE_CLOUD_DATA_PASSED: independent element-driven coverage/density/darkness, snapshots, material/shader");
        }
        public static void Smoke(){Configure();EditorSceneManager.OpenScene(FirstTutorialTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
