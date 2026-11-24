using TMPro;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class ResultsView
    {
        // Development readout only (DEV mode): a small corner label that never affects layout or rewards.
        void ShowDevCompositionReport()
        {
            if(!DevelopmentMode.Enabled || !GameServices.Session.TryGetDevReport(out string report))return;
            var rect=new GameObject("DEV composition report",typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(LayoutCanvas.transform,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
            rect.anchoredPosition=new Vector2(16,16);
            rect.sizeDelta=new Vector2(480,240);
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.text=report;label.fontSize=16;
            label.alignment=TextAlignmentOptions.BottomLeft;
            label.color=new Color(1,.97f,.86f,.9f);label.raycastTarget=false;
        }
    }
}
