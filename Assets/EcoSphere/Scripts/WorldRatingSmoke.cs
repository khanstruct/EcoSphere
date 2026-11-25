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
            if(LaunchFlags.Has("-worldRatingSmoke"))
                new GameObject("World rating checks").AddComponent<WorldRatingSmoke>();
        }
        static void Expect(bool condition,string message){if(!condition)throw new Exception("World rating: "+message);}
        IEnumerator Start()
        {
            yield return null;yield return null;
            var view=FindAnyObjectByType<SystemView>();
            // Each world's thresholds are shared fractions of its par; test just below one star, then exactly at each star.
            var scores=new double[4];
            for(int i=0;i<scores.Length;i++)
            {
                var record=WorldLevels.Get(1,i);
                scores[i]=i==0 ? record.rules.StarScore(1)-.25 : record.rules.StarScore(i);
                Expect(record.rules.StarScore(1)>0 && record.rules.StarScore(2)>record.rules.StarScore(1) && record.rules.StarScore(3)>record.rules.StarScore(2),"star thresholds must increase");
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
            // A smaller starting cluster has a smaller par, so the same saved score earns more stars.
            var rules=WorldLevels.Get(1,1).rules;rules.clusterRings=2;view.RefreshSystemHeading();
            Expect(WorldLevels.Get(1,1).EarnedStars==2,"rating ignored the world's board size");
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
