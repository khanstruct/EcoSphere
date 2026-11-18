using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EcoSphere
{
    public sealed class LevelPrepView : MonoBehaviour
    {
        public static int SystemNumber = 1, PlanetIndex, RequiredSpheres = 2;
        public static bool ReturningToSystem;
        static bool smokeStarted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            SystemNumber = 1; PlanetIndex = 0; RequiredSpheres = 2;
            ReturningToSystem = false; smokeStarted = false;
        }
        public TMP_FontAsset font;
        public Sprite frame, line, arch, starfield, sunModifier, rotationModifier;
        public Texture2D iceSurface, sunlightSurface;
        public Sprite iceIcon, sunlightIcon;
        public GameObject buttonPrefab;
        public TMP_Text Title { get; private set; }
        public Button Begin { get; private set; }
        public readonly List<int> Selected = new List<int>();
        readonly List<RectTransform> slots = new List<RectTransform>();
        readonly List<Image> tiles = new List<Image>();
        TMP_Text count;
        Transform root;
        Camera view;
        Canvas layoutCanvas;
        Vector2 previousScreen;
        Rect previousSafe;

        RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        TMP_Text Text(string name, Transform parent, string value, int size, Vector2 box, Vector2 pos)
        {
            var label = Rect(name,parent,box,pos).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.text = value; label.color = new Color(1,.98f,.9f);
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; return label;
        }
        Image Image(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos)
        {
            var image = Rect(name,parent,size,pos).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        Image Frame(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var image = Image(name,parent,frame,size,pos); image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 6; image.preserveAspect = false;
            var fill = Rect("Midnight backing",image.transform,size-new Vector2(42,42),Vector2.zero).gameObject.AddComponent<Image>();
            fill.color = new Color(.015f,.035f,.065f,.94f); fill.raycastTarget = false; fill.transform.SetAsFirstSibling();
            return image;
        }
        void Start()
        {
            RequiredSpheres = Mathf.Clamp(RequiredSpheres,1,4);
            view = new GameObject("Prep Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            view.transform.position = new Vector3(0,0,-10); view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.01f,.02f,.04f);
            view.cullingMask = ~(1 << 30);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f,.5f,.6f);
            var light = new GameObject("Preview light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.35f; light.transform.rotation = Quaternion.Euler(35,-30,0);
            var canvas = new GameObject("Level Prep Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            layoutCanvas=canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = view; canvas.planeDistance = 5;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900,1600); scaler.matchWidthOrHeight = 1;
            root = Rect("Portrait Layout",canvas.transform,new Vector2(900,1600),Vector2.zero);
            var bg = Image("Starfield",root,starfield,new Vector2(900,1600),Vector2.zero); bg.color = new Color(.6f,.65f,.8f);
            bg.transform.SetParent(canvas.transform,false);
            bg.rectTransform.anchorMin=Vector2.zero; bg.rectTransform.anchorMax=Vector2.one; bg.rectTransform.sizeDelta=Vector2.zero;
            bg.rectTransform.anchoredPosition=Vector2.zero; bg.transform.SetAsFirstSibling();
            bg.preserveAspect=false;
            var backgroundFit=bg.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFit.aspectRatio=starfield.rect.width/starfield.rect.height;
            backgroundFit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            var system = Text("System",root,"SYSTEM "+SystemNumber.ToString("D2"),22,new Vector2(500,40),new Vector2(0,750));
            system.characterSpacing = 6;
            float offset = system.preferredWidth*.5f+68;
            Image("Left ornament",root,line,new Vector2(170,57),new Vector2(-offset,750));
            Image("Right ornament",root,line,new Vector2(170,57),new Vector2(offset,750)).transform.localScale = new Vector3(-1,1,1);
            Title = Text("Planet",root,WorldLevels.Name(SystemNumber,PlanetIndex),44,new Vector2(760,65),new Vector2(0,700));
            Image("Arch",root,arch,new Vector2(500,167),new Vector2(0,653));
            var collection = Frame("Discovered Spheres",root,new Vector2(570,940),new Vector2(-140,120));
            Text("Collection heading",collection.transform,"Discovered Spheres",29,new Vector2(510,60),new Vector2(0,405));
            BuildCollection(collection.transform);
            var loadout = Frame("Your Spheres",root,new Vector2(280,940),new Vector2(290,120));
            Text("Loadout heading",loadout.transform,"Chosen Spheres",28,new Vector2(245,60),new Vector2(0,405));
            for(int i=0;i<RequiredSpheres;i++)
            {
                float y = (RequiredSpheres-1)*90 - i*180;
                var slot = Rect("Slot "+(i+1),loadout.transform,new Vector2(140,140),new Vector2(0,y));
                var ring = slot.gameObject.AddComponent<SystemMapGraphic>(); ring.circle=true; ring.thickness=2;
                ring.color=new Color(1,.76f,.24f);
                var button=slot.gameObject.AddComponent<Button>(); button.targetGraphic=ring;
                ButtonClickSound.Attach(button);
                int index=i; button.onClick.AddListener(()=>Remove(index)); slots.Add(slot);
            }
            count=Text("Selection count",loadout.transform,"",24,new Vector2(245,60),new Vector2(0,-405));
            var conditions=Frame("World Conditions",root,new Vector2(880,270),new Vector2(0,-505));
            Text("Conditions heading",conditions.transform,"World Conditions",29,new Vector2(750,45),new Vector2(0,88));
            Modifier(conditions.transform,sunModifier,"Close to the Sun","Sunlight and Fire are more effective.",25);
            Modifier(conditions.transform,rotationModifier,"Continuous Rotation","The cluster rotates throughout play.",-65);
            var back=Navigation("Back",new Vector2(-270,-738));
            back.onClick.AddListener(()=>{ReturningToSystem=true; SceneManager.LoadScene("System");});
            Begin=Navigation("Begin",new Vector2(270,-738));
            Begin.onClick.AddListener(BeginLevel);
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            Refresh();
            Reflow();
            if(!smokeStarted && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-prepSmoke")>=0)
            { smokeStarted=true; StartCoroutine(Smoke()); }
        }
        void Update()
        {
            if(layoutCanvas&&(previousScreen!=new Vector2(Screen.width,Screen.height)||previousSafe!=Screen.safeArea))Reflow();
        }
        public void Reflow()
        {
            previousScreen=new Vector2(Screen.width,Screen.height); previousSafe=Screen.safeArea;
            var m=ScreenLayout.Apply(layoutCanvas,(RectTransform)root,view);
            RectTransform Child(string name)=>(RectTransform)root.Find(name);
            void Move(string name,Vector2 size,Vector2 position)=>ScreenLayout.Place(Child(name),size,position);
            Move("System",new Vector2(500,40),new Vector2(0,m.height*.5f-50));
            Move("Planet",new Vector2(760,65),new Vector2(0,m.height*.5f-100));
            Move("Arch",new Vector2(500,167),new Vector2(0,m.height*.5f-147));
            var system=Child("System").GetComponent<TMP_Text>();
            float ornamentOffset=system.preferredWidth*.5f+68;
            Move("Left ornament",new Vector2(170,57),new Vector2(-ornamentOffset,m.height*.5f-50));
            Move("Right ornament",new Vector2(170,57),new Vector2(ornamentOffset,m.height*.5f-50));
            float top=m.height*.5f-210,bottom=-m.height*.5f+(m.wide?135:440);
            float panelHeight=top-bottom;
            float rightWidth=m.wide?Mathf.Clamp(m.width*.32f,440,620):Mathf.Min(340,m.width*.31f);
            float leftWidth=m.width-rightWidth-50;
            float rightX=m.width*.5f-20-rightWidth*.5f,leftX=-m.width*.5f+20+leftWidth*.5f;
            Move("Discovered Spheres",new Vector2(leftWidth,panelHeight),new Vector2(leftX,(top+bottom)*.5f));
            float loadoutHeight=m.wide?panelHeight*.56f:panelHeight;
            Move("Your Spheres",new Vector2(rightWidth,loadoutHeight),new Vector2(rightX,top-loadoutHeight*.5f));
            float conditionsHeight=m.wide?panelHeight-loadoutHeight-12:270;
            Move("World Conditions",new Vector2(m.wide?rightWidth:m.width-20,conditionsHeight),
                m.wide?new Vector2(rightX,bottom+conditionsHeight*.5f):new Vector2(0,-m.height*.5f+295));
            var collection=Child("Discovered Spheres"); var loadout=Child("Your Spheres"); var conditions=Child("World Conditions");
            void In(Transform parent,string name,Vector2 size,Vector2 pos)=>ScreenLayout.Place((RectTransform)parent.Find(name),size,pos);
            In(collection,"Midnight backing",new Vector2(leftWidth-42,panelHeight-42),Vector2.zero);
            In(collection,"Collection heading",new Vector2(leftWidth-50,60),new Vector2(0,panelHeight*.5f-65));
            float viewportHeight=panelHeight-165;
            In(collection,"Collection viewport",new Vector2(leftWidth-65,viewportHeight),new Vector2(0,-30));
            var content=(RectTransform)collection.Find("Collection viewport/Sphere list");
            int columns=Mathf.Max(2,Mathf.FloorToInt((leftWidth-65)/245));
            float tileWidth=Mathf.Min(245,(leftWidth-65)/columns-10);
            content.sizeDelta=new Vector2(leftWidth-65,Mathf.Max(viewportHeight,410)); content.anchoredPosition=Vector2.zero;
            for(int i=0;i<tiles.Count;i++)
            {
                var rect=tiles[i].rectTransform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
                rect.sizeDelta=new Vector2(tileWidth,260);
                rect.anchoredPosition=new Vector2(-(columns-1)*(tileWidth+10)*.5f+(i%columns)*(tileWidth+10),-145-(i/columns)*275);
                In(rect,"Midnight backing",rect.sizeDelta-new Vector2(42,42),Vector2.zero);
                In(rect,"Rotating sphere",new Vector2(Mathf.Min(185,tileWidth-35),Mathf.Min(185,tileWidth-35)),new Vector2(0,20));
            }
            In(loadout,"Midnight backing",new Vector2(rightWidth-42,loadoutHeight-42),Vector2.zero);
            In(loadout,"Loadout heading",new Vector2(rightWidth-35,60),new Vector2(0,loadoutHeight*.5f-55));
            for(int i=0;i<slots.Count;i++)
            {
                if(m.wide)
                {
                    int cols=RequiredSpheres==1?1:2;
                    float slotSize=RequiredSpheres>2?70:110;
                    ScreenLayout.Place(slots[i],Vector2.one*slotSize,new Vector2((i%cols-(cols-1)*.5f)*145,RequiredSpheres>2?30-(i/cols)*80:-10));
                }
                else
                {
                    float spacing=Mathf.Min(180,(loadoutHeight-190)/RequiredSpheres);
                    ScreenLayout.Place(slots[i],Vector2.one*Mathf.Min(140,spacing*.8f),new Vector2(0,(RequiredSpheres-1)*spacing*.5f-i*spacing));
                }
                var preview=slots[i].Find("Selected sphere") as RectTransform;
                if(preview)preview.sizeDelta=slots[i].sizeDelta-Vector2.one*10;
            }
            In(loadout,"Selection count",new Vector2(rightWidth-35,m.wide?30:50),new Vector2(0,-loadoutHeight*.5f+(m.wide?46:65)));
            In(conditions,"Midnight backing",new Vector2((m.wide?rightWidth:m.width-20)-42,conditionsHeight-42),Vector2.zero);
            In(conditions,"Conditions heading",new Vector2((m.wide?rightWidth:m.width)-60,40),new Vector2(0,conditionsHeight*.5f-40));
            float conditionWidth=m.wide?rightWidth:m.width-20;
            for(int i=0;i<2;i++)
            {
                string name=i==0?"Close to the Sun":"Continuous Rotation";
                float y=conditionsHeight*.5f-(m.wide?95:105)-i*(m.wide?72:82);
                In(conditions,name+" icon",new Vector2(62,62),new Vector2(-conditionWidth*.5f+70,y));
                In(conditions,name,new Vector2(conditionWidth-150,32),new Vector2(42,y+17));
                In(conditions,name+" description",new Vector2(conditionWidth-150,46),new Vector2(42,y-20));
                conditions.Find(name).GetComponent<TMP_Text>().fontSize=m.wide?22:24;
                conditions.Find(name+" description").GetComponent<TMP_Text>().fontSize=m.wide?18:21;
            }
            Move("Back",new Vector2(360,120),new Vector2(-m.width*.5f+180,-m.height*.5f+62));
            Move("Begin",new Vector2(360,120),new Vector2(m.width*.5f-180,-m.height*.5f+62));
        }
        void BuildCollection(Transform panel)
        {
            var viewport=Rect("Collection viewport",panel,new Vector2(510,755),new Vector2(-6,-22));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            var content=Rect("Sphere list",viewport,new Vector2(510,755),Vector2.zero);
            content.anchorMin=content.anchorMax=new Vector2(.5f,1); content.pivot=new Vector2(.5f,1);
            var scroll=panel.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content;
            scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
            for(int i=0;i<2;i++)
            {
                var tile=Frame(i==0?"Ice":"Sunlight",content,new Vector2(245,260),new Vector2(i==0?-128:128,-145));
                tile.rectTransform.anchorMin=tile.rectTransform.anchorMax=new Vector2(.5f,1);
                tile.color=new Color(1,1,1,.35f); tiles.Add(tile);
                var preview=Rect("Rotating sphere",tile.transform,new Vector2(185,185),new Vector2(0,20));
                Preview(preview,i);
                Text("Name",tile.transform,i==0?"Ice":"Sunlight",27,new Vector2(210,50),new Vector2(0,-85));
                tile.raycastTarget=true; var button=tile.gameObject.AddComponent<Button>(); button.targetGraphic=tile;
                ButtonClickSound.Attach(button);
                button.transition=Selectable.Transition.None;
                int type=i; button.onClick.AddListener(()=>AddSphere(type));
            }
            var note=Text("Collection note",content,"More spheres await discovery",22,new Vector2(480,80),new Vector2(0,-365));
            note.rectTransform.anchorMin=note.rectTransform.anchorMax=new Vector2(.5f,1);
            note.color=new Color(.6f,.66f,.74f);
        }
        void Preview(RectTransform parent,int type)
        {
            var raw=parent.gameObject.AddComponent<RawImage>(); raw.raycastTarget=false;
            var preview=parent.gameObject.AddComponent<SpherePreview>();
            preview.surface=type==0?iceSurface:sunlightSurface; preview.icon=type==0?iceIcon:sunlightIcon;
        }
        void Modifier(Transform parent,Sprite icon,string heading,string detail,float y)
        {
            Image(heading+" icon",parent,icon,new Vector2(70,70),new Vector2(-355,y));
            var title=Text(heading,parent,heading,24,new Vector2(650,32),new Vector2(30,y+17)); title.alignment=TextAlignmentOptions.Left;
            var description=Text(heading+" description",parent,detail,21,new Vector2(650,32),new Vector2(30,y-17));
            description.alignment=TextAlignmentOptions.Left; description.color=new Color(.78f,.81f,.86f);
        }
        Button Navigation(string title,Vector2 position)
        {
            var go=Instantiate(buttonPrefab,root); go.name=title;
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.anchoredPosition=position;
            go.GetComponent<CelestialButton>().label.text=title; return go.GetComponent<Button>();
        }
        public void AddSphere(int type)
        {
            if(type<0||type>1||Selected.Contains(type)||Selected.Count>=RequiredSpheres)return;
            Selected.Add(type); Refresh();
        }
        void BeginLevel()
        {
            if(!Begin.interactable || Selected.Count!=RequiredSpheres)return;
            string scene=WorldLevels.GameplayScene(SystemNumber,PlanetIndex);
            if(!string.IsNullOrEmpty(scene))SceneManager.LoadScene(scene);
        }
        public void Remove(int index)
        {
            if(index<0||index>=Selected.Count)return;
            Selected.RemoveAt(index); Refresh();
        }
        void Refresh()
        {
            for(int i=0;i<slots.Count;i++)
            {
                var old=slots[i].Find("Selected sphere"); if(old) Destroy(old.gameObject);
                if(i<Selected.Count) Preview(Rect("Selected sphere",slots[i],slots[i].sizeDelta-Vector2.one*10,Vector2.zero),Selected[i]);
            }
            for(int i=0;i<tiles.Count;i++) tiles[i].color=new Color(1,1,1,Selected.Contains(i)?1:.35f);
            count.text=Selected.Count+" / "+RequiredSpheres+" selected";
            Begin.interactable=Selected.Count==RequiredSpheres;
            if(layoutCanvas)Reflow();
        }
        IEnumerator Smoke()
        {
            yield return new WaitForSeconds(1);
            var animated=FindAnyObjectByType<SpherePreview>();
            Quaternion initial=animated.SurfaceRotation;
            yield return new WaitForSeconds(.3f);
            if(Quaternion.Angle(initial,animated.SurfaceRotation)<1)throw new System.Exception("Sphere surface must rotate");
            if(Title.text!=WorldLevels.Name(SystemNumber,PlanetIndex)||Begin.interactable)throw new System.Exception("Prep initial state");
            AddSphere(0); AddSphere(0); AddSphere(1);
            if(Selected.Count!=2||Begin.interactable!=(RequiredSpheres==2))throw new System.Exception("Prep selection");
            Remove(0); if(Selected.Count!=1||Selected[0]!=1)throw new System.Exception("Prep removal");
            AddSphere(0); yield return new WaitForSeconds(.4f);
            var target=new RenderTexture(900,1600,24); view.targetTexture=target; view.aspect=900f/1600;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach(var label in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            foreach(var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None))if(preview.PreviewCamera)preview.PreviewCamera.Render();
            view.Render(); RenderTexture.active=target;
            var image=new Texture2D(900,1600,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,900,1600),0,0); image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/LevelPrep-preview.png"),image.EncodeToPNG());
            RenderTexture.active=null; view.targetTexture=null; target.Release(); Destroy(target); Destroy(image);
            Debug.Log("ECOSPHERE_PREP_SMOKE_PASSED: titles, unique selection, slot compaction and Begin gating.");
            new GameObject("Prep navigation checks").AddComponent<PrepNavigationSmoke>();
        }
    }
}
