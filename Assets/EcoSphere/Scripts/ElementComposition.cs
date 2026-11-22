using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // A non-allocating view of one planet's whole-element composition. Calculate the denominator once.
    public readonly struct ElementComposition
    {
        public static readonly int Capacity=Enum.GetValues(typeof(ElementId)).Length;
        readonly IReadOnlyList<double> amounts;
        public double Total {get;}
        public ElementComposition(IReadOnlyList<double> amounts)
        {
            this.amounts=amounts;double total=0;
            if(amounts!=null)for(int i=0;i<amounts.Count;i++)total+=PositiveFinite(amounts[i]);
            Total=total;
        }
        public static double PositiveFinite(double value)=>value>0 && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0;
        public double Amount(ElementId element)
        {
            int index=(int)element;return amounts!=null && index>=0 && index<amounts.Count ? PositiveFinite(amounts[index]) : 0;
        }
        public float Share(ElementId element)=>Total>0 ? (float)(Amount(element)/Total) : 0;
        public float BoostedShare(ElementId element,float bonus)
        {
            float share=Share(element);
            return share+bonus*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,share));
        }
        public float Influence(ElementId element,float fullInfluence=250,float bonus=.25f)=>
            Mathf.Clamp01(BoostedShare(element,bonus)*Mathf.Clamp01((float)(Amount(element)/Math.Max(1,fullInfluence))));
    }
}
