using System;
using System.Collections.Generic;

namespace EcoSphere
{
    // A limited contribution line came up short: `element` from `sphere` was capped by too little `limitedBy`.
    public readonly struct ElementShortage
    {
        public readonly SphereDefinition sphere; public readonly ElementId element, limitedBy; public readonly double lost;
        public ElementShortage(SphereDefinition sphere, ElementId element, ElementId limitedBy, double lost)
        { this.sphere = sphere; this.element = element; this.limitedBy = limitedBy; this.lost = lost; }
    }

    // The one gameplay path that turns earned points into planet element totals, applying contribution limits.
    // Each limited line is capped per sphere type: the total that sphere type has added of the element
    // may not exceed the planet's total of the limiting element (times the line's ratio). Excess is lost.
    // Inside a Batch, unlimited yield lands first and limited lines are resolved when the batch ends, so
    // elements gained in the same resolution raise caps before they are checked, whatever the removal order.
    public sealed class ElementLedger
    {
        public readonly double[] Totals;
        readonly Dictionary<(int sphere, ElementId element), double> limitedYield = new Dictionary<(int, ElementId), double>();
        readonly List<(SphereDefinition sphere, double points)> pending = new List<(SphereDefinition, double)>();
        int depth;
        public event Action<ElementShortage> ShortageOccurred;

        public ElementLedger(double[] totals) { Totals = totals ?? throw new ArgumentNullException(nameof(totals)); }

        public void Clear() { Array.Clear(Totals, 0, Totals.Length); limitedYield.Clear(); pending.Clear(); }

        // Total of `element` the given sphere type has added through limited lines (what its cap is measured against).
        public double LimitedYield(int sphereId, ElementId element) => limitedYield.TryGetValue((sphereId, element), out var value) ? value : 0;

        public void Add(SphereDefinition sphere, double points)
        {
            if (!sphere || !sphere.Contributes) return;
            bool hasLimited = false;
            foreach (var entry in sphere.contributions)
                if (entry.limited) hasLimited = true;
                else Totals[(int)entry.element] += sphere.ContributionYield(entry, points);
            if (!hasLimited) return;
            pending.Add((sphere, points));
            if (depth == 0) Resolve();
        }

        public Scope Batch() { depth++; return new Scope(this); }
        public readonly struct Scope : IDisposable
        {
            readonly ElementLedger ledger;
            internal Scope(ElementLedger ledger) { this.ledger = ledger; }
            public void Dispose() { if (ledger != null && --ledger.depth == 0) ledger.Resolve(); }
        }

        void Resolve()
        {
            if (pending.Count == 0) return;
            var lost = new Dictionary<(int sphere, ElementId element), ElementShortage>();
            var order = new List<(int, ElementId)>();
            foreach (var (sphere, points) in pending)
                foreach (var entry in sphere.contributions)
                {
                    if (!entry.limited) continue;
                    var key = (sphere.id, entry.element);
                    double gain = sphere.ContributionYield(entry, points), used = LimitedYield(sphere.id, entry.element);
                    double allowed = Math.Min(gain, Math.Max(0, entry.Cap(Totals) - used));
                    Totals[(int)entry.element] += allowed; limitedYield[key] = used + allowed;
                    if (gain - allowed <= 1e-9) continue;
                    double previous = lost.TryGetValue(key, out var shortage) ? shortage.lost : 0;
                    if (!lost.ContainsKey(key)) order.Add(key);
                    lost[key] = new ElementShortage(sphere, entry.element, entry.limitedBy, previous + gain - allowed);
                }
            pending.Clear();
            foreach (var key in order) ShortageOccurred?.Invoke(lost[key]);
        }
    }
}
