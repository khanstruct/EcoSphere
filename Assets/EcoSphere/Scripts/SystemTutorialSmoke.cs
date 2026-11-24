using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed class SystemTutorialSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (LaunchFlags.Has("-systemTutorialSmoke"))
                new GameObject("System tutorial checks").AddComponent<SystemTutorialSmoke>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null; yield return null;
            var system = FindAnyObjectByType<SystemView>(); var dialog = system.Dialog;
            Expect(SceneUtility.GetScenePathByBuildIndex(0).EndsWith("System.unity"),"System must be the game entry scene");
            Expect(dialog && dialog.IsOpen && dialog.IsIntroduction && Time.timeScale == 0,"first System did not pause for its introduction");
            Expect(dialog.Message == "Welcome to EcoSphere. This is your first Star System.","welcome wording");
            Expect(!dialog.PointerVisible && GameServices.Progress.AvailableSphereCount == 2,"welcome pointer or starting sphere count");
            Reachable(dialog); Capture(system,dialog,"welcome");
            system.OpenPlanet(0); Expect(SceneManager.GetActiveScene().name == "System","welcome did not shield world selection");
            int clicks = UIClickAudio.PlayCount;
            dialog.ContinueButton.onClick.Invoke();
            Expect(UIClickAudio.PlayCount == clicks+1,"paused Continue click audio");
            Expect(dialog.Message == "You currently only have two Elemental Spheres, so click on the world with a Sphere Requirement of two.","requirement wording");
            Expect(dialog.IsOpen && Time.timeScale == 0 && dialog.PointerVisible && dialog.PointerTarget == system.planets[0].transform,"requirement pointer or pause");
            Reachable(dialog); Capture(system,dialog,"requirement");
            dialog.ContinueButton.onClick.Invoke();
            Expect(!dialog.IsOpen && Time.timeScale == 1 && GameServices.Progress.SystemIntroductionSeen,"introduction did not dismiss/resume");
            system.OpenPlanet(0); Expect(SceneManager.GetActiveScene().name == "System","Continue clicked through to a world");
            yield return null;
            system.OpenPlanet(1);
            Expect(SceneManager.GetActiveScene().name == "System" && dialog.IsOpen && !dialog.IsIntroduction && Time.timeScale == 0,"insufficient spheres did not block entry");
            Expect(dialog.Message == "You do not have enough spheres for this world. Unlock new elements and try again.","insufficient-spheres wording");
            Expect(!dialog.PointerVisible,"insufficient popup retained tutorial pointer");
            Reachable(dialog); Capture(system,dialog,"insufficient");
            system.OpenPlanet(0); Expect(SceneManager.GetActiveScene().name == "System","insufficient popup did not shield selection");
            dialog.ContinueButton.onClick.Invoke();
            Expect(!dialog.IsOpen && Time.timeScale == 1,"insufficient popup did not dismiss/unpause");
            system.OpenPlanet(0); Expect(SceneManager.GetActiveScene().name == "System","popup dismissal clicked through");
            yield return null;
            SceneManager.LoadScene("System"); yield return null; yield return null;
            system = FindAnyObjectByType<SystemView>();
            Expect(!system.Dialog && Time.timeScale == 1,"introduction repeated on a System revisit");
            var gains = new double[Enum.GetValues(typeof(ElementId)).Length];
            gains[(int)ElementId.Fire] = gains[(int)ElementId.Void] = 620;
            GameServices.Progress.Complete(1,0,-.02f,0,null,gains); GameServices.Progress.ClearPending();
            Expect(GameServices.Progress.AvailableSphereCount == 3,"unlock did not change the collection count");
            system.OpenPlanet(1); yield return null; yield return null;
            var prep = FindAnyObjectByType<LevelPrepView>();
            Expect(prep && prep.Title.text == "Genesis Beta" && LevelPrepView.RequiredSpheres == 3,"sufficient collection did not reach setup");
            GameObject.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return null; yield return null;
            system = FindAnyObjectByType<SystemView>();
            var completed = GameServices.Progress.Complete(2,0,.4f,50,null).world; GameServices.Progress.ClearPending();
            system.SetSystemNumber(2);
            Expect(GameServices.Progress.AvailableSphereCount < WorldLevels.Get(2,0).RequiredSpheres,"completed-world bypass precondition");
            system.OpenPlanet(0); yield return null; yield return null;
            Expect(SceneManager.GetActiveScene().name == "Results" && GameServices.Progress.Pending.world == completed,"collection gate blocked completed-world results");
            Debug.Log("ECOSPHERE_SYSTEM_TUTORIAL_SMOKE_PASSED: exact wording, two-step intro, first-world pointer, pause/shield/click protection, one-time display, insufficient/sufficient collections and completed-world results");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static void Expect(bool value,string message) { if (!value) throw new Exception("System tutorial: " + message); }
        static void Reachable(SystemDialog dialog)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null,dialog.ContinueButton.transform.position) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            Expect(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(dialog.ContinueButton.transform),"Continue is behind its input shield");
        }
        static void Capture(SystemView system,SystemDialog dialog,string name)
        {
            var camera = Camera.main; var mainCanvas = system.GetComponent<Canvas>(); var overlay = dialog.DialogCanvas;
            float aspect = camera.aspect;
            foreach (var size in new[] { new Vector2Int(900,1600),new Vector2Int(1600,900) })
            {
                var target = new RenderTexture(size.x,size.y,24); target.Create(); camera.targetTexture = target; camera.aspect = (float)size.x/size.y;
                mainCanvas.renderMode = overlay.renderMode = RenderMode.ScreenSpaceCamera;
                mainCanvas.worldCamera = overlay.worldCamera = camera; mainCanvas.planeDistance = 5; overlay.planeDistance = 1;
                system.GetComponent<SystemResponsiveLayout>().Apply(camera); Canvas.ForceUpdateCanvases();
                // Refresh camera/canvas projection after changing the capture aspect ratio.
                camera.Render(); dialog.RefreshLayout(camera);
                foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases(); camera.Render(); var old = RenderTexture.active; RenderTexture.active = target;
                var pixels = new Texture2D(size.x,size.y,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/SystemTutorial-"+name+"-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                RenderTexture.active = old; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(pixels);
            }
            camera.aspect = aspect; mainCanvas.renderMode = overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            system.GetComponent<SystemResponsiveLayout>().Apply(); dialog.RefreshLayout(); Canvas.ForceUpdateCanvases();
        }
    }
}
