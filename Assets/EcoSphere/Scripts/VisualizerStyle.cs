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

        [Header("Contribution shortage notice (rises just above the visualizer, spanning its width)")]
        [Tooltip("{0} is the limiting element's name. The text uses that element's visualizer color.")]
        public string shortageFormat="Not enough {0}";
        [Min(6),Tooltip("Largest font size; the text auto-sizes to fill the visualizer's width within the notice height.")]
        public float shortageFontSize=150;
        [Min(10),Tooltip("Height of one notice line; simultaneous notices stack by this much.")]
        public float shortageHeight=118;
        [Min(.1f)] public float shortageDuration=1.6f;
        [Min(0)] public float shortageRise=24;
        [Tooltip("Gap between the top of the visualizer and the notice.")]
        [Min(0)] public float shortageOffset=8;
        [Range(0,1)] public float shortageOutline=.4f;
        public Color shortageOutlineColor=new Color(0,0,0,.95f);
        [Tooltip("Soft drop shadow behind the outlined text, in TextMeshPro underlay units.")]
        public Vector2 shortageShadowOffset=new Vector2(.8f,-.8f);
        [Range(0,1),Tooltip("Spreads the shadow beyond the letter shapes.")] public float shortageShadowDilate=.5f;
        [Range(0,1)] public float shortageShadowSoftness=.35f;
        public Color shortageShadowColor=new Color(0,0,0,.85f);
        [Tooltip("Saved outline + shadow text material (built by ContributionLimitTools.Configure from the settings above), so builds include its shader variant.")]
        public Material shortageMaterial;
        // The one place that turns the shortage settings above into TextMeshPro material properties.
        public void ApplyShortageLook(Material material)
        {
            material.EnableKeyword(TMPro.ShaderUtilities.Keyword_Outline);
            material.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth,shortageOutline);
            material.SetColor(TMPro.ShaderUtilities.ID_OutlineColor,shortageOutlineColor);
            material.EnableKeyword(TMPro.ShaderUtilities.Keyword_Underlay);
            material.SetColor(TMPro.ShaderUtilities.ID_UnderlayColor,shortageShadowColor);
            material.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetX,shortageShadowOffset.x);
            material.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetY,shortageShadowOffset.y);
            material.SetFloat(TMPro.ShaderUtilities.ID_UnderlaySoftness,shortageShadowSoftness);
            material.SetFloat(TMPro.ShaderUtilities.ID_UnderlayDilate,shortageShadowDilate);
        }
    }
}
