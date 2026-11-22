using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class MudSphereTools
    {
        public static void Configure()
        {
            LifeSphereTools.Configure();PlanetStatChecks.Validate();
            var sphere=SphereCatalog.Current.Get(SphereIds.Mud);
            if(sphere.namesakeElement!=ElementId.Mud || sphere.color!=(Color)new Color32(202,156,107,255) || !sphere.AmmoEligible || sphere.availableAtStart || sphere.TotalContributionUnits!=4 || sphere.unallocatedWeight!=0)
                throw new Exception("Mud identity/color/contribution budget incorrect");
            if(!sphere.resourceVoice || sphere.resourceVoice.name!="pelitosphere" || LevelAudio.MatchVoice(SphereIds.Mud,30)!=sphere.resourceVoice)
                throw new Exception("Mud must use pelitosphere");
            var totals=new double[ElementComposition.Capacity];sphere.AddContributions(totals,12);sphere.AddContributions(totals,50);
            if(totals[(int)ElementId.Water]!=15.5 || totals[(int)ElementId.Stone]!=15.5 || totals[(int)ElementId.Mud]!=31)
                throw new Exception("Mud requires 1 Water / 1 Stone / 2 Mud");
            if(!SphereCatalog.Current.recipeBook.TryCombine(ElementId.Water,ElementId.Stone,out var output) || output!=sphere)
                throw new Exception("Stone + Water must unlock Mud");
            Array.Clear(totals,0,totals.Length);totals[(int)ElementId.Mud]=500;totals[(int)ElementId.Void]=500;
            float soil=PlanetStatCalculator.Evaluate(totals)[PlanetStat.Soil];
            totals[(int)ElementId.Mud]=0;totals[(int)ElementId.Life]=500;
            if(soil<PlanetStatCalculator.Evaluate(totals)[PlanetStat.Soil]*4.9f)throw new Exception("Mud soil tint must be much stronger than Life");
            Debug.Log("ECOSPHERE_MUD_DATA_PASSED: light brown #CA9C6B, pelitosphere, 1 Water / 1 Stone / 2 Mud, Stone + Water -> Mud, Water + Mud -> Life, strong Soil weight 1");
        }
        public static void Smoke(){EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);UnityEditor.EditorApplication.isPlaying=true;}
    }
}
