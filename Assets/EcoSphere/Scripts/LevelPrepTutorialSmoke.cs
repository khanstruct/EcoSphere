using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed partial class LevelPrepView
    {
        IEnumerator SetupTutorialSmoke()
        {
            yield return null;
            void Expect(bool value,string message) { if(!value)throw new Exception("Setup tutorial: "+message); }
            Expect(setupPopup && setupPopup.IsOpen && Time.timeScale==0 && slots.Count==2,"initial paused lesson or two empty slots missing");
            Expect(setupPopup.Message=="Each world requires a specific number of spheres. This level requires two, so it shows two empty slots." && setupPopup.Target==slots[0],"slot lesson text/target");
            AddSphere(SphereIds.Fire); Expect(Selected.Count==0,"modal allowed sphere selection");
            CaptureSetupTutorial("slots");
            setupPopup.ContinueButton.onClick.Invoke();
            Expect(setupPopup.IsOpen && Time.timeScale==0 && setupPopup.Message=="This area shows all Elemental Spheres you've discovered. Like the universe, you begin with only Fire and Void.","collection lesson");
            Expect(setupPopup.Target==(RectTransform)root.Find("Discovered Spheres/Collection viewport"),"collection pointer target");
            CaptureSetupTutorial("collection");
            setupPopup.ContinueButton.onClick.Invoke();
            Expect(setupPopup.Message=="Tap each of your spheres to add them to the level." && Time.timeScale==0,"selection instructions");
            setupPopup.ContinueButton.onClick.Invoke();
            Expect(!setupPopup.IsOpen && Time.timeScale==1,"selection was not resumed");
            AddSphere(SphereIds.Fire); Expect(Selected.Count==0,"Continue clicked through to selection");
            yield return null;
            AddSphere(SphereIds.Fire); Expect(Selected.Count==1 && !setupPopup.IsOpen,"final lesson appeared before both selections");
            AddSphere(SphereIds.Void);
            Expect(Selected.Count==2 && setupPopup.IsOpen && Time.timeScale==0 && setupPopup.Target==Begin.GetComponent<RectTransform>(),"Begin lesson trigger/target");
            Expect(setupPopup.Message=="Great work! Now tap the Begin button to play the level.","Begin lesson text");
            yield return null;
            CaptureSetupTutorial("begin");
            var pointer=new PointerEventData(EventSystem.current) { position=RectTransformUtility.WorldToScreenPoint(view,Begin.transform.position) };
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            Expect(hits.Count>0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==Begin,"final shield blocked the Begin button");
            // Continue is optional for the final callout: Begin can launch directly through it.
            DontDestroyOnLoad(gameObject);
            Begin.onClick.Invoke();
            yield return null; yield return null; yield return new WaitForSecondsRealtime(.3f);
            Expect(SceneManager.GetActiveScene().name=="Tutorial1","Begin did not launch Genesis Alpha");
            Expect(GameServices.Progress.SetupTutorialStep(world.id)==4,"Begin did not finish the tutorial progress");
            Debug.Log("ECOSPHERE_SETUP_TUTORIAL_PASSED: exact messages, pointers, paused input, selection gating, click-through guard, direct Begin and responsive previews");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        void CaptureSetupTutorial(string name)
        {
            foreach(var size in new[] { new Vector2Int(900,1600),new Vector2Int(1600,900) })
            {
                var target=new RenderTexture(size.x,size.y,24); target.Create();
                var previousTarget=view.targetTexture; float previousAspect=view.aspect;
                var previousActive=RenderTexture.active;
                setupPopup.DialogCanvas.renderMode=RenderMode.ScreenSpaceCamera; setupPopup.DialogCanvas.worldCamera=view; setupPopup.DialogCanvas.planeDistance=1;
                view.targetTexture=target; view.aspect=(float)size.x/size.y; view.Render();
                Reflow(); Canvas.ForceUpdateCanvases(); setupPopup.RefreshLayout(view); Canvas.ForceUpdateCanvases();
                foreach(var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None)) if(preview.PreviewCamera)preview.PreviewCamera.Render();
                view.Render();
                RenderTexture.active=target;
                var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Setup-tutorial-"+name+"-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());
                RenderTexture.active=previousActive; view.targetTexture=previousTarget; view.aspect=previousAspect;
                setupPopup.DialogCanvas.renderMode=RenderMode.ScreenSpaceOverlay; setupPopup.DialogCanvas.worldCamera=null;
                target.Release(); Destroy(target); Destroy(image); Reflow(); setupPopup.RefreshLayout();
            }
        }
    }
}
