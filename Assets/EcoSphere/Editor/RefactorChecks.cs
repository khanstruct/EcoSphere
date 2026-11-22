using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class RefactorChecks
    {
        static void Expect(bool value,string message){if(!value)throw new Exception("Refactor: "+message);}
        public static void Validate()
        {
            SphereCatalogTools.Validate();
            WeightedContributionTools.Validate();
            PlanetStatChecks.Validate();
            TestLevelTools.Validate();
            WorldCatalogTools.Validate();
            ProceduralLevelTools.Validate();
            var catalog=SphereCatalog.Current;
            foreach(var sphere in catalog.spheres)
                Expect(catalog.ElementAppearance(sphere.namesakeElement)==sphere,"mixed sphere lost namesake art");
            var recipes=ScriptableObject.CreateInstance<ElementRecipeBook>();
            var replacement=ScriptableObject.CreateInstance<SphereCatalog>();
            var edited=UnityEngine.Object.Instantiate(catalog.Get(SphereIds.Fire));
            GameObject globe=null;
            try
            {
                var fire=catalog.Get(SphereIds.Fire);var water=catalog.Get(SphereIds.Water);
                replacement.spheres=new List<SphereDefinition>{fire};
                Expect(replacement.Get(fire.id)==fire,"initial catalog index");
                replacement.spheres=new List<SphereDefinition>{water};
                Expect(replacement.Get(water.id)==water && !replacement.TryGet(fire.id,out _),"same-count catalog replacement");
                replacement.spheres=new List<SphereDefinition>{edited};
                Expect(replacement.ElementAppearance(ElementId.Fire)==edited,"initial namesake index");
                edited.namesakeElement=ElementId.Life;
                typeof(SphereDefinition).GetMethod("OnValidate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(edited,null);
                Expect(replacement.ElementAppearance(ElementId.Life)==edited && !replacement.ElementAppearance(ElementId.Fire),"Inspector identity edit did not invalidate index");
                recipes.recipes.Add(new ElementRecipe{first=ElementId.Fire,second=ElementId.Ice,output=water});
                recipes.Validate();
                Expect(recipes.TryCombine(ElementId.Ice,ElementId.Fire,out var output) && output==water,"unordered recipe lookup");
                recipes.recipes.Add(new ElementRecipe{first=ElementId.Ice,second=ElementId.Fire,output=fire});
                bool rejected=false;try{recipes.Validate();}catch(InvalidOperationException){rejected=true;}
                Expect(rejected,"reversed duplicate recipe accepted");
                recipes.recipes=new List<ElementRecipe>{new ElementRecipe{first=ElementId.Fire,second=ElementId.Fire,output=fire}};
                Expect(recipes.TryCombine(ElementId.Fire,ElementId.Fire,out output) && output==fire,"self recipe after list replacement");

                var totals=new double[ElementComposition.Capacity];
                totals[(int)ElementId.Fire]=30;totals[(int)ElementId.Water]=10;totals[(int)ElementId.Ice]=double.NaN;
                var composition=new ElementComposition(totals);
                Expect(composition.Total==40 && composition.Share(ElementId.Fire)==.75f,"whole-composition denominator");
                globe=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var surface=globe.AddComponent<PlanetSurface>();
                // Initialize only the renderer dependency; edit-mode checks must not spawn runtime atmosphere objects.
                typeof(PlanetSurface).GetField("surface",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(surface,globe.GetComponent<Renderer>());
                var counter=typeof(PlanetSurface).GetProperty("EvaluationCount",BindingFlags.Instance|BindingFlags.NonPublic);
                int Evaluations()=>(int)counter.GetValue(surface);
                surface.ApplyComposition(totals);int initial=Evaluations();
                surface.ApplyComposition((double[])totals.Clone());
                Expect(Evaluations()==initial,"unchanged composition recalculated");
                totals[(int)ElementId.Fire]=60;surface.ApplyComposition(totals);
                Expect(Evaluations()==initial+1,"changed composition missed");
                var saved=surface.Stats.Copy();float temperature=saved.TemperatureFahrenheit;
                PlanetStatCatalog.Current.NotifyChanged();surface.ApplyComposition(totals);
                Expect(Evaluations()==initial+2,"profile invalidation missed");
                totals[(int)ElementId.Fire]=0;surface.ApplyComposition(totals);
                Expect(saved.TemperatureFahrenheit==temperature,"snapshot shares mutable stat data");
                var path=new List<Vector2>{Vector2.one*100};
                BoardRules.TraceAimPath(Vector2.zero,Vector2.up,10,-2,2,3,-3,path);
                Expect(path.Count==3 && path[0]==Vector2.zero && path[1]==new Vector2(0,3) && path[2]==new Vector2(0,-3),"reused aim path or bottom boundary changed");
            }
            finally
            {
                if(globe)UnityEngine.Object.DestroyImmediate(globe);
                UnityEngine.Object.DestroyImmediate(recipes);UnityEngine.Object.DestroyImmediate(replacement);
                UnityEngine.Object.DestroyImmediate(edited);
            }
            Debug.Log("ECOSPHERE_REFACTOR_CHECKS_PASSED: namesakes, unordered/self recipes, duplicate rejection, index replacement, composition cache/invalidation, snapshots, reusable aim path");
        }
    }
}
