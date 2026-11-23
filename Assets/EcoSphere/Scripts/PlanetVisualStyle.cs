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
        public Material atmosphereMaterial;
        public Material previewMaterial;
        public Material cloudMaterial;
        [Range(1.001f,1.05f)] public float cloudShellScale=1.012f;
        public float cloudDriftSpeed=.008f;
        public System.Collections.Generic.List<PlanetTerrainStyle> terrains=new System.Collections.Generic.List<PlanetTerrainStyle>();
        public Material Terrain(string id,Material fallback)
        {
            foreach(var terrain in terrains)if(terrain.id==id)return terrain.material;
            return fallback;
        }
    }
}
