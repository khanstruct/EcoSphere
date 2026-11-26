using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    // Imports the city light masks onto the planet terrain materials and seeds the city light stats, then checks the data path.
    public static class CityLightTools
    {
        public const string ScatteredMask="Assets/EcoSphere/Art/Planets/city-lights-mask-v1.png";
        public const string DenseMask="Assets/EcoSphere/Art/Planets/city-lights-dense-mask-v1.png";

        public static void Configure()
        {
            AssetDatabase.Refresh();
            foreach(var path in new[]{ScatteredMask,DenseMask})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path) ?? throw new Exception("Missing city light mask "+path);
                // Linear grayscale data mask, tiled around the globe by the planet material.
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.None;
                importer.maxTextureSize=256;importer.mipmapEnabled=true;importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Repeat;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var terrain in PlanetVisualStyle.Current.terrains)
            {
                // Load each mask right before assigning it and confirm the material kept it.
                foreach(var (property,path) in new[]{("_CityLightsDenseMask",DenseMask),("_CityLightsMask",ScatteredMask)})
                {
                    var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(path);terrain.material.SetTexture(property,mask);
                    // The first assignment after a planet shader reimport can be dropped while the material reloads; assign again.
                    if(terrain.material.GetTexture(property)!=mask)terrain.material.SetTexture(property,mask);
                    if(terrain.material.GetTexture(property)!=mask)throw new Exception(terrain.id+": could not assign "+path);
                }
                EditorUtility.SetDirty(terrain.material);
            }
            var catalog=PlanetStatCatalog.Current;
            if(!catalog.responses.Exists(x=>x.stat==PlanetStat.CityLightIntensity))
                catalog.responses.Add(PlanetStatCatalog.DefaultResponses().Find(x=>x.stat==PlanetStat.CityLightIntensity));
            catalog.SeedCityLights();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            PlanetColorTools.Configure();   // adds the CityLights color response and its Technology/Magic tints
            Validate();
        }

        public static void Validate()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("City lights: "+message);}
            PlanetStats Sample(params (ElementId element,double points)[] parts)
            {
                var totals=new double[ElementComposition.Capacity];foreach(var part in parts)totals[(int)part.element]=part.points;
                return PlanetStatCalculator.Evaluate(totals);
            }
            foreach(var path in new[]{ScatteredMask,DenseMask})
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Expect(texture && Mathf.Max(texture.width,texture.height)<=256,path+" missing or over the 256px limit");
                Expect(texture.wrapModeU==TextureWrapMode.Repeat && texture.wrapModeV==TextureWrapMode.Repeat,path+" must repeat on both axes (the material tiles it)");
            }
            foreach(var terrain in PlanetVisualStyle.Current.terrains)
                Expect(terrain.material.GetTexture("_CityLightsMask") && terrain.material.GetTexture("_CityLightsDenseMask"),terrain.id+" material lacks its city light masks");
            var wild=Sample((ElementId.Water,500),(ElementId.Plant,500));
            Expect(wild[PlanetStat.Civilization]==0 && wild[PlanetStat.CityLightIntensity]==0,"a world without civilization must have no lights");
            var human=Sample((ElementId.Water,500),(ElementId.Human,500));var city=Sample((ElementId.Water,500),(ElementId.City,500));
            var tech=Sample((ElementId.Water,500),(ElementId.City,250),(ElementId.Technology,250));
            Expect(human[PlanetStat.Civilization]>0 && city[PlanetStat.Civilization]>human[PlanetStat.Civilization],"City must spread settlements further than Human");
            Expect(tech[PlanetStat.CityLightIntensity]>city[PlanetStat.CityLightIntensity],"Technology must brighten the lights");
            Expect(city[PlanetColorTrait.CityLights].a==0,"plain cities must keep the default warm light color");
            var sprawl=Sample((ElementId.Water,500),(ElementId.Civilization,500));var advanced=Sample((ElementId.Water,500),(ElementId.Civilization,250),(ElementId.Technology,250));
            Expect(sprawl[PlanetStat.Civilization]>human[PlanetStat.Civilization] && sprawl[PlanetStat.CityLightIntensity]==0 && human[PlanetStat.CityLightIntensity]==0,"Civilization must spread lights without brightening them");
            Expect(advanced[PlanetStat.CityLightIntensity]>0,"Technology must brighten a civilization's lights");
            var magic=Sample((ElementId.City,500),(ElementId.Magic,500));
            Expect(magic[PlanetColorTrait.CityLights].a>0 && magic[PlanetColorTrait.CityLights].b>magic[PlanetColorTrait.CityLights].g,"Magic must tint the lights violet");
            var restored=JsonUtility.FromJson<PlanetStats>(JsonUtility.ToJson(magic));
            Expect(restored[PlanetStat.CityLightIntensity]==magic[PlanetStat.CityLightIntensity] && restored[PlanetColorTrait.CityLights]==magic[PlanetColorTrait.CityLights],"snapshot lost city light settings");
            Expect(!ShaderUtil.ShaderHasError(Shader.Find("EcoSphere/Barren Planet")),"planet shader failed to compile");
            Debug.Log("ECOSPHERE_CITY_LIGHT_DATA_PASSED: masks within 256px on every terrain material, none without civilization, Human < City coverage, Technology brightness, default warm color, Magic violet, snapshots, shader");
        }

        public static void Smoke(){Configure();EditorSceneManager.OpenScene(TestLevelTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
