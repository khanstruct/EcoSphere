using UnityEngine;
namespace EcoSphere
{
    [CreateAssetMenu(menuName="EcoSphere/Visualizer Style")]
    public sealed class VisualizerStyle : ScriptableObject
    {
        public Material energyMaterial;
        public Sprite frame;
        [Min(1)] public float maximumBarWidth=82;
        [Min(0)] public float minimumGap=12;
        [Range(.5f,.95f)] public float leaderHeightFraction=.85f;
        [Range(0,.05f)] public float pulseFraction=.025f;
    }
}
