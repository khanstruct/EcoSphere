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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-resultsSmoke") < 0) throw new InvalidOperationException();
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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-resultsSmoke") >= 0)
                new GameObject("Results checks").AddComponent<ResultsSmoke>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return new WaitForSecondsRealtime(.3f);
            LevelPrepView.SystemNumber = 2; LevelPrepView.PlanetIndex = 1;
            var game = FindAnyObjectByType<EcoSpherePrototype>();
            if (game.resourceGoal < 1000000 || game.abundanceGoal < 3000000)
                throw new Exception("Temporary tutorial thresholds were not raised");
            var expectedMaterial = game.receivingPlanetMaterial;
            var announcements = game.GetComponent<LevelAudio>();
            announcements.AnnounceResources(ElementScoring.Inclusion, 0);
            announcements.transform.Find("Level result announcer").GetComponent<AudioSource>().pitch = .85f;
            game.CompleteForResultsSmoke();
            float deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != "Results" && Time.realtimeSinceStartup < deadline) yield return null;
            if (SceneManager.GetActiveScene().name != "Results") throw new Exception("Winning did not open Results");
            if (announcements.IsAnnouncing) throw new Exception("Results cut off a queued completion announcement");
            yield return new WaitForSecondsRealtime(.4f);
            var results = FindAnyObjectByType<ResultsView>();
            if (!results || results.DiscoverySpheres.Count != 1 || results.DiscoverySpheres[0].name != "Stone discovered sphere")
                throw new Exception("Stone discovery overlay missing after a successful level");
            if (results.DiscoveryPlayCount != 1 || results.ContinueButton.interactable || results.Settled)
                throw new Exception("Discovery sound or Continue gating incorrect");
            CheckGlobe(results, expectedMaterial);
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
            var stored = WorldProgress.Find(2, 1);
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

            if (!WorldProgress.TryOpenCompletedResults(stored.system,stored.planet) ||
                WorldProgress.Pending.discoveries.Count != 0 || WorldProgress.Pending.DisplayedDiscoveries.Count != 1 ||
                WorldProgress.Pending.DisplayedDiscoveries[0] != SphereIds.Stone)
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
            var three = WorldProgress.Complete(3, 2, -.02f, 30, false);
            three.world.iceCoverage=.6f; three.world.stoneRuggedness=.4f; three.world.seaLevel=.35f;
            three.world.fireHeat=.7f;three.world.atmosphereColor=new Color(1,.25f,.035f,1);three.world.atmosphereStrength=.6f;three.world.atmosphereWidth=.055f;
            var materials=new double[System.Enum.GetValues(typeof(ElementId)).Length];
            materials[(int)ElementId.Fire]=400;materials[(int)ElementId.Water]=400;materials[(int)ElementId.Stone]=200;
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
            var four = WorldProgress.Complete(3,3,-.02f,45,false);
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
            var repeat = WorldProgress.Complete(3, 2, -.02f, 60, true);
            if (repeat.discoveries.Count != 0) throw new Exception("Stone was awarded more than once");
            SceneManager.LoadScene("Results");
            yield return new WaitForSecondsRealtime(.4f);
            results = FindAnyObjectByType<ResultsView>();
            if (!results.Settled || !results.ContinueButton.interactable || results.DiscoveryPlayCount != 0 || results.OkButton)
                throw new Exception("No-discovery results should allow immediate Continue without discovery audio");
            Debug.Log("ECOSPHERE_RESULTS_SMOKE_PASSED: actual win transition, Stone sound, Ok animation, Continue, rotating saved map world, 0/1/3/4 discoveries, responsive renders");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static void CheckGlobe(ResultsView results, Material expectedMaterial)
        {
            var globe=results.Root.Find("Completed world globe");
            var preview=globe ? globe.GetComponent<SpherePreview>() : null;
            var stored=WorldProgress.Pending.world;
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
                var renderer=preview.PreviewCamera.transform.parent.Find("Sphere").GetComponent<Renderer>();
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
