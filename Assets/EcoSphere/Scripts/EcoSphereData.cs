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
        readonly List<EnergyBarGraphic> energyBars = new List<EnergyBarGraphic>();
        readonly List<TMP_Text> resourceLabels = new List<TMP_Text>(), barTotals = new List<TMP_Text>();
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
            GameServices.Session.Phase=GamePhase.Playing;
            Catalog.Validate();
            var selected = GameServices.Session.TakeLoadout();
            if(ProceduralLayout && selected==null)
            {
                var system=GameServices.Progress.GetSystem(GameServices.Session.SystemNumber);
                int index=GameServices.Session.PlanetIndex;
                if(index<0 || index>=system.worlds.Count || !system.worlds[index].IsProcedural)
                    index=system.worlds.FindIndex(world=>world.IsProcedural);
                if(index<0)throw new InvalidOperationException("This system has no procedural world");
                GameServices.Session.PlanetIndex=index; preparedWorld=system.worlds[index];
            }
            if (selected == null && !ProceduralLayout)
            {
                // Direct editor launches of a guaranteed world still use its identity and authored rules.
                string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                foreach (var definition in WorldCatalog.Current.predefinedWorlds)
                    if (definition.guaranteedSystem > 0 && definition.gameplayScene == scene)
                    {
                        GameServices.Session.SystemNumber = definition.guaranteedSystem; GameServices.Session.PlanetIndex = definition.guaranteedSlot;
                        preparedWorld = WorldLevels.Get(definition.guaranteedSystem, definition.guaranteedSlot);
                        break;
                    }
            }
            if (selected != null)
                preparedWorld = WorldLevels.Get(GameServices.Session.SystemNumber, GameServices.Session.PlanetIndex);
            if (preparedWorld != null)
            {
                abundanceGoal = preparedWorld.rules.abundanceGoal;
                rotationDegrees = preparedWorld.rules.rotationDegrees; rotationDuration = preparedWorld.rules.rotationDuration;
            }
            if(preparedWorld==null)throw new InvalidOperationException("Gameplay must be initialized from a world record");
            int required=preparedWorld.RequiredSpheres;
            int[] defaultLoadout=null;
            if(ProceduralLayout && selected==null && (startingSphereTypes==null || startingSphereTypes.Length==0))
            {
                var available=new List<int>();
                foreach(var sphere in Catalog.spheres)
                    if(sphere.AmmoEligible && GameServices.Progress.IsSphereAvailable(sphere.id) && available.Count<required)available.Add(sphere.id);
                defaultLoadout=available.ToArray();
            }
            activeSphereTypes = selected ??
                (startingSphereTypes != null && startingSphereTypes.Length > 0 ? startingSphereTypes :
                    ProceduralLayout ? defaultLoadout : StoneTutorial ? new[] { SphereIds.Fire, SphereIds.Void, SphereIds.Sunlight } : Catalog.openingSpheres);
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
            foreach (var entry in definition.contributions)
                if(entry.amount>0 && !displayedElements.Contains(entry.element)) displayedElements.Add(entry.element);
        }
        void UpdateComposition()
        {
            Array.Clear(selectedElementTotals,0,selectedElementTotals.Length);
            foreach(var element in displayedElements) selectedElementTotals[(int)element]=totals[(int)element];
            composition.Update(selectedElementTotals,displayedElements,displayedElements.Count);
            if (planet) planet.GetComponent<PlanetSurface>().ApplyComposition(selectedElementTotals);
        }
        void BuildResourceVisualizer(RectTransform strip)
        {
            visualizerStyle=Resources.Load<VisualizerStyle>("VisualizerStyle");
            if (!visualizerStyle || !visualizerStyle.energyMaterial || !visualizerStyle.frame)
                throw new InvalidOperationException("VisualizerStyle needs its energy material and shared frame");
            energyBars.Clear(); resourceLabels.Clear(); barTotals.Clear(); resourceValues.Clear();
            var frame=Panel("Shared visualizer frame",strip,Vector2.one*.5f,Vector2.zero,Vector2.zero,Color.white);
            frame.anchorMin=Vector2.zero; frame.anchorMax=Vector2.one; frame.offsetMin=frame.offsetMax=Vector2.zero;
            var image=frame.GetComponent<Image>(); image.sprite=visualizerStyle.frame; image.type=Image.Type.Sliced; image.fillCenter=false; image.raycastTarget=false;
            BuildThresholdGuides(strip);
            for (int i=0;i<displayedElements.Count;i++)
            {
                var bar=new GameObject("Element bar",typeof(RectTransform),typeof(CanvasRenderer),typeof(EnergyBarGraphic)).GetComponent<EnergyBarGraphic>();
                bar.transform.SetParent(strip,false); bar.rectTransform.anchorMin=bar.rectTransform.anchorMax=new Vector2(.5f,0);
                bar.rectTransform.pivot=new Vector2(.5f,0); bar.material=visualizerStyle.energyMaterial;
                bar.Phase=i*.13f; bar.color=Catalog.ElementAppearance(displayedElements[i])?.color ?? Color.white; energyBars.Add(bar);
                var label=Label("Element name",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(90,24),"",16);
                label.enableAutoSizing=true; label.fontSizeMin=6; label.fontSizeMax=16; resourceLabels.Add(label);
                var total=Label("Element points",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(90,24),"0",20);
                total.enableAutoSizing=true; total.fontSizeMin=6; total.fontSizeMax=22; barTotals.Add(total);
                var marker=Panel("True element value",strip,new Vector2(.5f,0),Vector2.zero,new Vector2(50,1),new Color(1,1,1,.45f));
                marker.GetComponent<Image>().raycastTarget=false; resourceValues.Add(marker);
            }
            BuildScoreStrip(strip);
            LayoutResourceChannels();
        }
        void LayoutResourceChannels()
        {
            int count=energyBars.Count; if(count==0)return;
            float available=Mathf.Max(1,resourcePanel.sizeDelta.x-64);
            float minimumGap=Mathf.Min(visualizerStyle.minimumGap,available/(count*3));
            float width=Mathf.Max(1,Mathf.Min(visualizerStyle.maximumBarWidth,(available-Mathf.Max(0,count-1)*minimumGap)/count));
            float gap=count>1 ? (available-width*count)/(count-1) : 0;
            float used=width*count+gap*Mathf.Max(0,count-1);
            float x=-used*.5f+width*.5f;
            // The top of the frame is reserved for the star score strip.
            barBaseline=38; maximumBarHeight=Mathf.Max(24,resourcePanel.sizeDelta.y-barBaseline-38-visualizerStyle.scoreStripHeight);
            for(int i=0;i<count;i++)
            {
                var rect=energyBars[i].rectTransform;
                rect.anchoredPosition=new Vector2(x,barBaseline); rect.sizeDelta=new Vector2(width,rect.sizeDelta.y);
                resourceLabels[i].rectTransform.anchoredPosition=new Vector2(x,barBaseline-15);
                resourceLabels[i].rectTransform.sizeDelta=new Vector2(width+Mathf.Min(gap*.5f,24),24);
                resourceValues[i].sizeDelta=new Vector2(width,1);
                barTotals[i].rectTransform.sizeDelta=new Vector2(width+Mathf.Min(gap*.5f,24),24);
                x+=width+gap;
            }
            LayoutVisualizerOverlays(available);
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
                // Motion stays close to the element's actual value.
                float pulse=totals[(int)element]>0 ? Mathf.Sin(Time.time*2.2f+(int)element*.83f)*Mathf.Min(2,height*visualizerStyle.pulseFraction) : 0;
                rect.sizeDelta=new Vector2(rect.sizeDelta.x,Mathf.Clamp(height+pulse,2,maximumBarHeight));
                bar.Leading=RecipesEnabled && composition.IsLeading(element);
                bar.SelfCombination=bar.Leading && composition.CanSelfCombine(element);
                string name=Catalog.ElementName(element);
                resourceLabels[i].text=name;
                barTotals[i].text=Math.Truncate(totals[(int)element]).ToString("0");
                barTotals[i].color=bar.SelfCombination ? new Color(1,.8f,.3f) : Color.white;
                barTotals[i].rectTransform.anchoredPosition=new Vector2(rect.anchoredPosition.x,barBaseline+rect.sizeDelta.y+17);
                resourceValues[i].anchoredPosition=new Vector2(rect.anchoredPosition.x,barBaseline+Mathf.Clamp((float)totals[(int)element]/scale*maximumBarHeight,2,maximumBarHeight));
            }
            UpdateVisualizerOverlays(scale);
        }
    }
}
