using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed class AlphaFollowupSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-alphaFollowupSmoke")>=0)
                new GameObject("Alpha followup checks").AddComponent<AlphaFollowupSmoke>();
        }
        static void Expect(bool value,string message) { if(!value)throw new Exception("Alpha followup: "+message); }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return new WaitForSecondsRealtime(.3f);
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            var overlay=game.transform.Find("Tutorial overlay");
            var message=overlay.GetComponentInChildren<TMP_Text>();
            Expect(message.text=="Your goal is to match and release elements to create a new planet.","first opening message");
            var continueButton=overlay.GetComponentInChildren<Button>(); continueButton.onClick.Invoke();
            Expect(message.text=="In the center of the cannon, you can see the sphere that is currently loaded. Aim, then tap to shoot. Connect 3 or more matching spheres to clear them.","second opening message");
            continueButton.onClick.Invoke(); yield return null;
            var amounts=new double[Enum.GetValues(typeof(ElementId)).Length];
            amounts[(int)ElementId.Fire]=amounts[(int)ElementId.Void]=620;
            var completed=GameServices.Progress.Complete(1,0,-.02f,0,null,amounts).world;
            SceneManager.LoadScene("Results"); yield return null; yield return null; yield return new WaitForSecondsRealtime(.3f);
            var results=FindAnyObjectByType<ResultsView>();
            Expect(results && results.OkButton && !results.DiscoveryTutorial,"lesson appeared before discovery prompt completed");
            float revealDeadline=Time.realtimeSinceStartup+8;
            while(results.IsRecipeReveal && results.RevealPhase!=DiscoveryRevealPhase.Ready && Time.realtimeSinceStartup<revealDeadline)yield return null;
            Expect(!results.IsRecipeReveal || results.RevealPhase==DiscoveryRevealPhase.Ready,"recipe reveal did not finish before confirmation");
            results.OkButton.onClick.Invoke();
            float deadline=Time.realtimeSinceStartup+5;
            while(!results.Settled && Time.realtimeSinceStartup<deadline)yield return null;
            Expect(results.DiscoveryTutorial && results.DiscoveryTutorial.IsOpen && Time.timeScale==0 && !results.ContinueButton.interactable,"missing paused post-discovery lesson");
            Expect(results.DiscoveryTutorial.Message=="Excellent work! An excess of Void and Fire on your world has unlocked a new element.","post-discovery wording");
            CaptureResults(results);
            results.ContinueButton.onClick.Invoke(); Expect(SceneManager.GetActiveScene().name=="Results","lesson allowed early navigation");
            results.DiscoveryTutorial.ContinueButton.onClick.Invoke();
            Expect(completed.discoveryTutorialSeen && Time.timeScale==1,"discovery lesson did not save/resume");
            results.ContinueButton.onClick.Invoke(); Expect(SceneManager.GetActiveScene().name=="Results","lesson dismissal clicked through");
            yield return null; results.ContinueButton.onClick.Invoke(); yield return null; yield return null;
            var system=FindAnyObjectByType<SystemView>(); var dialog=system.Dialog;
            Expect(dialog && dialog.IsOpen && Time.timeScale==0,"next-world lesson missing");
            Expect(dialog.Message=="With 3 elements now in your collection, you have enough for the next level." && dialog.PointerTarget==system.planets[1].transform && dialog.PointerVisible,"next-world wording/pointer");
            CaptureSystem(system);
            system.OpenPlanet(1); Expect(SceneManager.GetActiveScene().name=="System","next-world modal allowed navigation");
            dialog.ContinueButton.onClick.Invoke();
            Expect(completed.nextWorldTutorialSeen && Time.timeScale==1,"next-world dismissal did not save/resume");
            system.OpenPlanet(0); Expect(SceneManager.GetActiveScene().name=="System","next-world Continue clicked through");
            yield return null; system.OpenPlanet(0); yield return null; yield return null;
            results=FindAnyObjectByType<ResultsView>();
            Expect(results && results.Settled && !results.DiscoveryTutorial && results.DiscoveryPlayCount==0 && results.DiscoverySpheres.Count==1,"completed results repeated the discovery lesson or lost history");
            results.ContinueButton.onClick.Invoke(); yield return null; yield return null;
            system=FindAnyObjectByType<SystemView>();
            Expect(!system.Dialog || !system.Dialog.IsOpen,"next-world lesson repeated");
            system.OpenPlanet(1); yield return null; yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            Expect(prep && prep.Title.text=="Genesis Beta" && (!prep.SetupPopup || !prep.SetupPopup.IsOpen),"next-world target did not open Beta setup");
            Debug.Log("ECOSPHERE_ALPHA_FOLLOWUP_PASSED: exact opening copy, post-discovery ordering, paused input, next-world pointer, history and no repeat lessons");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        static void CaptureResults(ResultsView results)
        {
            foreach(var size in new[] { new Vector2Int(900,1600),new Vector2Int(1600,900) })
                Capture(results.View,results.DiscoveryTutorial.DialogCanvas,size,"Alpha-results-lesson",()=> {results.Reflow(); results.DiscoveryTutorial.RefreshLayout(results.View);});
        }
        static void CaptureSystem(SystemView system)
        {
            var canvas=system.GetComponent<Canvas>(); var mode=canvas.renderMode; var camera=canvas.worldCamera;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=Camera.main; canvas.planeDistance=5;
            foreach(var size in new[] { new Vector2Int(900,1600),new Vector2Int(1600,900) })
                Capture(Camera.main,system.Dialog.DialogCanvas,size,"Alpha-next-world-lesson",()=> {system.GetComponent<SystemResponsiveLayout>().Apply(Camera.main); system.Dialog.RefreshLayout(Camera.main);});
            canvas.renderMode=mode; canvas.worldCamera=camera; system.GetComponent<SystemResponsiveLayout>().Apply(Camera.main);
        }
        static void Capture(Camera camera,Canvas dialog,Vector2Int size,string name,Action layout)
        {
            var target=new RenderTexture(size.x,size.y,24); target.Create();
            var previousTarget=camera.targetTexture; float aspect=camera.aspect;
            var previousActive=RenderTexture.active;
            dialog.renderMode=RenderMode.ScreenSpaceCamera; dialog.worldCamera=camera; dialog.planeDistance=1;
            camera.targetTexture=target; camera.aspect=(float)size.x/size.y; camera.Render(); layout(); Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture.active=target;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/"+name+"-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());
            RenderTexture.active=previousActive; camera.targetTexture=previousTarget; camera.aspect=aspect;
            dialog.renderMode=RenderMode.ScreenSpaceOverlay; dialog.worldCamera=null;
            target.Release(); Destroy(target); Destroy(image); layout();
        }
    }
}
