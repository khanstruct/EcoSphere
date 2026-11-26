using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void WinForStarRevealSmoke()
        {
            totals[(int)ElementId.Fire]=1500;totals[(int)ElementId.Void]=1000;
            complete=true;ready=false;StartCoroutine(CompleteWorld());
        }
    }
    public sealed class StarRevealSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(LaunchFlags.Has("-starRevealSmoke"))
                new GameObject("Star reveal checks").AddComponent<StarRevealSmoke>();
        }
        static void Expect(bool value,string message){if(!value)throw new Exception("Star reveal: "+message);}
        // Overlay canvases cannot render offscreen, so the review image briefly routes the rating canvas through a camera, then restores it.
        static IEnumerator CaptureRating(StarRatingPresentation rating)
        {
            var canvas=rating.GetComponent<Canvas>();
            var camera=new GameObject("Rating capture camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(900,1600,24);target.Create();camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.transform.position=new Vector3(0,-5000,0);camera.nearClipPlane=.1f;camera.farClipPlane=2;camera.cullingMask=~0;
            yield return null;yield return null;Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(900,1600,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,900,1600),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/StarReveal-continue.png"),pixels.EncodeToPNG());
            RenderTexture.active=previous;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;camera.targetTexture=null;
            target.Release();Destroy(target);Destroy(pixels);Destroy(camera.gameObject);
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();var audio=game.GetComponent<LevelAudio>();
            Expect(GameAudioSettings.Current.starScore,"missing star-score audio");
            game.WinForStarRevealSmoke();
            float deadline=Time.realtimeSinceStartup+25;
            StarRatingPresentation rating=null;
            while(!(rating=FindAnyObjectByType<StarRatingPresentation>()) && Time.realtimeSinceStartup<deadline)yield return null;
            Expect(rating && !audio.IsAnnouncing,"rating must follow completed world-complete announcement");
            int seen=0;float previous=Time.realtimeSinceStartup;
            while(!rating.RevealComplete && Time.realtimeSinceStartup<deadline)
            {
                Expect(SceneManager.GetActiveScene().name!="Results","results opened before Continue");
                if(rating.RevealedStars>seen)
                {
                    if(seen>0)Expect(Time.realtimeSinceStartup-previous<.8f,"star reveal waited for audio");
                    previous=Time.realtimeSinceStartup;seen=rating.RevealedStars;
                }
                yield return null;
            }
            Expect(rating.RevealComplete && rating.RevealedStars==3 && rating.SoundCount==3 && rating.ContinueButton.interactable,"three stars/audio/Continue did not finish");
            yield return new WaitForSecondsRealtime(.7f);
            Expect(SceneManager.GetActiveScene().name!="Results","rating advanced without input");
            Canvas.ForceUpdateCanvases();
            // Continue is enlarged and centered just beneath the star row (screen-space corners, so scale counts).
            var corners=new Vector3[4];((RectTransform)rating.ContinueButton.transform).GetWorldCorners(corners);
            float buttonLeft=corners[0].x,buttonRight=corners[2].x,buttonBottom=corners[0].y,buttonTop=corners[1].y;
            float starsLeft=float.MaxValue,starsRight=float.MinValue,starsBottom=float.MaxValue;
            foreach(var star in rating.GetComponentsInChildren<Image>())
                if(star.name.StartsWith("Earned star")){star.rectTransform.GetWorldCorners(corners);starsLeft=Mathf.Min(starsLeft,corners[0].x);starsRight=Mathf.Max(starsRight,corners[2].x);starsBottom=Mathf.Min(starsBottom,corners[0].y);}
            float scale=((RectTransform)rating.transform).lossyScale.x,gap=(starsBottom-buttonTop)/scale;
            Expect(Mathf.Abs((buttonLeft+buttonRight)*.5f-(starsLeft+starsRight)*.5f)<1,"Continue must be centered under the stars");
            Expect(gap>0 && gap<80,"Continue must sit just beneath the stars (gap "+gap+")");
            Expect((buttonRight-buttonLeft)/scale>=360*1.3f && buttonBottom>0,"Continue must be enlarged and on screen");
            yield return CaptureRating(rating);
            var pointerPosition=RectTransformUtility.WorldToScreenPoint(null,rating.ContinueButton.transform.position);
            var pointer=new PointerEventData(EventSystem.current){position=pointerPosition};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Expect(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(rating.ContinueButton.transform),"Continue is blocked from pointer raycasts");
            var mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pointerPosition});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pointerPosition,buttons=1});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pointerPosition});yield return null;yield return null;yield return null;
            Expect(SceneManager.GetActiveScene().name=="Results","real pointer Continue did not open Results (scene "+SceneManager.GetActiveScene().name+")");
            var played=WorldLevels.Get(GameServices.Session.SystemNumber,GameServices.Session.PlanetIndex);
            Expect(played.highestScore==2500,"saved score "+played.highestScore+" is not 2500 on the played planet");
            Debug.Log("ECOSPHERE_STAR_REVEAL_PASSED: world-complete first, three staggered stars/sounds, real pointer Continue through UI input and saved score");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
