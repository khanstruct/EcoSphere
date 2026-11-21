using System;
using System.Collections.Generic;

namespace EcoSphere
{
    // One planet-wide ranking, independent of the spheres delivering each element.
    public sealed class ElementStandings
    {
        public const double LeadershipPercent = 35;
        readonly List<ElementId> ranked = new List<ElementId>();
        readonly Comparison<ElementId> comparison;
        IReadOnlyList<double> amounts;
        int[] previousPriority = Array.Empty<int>();
        public double Total { get; private set; }
        public IReadOnlyList<ElementId> Ranked => ranked;

        public ElementStandings() { comparison = Compare; }

        public void Update(IReadOnlyList<double> totals, IReadOnlyList<ElementId> firstAppearanceOrder = null)
        {
            amounts = totals; Total = 0;
            if (previousPriority.Length != totals.Count) previousPriority = new int[totals.Count];
            for (int i = 0; i < totals.Count; i++) { Total += Math.Max(0,totals[i]); previousPriority[i] = int.MaxValue; }
            for (int i = ranked.Count-1; i >= 0; i--) if (Amount(ranked[i]) == 0) ranked.RemoveAt(i);
            if (firstAppearanceOrder != null)
                foreach (var element in firstAppearanceOrder) if (Amount(element) > 0 && !ranked.Contains(element)) ranked.Add(element);
            for (int i = 0; i < totals.Count; i++) if (totals[i] > 0 && !ranked.Contains((ElementId)i)) ranked.Add((ElementId)i);
            for (int i = 0; i < ranked.Count; i++) previousPriority[(int)ranked[i]] = i;
            ranked.Sort(comparison);
        }

        public void Update(IReadOnlyList<int> totals, IReadOnlyList<ElementId> order = null)
        {
            var precise = new double[totals.Count];
            for (int i=0;i<precise.Length;i++) precise[i]=totals[i];
            Update(precise,order);
        }

        int Compare(ElementId first, ElementId second)
        {
            int amount = Amount(second).CompareTo(Amount(first));
            // Incumbents retain their place until another element has a strictly larger amount.
            return amount != 0 ? amount : previousPriority[(int)first].CompareTo(previousPriority[(int)second]);
        }
        public double Amount(ElementId element) => amounts != null && (int)element >= 0 && (int)element < amounts.Count ? Math.Max(0,amounts[(int)element]) : 0;
        public int Rank(ElementId element) { int index = ranked.IndexOf(element); return index < 0 ? 0 : index+1; }
        public bool IsLeading(ElementId element) => Total > 0 && Amount(element)*100 >= Total*LeadershipPercent;
        public bool MeetsRecipeMinimum(ElementId element,int minimum) => IsLeading(element) && Amount(element)>=Math.Max(1,minimum);
        public float Percentage(ElementId element) => Total > 0 ? (float)(Amount(element)*100.0/Total) : 0;
        public bool CanSelfCombine(ElementId element) => Total > 0 && Amount(element)*2L > Total;
    }
}
