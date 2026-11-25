using System;
using System.Collections.Generic;

namespace EcoSphere
{
    public enum ScoreBonusKind { Ricochet }

    // A level's rating score: elemental points (the visualizer totals) plus bonus points that never enter
    // element shares or recipes. Stars, the star strip and the saved highest score all read Total.
    public sealed class LevelScore
    {
        readonly Dictionary<ScoreBonusKind, double> bonuses = new Dictionary<ScoreBonusKind, double>();
        readonly Dictionary<ScoreBonusKind, int> events = new Dictionary<ScoreBonusKind, int>();
        public double Bonus { get; private set; }

        public static double Combine(double elemental, double bonus) => Math.Max(0, elemental) + Math.Max(0, bonus);
        public double Total(double elemental) => Combine(elemental, Bonus);

        public void Add(ScoreBonusKind kind, double points)
        {
            if (points <= 0 || double.IsNaN(points) || double.IsInfinity(points)) return;
            bonuses.TryGetValue(kind, out double previous); bonuses[kind] = previous + points;
            events.TryGetValue(kind, out int count); events[kind] = count + 1;
            Bonus += points;
        }
        public double BonusFrom(ScoreBonusKind kind) => bonuses.TryGetValue(kind, out double points) ? points : 0;
        public int EventsFrom(ScoreBonusKind kind) => events.TryGetValue(kind, out int count) ? count : 0;
        public void Clear() { bonuses.Clear(); events.Clear(); Bonus = 0; }
    }
}
