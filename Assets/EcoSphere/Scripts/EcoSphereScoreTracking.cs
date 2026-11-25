using System.Collections.Generic;
using System.Text;

namespace EcoSphere
{
    // Level score bookkeeping: bonus points (kept out of element shares) and the counts behind the DEV breakdown.
    public sealed partial class EcoSpherePrototype
    {
        readonly LevelScore levelScore = new LevelScore();
        readonly SortedDictionary<int, int> matchGroupSizes = new SortedDictionary<int, int>();
        int breakawaySpheres;

        // The score stars are earned from: elemental points plus bonuses. Completion saves the same value.
        public double RatingScore => levelScore.Total(composition.Total);

        void RecordResolution(int matched, int released, int ricochets)
        {
            if (matched >= 3)
            {
                matchGroupSizes.TryGetValue(matched, out int count); matchGroupSizes[matched] = count + 1;
                // Bank shots that land a match earn a small bonus per ricochet.
                if (ricochets > 0) levelScore.Add(ScoreBonusKind.Ricochet, ricochets * ElementScoring.RicochetBonus);
            }
            breakawaySpheres += released;
        }

        void ResetScoreTracking() { levelScore.Clear(); matchGroupSizes.Clear(); breakawaySpheres = 0; }

        string ScoreBreakdown()
        {
            var rules = preparedWorld.rules;
            var sizes = new StringBuilder();
            foreach (var entry in matchGroupSizes) sizes.Append(sizes.Length > 0 ? ", " : "").Append(entry.Key).Append("x").Append(entry.Value);
            return "Score " + RatingScore.ToString("N0") + " = elements " + composition.Total.ToString("N0") + " + ricochet bonus " +
                levelScore.BonusFrom(ScoreBonusKind.Ricochet).ToString("N0") + " (" + levelScore.EventsFrom(ScoreBonusKind.Ricochet) + " bank matches)\n" +
                "Stars " + rules.StarsForScore(RatingScore) + " at " + rules.StarScore(1).ToString("N0") + " / " +
                rules.StarScore(2).ToString("N0") + " / " + rules.StarScore(3).ToString("N0") + " (par " + rules.Par.ToString("N0") + ")\n" +
                "Shots " + shots + ", matches " + (sizes.Length > 0 ? sizes.ToString() : "none") + ", breakaway spheres " + breakawaySpheres + "\n" +
                "Recipe " + composition.RecipePercent.ToString("0.#") + "% / self >" + composition.SelfPercent.ToString("0.#") + "% at " + displayedElements.Count + " bars";
        }
    }
}
