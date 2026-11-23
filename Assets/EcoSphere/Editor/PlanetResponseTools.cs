using System;
using UnityEditor;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class PlanetResponseTools
    {
        public static void Configure()
        {
            var catalog=PlanetStatCatalog.Current;catalog.responses=PlanetStatCatalog.DefaultResponses();
            catalog.NotifyChanged();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Validate();
        }
        public static void Validate()
        {
            var catalog=PlanetStatCatalog.Current;
            var unique=new System.Collections.Generic.HashSet<PlanetStat>();
            foreach(var response in catalog.responses)
            {
                if(!unique.Add(response.stat) || response.contributionForFullEffect<=0 || response.negativeContributionForFullEffect<=0)throw new Exception("Invalid visual response settings");
                if(response.Evaluate(0)!=0 || response.Evaluate(response.contributionForFullEffect)!=1 || response.Evaluate(-response.negativeContributionForFullEffect)!=-1)throw new Exception("Visual response endpoints");
            }
            if(unique.Count!=Enum.GetValues(typeof(PlanetStat)).Length)throw new Exception("Missing stat response");
            PlanetStats Sample(params (ElementId element,double points)[] entries)
            {
                var points=new double[ElementComposition.Capacity];foreach(var entry in entries)points[(int)entry.element]+=entry.points;
                return PlanetStatCalculator.Evaluate(points);
            }
            var cloudy=Sample((ElementId.Air,500),(ElementId.Water,500));
            if(cloudy[PlanetStat.CloudCover]!=1 || cloudy[PlanetStat.CloudThickness]<=.5f)throw new Exception("Mixed Air/Water must reach overcast");
            if(Sample((ElementId.Water,500),(ElementId.Void,500)).SurfaceWater!=1 || Sample((ElementId.Stone,500),(ElementId.Void,500)).Ruggedness!=1 || Sample((ElementId.Plant,500),(ElementId.Void,500))[PlanetStat.BiologicalCoverage]!=1)
                throw new Exception("Mixed worlds must reach full water, rockiness and biological coverage");
            if(Sample((ElementId.Air,500),(ElementId.Water,500)).AtmosphereDensity!=1)throw new Exception("Mixed atmosphere response");
            var tiny=Sample((ElementId.Water,1),(ElementId.Void,1));if(tiny.SurfaceWater>=1)throw new Exception("Response removed point buildup");
            var fire=Sample((ElementId.Fire,500),(ElementId.Void,500));if(fire[PlanetStat.CloudCover]!=0 || fire[PlanetStat.CloudDarkness]<=0)throw new Exception("Cloud darkness created coverage");
            var snapshot=cloudy.Copy();string json=JsonUtility.ToJson(snapshot);
            var setting=catalog.responses.Find(r=>r.stat==PlanetStat.CloudCover);float previous=setting.contributionForFullEffect;
            try{setting.contributionForFullEffect=1;var muted=Sample((ElementId.Air,500),(ElementId.Water,500));if(muted[PlanetStat.CloudCover]>=1 || JsonUtility.ToJson(snapshot)!=json)throw new Exception("Tuning changed saved snapshot or ignored response");}
            finally{setting.contributionForFullEffect=previous;catalog.NotifyChanged();}
            Debug.Log("ECOSPHERE_PLANET_RESPONSE_PASSED: per-stat endpoints, mixed full effects, point buildup, independent cloud darkness and stable snapshots");
        }
        public static void ConfigureAndCheck(){Configure();RefactorChecks.Validate();}
    }
}
