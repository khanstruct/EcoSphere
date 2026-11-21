using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    [RequireComponent(typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster))]
    public sealed class SystemDialog : MonoBehaviour
    {
        public const string Welcome = "Welcome to EcoSphere. This is your first Star System.";
        public const string Requirement = "You currently only have two Elemental Spheres, so click on the world with a Sphere Requirement of two.";
        public const string Insufficient = "You do not have enough spheres for this world. Unlock new elements and try again.";
        SystemView owner;
        Canvas canvas;
        RectTransform root, box, textRect, leader, tip;
        TMP_Text message;
        Texture2D roundedTexture;
        Sprite roundedSprite;
        int step;
        CompletedWorld followupSource;
        Transform followupTarget;
        string followupMessage;
        float previousTimeScale;
        public bool IsOpen { get; private set; }
        public bool IsIntroduction => step < 2;
        public string Message => message.text;
        public Button ContinueButton { get; private set; }
        public Transform PointerTarget => !IsOpen ? null : step==1 ? owner.planets[0].transform : step==3 ? followupTarget : null;
        public bool PointerVisible => IsOpen && leader.gameObject.activeSelf;
        public Canvas DialogCanvas => canvas;

        RectTransform Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var image = new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent,false); image.color = color; image.raycastTarget = false;
            var rect = image.rectTransform; ScreenLayout.Place(rect,size,position); return rect;
        }

        public void Initialize(SystemView owner, Sprite frame, GameObject buttonPrefab, TMP_FontAsset font)
        {
            if (!frame || !buttonPrefab || !font) throw new InvalidOperationException("System dialog artwork, button and font are required");
            this.owner = owner;
            canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            root = Panel("System modal input shield",transform,Vector2.zero,Vector2.zero,new Color(0,.008f,.02f,.55f));
            root.GetComponent<Image>().raycastTarget = true;
            leader = Panel("System tutorial pointer",root,new Vector2(0,7),Vector2.zero,new Color(1,.79f,.36f)); leader.pivot = new Vector2(0,.5f);
            tip = Panel("System tutorial endpoint",root,Vector2.one*12,Vector2.zero,new Color(1,.85f,.48f)); tip.localRotation = Quaternion.Euler(0,0,45);
            box = Panel("System tutorial frame",root,new Vector2(740,450),Vector2.zero,Color.clear);
            var fill = Panel("Rounded interior",box,new Vector2(615,325),Vector2.zero,new Color(.015f,.025f,.045f,.98f)).GetComponent<Image>();
            roundedTexture = new Texture2D(64,64,TextureFormat.RGBA32,false);
            for (int y=0;y<64;y++) for (int x=0;x<64;x++)
            {
                var nearest = new Vector2(Mathf.Clamp(x,10,53),Mathf.Clamp(y,10,53));
                roundedTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(10-Vector2.Distance(new Vector2(x,y),nearest))));
            }
            roundedTexture.Apply();
            roundedSprite = Sprite.Create(roundedTexture,new Rect(0,0,64,64),Vector2.one*.5f,25,0,SpriteMeshType.FullRect,Vector4.one*12);
            fill.sprite = roundedSprite; fill.type = Image.Type.Sliced;
            var border = Panel("Frame artwork",box,box.sizeDelta,Vector2.zero,Color.white).GetComponent<Image>();
            border.sprite = frame; border.type = Image.Type.Sliced; border.pixelsPerUnitMultiplier = 2;
            textRect = new GameObject("System tutorial message",typeof(RectTransform)).GetComponent<RectTransform>();
            textRect.SetParent(box,false); ScreenLayout.Place(textRect,new Vector2(590,245),new Vector2(0,50));
            message = textRect.gameObject.AddComponent<TextMeshProUGUI>(); message.font = font; message.fontSize = 30;
            message.alignment = TextAlignmentOptions.Center; message.color = new Color(1,.96f,.85f); message.raycastTarget = false;
            var button = Instantiate(buttonPrefab,box); button.name = "System Continue";
            ScreenLayout.Place(button.GetComponent<RectTransform>(),new Vector2(360,120),new Vector2(0,-125));
            button.GetComponent<CelestialButton>().label.text = "Continue";
            ContinueButton = button.GetComponent<Button>(); ContinueButton.onClick.AddListener(Continue);
            root.gameObject.SetActive(false);
        }

        public void ShowIntroduction() { if(!DevelopmentMode.Enabled)Show(0); }
        public void ShowInsufficientSpheres() { Show(2); }
        public void ShowNextWorld(CompletedWorld source,Transform target,string message)
        { if(DevelopmentMode.Enabled)return; followupSource=source; followupTarget=target; followupMessage=message; Show(3); }
        void Show(int next)
        {
            if (!IsOpen) previousTimeScale = Time.timeScale;
            step = next; IsOpen = true; message.text = next == 0 ? Welcome : next == 1 ? Requirement : next==3 ? followupMessage : Insufficient;
            root.gameObject.SetActive(true); Time.timeScale = 0; RefreshLayout();
        }
        void Continue()
        {
            if (!IsOpen) return;
            if (step == 0) { Show(1); return; }
            if (step == 1) WorldProgress.FinishSystemIntroduction();
            if (step == 3) WorldProgress.FinishNextWorldTutorial(followupSource);
            IsOpen = false; root.gameObject.SetActive(false); Time.timeScale = previousTimeScale; owner.DialogDismissed();
        }

        void LateUpdate() { if (IsOpen) RefreshLayout(); }
        public void RefreshLayout(Camera capture = null)
        {
            if (!IsOpen) return;
            var metrics = ScreenLayout.Apply(canvas,root,capture);
            float width = Mathf.Min(740,metrics.width-48), height = Mathf.Min(450,metrics.height-64);
            box.sizeDelta = new Vector2(width,height);
            ((RectTransform)box.Find("Rounded interior")).sizeDelta = box.sizeDelta-Vector2.one*125;
            ((RectTransform)box.Find("Frame artwork")).sizeDelta = box.sizeDelta;
            textRect.sizeDelta = new Vector2(width-150,245);
            ContinueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,-height*.5f+100);
            float x = (step == 1 || step == 3) ? Mathf.Min(metrics.width*.18f,Mathf.Max(0,(metrics.width-width)*.5f-24)) : 0;
            float y = (step == 1 || step == 3) ? Mathf.Min(260,Mathf.Max(0,(metrics.height-height)*.5f-24)) : 0;
            if(step==3)x=-Mathf.Min(80,Mathf.Max(0,(metrics.width-width)*.5f-24));
            box.anchoredPosition = new Vector2(x,y);
            leader.gameObject.SetActive(PointerTarget); tip.gameObject.SetActive(PointerTarget);
            if (!PointerTarget) return;
            Canvas.ForceUpdateCanvases();
            var targetCanvas = PointerTarget.GetComponentInParent<Canvas>();
            var targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            var overlayCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(targetCamera,PointerTarget.position),overlayCamera,out var end);
            Vector2 delta = end-box.anchoredPosition;
            float edge = Mathf.Min((width*.5f-64)/Mathf.Max(.001f,Mathf.Abs(delta.x)),(height*.5f-64)/Mathf.Max(.001f,Mathf.Abs(delta.y)));
            Vector2 start = box.anchoredPosition+delta*edge;
            leader.anchoredPosition = start; leader.sizeDelta = new Vector2(Vector2.Distance(start,end),7);
            leader.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg); tip.anchoredPosition = end;
        }
        void OnDestroy()
        {
            if (IsOpen) Time.timeScale = previousTimeScale;
            if (roundedSprite) Destroy(roundedSprite);
            if (roundedTexture) Destroy(roundedTexture);
        }
    }
}
