using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // Shared completion presentation; world rules and saved score determine the award before it opens.
    public sealed class StarRatingPresentation : MonoBehaviour
    {
        public Button ContinueButton {get;private set;}
        public int RevealedStars {get;private set;}
        public int SoundCount {get;private set;}
        public bool RevealComplete {get;private set;}
        readonly System.Collections.Generic.List<RectTransform> stars=new System.Collections.Generic.List<RectTransform>();
        RectTransform root;
        bool continued;
        const float RevealInterval=.5f, PopDuration=.3f;

        public IEnumerator Present(int count,Sprite sprite,GameObject buttonPrefab,TMP_FontAsset font)
        {
            count=Mathf.Clamp(count,0,3);
            UIInput.EnsureEventSystem();
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1500;
            gameObject.AddComponent<GraphicRaycaster>();
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(900,1600);scaler.matchWidthOrHeight=1;
            root=(RectTransform)transform;
            var backing=new GameObject("Rating input shield",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            backing.transform.SetParent(root,false);backing.rectTransform.anchorMin=Vector2.zero;backing.rectTransform.anchorMax=Vector2.one;
            backing.rectTransform.offsetMin=backing.rectTransform.offsetMax=Vector2.zero;backing.color=new Color(0,.005f,.015f,.78f);
            var heading=new GameObject("Rating heading",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            heading.transform.SetParent(root,false);heading.font=font;heading.text="WORLD COMPLETE";heading.fontSize=40;
            heading.alignment=TextAlignmentOptions.Center;heading.color=new Color(1,.83f,.43f);heading.raycastTarget=false;
            heading.rectTransform.anchorMin=new Vector2(.1f,.86f);heading.rectTransform.anchorMax=new Vector2(.9f,.94f);
            heading.rectTransform.offsetMin=heading.rectTransform.offsetMax=Vector2.zero;
            var button=Instantiate(buttonPrefab,root);button.name="Rating Continue";
            ContinueButton=button.GetComponent<Button>();ContinueButton.interactable=false;
            button.GetComponent<CelestialButton>().label.text="Continue";
            var buttonRect=button.GetComponent<RectTransform>();buttonRect.anchorMin=buttonRect.anchorMax=new Vector2(.5f,.09f);
            buttonRect.anchoredPosition=Vector2.zero;buttonRect.sizeDelta=new Vector2(360,120);
            ContinueButton.onClick.AddListener(()=>continued=true);
            button.SetActive(false);
            var audio=GameServices.Audio.CreateSource(gameObject,"Star score",AudioChannel.Effects,.85f);
            var clip=GameAudioSettings.Current.starScore;if(clip)clip.LoadAudioData();
            for(int i=0;i<count;i++)
            {
                var star=new GameObject("Earned star "+(i+1),typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                star.transform.SetParent(root,false);star.sprite=sprite;star.preserveAspect=true;star.raycastTarget=false;
                stars.Add(star.rectTransform);LayoutStars(count);RevealedStars++;
                if(clip){audio.PlayOneShot(clip);SoundCount++;}
                for(float elapsed=0;elapsed<RevealInterval;elapsed+=Time.unscaledDeltaTime)
                {
                    float t=Mathf.Clamp01(elapsed/PopDuration);
                    float pop=t<.7f ? Mathf.Lerp(.15f,1.12f,Mathf.SmoothStep(0,1,t/.7f)) : Mathf.Lerp(1.12f,1,(t-.7f)/.3f);
                    star.transform.localScale=Vector3.one*pop;yield return null;
                }
                star.transform.localScale=Vector3.one;
            }
            RevealComplete=true;button.SetActive(true);ContinueButton.interactable=true;
            while(!continued)yield return null;
        }
        void LateUpdate(){if(root)LayoutStars(stars.Count==0 ? 0 : targetCount);}
        int targetCount;
        void LayoutStars(int count)
        {
            targetCount=count;
            float width=root.rect.width,height=root.rect.height;
            float size=Mathf.Min(width*.28f,height*.42f),step=size+width*.025f;
            for(int i=0;i<stars.Count;i++)
            {
                stars[i].anchorMin=stars[i].anchorMax=new Vector2(.5f,.58f);
                stars[i].sizeDelta=Vector2.one*size;stars[i].anchoredPosition=new Vector2((i-(count-1)*.5f)*step,0);
            }
        }
    }
}
