using UnityEditor;
using UnityEditor.SceneManagement;

namespace EcoSphere.Editor
{
    public static class WorldRatingTools
    {
        public static void Smoke()
        {
            AssetDatabase.ImportAsset("Assets/EcoSphere/Resources/WorldCatalog.asset",ImportAssetOptions.ForceUpdate);
            WorldCatalog.Current.Validate();
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying=true;
        }
    }
}
