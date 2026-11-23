using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class SphereCategoryTools
    {
        public static void Validate()
        {
            SphereCatalogTools.Validate();
            foreach(var sphere in SphereCatalog.Current.spheres)
            {
                if(sphere.id>SphereIds.Mud)continue; // New sphere assets choose their own category; catalog validation checks the enum.
                var expected=sphere.id==SphereIds.Plant || sphere.id==SphereIds.Life ? SphereCategory.Life :
                    sphere.id==SphereIds.Void || sphere.role==SphereRole.Core ? SphereCategory.Forces :
                    sphere.role==SphereRole.Laser ? SphereCategory.Technology : SphereCategory.Basic;
                if(sphere.category!=expected)throw new Exception("Unexpected category on "+sphere.displayName);
            }
            Debug.Log("ECOSPHERE_SPHERE_CATEGORY_DATA_PASSED: current definitions categorized, category validation, stable sphere/element IDs");
        }
        public static void Smoke(){EditorSceneManager.OpenScene(LevelPrepTools.ScenePath);UnityEditor.EditorApplication.isPlaying=true;}
    }
}
