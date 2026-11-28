using UnityEngine;
namespace EcoSphere
{
    // One possible moon: it orbits once the planet's Moons stat reaches its threshold. Distance is in planet radii; size is a share of the planet's diameter.
    [System.Serializable] public sealed class MoonOrbit
    {
        [Range(0,1)] public float threshold;
        [Min(1.1f)] public float distance=1.6f;
        [Range(.02f,.6f)] public float size=.2f;
        [Min(1),Tooltip("Seconds per orbit.")] public float period=45;
        [Range(-89,89),Tooltip("Tilt of the orbit toward the viewer (0 = edge-on, so it passes in front of and behind the planet).")] public float inclination=12;
        [Range(-45,45)] public float roll;
        [Range(0,360)] public float startAngle;
    }
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
        [Header("Moons (driven by the Moons stat)")]
        [Tooltip("Lit material for orbiting moons; uses the Moon sphere's surface texture.")] public Material moonMaterial;
        [Tooltip("In order of threshold: the first is easy to reach, later ones need more Moon.")]
        public System.Collections.Generic.List<MoonOrbit> moonOrbits=DefaultMoonOrbits();
        [Min(.01f),Tooltip("Seconds for a moon to grow in or shrink away when the count changes.")] public float moonAppearSeconds=.8f;
        public static System.Collections.Generic.List<MoonOrbit> DefaultMoonOrbits()=>new System.Collections.Generic.List<MoonOrbit>
        {
            new MoonOrbit{threshold=.1f,distance=1.55f,size=.22f,period=40,inclination=14,roll=6,startAngle=30},
            new MoonOrbit{threshold=.55f,distance=1.85f,size=.16f,period=62,inclination=-22,roll=-10,startAngle=160},
            new MoonOrbit{threshold=.9f,distance=2.15f,size=.12f,period=90,inclination=30,roll=16,startAngle=260},
        };
        public int MoonCount(float moons){int count=0;foreach(var orbit in moonOrbits)if(moons>=orbit.threshold && orbit.threshold>0)count++;return count;}
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
