using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        Rect playableScreen;
        public Rect PlayableScreenRect => playableScreen;
        Canvas gameplayCanvas;
        RectTransform gameplayRoot,resourcePanel;
        TMP_Text gameplayTitle,instructions;

        Vector2 previousPresentation;
        Rect previousPresentationSafe;

        void FitPresentation()
        {
            int width=view.targetTexture?view.targetTexture.width:Screen.width;
            int height=view.targetTexture?view.targetTexture.height:Screen.height;
            Rect safe=view.targetTexture?new Rect(0,0,width,height):Screen.safeArea;
            var metrics=ScreenLayout.Measure(width,height,safe);
            float panelHeight=metrics.wide ? 200 : 260;
            var boardSafe=new Rect(safe.x,safe.y+(panelHeight+20)*metrics.scale,safe.width,safe.height-(panelHeight+20)*metrics.scale);
            Rect field=ScreenLayout.Playfield(boardSafe);
            playableScreen=field;
            float worldPerPixel=9.3f/field.width;
            view.rect=new Rect(0,0,1,1);
            view.aspect=(float)width/height;
            view.orthographicSize=height*worldPerPixel*.5f;
            view.transform.position=new Vector3(-(field.center.x-width*.5f)*worldPerPixel,-(field.center.y-height*.5f)*worldPerPixel,-25);
            view.cullingMask=~0;
            view.clearFlags=CameraClearFlags.SolidColor;
            if(!gameplayCanvas)return;
            var m=ScreenLayout.Apply(gameplayCanvas,gameplayRoot,view);
            if(previousPresentation==new Vector2(m.width,m.height)&&previousPresentationSafe==safe)return;
            previousPresentation=new Vector2(m.width,m.height); previousPresentationSafe=safe;

            float halfH=m.height*.5f;
            if(m.wide)
            {
                float boardWidth=m.height*9f/16;
                float side=Mathf.Min(460,(m.width-boardWidth)*.5f-45);
                float x=boardWidth*.5f+side*.5f+15;
                ScreenLayout.Place(gameplayTitle.rectTransform,new Vector2(side,90),new Vector2(-x,halfH-100));
                ScreenLayout.Place(status.rectTransform,new Vector2(side,100),new Vector2(-x,100));
                ScreenLayout.Place(detail.rectTransform,new Vector2(side,100),new Vector2(-x,-20));
                ScreenLayout.Place(instructions.rectTransform,new Vector2(side,100),new Vector2(-x,-170));
            }
            else
            {
                ScreenLayout.Place(gameplayTitle.rectTransform,new Vector2(850,60),new Vector2(0,halfH-45));
                ScreenLayout.Place(status.rectTransform,new Vector2(850,45),new Vector2(0,halfH-100));
                ScreenLayout.Place(detail.rectTransform,new Vector2(850,42),new Vector2(0,-halfH+355));
                ScreenLayout.Place(instructions.rectTransform,new Vector2(850,35),new Vector2(0,-halfH+315));


            }
            ScreenLayout.Place(resourcePanel,new Vector2(Mathf.Min(m.width-40,field.width/m.scale),panelHeight),new Vector2(0,-halfH+panelHeight*.5f+10));
            resourcePanel.localScale=Vector3.one;
            LayoutResourceChannels();
            UpdateVisualizer();
        }
        public Camera GameplayView => view;
        public void SetPreviewTarget(RenderTexture target)
        {
            view.targetTexture=target; FitPresentation();
        }
        public void RenderPresentation()
        {
            Canvas.ForceUpdateCanvases(); view.Render();
        }
    }
}
