using UnityEngine;

namespace EcoSphere
{
    // Point values read from the shared ScoringSettings asset. Abundance is a world-rule default.
    public static class ElementScoring
    {
        public static int StandardMatch => ScoringSettings.Current.matchPointsPerSphere;
        public static int Breakaway => ScoringSettings.Current.breakawayPoints;
        public static int RicochetBonus => ScoringSettings.Current.ricochetBonus;
        public const int Abundance = 1500;
        public static int MatchRate(int count) => ScoringSettings.Current.MatchRate(count);
    }
}
