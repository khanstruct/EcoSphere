using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Checks a sphere entirely from its definition: board presence and body shape, element yield through the real
        // removal path, one visualizer bar per contributed element in the shared colors, streak color and voice.
        internal void CheckSphereIntro(SphereDefinition sphere)
        {
            void Expect(bool value,string message){if(!value)throw new Exception(sphere.displayName+" intro: "+message);}
            var cells=new List<Vector2Int>();foreach(var cell in board.Cells)if(cell.Value==sphere.id)cells.Add(cell.Key);
            Expect(cells.Count>=2,"sphere missing from the selected board");
            foreach(var cell in cells)Expect(visuals[cell].GetComponent<SphereAppearance>().shape==sphere.shape,"board body does not follow the sphere shape");
            var before=(double[])totals.Clone();
            var visual=visuals[cells[0]];
            RemoveCells(new HashSet<Vector2Int>{cells[0]},ElementScoring.Breakaway,true);UpdateVisualizer();
            var expected=new double[totals.Length];sphere.AddContributions(expected,ElementScoring.Breakaway);
            for(int i=0;i<totals.Length;i++)Expect(Math.Abs(totals[i]-before[i]-expected[i])<1e-9,"breakaway yield wrong for "+(ElementId)i);
            foreach(var entry in sphere.contributions)
            {
                int bar=displayedElements.IndexOf(entry.element);
                Expect(bar>=0 && energyBars[bar].color==Catalog.ElementColor(entry.element) && resourceLabels[bar].text==Catalog.ElementName(entry.element),"missing or miscolored "+entry.element+" bar");
            }
            Expect(Catalog.ElementColor(sphere.namesakeElement)==sphere.color,"namesake element must use the sphere color");
            Expect(visual.GetComponent<TrailRenderer>().startColor==sphere.color,"streak must use the sphere color");
            Expect(sphere.resourceVoice && LevelAudio.MatchVoice(sphere.id,GameAudioSettings.Current.minimumMatchedSpheresForVoice)==sphere.resourceVoice,"large matches must play the sphere voice");
        }
    }

    // -sphereIntroSmoke <sphere id>: DEV/normal availability, setup selection, gameplay data and preview for one sphere.
    public sealed class SphereIntroSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-sphereIntroSmoke"))new GameObject("Sphere intro checks").AddComponent<SphereIntroSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            var sphere=SphereCatalog.Current.Get(int.Parse(LaunchFlags.Value("-sphereIntroSmoke")));
            void Expect(bool value,string message){if(!value)throw new Exception(sphere.displayName+" intro: "+message);}
            Expect(GameServices.Progress.IsSphereAvailable(sphere.id),"unavailable in the DEV collection");
            var settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previous=settings.devModeEnabled;
            try{settings.devModeEnabled=false;Expect(GameServices.Progress.IsSphereAvailable(sphere.id)==sphere.availableAtStart,"normal availability must follow availableAtStart");}
            finally{settings.devModeEnabled=previous;}
            FindAnyObjectByType<SystemView>().OpenPlanet(3);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            var picks=new List<int>(SphereCatalog.Current.openingSpheres);picks.Add(SphereIds.Stone);picks.Add(sphere.id);
            foreach(int id in picks)prep.AddSphere(id);
            Expect(prep.Begin.interactable,"not selectable in setup");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            for(float t=0;t<1.5f;t+=Time.deltaTime)yield return null;
            game.CheckSphereIntro(sphere);
            // Preview render for review, built from the definition like setup and results.
            var canvas=new GameObject("Intro preview canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var image=new GameObject("Intro preview",typeof(RectTransform),typeof(RawImage));image.transform.SetParent(canvas.transform,false);
            var preview=image.AddComponent<SpherePreview>();preview.definition=sphere;
            for(float t=0;t<1f;t+=Time.deltaTime)yield return null;
            preview.PreviewCamera.Render();
            var texture=(RenderTexture)image.GetComponent<RawImage>().texture;var active=RenderTexture.active;RenderTexture.active=texture;
            var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/"+sphere.displayName+"-preview.png"),pixels.EncodeToPNG());
            RenderTexture.active=active;Destroy(pixels);Destroy(canvas.gameObject);
            Debug.Log("ECOSPHERE_SPHERE_INTRO_PASSED: "+sphere.displayName+" availability, setup, board shape, yield "+sphere.ContributionSummary()+", bars/colors, streak, voice, preview");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
