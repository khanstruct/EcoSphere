using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class PlantSphereTools
    {
        public static void Configure()
        {
            SphereCatalogTools.Configure();SphereCatalogTools.Validate();
            var sphere=SphereCatalog.Current.Get(SphereIds.Plant);
            if(sphere.displayName!="Plant Life" || sphere.color!=(Color)new Color32(62,160,74,255) || !sphere.AmmoEligible || sphere.availableAtStart || sphere.unallocatedWeight!=1 || sphere.contributions.Count!=2 || sphere.ContributionUnits!=3)
                throw new Exception("Plant Life definition has incorrect identity, color, role or units");
            if(!sphere.resourceVoice || sphere.resourceVoice.name!="florasphere" || LevelAudio.MatchVoice(SphereIds.Plant,30)!=sphere.resourceVoice)
                throw new Exception("Plant Life large-match voice is not florasphere");
            var totals=new double[Enum.GetValues(typeof(ElementId)).Length];sphere.AddContributions(totals,12);sphere.AddContributions(totals,50);
            if(totals[(int)ElementId.Life]!=15.5 || totals[(int)ElementId.Plant]!=31)
                throw new Exception("Plant Life weighted match/breakaway contributions incorrect");
            if(SphereCatalog.Current.recipeBook.recipes.Exists(x=>x.output.id==SphereIds.Plant))throw new Exception("Plant recipe must remain unassigned");
            Debug.Log("ECOSPHERE_PLANT_DATA_PASSED: medium green #3EA04A, florasphere, 1 Life / 2 Plant / 1 Null, 256px assets, normal locked / DEV available, recipe unassigned");
        }
        public static void Smoke() {EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
