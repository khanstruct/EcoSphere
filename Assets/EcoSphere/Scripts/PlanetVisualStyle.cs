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
        [Header("Lightning in the clouds (driven by PlanetStats.CloudLightning)")]
        [Range(0,1),Tooltip("Storm strength (cloud cover x weather x electrical activity) below which no flashes appear.")]
        public float lightningMinimumStorm=.01f;
        [Min(.05f),Tooltip("Average seconds between flashes in the weakest storm.")] public float lightningMaximumInterval=4f;
        [Min(.05f),Tooltip("Average seconds between flashes in the strongest storm.")] public float lightningMinimumInterval=.6f;
        [Range(0,4),Tooltip("Peak flash brightness in the strongest storm (weaker storms flash a little dimmer).")] public float lightningBrightness=3f;
        [Range(.01f,.6f),Tooltip("Flash size on the cloud shell (larger spreads the glow wider).")] public float lightningRadius=.15f;
        [Min(.005f)] public float lightningRise=.03f;
        [Min(.02f)] public float lightningFade=.22f;
        [Range(0,1),Tooltip("Chance a flash flickers a second time.")] public float lightningFlickerChance=.35f;
        [Min(0)] public float lightningFlickerDelay=.11f;
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
