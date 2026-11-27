using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    // Seeds the default element tints into PlanetStatCatalog.asset without touching other stat tuning, then checks the data path.
    public static class PlanetColorTools
    {
        public static void Configure()
        {
            var catalog=PlanetStatCatalog.Current;
            if(catalog.colorResponses==null || catalog.colorResponses.Count==0)catalog.colorResponses=PlanetStatCatalog.DefaultColorResponses();
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))
                if(!catalog.colorResponses.Exists(x=>x.trait==trait))catalog.colorResponses.Add(new PlanetColorResponse{trait=trait});
#pragma warning disable CS0618
            // Plant's former green-water amount now lives in its Water color trait.
            foreach(var profile in catalog.elements)profile.effects.RemoveAll(effect=>effect.stat==PlanetStat.WaterGreenness);
#pragma warning restore CS0618
            catalog.SeedColorTraits();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Validate();
        }

        public static void Validate()
        {
            var catalog=PlanetStatCatalog.Current;catalog.Validate();
            PlanetStats Sample(params (ElementId element,double points)[] parts)
            {
                var totals=new double[ElementComposition.Capacity];foreach(var part in parts)totals[(int)part.element]=part.points;
                return PlanetStatCalculator.Evaluate(totals);
            }
            void Expect(bool value,string message){if(!value)throw new Exception("Planet color traits: "+message);}
            var plain=Sample((ElementId.Water,500),(ElementId.Stone,500));
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))Expect(plain[trait].a==0,"untinted elements must leave "+trait+" untouched");
            var crystal=Sample((ElementId.Stone,500),(ElementId.Crystal,1000));
            Expect(crystal[PlanetColorTrait.Land].a>0 && crystal[PlanetColorTrait.Land].r>crystal[PlanetColorTrait.Land].g,"Crystal must tint land pink");
            var magic=Sample((ElementId.Water,500),(ElementId.Magic,1000));
            Expect(magic[PlanetColorTrait.Vegetation].b>magic[PlanetColorTrait.Vegetation].g && magic[PlanetColorTrait.Water].a==0 && magic[PlanetColorTrait.Land].a==0,"Magic must tint vegetation violet and leave water and land alone");
            // Mixed tints average their hues by weight; the amount grows with total strength but never past the trait maximum.
            var both=Sample((ElementId.Crystal,1000),(ElementId.Clay,1000));
            var crystalLand=Sample((ElementId.Crystal,1000))[PlanetColorTrait.Land];var clayLand=Sample((ElementId.Clay,1000))[PlanetColorTrait.Land];
            var mixed=both[PlanetColorTrait.Land];
            Expect(mixed.r<=Mathf.Max(crystalLand.r,clayLand.r)+1e-4f && mixed.r>=Mathf.Min(crystalLand.r,clayLand.r)-1e-4f,"mixed land hue must sit between its sources");
            foreach(var response in catalog.colorResponses)Expect(both[response.trait].a<=response.maximumAmount+1e-5f,response.trait+" exceeded its maximum amount");
            // Plant keeps the faint water tint it had as WaterGreenness.
            var plant=Sample((ElementId.Plant,600),(ElementId.Life,300),(ElementId.Water,300));
            Expect(plant[PlanetColorTrait.Water].a>0 && plant[PlanetColorTrait.Water].a<=.04f,"Plant water tint must stay very subtle ("+plant[PlanetColorTrait.Water]+")");
            // Forest shapes the planet more than Plant at equal points: denser vegetation and a deep green tint.
            // Compared at a minority share: at a majority both already saturate vegetation coverage.
            var plantWorld=Sample((ElementId.Plant,150),(ElementId.Water,850));var forestWorld=Sample((ElementId.Forest,150),(ElementId.Water,850));
            var forestGreen=forestWorld[PlanetColorTrait.Vegetation];
            Expect(forestWorld[PlanetStat.BiologicalCoverage]>plantWorld[PlanetStat.BiologicalCoverage] && forestWorld.VegetationPotential>plantWorld.VegetationPotential,"Forest must grow denser vegetation than Plant");
            Expect(forestGreen.a>0 && forestGreen.g>forestGreen.r && forestGreen.g>forestGreen.b && plantWorld[PlanetColorTrait.Vegetation].a==0,"Forest must deepen the vegetation green; Plant keeps the default");
            // Snapshots keep traits; old snapshots with only WaterGreenness still draw their green water.
            var restored=JsonUtility.FromJson<PlanetStats>(JsonUtility.ToJson(magic));
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))Expect(restored[trait]==magic[trait],trait+" did not survive a snapshot round trip");
            var legacy=JsonUtility.FromJson<PlanetStats>("{\"version\":1,\"values\":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.05]}");
            Expect(legacy[PlanetColorTrait.Water].a==0 && Mathf.Approximately(legacy.AppliedTint(PlanetColorTrait.Water).a,.05f) &&
                legacy.AppliedTint(PlanetColorTrait.Water).g>legacy.AppliedTint(PlanetColorTrait.Water).r,"legacy green-water snapshot lost its tint");
            Expect(legacy.AppliedTint(PlanetColorTrait.Land).a==0,"legacy snapshot gained a land tint");
            // A trait fading in starts at its own hue, not black.
            var fading=new PlanetStats();fading.Approach(crystal,.05f);
            var start=fading[PlanetColorTrait.Land];
            Expect(start.a>0 && start.a<crystal[PlanetColorTrait.Land].a && Mathf.Approximately(start.r,crystal[PlanetColorTrait.Land].r),"tint must fade in at its own hue");
            Expect(!ShaderUtil.ShaderHasError(Shader.Find("EcoSphere/Barren Planet")),"planet shader failed to compile");
            Debug.Log("ECOSPHERE_PLANET_COLOR_DATA_PASSED: untinted default, Crystal land, Magic vegetation only, weighted hue blend, maximum amounts, Plant parity, snapshots, legacy green water, fade-in hue, shader");
        }

        public static void Smoke(){Configure();EditorSceneManager.OpenScene(TestLevelTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
