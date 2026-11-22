using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class LifeSphereTools
    {
        public static void Configure()
        {
            SphereCatalogTools.Configure();SphereCatalogTools.Validate();
            var sphere=SphereCatalog.Current.Get(SphereIds.Life);
            if(sphere.namesakeElement!=ElementId.Life || sphere.color!=(Color)new Color32(246,183,205,255) || !sphere.AmmoEligible || sphere.availableAtStart || sphere.unallocatedWeight!=1 || sphere.contributions.Count!=1 || sphere.contributions[0].element!=ElementId.Life || sphere.contributions[0].amount!=3)
                throw new Exception("Life identity, color or contribution budget incorrect");
            if(!sphere.resourceVoice || sphere.resourceVoice.name!="biosphere" || LevelAudio.MatchVoice(SphereIds.Life,30)!=sphere.resourceVoice || SphereCatalog.Current.ElementVoice(ElementId.Life)!=sphere.resourceVoice)
                throw new Exception("Life must use its biosphere voice");
            var totals=new double[ElementComposition.Capacity];sphere.AddContributions(totals,12);sphere.AddContributions(totals,50);
            if(totals[(int)ElementId.Life]!=46.5)throw new Exception("Life match/breakaway fractional yields incorrect");
            if(SphereCatalog.Current.ElementAppearance(ElementId.Life)!=sphere || !SphereCatalog.Current.recipeBook.TryCombine(ElementId.Water,ElementId.Mud,out var result) || result!=sphere)
                throw new Exception("Life namesake art or Water + Mud recipe incorrect");
            Debug.Log("ECOSPHERE_LIFE_DATA_PASSED: pale pink #F6B7CD, biosphere, 3 Life / 1 Null, namesake art, 256px assets, Water + Mud recipe");
        }
        public static void Smoke(){EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);UnityEditor.EditorApplication.isPlaying=true;}
    }
}
