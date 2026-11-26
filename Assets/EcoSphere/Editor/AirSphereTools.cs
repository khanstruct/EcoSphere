using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class AirSphereTools
    {
        public static void Configure()
        {
            SphereCatalogTools.ImportSphereArt("Air");
            SphereCatalogTools.Configure();SphereCatalogTools.Validate();
            var sphere=SphereCatalog.Current.Get(SphereIds.Air);
            var totals=new double[ElementComposition.Capacity];sphere.AddContributions(totals,12);sphere.AddContributions(totals,50);
            if(sphere.color!=(Color)new Color32(232,245,250,255) || sphere.namesakeElement!=ElementId.Air || sphere.unallocatedWeight!=1 || sphere.TotalContributionUnits!=4 || totals[(int)ElementId.Air]!=46.5 || !sphere.resourceVoice || sphere.resourceVoice.name!="atmosphere")
                throw new Exception("Air color, identity, weighted contributions or voice incorrect");
            foreach(var recipe in SphereCatalog.Current.recipeBook.recipes)if(recipe.output==sphere)throw new Exception("Air recipe should remain unset");
            Debug.Log("ECOSPHERE_AIR_DATA_PASSED: pale blue, atmosphere voice, 3 Air / 1 Null, namesake artwork and no recipe");
        }
        public static void Smoke(){Configure();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
