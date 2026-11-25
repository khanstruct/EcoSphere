using System;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class ScoringChecks
    {
        [MenuItem("EcoSphere/Validate Scoring")]
        public static void Validate()
        {
            int checks = 0;
            void Expect(bool value, string message) { if (!value) throw new Exception("Scoring: " + message); checks++; }
            var scoring = ScoringSettings.Current; scoring.Validate();

            // Level score: elemental points plus bonuses; bad bonuses are ignored.
            var score = new LevelScore();
            score.Add(ScoreBonusKind.Ricochet, 30); score.Add(ScoreBonusKind.Ricochet, 15); score.Add(ScoreBonusKind.Ricochet, -5); score.Add(ScoreBonusKind.Ricochet, double.NaN);
            Expect(score.Bonus == 45 && score.Total(100) == 145 && score.EventsFrom(ScoreBonusKind.Ricochet) == 2, "bonus totals or events");
            Expect(LevelScore.Combine(-10, 5) == 5, "negative elemental points must not lower the score");
            score.Clear(); Expect(score.Bonus == 0 && score.Total(100) == 100, "clear");

            // Star thresholds are shared fractions of par, which follows the board size and breakaway value.
            var rules = new WorldRules { clusterRings = 3 };
            double par = GameplayGeometry.CellCount(3) * (double)scoring.breakawayPoints;
            Expect(Math.Abs(rules.Par - par) < 1e-9, "par = starting cells x breakaway points");
            Expect(rules.StarScore(1) == Math.Round(par * scoring.oneStarPar) && rules.StarScore(3) == Math.Round(par * scoring.threeStarPar), "star scores from par fractions");
            Expect(rules.StarsForScore(rules.StarScore(1) - .01) == 0 && rules.StarsForScore(rules.StarScore(1)) == 1 &&
                rules.StarsForScore(rules.StarScore(2)) == 2 && rules.StarsForScore(rules.StarScore(3)) == 3, "star boundaries");
            var small = new WorldRules { clusterRings = 2 };
            Expect(small.StarScore(1) < rules.StarScore(1), "a smaller board must need fewer points");

            // Match rates: zero below three, a streak bonus per extra sphere, and a cap.
            Expect(scoring.MatchRate(2) == 0 && scoring.MatchRate(3) == scoring.matchPointsPerSphere, "base match rate");
            Expect(scoring.MatchRate(4) == Math.Min(scoring.maximumMatchPointsPerSphere, scoring.matchPointsPerSphere + scoring.streakBonusPerExtraSphere), "streak bonus");
            Expect(scoring.MatchRate(100) == scoring.maximumMatchPointsPerSphere, "match rate cap");

            // Recipe thresholds: relative to the bar count, capped for few bars, self above recipe.
            for (int bars = 1; bars <= 20; bars++)
            {
                Expect(scoring.RecipePercent(bars) <= scoring.maximumRecipePercent && scoring.SelfPercent(bars) <= scoring.maximumSelfPercent, "thresholds above their caps at " + bars);
                Expect(scoring.SelfPercent(bars) >= scoring.RecipePercent(bars), "self threshold below recipe threshold at " + bars);
                Expect(scoring.RecipePercent(bars) * 2 <= 100 + 1e-9, "a distinct pair must stay possible at " + bars);
            }
            Debug.Log("ECOSPHERE_SCORING_CHECKS_PASSED " + checks + ": level score bonuses, par-based stars, streak rates and relative recipe thresholds");
        }
    }
}
