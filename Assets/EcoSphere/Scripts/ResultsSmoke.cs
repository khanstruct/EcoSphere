using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CompleteForResultsSmoke()
        {
            if (!LaunchFlags.Has("-resultsSmoke")) throw new InvalidOperationException();
            if(tutorialPaused) ContinueTutorial();
            RemoveCells(new HashSet<Vector2Int>(board.Cells.Keys), ElementScoring.Breakaway, true);
            complete = true; ready = false;
            if (loaded) { Destroy(loaded); loaded = null; }
            StartCoroutine(FinishTurn());
        }
    }

    public sealed class ResultsSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (LaunchFlags.Has("-resultsSmoke"))
                new GameObject("Results checks").AddComponent<ResultsSmoke>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return new WaitForSecondsRealtime(.3f);
            LevelPrepView.SystemNumber = 2; LevelPrepView.PlanetIndex = 1;
            foreach(var natural in WorldLevels.Get(1,1).naturalSpheres)WorldLevels.Get(2,1).naturalSpheres.Add(natural.Copy());
            var game = FindAnyObjectByType<EcoSpherePrototype>();
            if (game.RecipesEnabled || game.abundanceGoal < 3000000)
                throw new Exception("Genesis Beta must award no recipes and keep its raised excess threshold");
            var expectedMaterial = game.receivingPlanetMaterial;
            var announcements = game.GetComponent<LevelAudio>();
            announcements.AnnounceResources(LevelAudio.SphereVoiceMinimumPoints, 0);
            announcements.transform.Find("Level result announcer").GetComponent<AudioSource>().pitch = .85f;
            game.CompleteForResultsSmoke();
            float deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != "Results" && Time.realtimeSinceStartup < deadline) { var rating=FindAnyObjectByType<StarRatingPresentation>(); if(rating && rating.RevealComplete)rating.ContinueButton.onClick.Invoke(); yield return null; }
            if (SceneManager.GetActiveScene().name != "Results") throw new Exception("Winning did not open Results");
            if (announcements.IsAnnouncing) throw new Exception("Results cut off a queued completion announcement");
            yield return new WaitForSecondsRealtime(.4f);
            var results = FindAnyObjectByType<ResultsView>();
            if (!results || results.DiscoverySpheres.Count != 1 || results.DiscoverySpheres[0].name != "Stone discovered sphere")
                throw new Exception("Stone discovery overlay missing after a successful level");
            if (results.DiscoveryPlayCount != 1 || results.ContinueButton.interactable || results.Settled)
                throw new Exception("Discovery sound or Continue gating incorrect");
            CheckGlobe(results, expectedMaterial);
            var stoneDescription = GameObject.Find("Stone description")?.GetComponent<TMPro.TMP_Text>();
            if (!stoneDescription || !stoneDescription.gameObject.activeInHierarchy || stoneDescription.text != SphereCatalog.Current.Get(SphereIds.Stone).description)
                throw new Exception("Discovered sphere description missing from the discovery overlay");
            Capture(results, "stone-overlay", 900, 1600);
            Capture(results, "stone-overlay-wide", 1600, 900);
            int clicks = UIClickAudio.PlayCount;
            results.OkButton.onClick.Invoke();
            if (UIClickAudio.PlayCount != clicks + 1 || results.ContinueButton.interactable)
                throw new Exception("Ok click or collection animation gating incorrect");
            deadline = Time.realtimeSinceStartup + 5;
            while (!results.Settled && Time.realtimeSinceStartup < deadline) yield return null;
            if (!results.Settled || !results.ContinueButton.interactable || results.DiscoveryPlayCount != 1 ||
                results.DiscoverySpheres[0].parent.name != "Stone collected" || Mathf.Abs(results.DiscoverySpheres[0].sizeDelta.x - 100) > .01f)
                throw new Exception($"Stone settle: settled={results.Settled} continue={results.ContinueButton.interactable} audio={results.DiscoveryPlayCount} parent={results.DiscoverySpheres[0].parent.name} size={results.DiscoverySpheres[0].sizeDelta}");
            Capture(results, "stone-collected", 900, 1600);
            var stored = GameServices.Progress.Find(2, 1);
            if (stored == null) throw new Exception("Completed world was not recorded");
            results.ContinueButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            var system = FindAnyObjectByType<SystemView>();
            if (!system || system.systemNumber != 2) throw new Exception("Continue did not return to the correct system");
            var completed = system.planets[1].transform.Find("Completed world");
            if (!completed) throw new Exception("Completed planet was not displayed on the map");
            var preview = completed.GetComponent<SpherePreview>();
            if(preview.planetStats!=stored.planetStats)throw new Exception("System globe lost saved planet stats");
            if (preview.surfaceMaterial != expectedMaterial || preview.seaLevel != stored.seaLevel || preview.stoneRuggedness != stored.stoneRuggedness || preview.iceCoverage != stored.iceCoverage || preview.fireHeat!=stored.fireHeat || preview.atmosphereColor!=stored.atmosphereColor || preview.atmosphereStrength!=stored.atmosphereStrength || preview.atmosphereWidth!=stored.atmosphereWidth)
                throw new Exception("Map world appearance differs from the completed gameplay world");
            var rotation = preview.SurfaceRotation;
            yield return new WaitForSecondsRealtime(.3f);
            if (Quaternion.Angle(rotation, preview.SurfaceRotation) < .1f) throw new Exception("Completed planet is not rotating");
            if (system.planets[0].transform.Find("Completed world")) throw new Exception("Unplayed planet was marked complete");

            if (!GameServices.Progress.TryOpenCompletedResults(stored.system,stored.planet) ||
                GameServices.Progress.Pending.discoveries.Count != 0 || GameServices.Progress.Pending.DisplayedDiscoveries.Count != 1 ||
                GameServices.Progress.Pending.DisplayedDiscoveries[0] != SphereIds.Stone)
                throw new Exception("Completed world lost its original discovery history or repeated a reward");
            SceneManager.LoadScene("Results");
            yield return new WaitForSecondsRealtime(.4f);
            results = FindAnyObjectByType<ResultsView>();
            CheckGlobe(results, expectedMaterial);
            if (results.DiscoverySpheres.Count != 1 || results.DiscoverySpheres[0].name != "Stone discovered sphere" ||
                !results.Settled || !results.ContinueButton.interactable || results.DiscoveryPlayCount != 0 || results.OkButton)
                throw new Exception("Revisited results must show saved Stone without rediscovery audio or animation");
            Capture(results,"revisited-stone",900,1600);
            // Exercise the layout/animation capacity with three sample discoveries, without changing player saves.
            var three = GameServices.Progress.Complete(3, 2, -.02f, 30,null);
            three.world.seaLevel=.35f;
            var materials=new double[System.Enum.GetValues(typeof(ElementId)).Length];
            materials[(int)ElementId.Fire]=400;materials[(int)ElementId.Water]=400;materials[(int)ElementId.Stone]=200;
            materials[(int)ElementId.Civilization]=300;materials[(int)ElementId.Technology]=300;   // city lights for the day/night check
            three.world.SetStats(PlanetStatCalculator.Evaluate(materials));
            three.discoveries.AddRange(new[] { 0, 1, 3 });
            SceneManager.LoadScene("Results");
            yield return new WaitForSecondsRealtime(.4f);
            results = FindAnyObjectByType<ResultsView>();
            CheckGlobe(results,expectedMaterial);
            if (results.DiscoverySpheres.Count != 3 || results.DiscoveryPlayCount != 1) throw new Exception("Three-discovery overlay missing");
            Capture(results, "three-overlay", 900, 1600);
            Capture(results, "three-overlay-tablet", 1200, 1600);
            Capture(results, "three-overlay-wide", 1600, 900);
            results.OkButton.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 5;
            while (!results.Settled && Time.realtimeSinceStartup < deadline) yield return null;
            if (!results.Settled || results.DiscoverySpheres.Exists(s => !s.parent.name.EndsWith("collected")))
                throw new Exception("Multiple discoveries did not settle");
            Capture(results, "three-collected-wide", 1600, 900);
            var four = GameServices.Progress.Complete(3,3,-.02f,45,null);
            four.discoveries.AddRange(new[] { SphereIds.Ice,SphereIds.Sunlight,SphereIds.Stone,SphereIds.Fire });
            SceneManager.LoadScene("Results"); yield return new WaitForSecondsRealtime(.4f);
            results = FindAnyObjectByType<ResultsView>();
            if (results.DiscoverySpheres.Count != 4) throw new Exception("Fourth discovery was omitted");
            Capture(results,"four-overlay",900,1600); Capture(results,"four-overlay-wide",1600,900);
            results.OkButton.onClick.Invoke();
            deadline = Time.realtimeSinceStartup+5;
            while (!results.Settled && Time.realtimeSinceStartup < deadline) yield return null;
            if (!results.Settled || results.DiscoverySpheres.Exists(s => !s.parent.name.EndsWith("collected")))
                throw new Exception("Four discoveries did not settle");
            Capture(results,"four-collected-wide",1600,900);
            var repeat = GameServices.Progress.Complete(3, 2, -.02f, 60,new int[]{SphereIds.Stone});
            if (repeat.discoveries.Count != 0) throw new Exception("Stone was awarded more than once");
            SceneManager.LoadScene("Results");
            yield return new WaitForSecondsRealtime(.4f);
            results = FindAnyObjectByType<ResultsView>();
            if (!results.Settled || !results.ContinueButton.interactable || results.DiscoveryPlayCount != 0 || results.OkButton)
                throw new Exception("No-discovery results should allow immediate Continue without discovery audio");
            yield return CheckDayNight(results);
            Debug.Log("ECOSPHERE_RESULTS_SMOKE_PASSED: actual win transition, Stone sound, Ok animation, Continue, rotating saved map world, 0/1/3/4 discoveries, responsive renders, day/night toggle");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        // Real pointer clicks on "Toggle Day/Night" over the globe: night dims the planet without blacking it out and
        // city lights become a larger share of what is visible; a second click restores day.
        IEnumerator CheckDayNight(ResultsView results)
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Day/night: "+message);}
            var button=results.DayNightButton;var preview=results.Root.Find("Completed world globe").GetComponent<SpherePreview>();
            Expect(button && button.GetComponent<CelestialButton>().label.text=="Toggle Day/Night" && button.transform.parent==preview.transform,"button missing from the globe");
            Expect(((RectTransform)button.transform).anchoredPosition==Vector2.zero,"button must be centered over the planet");
            Expect(GameServices.Progress.Pending.world.planetStats[PlanetStat.Civilization]>0,"check world needs city lights");
            var sun=GameObject.Find("Results light").GetComponent<Light>();float dayIntensity=sun.intensity;
            var mouse=UnityEngine.InputSystem.Mouse.current ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            IEnumerator Click()
            {
                Canvas.ForceUpdateCanvases();
                Vector2 point=RectTransformUtility.WorldToScreenPoint(results.View,button.transform.position);
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point},hits);
                Expect(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(button.transform),"button is blocked from pointer input");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=point});yield return null;yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=point,buttons=1});yield return null;yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=point});yield return null;yield return null;
                yield return new WaitForSecondsRealtime(PlanetVisualStyle.Current.dayNightTransitionSeconds+.2f);
            }
            // Light and city-light emission of the globe render: emission is the difference with lights switched off.
            (float total,float lights,float unlit) Measure(string name)
            {
                Texture2D Read()
                {
                    preview.PreviewCamera.Render();var texture=preview.PreviewCamera.targetTexture;var previous=RenderTexture.active;RenderTexture.active=texture;
                    var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();
                    RenderTexture.active=previous;return pixels;
                }
                var with=Read();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/results-"+name+"-globe.png"),with.EncodeToPNG());
                var block=new MaterialPropertyBlock();preview.BodyRenderer.GetPropertyBlock(block);float civilization=block.GetFloat("_Civilization");
                block.SetFloat("_Civilization",0);preview.BodyRenderer.SetPropertyBlock(block);var without=Read();
                block.SetFloat("_Civilization",civilization);preview.BodyRenderer.SetPropertyBlock(block);
                float total=0,lights=0,unlit=0;
                for(int y=0;y<with.height;y+=2)for(int x=0;x<with.width;x+=2)
                {
                    Color a=with.GetPixel(x,y),b=without.GetPixel(x,y);
                    total+=a.r*.2126f+a.g*.7152f+a.b*.0722f;unlit+=b.r*.2126f+b.g*.7152f+b.b*.0722f;lights+=Mathf.Max(0,(a.r-b.r)*.2126f+(a.g-b.g)*.7152f+(a.b-b.b)*.0722f);
                }
                Destroy(with);Destroy(without);return (total,lights,unlit);
            }
            var day=Measure("day");Capture(results,"day",900,1600);
            yield return Click();
            Expect(results.DayNight.IsNight && Mathf.Approximately(results.DayNight.Blend,1) && Mathf.Approximately(Shader.GetGlobalFloat("_PlanetNight"),1),"click did not switch to night");
            Expect(Mathf.Approximately(sun.intensity,dayIntensity*PlanetVisualStyle.Current.nightLightScale),"night must dim the sun to the style level");
            var night=Measure("night");Capture(results,"night",900,1600);
            // Darkness is judged with city lights off (they are meant to stay bright). Lava and the half-strength
            // atmosphere still glow, so a fire world keeps more light than its surface alone would.
            Expect(night.unlit<day.unlit*.7f && night.unlit>day.unlit*.15f,$"night must darken the planet without blacking it out (day {day.unlit:0}, night {night.unlit:0})");
            Expect(night.lights/night.total>day.lights/day.total*1.5f,$"city lights must stand out more at night (day {day.lights/day.total:0.000}, night {night.lights/night.total:0.000})");
            yield return Click();
            Expect(!results.DayNight.IsNight && results.DayNight.Blend==0 && Shader.GetGlobalFloat("_PlanetNight")==0 &&
                Mathf.Approximately(sun.intensity,dayIntensity) && Shader.GetGlobalFloat("_PlanetNightAmbientDim")==0 && Shader.GetGlobalFloat("_PlanetNightAtmosphereDim")==0,"second click must restore day");
        }

        static void CheckGlobe(ResultsView results, Material expectedMaterial)
        {
            var globe=results.Root.Find("Completed world globe");
            var preview=globe ? globe.GetComponent<SpherePreview>() : null;
            var stored=GameServices.Progress.Pending.world;
            var climate=stored.planetStats ?? PlanetStats.Legacy(stored.stoneRuggedness,stored.iceCoverage,stored.fireHeat,stored.atmosphereColor,stored.atmosphereStrength,stored.atmosphereWidth);
            if(results.TemperatureLabel.text!=PlanetTemperatureDisplay.Average(climate.TemperatureFahrenheit))throw new Exception("Results lost saved average temperature");
            if(!preview || preview.surfaceMaterial!=expectedMaterial || preview.seaLevel!=stored.seaLevel ||
                preview.initialRotation!=stored.rotation || preview.stoneRuggedness!=stored.stoneRuggedness || preview.iceCoverage!=stored.iceCoverage || !globe.GetComponent<RawImage>().texture ||
                preview.fireHeat!=stored.fireHeat || preview.atmosphereColor!=stored.atmosphereColor || preview.atmosphereStrength!=stored.atmosphereStrength || preview.atmosphereWidth!=stored.atmosphereWidth ||
                preview.planetStats!=stored.planetStats ||
                globe.GetComponent<Image>() || globe.Find("Backing") || results.Root.Find("World image placeholder"))
                throw new Exception("Results globe must show saved appearance without an image frame");
            if(stored.planetStats!=null)
            {
                var renderer=preview.BodyRenderer;
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                if(block.GetFloat("_Temperature")!=stored.planetStats.Temperature || block.GetFloat("_GeologicalActivity")!=stored.planetStats.Geology)
                    throw new Exception("Results globe changed temperature/geology");
            }
        }

        static void Capture(ResultsView results, string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            var camera = results.View; float aspect = camera.aspect;
            camera.targetTexture = target; camera.aspect = (float)width / height;
            results.Reflow(); Canvas.ForceUpdateCanvases();
            foreach (var preview in FindObjectsByType<SpherePreview>()) if (preview.PreviewCamera) preview.PreviewCamera.Render();
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../Logs/results-" + name + ".png"), pixels.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; camera.aspect = aspect;
            results.Reflow(); target.Release(); Destroy(target); Destroy(pixels);
        }
    }
}
