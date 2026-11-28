using UnityEditor;
using UnityEditor.SceneManagement;
namespace EcoSphere.Editor
{
    // Setup and checks for newly added spheres. Each label names an Art/Spheres/<Label> folder with surface and icon art.
    public static class SphereIntroTools
    {
        // (label, art version). Fish has only an icon in its folder (it borrows Water's surface); Civilization's v2 is its house icon; Bird uses its v1 surface and v2 icon.
        static readonly (string label,string version)[] Spheres = { ("Civilization","v2"), ("Magic","v1"), ("Technology","v1"), ("Spirit","v1"), ("Forest","v1"), ("Fish","v2"), ("Sand","v1"), ("Bird","v1"), ("Bird","v2"), ("Metal","v1"), ("Wind","v1"), ("Lava","v1"), ("Mountain","v1"), ("Obsidian","v1"), ("Volcano","v1"), ("Lightning","v1"), ("Tools","v1"), ("Moon","v1") };

        // Imports the art, seeds the spheres and their planet profiles, then validates the catalog and city light data.
        public static void Configure()
        {
            foreach (var (label,version) in Spheres) SphereCatalogTools.ImportSphereArt(label,version);
            SphereCatalogTools.Configure();
            // Forest became an element after its sphere was first created: re-apply its namesake and contributions.
            SphereCatalogTools.ReseedSphere(SphereIds.Forest,ElementId.Forest);
            // Civilization moved to its house icon so it no longer looks like Sand; Stone moved to its v3 icon.
            SphereCatalogTools.ImportSphereArt("Stone","v3");
            SphereCatalogTools.ReapplyArt(SphereIds.Civilization);
            SphereCatalogTools.ReapplyArt(SphereIds.Stone);
            // Civilization's Civilization is now capped by Human: re-apply its contributions.
            SphereCatalogTools.ReseedSphere(SphereIds.Civilization,ElementId.Civilization);
            // Crystal now cracks on its first hit and shatters on the next.
            SphereCatalogTools.ConfigureCrystalDamage();
            // Spirit lost its recipe and now occurs naturally.
            SphereCatalogTools.SeedNaturalWeight(SphereIds.Spirit);
            // Profiles first: the color and city light checks below sample them.
            var planetStats=PlanetStatCatalog.Current;planetStats.EnsureProfiles();planetStats.SeedForest();
            // Mountain lifts the land well above the sea (Stone only roughens it): negative SurfaceWater lowers the sea level, plus rugged peaks.
            planetStats.SeedEffects(ElementId.Mountain,(PlanetStat.SurfaceWater,-.7f),(PlanetStat.TerrainRuggedness,1),(PlanetStat.Mineralization,.15f));
            // Lava and Volcano draw the glowing fissures through Magma (Fire is heat only) and warm the world only slightly.
            planetStats.SeedEffects(ElementId.Lava,(PlanetStat.Magma,1),(PlanetStat.Temperature,.3f));
            // Volcano also shakes the world, adds a few clouds darkened with ash, and builds new land.
            planetStats.SeedEffects(ElementId.Volcano,(PlanetStat.Magma,1),(PlanetStat.GeologicalActivity,1),(PlanetStat.Temperature,.25f),(PlanetStat.CloudCover,.25f),(PlanetStat.CloudDarkness,.5f),(PlanetStat.Pollution,.2f),(PlanetStat.SurfaceWater,-.4f),(PlanetStat.TerrainRuggedness,.8f));
            EditorUtility.SetDirty(planetStats);AssetDatabase.SaveAssets();
            CityLightTools.Configure();
            ConfigureCrumble();
            SphereCatalogTools.Validate();
            PlanetStatChecks.Validate();
        }

        const string CrumbleMaterialPath = "Assets/EcoSphere/Art/Effects/sphere-crumble.mat";
        // Saved particle material for eroding spheres' falling grains, referenced by SphereShapeStyle so builds include its shader.
        static void ConfigureCrumble()
        {
            System.IO.Directory.CreateDirectory("Assets/EcoSphere/Art/Effects");
            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(CrumbleMaterialPath);
            if (!material)
            {
                material = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Sphere crumble" };
                AssetDatabase.CreateAsset(material, CrumbleMaterialPath);
            }
            // Untextured, alpha-blended grains tinted by each particle's color.
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", UnityEngine.Color.white);
            EditorUtility.SetDirty(material);
            var style = SphereShapeStyle.Current; style.burstMaterial = material; EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            if (!style.burstMaterial || UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit") == null)
                throw new System.Exception("Crumble particle material missing");
        }
        // Run with -sphereIntroSmoke <sphere id> (DEV mode).
        public static void Smoke(){Configure();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
