using System;
using UnityEngine;

namespace EcoSphere
{
    // Shared scoring and recipe-threshold values. Every level, the visualizer, completion and the checks read these.
    [CreateAssetMenu(menuName = "EcoSphere/Scoring Settings")]
    public sealed class ScoringSettings : ScriptableObject
    {
        [Header("Points")]
        [Tooltip("Points per sphere in a three-sphere match.")]
        [Min(1)] public int matchPointsPerSphere = 20;
        [Tooltip("Extra points per sphere for each sphere beyond three in the matched group (a streak).")]
        [Min(0)] public int streakBonusPerExtraSphere = 5;
        [Tooltip("Highest points per matched sphere, however large the group.")]
        [Min(1)] public int maximumMatchPointsPerSphere = 50;
        [Tooltip("Points per sphere released from the cluster.")]
        [Min(1)] public int breakawayPoints = 75;
        [Tooltip("Bonus points per ricochet on a shot that makes a match. Bonus points count toward stars but not element shares.")]
        [Min(0)] public int ricochetBonus = 15;

        [Header("Recipe thresholds (relative to the number of element bars)")]
        [Tooltip("An element qualifies for a recipe at this multiple of its fair share (100% / bars).")]
        [Min(1)] public float recipeShareMultiple = 1.1f;
        [Tooltip("The recipe threshold never exceeds this share, so levels with few bars keep their pairs reachable.")]
        [Range(1, 100)] public float maximumRecipePercent = 35;
        [Tooltip("A self-pair needs strictly more than this multiple of the fair share.")]
        [Min(1)] public float selfShareMultiple = 1.6f;
        [Tooltip("The self-pair threshold never exceeds this share (a strict majority).")]
        [Range(1, 100)] public float maximumSelfPercent = 50;

        [Header("Star thresholds (fractions of par = starting cluster cells x breakaway points)")]
        [Range(0, 2)] public float oneStarPar = .30f, twoStarPar = .46f, threeStarPar = .60f;

        public int MatchRate(int count) => count < 3 ? 0 :
            Mathf.Min(maximumMatchPointsPerSphere, matchPointsPerSphere + streakBonusPerExtraSphere * (count - 3));
        public double RecipePercent(int channels) => Math.Min(maximumRecipePercent, 100.0 / Math.Max(1, channels) * recipeShareMultiple);
        public double SelfPercent(int channels) => Math.Min(maximumSelfPercent, 100.0 / Math.Max(1, channels) * selfShareMultiple);
        public double Par(int cells) => cells * (double)breakawayPoints;

        public void Validate()
        {
            if (maximumMatchPointsPerSphere < matchPointsPerSphere) throw new InvalidOperationException("Match point cap is below the base match rate");
            if (!(oneStarPar > 0 && twoStarPar > oneStarPar && threeStarPar > twoStarPar)) throw new InvalidOperationException("Star fractions of par must increase from one to three stars");
            if (maximumSelfPercent < maximumRecipePercent || selfShareMultiple < recipeShareMultiple) throw new InvalidOperationException("Self-pair thresholds must not be below recipe thresholds");
        }

        static ScoringSettings current;
        public static ScoringSettings Current
        {
            get
            {
                if (current) return current;
                current = Resources.Load<ScoringSettings>("ScoringSettings");
                if (!current) { current = CreateInstance<ScoringSettings>(); current.hideFlags = HideFlags.DontSave; }
                return current;
            }
        }
    }
}
