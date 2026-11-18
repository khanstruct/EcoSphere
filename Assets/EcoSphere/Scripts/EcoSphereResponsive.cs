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
        RectTransform secondaryGoal, secondaryAbundance;
        bool widePresentation;
        Vector2 previousPresentation;
        Rect previousPresentationSafe;

        void FitPresentation()
        {
            int width=view.targetTexture?view.targetTexture.width:Screen.width;
            int height=view.targetTexture?view.targetTexture.height:Screen.height;
            Rect safe=view.targetTexture?new Rect(0,0,width,height):Screen.safeArea;
            Rect field=ScreenLayout.Playfield(safe);
            playableScreen=field;
            float worldPerPixel=9.3f/field.width;
            view.rect=new Rect(0,0,1,1);
            view.aspect=(float)width/height;
            view.orthographicSize=height*worldPerPixel*.5f;
            view.transform.position=new Vector3(-(safe.center.x-width*.5f)*worldPerPixel,-(safe.center.y-height*.5f)*worldPerPixel,-25);
            view.cullingMask=~0;
            view.clearFlags=CameraClearFlags.SolidColor;
            if(!gameplayCanvas)return;
            var m=ScreenLayout.Apply(gameplayCanvas,gameplayRoot,view);
            if(previousPresentation==new Vector2(m.width,m.height)&&previousPresentationSafe==safe)return;
            previousPresentation=new Vector2(m.width,m.height); previousPresentationSafe=safe;
            widePresentation=m.wide;
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
                ScreenLayout.Place(resourcePanel,new Vector2(440,470),new Vector2(x,0));
                resourcePanel.localScale=Vector3.one*Mathf.Min(1,side/440);
                for(int type=0;type<2;type++)for(int i=0;i<10;i++)
                    bars[type][i].anchoredPosition=new Vector2(-175+i*39,type==0?285:70);
                iceLabel.rectTransform.anchorMin=iceLabel.rectTransform.anchorMax=new Vector2(.5f,0);
                sunLabel.rectTransform.anchorMin=sunLabel.rectTransform.anchorMax=new Vector2(.5f,0);
                iceLabel.rectTransform.anchoredPosition=new Vector2(0,260);
                sunLabel.rectTransform.anchoredPosition=new Vector2(0,45);
                iceLabel.fontSize=sunLabel.fontSize=24;
                goalLine.sizeDelta=new Vector2(390,2);
                abundanceLine.sizeDelta=new Vector2(390,2);
                if(!secondaryGoal)secondaryGoal=Panel("Sunlight resource goal",resourcePanel,new Vector2(.5f,0),Vector2.zero,new Vector2(390,2),Color.white);
                secondaryGoal.gameObject.SetActive(true);
                if(!secondaryAbundance)secondaryAbundance=Panel("Sunlight excess abundance",resourcePanel,new Vector2(.5f,0),Vector2.zero,new Vector2(390,2),new Color(1,.76f,.24f));
                secondaryAbundance.gameObject.SetActive(true);
            }
            else
            {
                ScreenLayout.Place(gameplayTitle.rectTransform,new Vector2(850,60),new Vector2(0,halfH-45));
                ScreenLayout.Place(status.rectTransform,new Vector2(850,45),new Vector2(0,halfH-100));
                ScreenLayout.Place(detail.rectTransform,new Vector2(850,42),new Vector2(0,-halfH+525));
                ScreenLayout.Place(instructions.rectTransform,new Vector2(850,35),new Vector2(0,-halfH+485));
                ScreenLayout.Place(resourcePanel,new Vector2(860,180),new Vector2(0,-halfH+95));
                resourcePanel.localScale=Vector3.one;
                for(int type=0;type<2;type++)for(int i=0;i<10;i++)bars[type][i].anchoredPosition=new Vector2(-398+type*420+i*39,36);
                iceLabel.rectTransform.anchorMin=iceLabel.rectTransform.anchorMax=new Vector2(.25f,0);
                sunLabel.rectTransform.anchorMin=sunLabel.rectTransform.anchorMax=new Vector2(.75f,0);
                iceLabel.rectTransform.anchoredPosition=sunLabel.rectTransform.anchoredPosition=new Vector2(0,16);
                iceLabel.fontSize=sunLabel.fontSize=18;
                goalLine.sizeDelta=new Vector2(825,2);
                abundanceLine.sizeDelta=new Vector2(825,2);
                if(secondaryGoal)secondaryGoal.gameObject.SetActive(false);
                if(secondaryAbundance)secondaryAbundance.gameObject.SetActive(false);
            }
        }
        void PositionResourceGoals()
        {
            if(!widePresentation)return;
            float scale=Mathf.Max(resourceGoal+1,abundanceGoal)*1.2f;
            float threshold=Mathf.Max(1,resourceGoal)/scale*125;
            float excess=Mathf.Max(resourceGoal+1,abundanceGoal)/scale*125;
            goalLine.anchoredPosition=new Vector2(0,285+threshold);
            if(secondaryGoal)secondaryGoal.anchoredPosition=new Vector2(0,70+threshold);
            abundanceLine.anchoredPosition=new Vector2(0,285+excess);
            if(secondaryAbundance)secondaryAbundance.anchoredPosition=new Vector2(0,70+excess);
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
