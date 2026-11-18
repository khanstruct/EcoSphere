using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public static class ScreenLayout
    {
        public struct Metrics
        {
            public float width,height,scale;
            public bool wide;
            public Rect safe;
        }
        public static Metrics Measure(int width,int height,Rect safe)
        {
            bool wide=safe.width/Mathf.Max(1,safe.height)>=1.2f;
            float scale=Mathf.Min(safe.width/(wide?1600:900),safe.height/(wide?900:1600));
            return new Metrics{width=safe.width/scale,height=safe.height/scale,scale=scale,wide=wide,safe=safe};
        }
        public static Metrics Apply(Canvas canvas,RectTransform root,Camera captureCamera=null)
        {
            int width=captureCamera&&captureCamera.targetTexture?captureCamera.targetTexture.width:Screen.width;
            int height=captureCamera&&captureCamera.targetTexture?captureCamera.targetTexture.height:Screen.height;
            Rect safe=captureCamera&&captureCamera.targetTexture?new Rect(0,0,width,height):Screen.safeArea;
            var metrics=Measure(width,height,safe);
            var scaler=canvas.GetComponent<CanvasScaler>(); if(scaler)scaler.enabled=false;
            canvas.scaleFactor=metrics.scale;
            root.anchorMin=new Vector2(safe.x/width,safe.y/height);
            root.anchorMax=new Vector2(safe.xMax/width,safe.yMax/height);
            root.offsetMin=root.offsetMax=Vector2.zero;
            return metrics;
        }
        public static void Place(RectTransform rect,Vector2 size,Vector2 position)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.sizeDelta=size; rect.anchoredPosition=position;
        }
        public static Rect Playfield(Rect safe)
        {
            const float aspect=9f/16;
            float width=Mathf.Min(safe.width,safe.height*aspect),height=width/aspect;
            return new Rect(safe.center.x-width*.5f,safe.center.y-height*.5f,width,height);
        }
    }
    public sealed class SystemResponsiveLayout : MonoBehaviour
    {
        RectTransform safeRoot;
        Canvas canvas;
        SystemView system;
        Vector2 previous;
        Rect previousSafe;
        void Start()
        {
            system=GetComponent<SystemView>(); canvas=GetComponent<Canvas>();
            var background=transform.Find("Starfield").GetComponent<Image>();
            background.preserveAspect=false;
            var fit=background.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio=background.sprite.rect.width/background.sprite.rect.height;
            fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            safeRoot=new GameObject("Safe Layout",typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(transform,false);
            var children=new System.Collections.Generic.List<Transform>();
            foreach(Transform child in transform)if(child!=safeRoot&&child.name!="Starfield")children.Add(child);
            foreach(var child in children)child.SetParent(safeRoot,false);
            Apply();
        }
        void LateUpdate(){if(previous!=new Vector2(Screen.width,Screen.height)||previousSafe!=Screen.safeArea)Apply();}
        public void Apply(Camera capture=null)
        {
            previous=new Vector2(Screen.width,Screen.height); previousSafe=Screen.safeArea;
            var m=ScreenLayout.Apply(canvas,safeRoot,capture);
            var heading=(RectTransform)safeRoot.Find("System Heading");
            heading.anchorMin=heading.anchorMax=new Vector2(.5f,1); heading.anchoredPosition=new Vector2(0,-100);
            var sun=(RectTransform)safeRoot.Find("Yellow Sun");
            sun.anchorMin=sun.anchorMax=new Vector2(.5f,m.wide?.21f:.18f);
            sun.sizeDelta=Vector2.one*(m.wide?440:Mathf.Min(620,m.width*.7f));
            float[] heights=m.wide?new[]{.39f,.49f,.58f,.67f,.76f}:new[]{.40f,.50f,.60f,.70f,.80f};
            var random=new System.Random(3187);
            for(int i=1;i<heights.Length-1;i++)heights[i]+=(float)(random.NextDouble()*2-1)*.022f;
            for(int i=0;i<system.planets.Length;i++)
            {
                var orbit=safeRoot.Find("Orbit "+(i+1)).GetComponent<SystemMapGraphic>();
                orbit.orbitHeight=heights[i]; orbit.SetVerticesDirty();
                var planet=system.planets[i].GetComponent<RectTransform>();
                planet.anchorMin=planet.anchorMax=new Vector2(.5f+orbit.planetX,SystemMapGraphic.OrbitY(orbit.planetX,heights[i]));
            }
        }
    }
}
