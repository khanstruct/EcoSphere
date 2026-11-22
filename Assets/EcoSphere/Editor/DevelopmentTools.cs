using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class DevelopmentTools
    {
        [MenuItem("EcoSphere/Configure Development Mode")]
        public static void Configure()
        {
            SphereCatalogTools.Configure();
            const string path="Assets/EcoSphere/Resources/DevelopmentSettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<DevelopmentSettings>(path);
            if(!settings) { settings=ScriptableObject.CreateInstance<DevelopmentSettings>(); AssetDatabase.CreateAsset(settings,path); }
            AssetDatabase.SaveAssets();
            var water=SphereCatalog.Current.Get(SphereIds.Water);
            if(water.contributions.Count!=1 || water.contributions[0].element!=ElementId.Water || water.contributions[0].amount!=3 || water.unallocatedWeight!=1 || water.availableAtStart || !water.AmmoEligible)
                throw new Exception("Water definition needs three Water units, one Null and ordinary unlock/selection rules");
            if(!water.resourceVoice || water.resourceVoice.name!="hydrosphere" || water.color.b<=water.color.r || water.color.g>=SphereCatalog.Current.Get(SphereIds.Ice).color.g)
                throw new Exception("Water needs its hydrosphere voice and a blue darker than Ice");
            SphereCatalogTools.Validate();
            Debug.Log("ECOSPHERE_WATER_DEV_CONFIGURED: Water assets/voice/contribution validated; development toggle created without overwriting preferences");
        }
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath); EditorApplication.isPlaying=true;
        }
    }
}
