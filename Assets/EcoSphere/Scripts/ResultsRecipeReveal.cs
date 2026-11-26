using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    public enum DiscoveryRevealPhase { None, Ingredients, Merge, Volatile, Flash, Reveal, Ready, Collecting, Complete }
    public sealed partial class ResultsView
    {
        bool recipeMode;
        RecipeRevealStyle revealStyle;
        readonly List<Material> revealMaterials=new List<Material>();
        readonly List<Material> outputMaterials=new List<Material>();
        readonly List<DiscoveryRecord> revealRecords=new List<DiscoveryRecord>();
        readonly List<RectTransform> ingredients=new List<RectTransform>();
        readonly List<Material> ingredientMaterials=new List<Material>();
        readonly List<TMP_Text> ingredientNames=new List<TMP_Text>();
        RectTransform recipeStage,flareRect;
        Material flareMaterial;
        Image screenWash;
        TMP_Text revealTitle,revealName,revealDescription;
        public string RevealedDescription => revealDescription ? revealDescription.text : "";
        AudioSource revealAudio,combineAudio;
        public int CombinePlayCount { get; private set; }
        public double CombineAudioStart { get; private set; }
        public double DiscoveryAudioStart { get; private set; }
        public double CombinationFinishedAt { get; private set; }
        float merge,shake,whiten,outputSize=190;
        public bool IsRecipeReveal => recipeMode;
        public int RevealIndex { get; private set; }
        public DiscoveryRevealPhase RevealPhase { get; private set; }
        public DiscoveryRecord CurrentDiscovery => recipeMode && RevealIndex<revealRecords.Count ? revealRecords[RevealIndex] : null;

        Material RevealMaterial(Material source)
        {
            var material=new Material(source); revealMaterials.Add(material); return material;
        }
        void BuildRecipeDiscoveries()
        {
            revealStyle=Resources.Load<RecipeRevealStyle>("RecipeRevealStyle");
            if(!revealStyle || !revealStyle.sphereMaterial || !revealStyle.flareMaterial)
                throw new InvalidOperationException("RecipeRevealStyle needs its sphere and flare materials");
            recipeMode=true;
            overlay=Rect("Recipe discovery overlay",LayoutCanvas.transform,Vector2.zero,Vector2.zero);
            overlay.anchorMin=Vector2.zero; overlay.anchorMax=Vector2.one;
            var shield=overlay.gameObject.AddComponent<Image>(); shield.sprite=fillSprite;
            shield.color=new Color(0,.005f,.015f,.85f); shield.raycastTarget=true;
            modal=Frame("Recipe discovery panel",overlay);
            revealTitle=Text("Discovery title",modal,"Element Combination",36,new Vector2(700,70),Vector2.zero);
            recipeStage=Rect("Recipe stage",modal,new Vector2(650,340),new Vector2(0,20));
            flareRect=Rect("Collision flare",recipeStage,Vector2.one*600,Vector2.zero);
            var flare=flareRect.gameObject.AddComponent<RawImage>(); flare.raycastTarget=false;
            flareMaterial=RevealMaterial(revealStyle.flareMaterial); flare.material=flareMaterial;
            flareMaterial.SetFloat("_Opacity",0);
            for(int i=0;i<result.DisplayedDiscoveries.Count;i++)
            {
                int id=result.DisplayedDiscoveries[i]; var definition=SphereCatalog.Current.Get(id);
                revealRecords.Add(result.discoveryDetails.Find(record=>record.sphereId==id) ?? new DiscoveryRecord { sphereId=id,kind=DiscoveryKind.Natural });
                var sphere=Rect(definition.displayName+" discovered sphere",recipeStage,Vector2.one*outputSize,Vector2.zero);
                var graphic=sphere.gameObject.AddComponent<RawImage>(); graphic.raycastTarget=false;
                var material=RevealMaterial(revealStyle.sphereMaterial); graphic.material=material; graphic.enabled=false;
                outputMaterials.Add(material);
                sphere.gameObject.AddComponent<SpherePreview>().definition=definition; DiscoverySpheres.Add(sphere);
                var slot=Rect(definition.displayName+" collected",collectionFrame,Vector2.one*100,Vector2.zero); slots.Add(slot);
                var label=Text(definition.displayName+" collected name",slot,definition.displayName,25,new Vector2(170,40),new Vector2(0,-75));
                label.gameObject.SetActive(false); settledNames.Add(label);
            }
            revealName=Text("Revealed element",modal,"",34,new Vector2(700,55),new Vector2(0,-155));
            revealDescription=DescriptionText("Revealed description",modal,"");
            OkButton=Button("Continue",modal,()=> { if(RevealPhase==DiscoveryRevealPhase.Ready)StartCoroutine(CollectRevealedDiscovery()); });
            OkButton.interactable=false;
            flightLayer=Rect("Discovery animation",overlay,Vector2.zero,Vector2.zero);
            flightLayer.anchorMin=Vector2.zero; flightLayer.anchorMax=Vector2.one;
            screenWash=Image("White discovery wash",overlay,fillSprite,Vector2.zero,Vector2.zero,new Color(1,1,1,0));
            screenWash.rectTransform.anchorMin=Vector2.zero; screenWash.rectTransform.anchorMax=Vector2.one;
            screenWash.rectTransform.offsetMin=screenWash.rectTransform.offsetMax=Vector2.zero;
            revealAudio=GameServices.Audio.CreateSource(gameObject,"Discovery effects",AudioChannel.Effects,1,true);
            revealAudio.ignoreListenerPause=true;
            combineAudio=GameServices.Audio.CreateSource(gameObject,"Sphere combine",AudioChannel.Effects,1,true);
        }
        void Ingredient(ElementId element)
        {
            var sphere=Rect(SphereCatalog.Current.ElementName(element)+" ingredient",recipeStage,Vector2.one*180,Vector2.zero);
            var graphic=sphere.gameObject.AddComponent<RawImage>(); graphic.raycastTarget=false; graphic.enabled=false;
            var material=RevealMaterial(revealStyle.sphereMaterial); graphic.material=material;
            var appearance=SphereCatalog.Current.ElementAppearance(element);
            if(appearance) sphere.gameObject.AddComponent<SpherePreview>().definition=appearance;
            else
            {
                // Elements without a registered namesake sphere use a labeled procedural orb.
                material.SetFloat("_OrbFallback",1);
                graphic.color=SphereCatalog.Current.ElementColor(element);
            }
            ingredients.Add(sphere); ingredientMaterials.Add(material);
            ingredientNames.Add(Text("Ingredient name",recipeStage,SphereCatalog.Current.ElementName(element),26,new Vector2(240,45),Vector2.zero));
        }
        IEnumerator RevealNextDiscovery()
        {
            OkButton.interactable=false; merge=shake=whiten=0; revealName.text=revealDescription.text="";
            foreach(var item in ingredients)Destroy(item.gameObject);
            foreach(var item in ingredientNames)Destroy(item.gameObject);
            ingredients.Clear(); ingredientMaterials.Clear(); ingredientNames.Clear();
            var record=CurrentDiscovery; var output=DiscoverySpheres[RevealIndex]; var material=outputMaterials[RevealIndex];
            var graphic=output.GetComponent<RawImage>(); graphic.enabled=false;
            material.SetFloat("_Whiten",0); material.SetFloat("_Glow",0);
            revealTitle.text=record.kind==DiscoveryKind.Recipe ? "Element Combination" : "Sphere Discovered";
            if(record.kind==DiscoveryKind.Recipe)
            {
                Ingredient(record.first); Ingredient(record.second);
                RevealPhase=DiscoveryRevealPhase.Ingredients; ApplyRecipePose();
                yield return null; // Initialize the 256px preview textures before making them visible.
                var combination=GameAudioSettings.Current.sphereCombine;
                if(!combination || combination.length<=0)throw new InvalidOperationException("Missing sphere combination sound");
                if(combination.loadState==AudioDataLoadState.Unloaded)combination.LoadAudioData();
                if(elementDiscovered && elementDiscovered.loadState==AudioDataLoadState.Unloaded)elementDiscovered.LoadAudioData();
                while(combination.loadState==AudioDataLoadState.Loading || (elementDiscovered && elementDiscovered.loadState==AudioDataLoadState.Loading))yield return null;
                if(combination.loadState==AudioDataLoadState.Failed)throw new InvalidOperationException("Could not load sphere combination sound");
                foreach(var item in ingredients)item.GetComponent<RawImage>().enabled=true;
                CombineAudioStart=AudioSettings.dspTime+.05;
                DiscoveryAudioStart=CombineAudioStart+combination.length;
                combineAudio.clip=combination; combineAudio.PlayScheduled(CombineAudioStart); combineAudio.SetScheduledEndTime(DiscoveryAudioStart); CombinePlayCount++;
                if(elementDiscovered)
                {
                    // Keep earlier announcement tails intact when the player confirms consecutive discoveries quickly.
                    var announcement=GameServices.Audio.CreateSource(gameObject,"Recipe discovery announcement",AudioChannel.Voice,1,true);
                    announcement.spatialBlend=0; announcement.ignoreListenerPause=true; announcement.clip=elementDiscovered;
                    announcement.PlayScheduled(DiscoveryAudioStart);
                }
                float weightSum=Mathf.Max(.01f,revealStyle.ingredientHold)+Mathf.Max(.01f,revealStyle.mergeDuration)+Mathf.Max(.01f,revealStyle.volatileDuration)+Mathf.Max(.01f,revealStyle.flashDuration)+Mathf.Max(.01f,revealStyle.revealDuration);
                double cursor=CombineAudioStart;
                double SegmentEnd(float weight) { cursor+=combination.length*Mathf.Max(.01f,weight)/weightSum; return cursor; }
                double segmentStart=cursor;
                yield return AnimateCombination(segmentStart,SegmentEnd(revealStyle.ingredientHold),t=> { });
                RevealPhase=DiscoveryRevealPhase.Merge;
                segmentStart=cursor;
                yield return AnimateCombination(segmentStart,SegmentEnd(revealStyle.mergeDuration),t=> { merge=t*t; whiten=t*.75f; ApplyRecipePose(); });
                foreach(var item in ingredients)item.GetComponent<RawImage>().enabled=false;
                foreach(var item in ingredientNames)item.gameObject.SetActive(false);
                graphic.enabled=true; material.SetFloat("_Whiten",1);
                RevealPhase=DiscoveryRevealPhase.Volatile;
                segmentStart=cursor;
                yield return AnimateCombination(segmentStart,SegmentEnd(revealStyle.volatileDuration),t=> {
                    shake=t; material.SetFloat("_Glow",1+t*2); ApplyRecipePose();
                    flareMaterial.SetFloat("_Progress",.1f+t*.15f); flareMaterial.SetFloat("_Opacity",t*.5f);
                });
                shake=0; ApplyRecipePose(); RevealPhase=DiscoveryRevealPhase.Flash;
                segmentStart=cursor;
                yield return AnimateCombination(segmentStart,SegmentEnd(revealStyle.flashDuration),t=> {
                    flareMaterial.SetFloat("_Progress",.2f+t*.65f); flareMaterial.SetFloat("_Opacity",1-t*.65f);
                    screenWash.color=new Color(1,1,1,revealStyle.screenWash*(1-t));
                });
                screenWash.color=Color.clear; RevealPhase=DiscoveryRevealPhase.Reveal;
                yield return AnimateCombination(cursor,DiscoveryAudioStart,t=> {
                    material.SetFloat("_Whiten",1-t); material.SetFloat("_Glow",2*(1-t));
                    flareMaterial.SetFloat("_Opacity",.35f*(1-t));
                });
                CombinationFinishedAt=AudioSettings.dspTime;
                if(elementDiscovered)DiscoveryPlayCount++;
            }
            else
            {
                RevealPhase=DiscoveryRevealPhase.Reveal; graphic.enabled=true; PlayDiscoverySound();
                yield return AnimateReveal(.35f,t=> { graphic.color=new Color(1,1,1,t); });
            }
            material.SetFloat("_Whiten",0); material.SetFloat("_Glow",0); flareMaterial.SetFloat("_Opacity",0);
            var revealed=SphereCatalog.Current.Get(record.sphereId);
            revealTitle.text="Sphere Discovered"; revealName.text=revealed.displayName; revealDescription.text=revealed.description;
            yield return AnimateReveal(revealStyle.revealHold,t=> { });
            RevealPhase=DiscoveryRevealPhase.Ready; OkButton.interactable=true;
        }
        IEnumerator AnimateCombination(double start,double end,Action<float> update)
        {
            // The audio clock, rather than accumulated frame times, keeps every phase aligned with the clip.
            while(AudioSettings.dspTime<end)
            {
                update(Mathf.Clamp01((float)((AudioSettings.dspTime-start)/Math.Max(.001,end-start))));
                yield return null;
            }
            update(1);
        }
        IEnumerator AnimateReveal(float duration,Action<float> update)
        {
            duration=Mathf.Max(.01f,duration);
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            { update(Mathf.Clamp01(elapsed/duration)); yield return null; }
            update(1);
        }
        void PlayDiscoverySound()
        {
            if(elementDiscovered) { revealAudio.PlayOneShot(elementDiscovered); DiscoveryPlayCount++; }
        }
        void ApplyRecipePose()
        {
            float separation=Mathf.Min(180,recipeStage.sizeDelta.x*.27f);
            float ingredientSize=Mathf.Min(180,recipeStage.sizeDelta.x*.28f);
            for(int i=0;i<ingredients.Count;i++)
            {
                float x=(i==0 ? -1 : 1)*separation*(1-merge);
                ingredients[i].anchoredPosition=new Vector2(x,0); ingredients[i].sizeDelta=Vector2.one*ingredientSize;
                ingredientNames[i].rectTransform.anchoredPosition=new Vector2((i==0 ? -1 : 1)*separation,-132);
                ingredientNames[i].color=new Color(1,.97f,.86f,1-merge);
                ingredientMaterials[i].SetFloat("_Whiten",whiten); ingredientMaterials[i].SetFloat("_Glow",merge);
            }
            if(RevealIndex<DiscoverySpheres.Count && RevealPhase!=DiscoveryRevealPhase.Collecting)
            {
                var output=DiscoverySpheres[RevealIndex];
                output.anchoredPosition=new Vector2(Mathf.Sin(Time.unscaledTime*71),Mathf.Cos(Time.unscaledTime*89))*shake*revealStyle.shakeDistance;
                output.sizeDelta=Vector2.one*outputSize*(1+shake*.025f*Mathf.Sin(Time.unscaledTime*44));
            }
        }
        IEnumerator CollectRevealedDiscovery()
        {
            if(RevealPhase!=DiscoveryRevealPhase.Ready)yield break;
            RevealPhase=DiscoveryRevealPhase.Collecting; OkButton.interactable=false;
            var sphere=DiscoverySpheres[RevealIndex]; sphere.SetParent(flightLayer,true);
            Vector3 from=Root.InverseTransformPoint(sphere.position); float startSize=sphere.sizeDelta.x;
            yield return AnimateReveal(revealStyle.collectDuration,t=> {
                float p=Mathf.SmoothStep(0,1,t);
                sphere.position=Root.TransformPoint(Vector3.Lerp(from,Root.InverseTransformPoint(slots[RevealIndex].position),p));
                sphere.sizeDelta=Vector2.one*Mathf.Lerp(startSize,100,p);
            });
            sphere.SetParent(slots[RevealIndex],false); sphere.anchoredPosition=Vector2.zero; sphere.sizeDelta=Vector2.one*100;
            settledNames[RevealIndex].gameObject.SetActive(true); RevealIndex++;
            if(RevealIndex<DiscoverySpheres.Count) { yield return RevealNextDiscovery(); yield break; }
            RevealPhase=DiscoveryRevealPhase.Complete; overlay.gameObject.SetActive(false);
            Settled=true; ContinueButton.interactable=true; ShowDiscoveryTutorial();
        }
        void LayoutRecipeReveal(ScreenLayout.Metrics m)
        {
            // Title, stage, name, description (beneath the sphere), then the button.
            float width=Mathf.Min(820,m.width-50),height=Mathf.Min(740,m.height-110);
            ScreenLayout.Place(modal,new Vector2(width,height),Vector2.zero);
            ((RectTransform)modal.Find("Backing")).sizeDelta=modal.sizeDelta-Vector2.one*42;
            ScreenLayout.Place(revealTitle.rectTransform,new Vector2(width-80,70),new Vector2(0,height*.5f-65));
            ScreenLayout.Place(recipeStage,new Vector2(width-90,height-290),new Vector2(0,20));
            flareRect.sizeDelta=Vector2.one*Mathf.Min(620,width-60);
            ScreenLayout.Place(revealName.rectTransform,new Vector2(width-80,55),new Vector2(0,-height*.5f+235));
            ScreenLayout.Place(revealDescription.rectTransform,new Vector2(Mathf.Min(620,width-120),64),new Vector2(0,-height*.5f+165));
            ScreenLayout.Place(OkButton.GetComponent<RectTransform>(),new Vector2(360,120),new Vector2(0,-height*.5f+75));
            ApplyRecipePose();
        }
        void DisposeRecipeMaterials()
        {
            foreach(var material in revealMaterials)if(material)Destroy(material);
        }
    }
}
