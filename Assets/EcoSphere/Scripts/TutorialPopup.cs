using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // Reusable modal with an optional pointer and a hole for an explicitly allowed target.
    public sealed class TutorialPopup : MonoBehaviour, ICanvasRaycastFilter
    {
        Canvas canvas;
        RectTransform root,box,pointer,tip,target;
        TMP_Text text;
        RectTransform interior;
        Texture2D roundedTexture;
        Sprite roundedSprite;
        Action next;

        bool allowTarget;
        public bool IsOpen { get; private set; }
        public bool PreferLeft { get; set; }
        public Button ContinueButton { get; private set; }
        public string Message => text.text;
        public RectTransform Target => target;
        public Canvas DialogCanvas => canvas;
        RectTransform Panel(string name,Transform parent,Color color)
        {
            var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent,false); image.color=color; image.raycastTarget=false;
            return image.rectTransform;
        }
        public void Initialize(Sprite frame,GameObject buttonPrefab,TMP_FontAsset font)
        {
            canvas=gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=1100;
            gameObject.AddComponent<CanvasScaler>(); gameObject.AddComponent<GraphicRaycaster>();
            root=Panel("Tutorial input shield",transform,new Color(0,.005f,.015f,.48f));
            root.GetComponent<Image>().raycastTarget=true;
            pointer=Panel("Tutorial pointer",root,new Color(1,.78f,.3f)); pointer.pivot=new Vector2(0,.5f);
            tip=Panel("Tutorial pointer tip",root,new Color(1,.85f,.5f)); tip.sizeDelta=Vector2.one*12; tip.localRotation=Quaternion.Euler(0,0,45);
            box=Panel("Tutorial box",root,Color.clear);
            interior=Panel("Rounded interior",box,new Color(.015f,.025f,.045f,.99f));
            roundedTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                var nearest=new Vector2(Mathf.Clamp(x,10,53),Mathf.Clamp(y,10,53));
                roundedTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(10-Vector2.Distance(new Vector2(x,y),nearest))));
            }
            roundedTexture.Apply();
            roundedSprite=Sprite.Create(roundedTexture,new Rect(0,0,64,64),Vector2.one*.5f,25,0,SpriteMeshType.FullRect,Vector4.one*12);
            interior.GetComponent<Image>().sprite=roundedSprite; interior.GetComponent<Image>().type=Image.Type.Sliced;
            var border=Panel("Tutorial frame",box,Color.white); border.anchorMin=Vector2.zero; border.anchorMax=Vector2.one; border.offsetMin=border.offsetMax=Vector2.zero;
            border.GetComponent<Image>().sprite=frame; border.GetComponent<Image>().type=Image.Type.Sliced; border.GetComponent<Image>().pixelsPerUnitMultiplier=3;
            text=new GameObject("Setup tutorial message",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(box,false); text.font=font; text.fontSize=29; text.alignment=TextAlignmentOptions.Center;
            text.color=new Color(1,.96f,.85f); text.raycastTarget=false;
            var button=Instantiate(buttonPrefab,box); button.name="Tutorial Continue";
            button.GetComponent<CelestialButton>().label.text="Continue";
            ContinueButton=button.GetComponent<Button>(); ContinueButton.onClick.AddListener(()=> { if(!IsOpen)return; var callback=next; Close(); callback?.Invoke(); });
            root.gameObject.SetActive(false);
        }
        public void Show(string message,RectTransform target,Action next,bool allowTarget=false)
        {
            if(!IsOpen) GameServices.Pause.Acquire(this);
            this.target=target; this.next=next; this.allowTarget=allowTarget;
            text.text=message; IsOpen=true; root.gameObject.SetActive(true);  RefreshLayout();
        }
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false; root.gameObject.SetActive(false); GameServices.Pause.Release(this);
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera eventCamera)
        {
            if(!IsOpen || !allowTarget || !target)return true;
            var targetCanvas=target.GetComponentInParent<Canvas>();
            return !RectTransformUtility.RectangleContainsScreenPoint(target,point,targetCanvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera);
        }
        void LateUpdate(){if(IsOpen)RefreshLayout();}
        public void RefreshLayout(Camera capture=null)
        {
            if(!IsOpen)return;
            var m=ScreenLayout.Apply(canvas,root,capture);
            float width=Mathf.Min(700,m.width-50),height=380;
            Vector2 end=Vector2.zero;
            if(target)
            {
                var owner=target.GetComponentInParent<Canvas>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(owner.renderMode==RenderMode.ScreenSpaceOverlay ? null : owner.worldCamera,target.position),canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,out end);
            }
            // Place the box opposite the target; collection panels use a lower overlay.
            float x=m.wide ? (end.x>0 ? -m.width*.22f : m.width*.22f) : 0;
            float y=end.y< -m.height*.25f ? -m.height*.5f+height*.5f+230 : -m.height*.5f+height*.5f+180;
            if(PreferLeft)
            {
                x=-Mathf.Max(0,(m.width-width)*.5f-24);
                if(!m.wide)y=0;
            }
            box.sizeDelta=new Vector2(width,height); box.anchoredPosition=new Vector2(x,y);
            interior.sizeDelta=box.sizeDelta-Vector2.one*80;
            ScreenLayout.Place(text.rectTransform,new Vector2(width-90,210),new Vector2(0,48));
            ScreenLayout.Place(ContinueButton.GetComponent<RectTransform>(),new Vector2(360,120),new Vector2(0,-height*.5f+78));
            pointer.gameObject.SetActive(target); tip.gameObject.SetActive(target);
            if(!target)return;
            Vector2 delta=end-box.anchoredPosition;
            float edge=Mathf.Min((width*.5f-40)/Mathf.Max(.001f,Mathf.Abs(delta.x)),(height*.5f-40)/Mathf.Max(.001f,Mathf.Abs(delta.y)));
            Vector2 start=box.anchoredPosition+delta*Mathf.Min(edge,1);
            pointer.anchoredPosition=start; pointer.sizeDelta=new Vector2(Vector2.Distance(start,end),5);
            pointer.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg); tip.anchoredPosition=end;
        }
        void OnDestroy(){Close(); if(roundedSprite)Destroy(roundedSprite); if(roundedTexture)Destroy(roundedTexture);}
    }
}
