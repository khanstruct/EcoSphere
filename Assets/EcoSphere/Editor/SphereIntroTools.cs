using UnityEditor;
using UnityEditor.SceneManagement;
namespace EcoSphere.Editor
{
    // Setup and checks for newly added spheres. Each label names an Art/Spheres/<Label> folder with surface and icon art.
    public static class SphereIntroTools
    {
        static readonly string[] Labels = { "Civilization", "Magic", "Technology", "Spirit", "Forest" };

        // Imports the art, seeds the spheres and their planet profiles, then validates the catalog and city light data.
        public static void Configure()
        {
            foreach (var label in Labels) SphereCatalogTools.ImportSphereArt(label);
            SphereCatalogTools.Configure();
            // Forest became an element after its sphere was first created: re-apply its namesake and contributions.
            SphereCatalogTools.ReseedSphere(SphereIds.Forest,ElementId.Forest);
            // Profiles first: the color and city light checks below sample them.
            var planetStats=PlanetStatCatalog.Current;planetStats.EnsureProfiles();planetStats.SeedForest();
            EditorUtility.SetDirty(planetStats);AssetDatabase.SaveAssets();
            CityLightTools.Configure();
            SphereCatalogTools.Validate();
            PlanetStatChecks.Validate();
        }
        // Run with -sphereIntroSmoke <sphere id> (DEV mode).
        public static void Smoke(){Configure();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
