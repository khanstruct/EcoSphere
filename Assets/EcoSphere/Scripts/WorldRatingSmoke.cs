using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed class WorldRatingSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-worldRatingSmoke")>=0)
                new GameObject("World rating checks").AddComponent<WorldRatingSmoke>();
        }
        static void Expect(bool condition,string message){if(!condition)throw new Exception("World rating: "+message);}
        IEnumerator Start()
        {
            yield return null;yield return null;
            var view=FindAnyObjectByType<SystemView>();
            double[] scores={499.75,500,1000,2500};
            for(int i=0;i<scores.Length;i++)
            {
                var record=WorldLevels.Get(1,i);
                Expect(record.rules.oneStarScore==500 && record.rules.twoStarScore==1000 && record.rules.threeStarScore==2500,"default thresholds missing");
                var amounts=new double[ElementComposition.Capacity];amounts[(int)ElementId.Void]=scores[i]*.25;amounts[(int)ElementId.Fire]=scores[i]*.75;
                GameServices.Progress.Complete(1,i,0,0,null,amounts);
                Expect(record.highestScore==scores[i] && record.EarnedStars==i,"score or threshold boundary incorrect");
                var restored=JsonUtility.FromJson<WorldRecord>(JsonUtility.ToJson(record));
                Expect(restored.highestScore==scores[i] && restored.EarnedStars==i,"world score/rules serialization");
                GameServices.Progress.TryOpenCompletedResults(1,i);
                Expect(record.highestScore==scores[i],"viewing results changed highest score");
            }
            view.RefreshSystemHeading(); Check(view);
            view.SetSystemNumber(2);
            foreach(var planet in view.planets)
            {
                var badges=planet.transform.Find("Sphere Requirement");
                Expect(badges.Find("Sphere Icon").gameObject.activeSelf && !badges.Find("World Stars").gameObject.activeSelf,"stale completed badges in another system");
            }
            view.SetSystemNumber(1);view.RefreshSystemHeading();Check(view);
            var rules=WorldLevels.Get(1,1).rules;rules.twoStarScore=500;view.RefreshSystemHeading();
            Expect(WorldLevels.Get(1,1).EarnedStars==2,"rating ignored per-world thresholds");
            CheckStars(view.planets[1].transform,2);
            Capture(view);
            Debug.Log("ECOSPHERE_WORLD_RATING_PASSED: fractional elemental scores, threshold boundaries, saved highest scores/rules, result revisits, completed badge replacement and system switching");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        static void Check(SystemView view)
        {
            for(int i=0;i<4;i++)CheckStars(view.planets[i].transform,i);
            Expect(view.planets[4].transform.Find("Sphere Requirement/Sphere Icon").gameObject.activeSelf,"unfinished world lost requirements");
        }
        static void CheckStars(Transform planet,int expected)
        {
            var badges=planet.Find("Sphere Requirement");
            Expect(!badges.Find("Sphere Icon").gameObject.activeSelf && !badges.Find("Sphere Count").gameObject.activeSelf && !badges.Find("World Modifiers").gameObject.activeSelf,"completed traits still visible");
            int count=0;
            foreach(Transform child in badges.Find("World Stars"))
            {
                if(!child.gameObject.activeSelf)continue;
                var image=child.GetComponent<Image>();
                Expect(image.sprite && image.sprite==WorldCatalog.Current.ratingStar && !image.raycastTarget,"star artwork or click handling incorrect");
                count++;
            }
            Expect(count==expected,"incorrect visible star count");
        }
        static void Capture(SystemView view)
        {
            var camera=Camera.main;var target=new RenderTexture(900,1600,24);target.Create();camera.targetTexture=target;
            var canvas=view.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            view.GetComponent<SystemResponsiveLayout>().Apply(camera);Canvas.ForceUpdateCanvases();
            foreach(var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None))if(preview.PreviewCamera)preview.PreviewCamera.Render();
            camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(900,1600,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,900,1600),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/WorldRatings.png"),pixels.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(pixels);
        }
    }
}
