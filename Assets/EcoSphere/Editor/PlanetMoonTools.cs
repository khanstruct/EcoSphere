using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    // Orbiting moons: a lit material wearing the Moon sphere's surface, plus checks on the orbit data and the Moon profile.
    public static class PlanetMoonTools
    {
        const string MaterialPath="Assets/EcoSphere/Art/Planets/planet-moon.mat";
        public static void Configure()
        {
            // The texture comes from whichever sphere represents the Moon element, so the two can't drift apart.
            var moonSphere=SphereCatalog.Current.ElementAppearance(ElementId.Moon) ?? throw new Exception("No sphere represents the Moon element");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Planet moon"};AssetDatabase.CreateAsset(material,MaterialPath);}
            material.SetTexture("_BaseMap",moonSphere.surface);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.08f);
            EditorUtility.SetDirty(material);
            var style=PlanetVisualStyle.Current;style.moonMaterial=material;
            if(style.moonOrbits==null || style.moonOrbits.Count==0)style.moonOrbits=PlanetVisualStyle.DefaultMoonOrbits();
            EditorUtility.SetDirty(style);AssetDatabase.SaveAssets();
            Validate();
        }
        public static void Validate()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Planet moons: "+message);}
            var style=PlanetVisualStyle.Current;var moonSphere=SphereCatalog.Current.ElementAppearance(ElementId.Moon);
            Expect(style.moonMaterial && moonSphere && style.moonMaterial.GetTexture("_BaseMap")==moonSphere.surface,"moon material must wear the Moon sphere's surface");
            Expect(style.moonOrbits.Count>=1 && style.moonOrbits.Count<=3,"one to three moon orbits");
            for(int i=0;i<style.moonOrbits.Count;i++)
            {
                var orbit=style.moonOrbits[i];
                Expect(orbit.threshold>0 && orbit.threshold<=1 && (i==0 || orbit.threshold>style.moonOrbits[i-1].threshold),"moon thresholds must rise in order");
                Expect(orbit.distance>1+orbit.size,"moon "+(i+1)+" must orbit clear of the planet");
            }
            Expect(style.moonOrbits[0].threshold<=.2f,"the first moon must come at a low threshold");
            var profile=PlanetStatCatalog.Current.elements.Find(x=>x.element==ElementId.Moon);
            Expect(profile!=null && profile.effects.Exists(e=>e.stat==PlanetStat.Moons && e.amount>0),"the Moon element must raise the Moons stat");
            Debug.Log("ECOSPHERE_PLANET_MOONS_DATA_PASSED: material wears the Moon surface, rising thresholds, clear orbits, Moon profile");
        }
        // Run with -planetMoonSmoke (DEV mode).
        public static void Smoke(){SphereIntroTools.Configure();EditorSceneManager.OpenScene(FirstTutorialTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
