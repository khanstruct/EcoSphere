using UnityEngine;
namespace EcoSphere
{
    // Rendering assets only. Element-to-planet mappings live in PlanetStatCatalog.
    [CreateAssetMenu(menuName="EcoSphere/Planet Visual Style")]
    public sealed class PlanetVisualStyle : ScriptableObject
    {
        public Material atmosphereMaterial;
        public Material previewMaterial;
    }
}
