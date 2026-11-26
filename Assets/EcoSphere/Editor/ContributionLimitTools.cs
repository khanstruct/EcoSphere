using UnityEditor;
using UnityEditor.SceneManagement;
namespace EcoSphere.Editor
{
    public static class ContributionLimitTools
    {
        public static void Smoke(){SphereCatalogTools.Validate();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
