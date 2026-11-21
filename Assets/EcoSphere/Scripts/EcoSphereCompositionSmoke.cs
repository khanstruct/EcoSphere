using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        bool CompositionTesting => Array.IndexOf(Environment.GetCommandLineArgs(),"-compositionSmoke") >= 0;
        IEnumerator CompositionSmokeRun()
        {
            yield return new WaitForSeconds(.3f);
            void Expect(bool value,string message) { if (!value) throw new Exception("Composition: "+message); }
            Expect(!displayedElements.Contains(ElementId.Stone),"unselected natural element has a channel");
            totals[(int)ElementId.Fire]=12; totals[(int)ElementId.Void]=12; totals[(int)ElementId.Stone]=1000;
            UpdateVisualizer();
            Expect(composition.Total==24 && !composition.IsLeading(ElementId.Stone) && !composition.CanSelfCombine(ElementId.Fire),"natural-only element diluted composition or entered standings");
            displayedElements.Clear(); channelSphereTypes.Clear();
            foreach (int id in new[] { SphereIds.Fire,SphereIds.Void,SphereIds.Sunlight,SphereIds.Stone }) IncludeElements(Definition(id));
            var duplicate = ScriptableObject.CreateInstance<SphereDefinition>(); duplicate.id=SphereIds.Ice;
            duplicate.contributions.Add(new ElementContribution(ElementId.Fire,2)); IncludeElements(duplicate); Destroy(duplicate);
            Array.Clear(totals,0,totals.Length);
            totals[(int)ElementId.Fire]=600; totals[(int)ElementId.Void]=250; totals[(int)ElementId.Sunlight]=100; totals[(int)ElementId.Stone]=50;
            visualizerScale=0;
            for (int i=0;i<shown.Length;i++) shown[i]=(float)totals[i];
            UpdateVisualizer(); yield return null;
            Expect(composition.Total==1000 && energyBars.Count==6,"duplicate delivery channels inflated composition or leader count");
            Expect(energyBars[0].Leading && energyBars[4].Leading && !energyBars[3].Leading,"all channels for a leading element must highlight consistently");
            Expect(energyBars[0].SelfCombination && composition.CanSelfCombine(ElementId.Fire),"majority indicator");
            Expect(energyBars[0].SelfCombination && !energyBars[1].SelfCombination,"tinted background or incorrect self flare");
            Expect(energyBars[0].material==visualizerStyle.energyMaterial && resourceValues.Count==displayedElements.Count,"shared texture or steady value marker missing");
            Vector2 firstPosition=resourceValues[0].anchoredPosition;
            float firstHeight=energyBars[0].rectTransform.sizeDelta.y;
            Expect(Mathf.Abs(energyBars[0].rectTransform.sizeDelta.y/maximumBarHeight-.85f)<.04f,"leader must occupy about 85 percent of available height");
            float oldHeight=energyBars[0].rectTransform.sizeDelta.y;
            int savedGoal=resourceGoal,savedExcess=abundanceGoal;
            resourceGoal=1000000; abundanceGoal=3000000; UpdateVisualizer();
            Expect(!energyBars.Exists(bar=>bar.Leading || bar.SelfCombination),"Beta-level minimum must suppress every flare");
            Expect(Mathf.Abs(energyBars[0].rectTransform.sizeDelta.y-oldHeight)<.1f,"authored thresholds must not shrink the visualizer");
            resourceGoal=savedGoal; abundanceGoal=savedExcess; UpdateVisualizer();
            CaptureComposition("majority");
            yield return new WaitForSeconds(.5f); UpdateVisualizer();
            Expect(resourceValues[0].anchoredPosition==firstPosition && Mathf.Abs(energyBars[0].rectTransform.sizeDelta.y-firstHeight)<4.1f,"pulse moved the true value marker or exceeded its bound");
            CaptureComposition("majority-motion");
            totals[(int)ElementId.Fire]=60; totals[(int)ElementId.Void]=25; totals[(int)ElementId.Sunlight]=10; totals[(int)ElementId.Stone]=5;
            UpdateVisualizer();
            Expect(!energyBars[0].Leading && !energyBars[0].SelfCombination,"majority below minimum must not flare");
            totals[(int)ElementId.Sunlight]=100; totals[(int)ElementId.Fire]=500; totals[(int)ElementId.Void]=300; totals[(int)ElementId.Stone]=100; UpdateVisualizer();
            Expect(!energyBars[0].SelfCombination && !composition.CanSelfCombine(ElementId.Fire),"exact 50 percent retained self marker");
            Expect(!composition.IsLeading(ElementId.Sunlight) && !composition.IsLeading(ElementId.Stone),"minority ingredients must not qualify through their rank");
            CaptureComposition("exact-half");
            totals[(int)ElementId.Stone]=101; UpdateVisualizer();
            Expect(!composition.IsLeading(ElementId.Stone) && !energyBars[2].Leading,"overtaking third must not bypass the 35-percent rule");
            totals[(int)ElementId.Fire]=3000; totals[(int)ElementId.Void]=900; totals[(int)ElementId.Sunlight]=800; totals[(int)ElementId.Stone]=700;
            UpdateVisualizer();
            Expect(!energyBars[2].Leading && !energyBars[3].Leading,"ingredients under 35 percent must not flare even above the minimum");
            Expect(energyBars[4].rectTransform.sizeDelta.y==energyBars[5].rectTransform.sizeDelta.y && barTotals[4].text==barTotals[5].text,"repeated element units must share the same total and height");
            foreach (int groupCount in new[] { 4,5 })
            {
                displayedElements.Clear(); channelSphereTypes.Clear();
                int[] sources={SphereIds.Fire,SphereIds.Void,SphereIds.Sunlight,SphereIds.Ice,SphereIds.Stone};
                for(int group=0;group<groupCount;group++)
                {
                    var sample=ScriptableObject.CreateInstance<SphereDefinition>(); sample.id=sources[group];
                    foreach(var element in new[] { ElementId.Fire,ElementId.Void,ElementId.Water,ElementId.Stone }) sample.contributions.Add(new ElementContribution(element,1));
                    IncludeElements(sample); Destroy(sample);
                }
                UpdateVisualizer(); yield return null;
                for(int i=0;i<shown.Length;i++) shown[i]=(float)totals[i];
                UpdateVisualizer();
                Expect(energyBars.Count==groupCount*4 && sphereLabels.Count==groupCount,"dense loadout bar/group count");
                CaptureComposition("dense-"+(groupCount*4));
                for(int i=0;i<energyBars.Count;i++)
                {
                    RectTransform rect=energyBars[i].rectTransform;
                    Expect(rect.sizeDelta.x<=visualizerStyle.maximumBarWidth && rect.anchoredPosition.x-rect.sizeDelta.x*.5f>=-resourcePanel.sizeDelta.x*.5f+20 && rect.anchoredPosition.x+rect.sizeDelta.x*.5f<=resourcePanel.sizeDelta.x*.5f-20,"dense bar exceeded its panel or width limit");
                    if(i>0) Expect(rect.anchoredPosition.x-rect.sizeDelta.x*.5f>energyBars[i-1].rectTransform.anchoredPosition.x+energyBars[i-1].rectTransform.sizeDelta.x*.5f,"dense bars overlapped");
                }
            }
            var book=Catalog.recipeBook; var original=book.recipes;
            try
            {
                book.recipes=new List<ElementRecipe> { new ElementRecipe { first=ElementId.Fire,second=ElementId.Fire,output=Definition(SphereIds.Ice) } };
                var gains=new double[totals.Length]; gains[(int)ElementId.Fire]=gains[(int)ElementId.Void]=500;
                var equal=WorldProgress.Complete(9,0,-.02f,0,false,gains);
                Expect(!equal.discoveries.Contains(SphereIds.Ice),"50 percent self-recipe unlocked");
                gains[(int)ElementId.Fire]=501; gains[(int)ElementId.Void]=499;
                var majority=WorldProgress.Complete(9,1,-.02f,0,false,gains);
                Expect(majority.discoveries.Contains(SphereIds.Ice),"authored self-recipe did not unlock above 50 percent");
            }
            finally { book.recipes=original; }
            Debug.Log("ECOSPHERE_COMPOSITION_SMOKE_PASSED: live highlights, percentages, duplicate channels, incumbent ties, strict 50-percent marker and actual self-recipe unlock");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        void CaptureComposition(string name)
        {
            foreach (var size in new[] { new Vector2Int(900,1600),new Vector2Int(1600,900) })
            {
                var target=new RenderTexture(size.x,size.y,24); target.Create(); SetPreviewTarget(target); RenderPresentation();
                var old=RenderTexture.active; RenderTexture.active=target;
                var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Composition-"+name+"-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                RenderTexture.active=old; SetPreviewTarget(null); target.Release(); Destroy(target); Destroy(pixels);
            }
        }
    }
}
