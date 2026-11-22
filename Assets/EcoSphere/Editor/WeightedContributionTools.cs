using System;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class WeightedContributionTools
    {
        public static void Validate()
        {
            SphereCatalogTools.Configure();SphereCatalogTools.Validate();
            Check(SphereIds.Fire,1,(ElementId.Fire,3));
            Check(SphereIds.Sunlight,1,(ElementId.Fire,1),(ElementId.Sunlight,2));
            Check(SphereIds.Water,1,(ElementId.Water,3));
            Check(SphereIds.Ice,1,(ElementId.Water,1),(ElementId.Ice,2));
            Check(SphereIds.Stone,1,(ElementId.Stone,3));
            Check(SphereIds.Void,2,(ElementId.Void,2));
            foreach(var pair in new[]{(ElementId.Fire,SphereIds.Fire),(ElementId.Water,SphereIds.Water),(ElementId.Ice,SphereIds.Ice),(ElementId.Sunlight,SphereIds.Sunlight)})
                if(SphereCatalog.Current.ElementVoice(pair.Item1)!=SphereCatalog.Current.Get(pair.Item2).resourceVoice)throw new Exception("Mixed sphere stole an element voice: "+pair.Item1);
            var amounts=new double[Enum.GetValues(typeof(ElementId)).Length];
            SphereCatalog.Current.Get(SphereIds.Fire).AddContributions(amounts,620);
            SphereCatalog.Current.Get(SphereIds.Void).AddContributions(amounts,620);
            if(amounts[(int)ElementId.Fire]!=465 || amounts[(int)ElementId.Void]!=310)throw new Exception("Unexpected Alpha yield");
            Debug.Log("ECOSPHERE_WEIGHTED_CONTRIBUTIONS_PASSED: all six exact mixes; four-unit budgets; 12-point match/50-point breakaway fractional splits; Alpha yields Fire 465 / Void 310 above its 300 minimum");
        }
        static void Check(int id,int nulls,params (ElementId element,int units)[] expected)
        {
            var sphere=SphereCatalog.Current.Get(id);
            if(sphere.unallocatedWeight!=nulls || sphere.ContributionUnits+nulls!=4 || sphere.contributions.Count!=expected.Length)throw new Exception("Wrong authored mix: "+sphere.displayName);
            foreach(var part in expected)
                if(!sphere.contributions.Exists(x=>x.element==part.element && x.amount==part.units))throw new Exception("Missing authored contribution: "+sphere.displayName);
            foreach(int points in new[]{12,50})
            {
                var amounts=new double[Enum.GetValues(typeof(ElementId)).Length];sphere.AddContributions(amounts,points);
                double total=0;
                foreach(var part in expected)
                {
                    if(amounts[(int)part.element]!=points*part.units/4d)throw new Exception("Wrong point split: "+sphere.displayName);
                    total+=amounts[(int)part.element];
                }
                if(total!=points*(4-nulls)/4d)throw new Exception("Null did not consume yield");
            }
        }
    }
}
