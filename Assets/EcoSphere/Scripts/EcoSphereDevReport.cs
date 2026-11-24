using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Development readout only (DEV mode): never alters gameplay state, scoring or rewards.
        double[] startingNeutralTotals;

        void CaptureCompositionBaseline()
        {
            startingNeutralTotals=DevelopmentMode.Enabled ? CompositionReport.NeutralTotals(board.Cells.Values) : null;
        }

        // Call before the running totals are cleared on a loss.
        void ReportComposition(string outcome,bool keepForResults)
        {
            if(smoke || startingNeutralTotals==null || !DevelopmentMode.Enabled)return;
            string report=CompositionReport.Build("DEV composition - "+outcome,displayedElements,startingNeutralTotals,totals);
            Debug.Log(report);
            if(keepForResults)GameServices.Session.SetDevReport(report);
        }
    }
}
