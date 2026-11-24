using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            Expect(SceneManager.GetActiveScene().name=="Results" && WorldLevels.Get(1,0).highestScore==2500,"Continue failed to open saved Results");
            Debug.Log("ECOSPHERE_STAR_REVEAL_PASSED: world-complete first, three staggered stars/sounds, real pointer Continue through UI input and saved score");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
