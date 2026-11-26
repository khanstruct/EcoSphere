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

        [Header("Threshold guides (behind the bars)")]
        [Tooltip("Zone between the match line (qualifies for a recipe) and the self-match line.")]
        public Color matchZoneColor=new Color(.45f,.85f,1,.07f);
        [Tooltip("Zone above the self-match line.")]
        public Color selfZoneColor=new Color(1,.78f,.3f,.09f);
        public Color matchLineColor=new Color(.55f,.9f,1,.55f);
        public Color selfLineColor=new Color(1,.8f,.35f,.65f);
        [Min(1)] public float guideLineThickness=2;
        public string matchLabel="MATCH",selfLabel="SELF";

        [Header("Star score strip (top of the frame)")]
        public Sprite emptyStar;
        [Min(10)] public float scoreStripHeight=30;
        [Min(8)] public float starSize=28;
        [Tooltip("The track ends this far past the three-star score, so the third star isn't pinned to the end.")]
        [Range(1,2)] public float scoreRangePastThreeStars=1.15f;
        public Color scoreTrackColor=new Color(1,1,1,.08f);
        public Color scoreFillColor=new Color(1,.78f,.3f,.85f);

        [Header("Contribution shortage notice (rises above the capped element's bar)")]
        [Tooltip("{0} is the limiting element's name. The text uses that element's visualizer color.")]
        public string shortageFormat="Not enough {0}";
        [Min(6)] public float shortageFontSize=18;
        [Min(.1f)] public float shortageDuration=1.6f;
        [Min(0)] public float shortageRise=24;
        [Tooltip("Start height above the capped bar's top, clearing its value label.")]
        [Min(0)] public float shortageOffset=42;
        [Range(0,.5f)] public float shortageOutline=.25f;
        public Color shortageOutlineColor=new Color(0,0,0,.9f);
    }
}
