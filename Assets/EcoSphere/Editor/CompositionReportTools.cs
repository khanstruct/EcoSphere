using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class CompositionReportTools
    {
        [MenuItem("EcoSphere/Validate Composition Report")]
        public static void Validate()
        {
            int checks=0;
            void Expect(bool value,string message){if(!value)throw new Exception("Composition report: "+message);checks++;}
            var elements=new List<ElementId>{ElementId.Fire,ElementId.Void};
            var size=Enum.GetValues(typeof(ElementId)).Length;
            var neutral=CompositionReport.NeutralTotals(new[]{SphereIds.Fire,SphereIds.Void});
            Expect(neutral.Length==size && neutral[(int)ElementId.Fire]>0 && neutral[(int)ElementId.Void]>0,"neutral totals include the board's elements");
            var same=(double[])neutral.Clone();
            Expect(CompositionReport.Build("t",elements,neutral,same).Contains("No shift"),"identical totals report no shift");
            var tilted=(double[])neutral.Clone();tilted[(int)ElementId.Fire]*=3;
            var before=(double[])neutral.Clone();
            var text=CompositionReport.Build("t",elements,neutral,tilted);
            Expect(text.Contains("Largest shift: Fire +") || text.Contains("Largest shift: Void -"),"tilting toward Fire raises Fire's share");
            for(int i=0;i<neutral.Length;i++)Expect(neutral[i]==before[i],"the report must not modify its inputs");
            Expect(CompositionReport.Build("t",elements,neutral,new double[size]).Contains("No elemental points"),"zero totals are handled without division errors");
            Debug.Log("ECOSPHERE_COMPOSITION_REPORT_PASSED: "+checks+" checks");
        }
    }
}
