using UnityEditor;
using UnityEditor.SceneManagement;
namespace EcoSphere.Editor
{
    // Setup and checks for newly added spheres. Each label names an Art/Spheres/<Label> folder with surface and icon art.
    public static class SphereIntroTools
    {
        // (label, art version). Fish has only an icon in its folder; it borrows Water's surface.
        static readonly (string label,string version)[] Spheres = { ("Civilization","v1"), ("Magic","v1"), ("Technology","v1"), ("Spirit","v1"), ("Forest","v1"), ("Fish","v2") };

        // Imports the art, seeds the spheres and their planet profiles, then validates the catalog and city light data.
        public static void Configure()
        {
            foreach (var (label,version) in Spheres) SphereCatalogTools.ImportSphereArt(label,version);
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
