using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class ElementStandingsTools
    {
        [MenuItem("EcoSphere/Validate Element Standings")]
        public static void Validate()
        {
            int checks = 0;
            void Expect(bool value,string message) { if (!value) throw new Exception("Element standings: "+message); checks++; }
            var totals = new int[Enum.GetValues(typeof(ElementId)).Length];
            var standings = new ElementStandings();
            var order = new[] { ElementId.Fire,ElementId.Void,ElementId.Water,ElementId.Stone,ElementId.Fire };
            standings.Update(totals,order);
            Expect(standings.Total == 0 && standings.Ranked.Count == 0 && !standings.CanSelfCombine(ElementId.Fire),"zero resources must not produce leaders or self-pairs");
            totals[(int)ElementId.Fire]=60; totals[(int)ElementId.Void]=25; totals[(int)ElementId.Water]=15;
            standings.Update(totals,order);
            Expect(standings.Total == 100 && standings.Rank(ElementId.Fire)==1 && standings.Rank(ElementId.Water)==3,"unique element totals/ranks");
            Expect(standings.CanSelfCombine(ElementId.Fire) && standings.Percentage(ElementId.Fire)==60,"strict majority");
            totals[(int)ElementId.Stone]=15; standings.Update(totals,order);
            Expect(standings.Rank(ElementId.Water)==3 && standings.Rank(ElementId.Stone)==4,"incumbent third must retain a tie");
            totals[(int)ElementId.Stone]=16; standings.Update(totals,order);
            Expect(standings.Rank(ElementId.Stone)==3 && standings.Rank(ElementId.Water)==4,"strictly larger challenger must take third");
            totals[(int)ElementId.Water]=16; standings.Update(totals,order);
            Expect(standings.Rank(ElementId.Stone)==3,"new incumbent must retain a later tie");
            totals[(int)ElementId.Water]=17; standings.Update(totals,order);
            Expect(standings.Rank(ElementId.Water)==3,"overtaken incumbent must yield");
            Array.Clear(totals,0,totals.Length); totals[(int)ElementId.Fire]=500; totals[(int)ElementId.Void]=500;
            standings.Update(totals,order);
            Expect(!standings.CanSelfCombine(ElementId.Fire),"exactly 50 percent is not a self-pair");
            totals[(int)ElementId.Fire]=501; totals[(int)ElementId.Void]=499; standings.Update(totals,order);
            Expect(standings.CanSelfCombine(ElementId.Fire),"just over 50 percent must permit a self-pair");
            totals[(int)ElementId.Fire]=totals[(int)ElementId.Void]=int.MaxValue; standings.Update(totals,order);
            Expect(standings.Total==2L*int.MaxValue && !standings.CanSelfCombine(ElementId.Fire),"majority math must not overflow");
            Array.Clear(totals,0,totals.Length);
            totals[(int)ElementId.Fire]=3000; totals[(int)ElementId.Void]=900; totals[(int)ElementId.Water]=800; totals[(int)ElementId.Stone]=700;
            standings.Update(totals,order);
            var possible = new[] { ElementId.Fire,ElementId.Void,ElementId.Water,ElementId.Stone }; int eligible=0;
            for (int i=0;i<possible.Length;i++) for (int j=i;j<possible.Length;j++)
                if (ElementRecipeBook.Qualifies(new ElementRecipe { first=possible[i],second=possible[j] },standings,500)) eligible++;
            Expect(eligible==1,"only the majority self-pair qualifies in a diluted composition");
            Expect(!ElementRecipeBook.Qualifies(new ElementRecipe { first=ElementId.Fire,second=ElementId.Stone },standings,500),"fourth element must not qualify despite reaching the minimum");
            Expect(!ElementRecipeBook.Qualifies(new ElementRecipe { first=ElementId.Fire,second=ElementId.Fire },standings,4000),"existing minimum still applies to self-pairs");
            var fractional = new double[totals.Length];
            fractional[(int)ElementId.Fire]=500.25; fractional[(int)ElementId.Void]=499.75;
            standings.Update(fractional,order);
            Expect(standings.CanSelfCombine(ElementId.Fire),"fractional majority must not be truncated before ranking");
            Expect(!ElementRecipeBook.Qualifies(new ElementRecipe { first=ElementId.Fire,second=ElementId.Void },standings,500),"499.75 must not round up to the inclusion minimum");
            Array.Clear(fractional,0,fractional.Length);
            fractional[(int)ElementId.Fire]=350; fractional[(int)ElementId.Void]=350; fractional[(int)ElementId.Water]=300;
            standings.Update(fractional,order);
            Expect(standings.IsLeading(ElementId.Fire) && standings.IsLeading(ElementId.Void) && !standings.IsLeading(ElementId.Water),"exactly 35 percent qualifies; 30 percent does not");
            Expect(ElementRecipeBook.Qualifies(new ElementRecipe { first=ElementId.Fire,second=ElementId.Void },standings,350),"two 35-percent ingredients at the minimum must qualify");
            Expect(!ElementRecipeBook.Qualifies(new ElementRecipe { first=ElementId.Fire,second=ElementId.Void },standings,351),"minimum still applies to a 35-percent leader");
            fractional[(int)ElementId.Fire]=349.999; fractional[(int)ElementId.Water]=300.001; standings.Update(fractional,order);
            Expect(!standings.IsLeading(ElementId.Fire),"just below 35 percent must not round up");
            fractional[(int)ElementId.Fire]=fractional[(int)ElementId.Void]=fractional[(int)ElementId.Water]=1;
            standings.Update(fractional,order);
            Expect(!standings.IsLeading(ElementId.Fire) && !standings.IsLeading(ElementId.Void) && !standings.IsLeading(ElementId.Water),"a three-way equal split has no leaders");
            SphereCatalog.Current.Validate();
            Debug.Log("ECOSPHERE_ELEMENT_STANDINGS_CHECKS_PASSED "+checks+": distinct totals, stable incumbent ties, strict majority, overflow safety and shared recipe eligibility");
        }
        public static void ConfigureAndValidate() { SphereCatalogTools.Configure(); Validate(); }
        public static void Smoke()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(FirstTutorialTools.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
