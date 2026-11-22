using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        [Header("Sphere loadout (empty uses tutorial defaults)")]
        public int[] startingSphereTypes = Array.Empty<int>();
        int[] activeSphereTypes;
        WorldRecord preparedWorld;
        readonly Dictionary<int,Material> sphereMaterials = new Dictionary<int,Material>();
        readonly List<ElementId> displayedElements = new List<ElementId>();
        readonly List<int> channelSphereTypes = new List<int>();
        readonly List<EnergyBarGraphic> energyBars = new List<EnergyBarGraphic>();
        readonly List<TMP_Text> resourceLabels = new List<TMP_Text>(), barTotals = new List<TMP_Text>(), sphereLabels = new List<TMP_Text>();
        readonly List<RectTransform> resourceValues = new List<RectTransform>();
        readonly double[] selectedElementTotals = new double[Enum.GetValues(typeof(ElementId)).Length];
        readonly ElementStandings composition = new ElementStandings();
        float barBaseline, maximumBarHeight, visualizerScale;
        VisualizerStyle visualizerStyle;
        SphereCatalog Catalog => SphereCatalog.Current;
        SphereDefinition Definition(int type) => Catalog.Get(type);
        Sprite coreHalo => Definition(SphereIds.Core).icon;
        Color ColorFor(int type) => Definition(type).color;

        void InitializeSphereData()
        {
            Catalog.Validate();
            var selected = LevelPrepView.TakeLoadout();
            if(ProceduralLayout && selected==null)
            {
                var system=WorldProgress.GetSystem(LevelPrepView.SystemNumber);
                int index=LevelPrepView.PlanetIndex;
                if(index<0 || index>=system.worlds.Count || !system.worlds[index].IsProcedural)
                    index=system.worlds.FindIndex(world=>world.IsProcedural);
                if(index<0)throw new InvalidOperationException("This system has no procedural world");
                LevelPrepView.PlanetIndex=index; preparedWorld=system.worlds[index];
            }
            if (selected == null && !ProceduralLayout)
            {
                // Direct editor launches of a guaranteed world still use its identity and authored rules.
                string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                foreach (var definition in WorldCatalog.Current.predefinedWorlds)
                    if (definition.guaranteedSystem > 0 && definition.gameplayScene == scene)
                    {
                        LevelPrepView.SystemNumber = definition.guaranteedSystem; LevelPrepView.PlanetIndex = definition.guaranteedSlot;
                        preparedWorld = WorldLevels.Get(definition.guaranteedSystem, definition.guaranteedSlot);
                        break;
                    }
            }
            if (selected != null)
                preparedWorld = WorldLevels.Get(LevelPrepView.SystemNumber, LevelPrepView.PlanetIndex);
            if (preparedWorld != null)
            {
                resourceGoal = preparedWorld.rules.inclusionGoal; abundanceGoal = preparedWorld.rules.abundanceGoal;
                rotationDegrees = preparedWorld.rules.rotationDegrees; rotationDuration = preparedWorld.rules.rotationDuration;
            }
            int required=ProceduralLayout ? preparedWorld.RequiredSpheres : StoneTutorial ? 3 : 2;
            int[] defaultLoadout=null;
            if(ProceduralLayout && selected==null && (startingSphereTypes==null || startingSphereTypes.Length==0))
            {
                var available=new List<int>();
                foreach(var sphere in Catalog.spheres)
                    if(sphere.AmmoEligible && WorldProgress.IsSphereAvailable(sphere.id) && available.Count<required)available.Add(sphere.id);
                defaultLoadout=available.ToArray();
            }
            activeSphereTypes = selected ??
                (startingSphereTypes != null && startingSphereTypes.Length > 0 ? startingSphereTypes :
                    ProceduralLayout ? defaultLoadout : StoneTutorial ? new[] { SphereIds.Fire, SphereIds.Void, SphereIds.Sunlight } : Catalog.openingSpheres);
            LevelPrepView.RequiredSpheres=required;
            if (activeSphereTypes.Length != required || new HashSet<int>(activeSphereTypes).Count != required)
                throw new InvalidOperationException("This level layout needs " + required + " distinct sphere types");
            foreach (int type in activeSphereTypes)
            {
                if (!Definition(type).AmmoEligible) throw new InvalidOperationException("Invalid ammo sphere: " + type);
                IncludeElements(Definition(type));
            }
        }


        void IncludeElements(SphereDefinition definition)
        {
            if (channelSphereTypes.Contains(definition.id)) return;
            foreach (var entry in definition.contributions)
                for (int unit=0;unit<entry.amount;unit++)
                { displayedElements.Add(entry.element); channelSphereTypes.Add(definition.id); }
        }
        void UpdateComposition()
        {
            Array.Clear(selectedElementTotals,0,selectedElementTotals.Length);
            foreach(var element in displayedElements) selectedElementTotals[(int)element]=totals[(int)element];
            composition.Update(selectedElementTotals,displayedElements);
            if (planet) planet.GetComponent<PlanetSurface>().ApplyComposition(selectedElementTotals);
        }
        void BuildResourceVisualizer(RectTransform strip)
        {
            visualizerStyle=Resources.Load<VisualizerStyle>("VisualizerStyle");
            if (!visualizerStyle || !visualizerStyle.energyMaterial || !visualizerStyle.frame)
                throw new InvalidOperationException("VisualizerStyle needs its energy material and shared frame");
            energyBars.Clear(); resourceLabels.Clear(); barTotals.Clear(); sphereLabels.Clear(); resourceValues.Clear();
            var frame=Panel("Shared visualizer frame",strip,Vector2.one*.5f,Vector2.zero,Vector2.zero,Color.white);
            frame.anchorMin=Vector2.zero; frame.anchorMax=Vector2.one; frame.offsetMin=frame.offsetMax=Vector2.zero;
            var image=frame.GetComponent<Image>(); image.sprite=visualizerStyle.frame; image.type=Image.Type.Sliced; image.fillCenter=false; image.raycastTarget=false;
            for (int i=0;i<displayedElements.Count;i++)
            {
                var bar=new GameObject("Element contribution unit",typeof(RectTransform),typeof(CanvasRenderer),typeof(EnergyBarGraphic)).GetComponent<EnergyBarGraphic>();
                bar.transform.SetParent(strip,false); bar.rectTransform.anchorMin=bar.rectTransform.anchorMax=new Vector2(.5f,0);
                bar.rectTransform.pivot=new Vector2(.5f,0); bar.material=visualizerStyle.energyMaterial;
                bar.Phase=i*.13f; bar.color=ColorFor(channelSphereTypes[i]); energyBars.Add(bar);
                var label=Label("Element name",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(90,24),"",16);
                label.enableAutoSizing=true; label.fontSizeMin=6; label.fontSizeMax=16; resourceLabels.Add(label);
                var total=Label("Element points",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(90,24),"0",20);
                total.enableAutoSizing=true; total.fontSizeMin=6; total.fontSizeMax=22; barTotals.Add(total);
                var marker=Panel("True element value",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(50,1),new Color(1,1,1,.45f));
                marker.GetComponent<Image>().raycastTarget=false; resourceValues.Add(marker);
                if (i==0 || channelSphereTypes[i]!=channelSphereTypes[i-1])
                {
                    var source=Label("Source sphere",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(150,24),Definition(channelSphereTypes[i]).displayName,18);
                    source.enableAutoSizing=true; source.fontSizeMin=6; source.fontSizeMax=20; source.textWrappingMode=TextWrappingModes.NoWrap; sphereLabels.Add(source);
                }
            }
            LayoutResourceChannels();
        }
        void LayoutResourceChannels()
        {
            int count=energyBars.Count; if(count==0)return;
            float available=Mathf.Max(1,resourcePanel.sizeDelta.x-64);
            float minimumGap=Mathf.Min(visualizerStyle.minimumGap,available/(count*3));
            float groupGap=Mathf.Min(visualizerStyle.sphereGroupGap,minimumGap);
            int groups=sphereLabels.Count;
            float width=Mathf.Min(visualizerStyle.maximumBarWidth,
                (available-Mathf.Max(0,count-1)*minimumGap-Mathf.Max(0,groups-1)*groupGap)/count);
            width=Mathf.Max(1,width);
            float gap=count>1 ? minimumGap : 0;
            groupGap=groups>1 ? (available-width*count-Mathf.Max(0,count-1)*gap)/(groups-1) : 0;
            float used=width*count+gap*Mathf.Max(0,count-1)+groupGap*Mathf.Max(0,groups-1);
            float x=-used*.5f+width*.5f;
            barBaseline=62; maximumBarHeight=Mathf.Max(24,resourcePanel.sizeDelta.y-barBaseline-38);
            int start=0,group=0;
            for(int i=0;i<count;i++)
            {
                if(i>0 && channelSphereTypes[i]!=channelSphereTypes[i-1]) x+=groupGap;
                var rect=energyBars[i].rectTransform;
                rect.anchoredPosition=new Vector2(x,barBaseline); rect.sizeDelta=new Vector2(width,rect.sizeDelta.y);
                resourceLabels[i].rectTransform.anchoredPosition=new Vector2(x,barBaseline-15);
                resourceLabels[i].rectTransform.sizeDelta=new Vector2(width+gap*.5f,24);
                resourceValues[i].sizeDelta=new Vector2(width,1);
                barTotals[i].rectTransform.sizeDelta=new Vector2(width+gap*.5f,24);
                if(i==count-1 || channelSphereTypes[i]!=channelSphereTypes[i+1])
                {
                    float left=energyBars[start].rectTransform.anchoredPosition.x-width*.5f;
                    var label=sphereLabels[group++].rectTransform;
                    label.anchoredPosition=new Vector2((left+x+width*.5f)*.5f,24);
                    label.sizeDelta=new Vector2(x+width*.5f-left,24); start=i+1;
                }
                x+=width+gap;
            }
        }
        void UpdateVisualizer()
        {
            if(energyBars.Count!=displayedElements.Count)
            {
                foreach(Transform child in resourcePanel) Destroy(child.gameObject);
                BuildResourceVisualizer(resourcePanel); previousPresentation=Vector2.zero;
            }
            UpdateComposition();
            if(TemperatureLabel && planet)
            {
                float fahrenheit=planet.GetComponent<PlanetSurface>().DisplayedTemperatureFahrenheit;
                int rounded=Mathf.RoundToInt(fahrenheit);
                if(rounded!=displayedTemperature){displayedTemperature=rounded;TemperatureLabel.text=PlanetTemperatureDisplay.Average(fahrenheit);}
            }
            float largest=0;
            foreach(var element in displayedElements) largest=Mathf.Max(largest,(float)totals[(int)element]);
            float targetScale=largest/Mathf.Clamp(visualizerStyle.leaderHeightFraction,.5f,.95f);
            if(largest<=0) visualizerScale=0;
            else visualizerScale=visualizerScale<=0 ? targetScale : Mathf.Lerp(visualizerScale,targetScale,1-Mathf.Exp(-Time.deltaTime*7));
            float scale=Mathf.Max(.0001f,visualizerScale);
            for(int element=0;element<shown.Length;element++)
                shown[element]=Mathf.Lerp(shown[element],(float)totals[element],1-Mathf.Exp(-Time.deltaTime*7));
            for(int i=0;i<energyBars.Count;i++)
            {
                var element=displayedElements[i]; var bar=energyBars[i]; var rect=bar.rectTransform;
                float height=Mathf.Clamp(shown[(int)element]/scale*maximumBarHeight,2,maximumBarHeight);
                // Repeated units share their element's height and pulse, as well as its actual total.
                float pulse=totals[(int)element]>0 ? Mathf.Sin(Time.time*2.2f+(int)element*.83f)*Mathf.Min(2,height*visualizerStyle.pulseFraction) : 0;
                rect.sizeDelta=new Vector2(rect.sizeDelta.x,Mathf.Clamp(height+pulse,2,maximumBarHeight));
                bar.Leading=composition.MeetsRecipeMinimum(element,resourceGoal);
                bar.SelfCombination=bar.Leading && composition.CanSelfCombine(element);
                string name=Catalog.ElementName(element);
                resourceLabels[i].text=rect.sizeDelta.x<48 ? name.Substring(0,Mathf.Min(rect.sizeDelta.x<22 ? 1 : 3,name.Length)).ToUpperInvariant() : name;
                barTotals[i].text=Math.Truncate(totals[(int)element]).ToString("0");
                barTotals[i].color=bar.SelfCombination ? new Color(1,.8f,.3f) : Color.white;
                barTotals[i].rectTransform.anchoredPosition=new Vector2(rect.anchoredPosition.x,barBaseline+rect.sizeDelta.y+17);
                resourceValues[i].anchoredPosition=new Vector2(rect.anchoredPosition.x,barBaseline+Mathf.Clamp((float)totals[(int)element]/scale*maximumBarHeight,2,maximumBarHeight));
            }
            detail.text=$"Score {score} · Matched {lastMatch} · Released {lastRelease}";

        }
    }
}
