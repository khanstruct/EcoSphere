using System;
using System.Collections.Generic;
using System.Text;

namespace EcoSphere
{
    // Development readout: how far a finished level moved the planet's element mix away from its neutral
    // starting board. Neutral means every starting sphere is worth the same points, divided by its authored
    // contribution weights, so it reuses SphereDefinition.AddContributions instead of restating those rules.
    public static class CompositionReport
    {
        public static double[] NeutralTotals(IEnumerable<int> sphereTypes)
        {
            var neutral=new double[Enum.GetValues(typeof(ElementId)).Length];
            var catalog=SphereCatalog.Current;
            foreach(int type in sphereTypes)catalog.Get(type).AddContributions(neutral,1);
            return neutral;
        }

        // Shares use the same denominator as recipe evaluation: only elements supplied by the loadout.
        public static string Build(string heading,IReadOnlyList<ElementId> elements,double[] baseline,double[] finished)
        {
            double baselineSum=0,finishedSum=0;
            foreach(var element in elements){baselineSum+=baseline[(int)element];finishedSum+=finished[(int)element];}
            if(baselineSum<=0 || finishedSum<=0)return heading+"\nNo elemental points earned.";
            var text=new StringBuilder(heading).Append("\n(starting board -> finished level)");
            ElementId largest=elements[0];int largestShift=0;
            foreach(var element in elements)
            {
                int before=(int)Math.Round(100*baseline[(int)element]/baselineSum);
                int after=(int)Math.Round(100*finished[(int)element]/finishedSum);
                int shift=after-before;
                text.Append('\n').Append(element.ToString()).Append(": ").Append(before).Append("% -> ").Append(after).Append("%  (")
                    .Append(shift>0 ? "+" : "").Append(shift).Append(')');
                if(Math.Abs(shift)>Math.Abs(largestShift)){largest=element;largestShift=shift;}
            }
            if(largestShift==0)text.Append("\nNo shift from the neutral board.");
            else text.Append("\nLargest shift: ").Append(largest.ToString()).Append(' ').Append(largestShift>0 ? "+" : "").Append(largestShift);
            return text.ToString();
        }
    }
}
