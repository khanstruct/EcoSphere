using UnityEngine;
namespace EcoSphere
{
    [System.Serializable] public sealed class PlanetTerrainStyle
    {
        public string id;
        public Material material;
    }
    // Rendering assets only. Element-to-planet mappings live in PlanetStatCatalog.
    [CreateAssetMenu(menuName="EcoSphere/Planet Visual Style")]
    public sealed class PlanetVisualStyle : ScriptableObject
    {
        static PlanetVisualStyle current;
        public static PlanetVisualStyle Current
        {
            get{if(!current)current=Resources.Load<PlanetVisualStyle>("PlanetVisualStyle");return current;}
        }
        public Material atmosphereMaterial;
        public Material previewMaterial;
        public Material cloudMaterial;
        [Range(1.001f,1.05f)] public float cloudShellScale=1.012f;
        public float cloudDriftSpeed=.008f;
        [Header("Day/night view (Results)")]
        [Range(0,1),Tooltip("Sunlight at night, as a share of daylight. Kept above zero so the planet never goes fully dark.")]
        public float nightLightScale=.25f;
        [Range(0,1),Tooltip("Ambient light on the surface and clouds at night, as a share of daylight.")]
        public float nightAmbientScale=.4f;
        [Range(0,1),Tooltip("Atmosphere glow at night, as a share of daylight.")]
        public float nightAtmosphereScale=.5f;
        [Min(.01f)] public float dayNightTransitionSeconds=.6f;
        public System.Collections.Generic.List<PlanetTerrainStyle> terrains=new System.Collections.Generic.List<PlanetTerrainStyle>();
        public Material Terrain(string id,Material fallback)
        {
            foreach(var terrain in terrains)if(terrain.id==id)return terrain.material;
            return fallback;
        }
    }
}
