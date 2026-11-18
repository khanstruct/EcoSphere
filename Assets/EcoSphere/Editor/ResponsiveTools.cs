using UnityEditor;
using UnityEditor.SceneManagement;
namespace EcoSphere.Editor
{
    public static class ResponsiveTools
    {
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying=true;
        }
    }
}
