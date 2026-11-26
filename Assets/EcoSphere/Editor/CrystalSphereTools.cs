using UnityEditor;
using UnityEditor.SceneManagement;
namespace EcoSphere.Editor
{
    public static class CrystalSphereTools
    {
        public static void Smoke(){SphereCatalogTools.Validate();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
