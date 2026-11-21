using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;

namespace EcoSphere
{
    public sealed class WorldPreparationSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-worldPrepSmoke") >= 0)
                new GameObject("World preparation checks").AddComponent<WorldPreparationSmoke>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return null; yield return null;
            var system = FindAnyObjectByType<SystemView>();
            system.DismissIntroductionForSmoke(); yield return null;
            if (system.planets[0].name != "Genesis Alpha" || system.planets[1].name != "Genesis Beta")
                throw new Exception("System did not display the predefined world names");
            for (int slot = 0; slot < system.planets.Length; slot++)
            {
                string count = system.planets[slot].transform.Find("Sphere Requirement/Sphere Count").GetComponent<TMP_Text>().text;
                if (count != WorldLevels.Get(1, slot).RequiredSpheres.ToString()) throw new Exception("System requirement badge did not read world data");
            }
            system.SetSystemNumber(8);
            foreach (var world in WorldProgress.GetSystem(8).worlds)
                if (!world.IsProcedural || world.RequiredSpheres < 4) throw new Exception("Later system reused tutorial requirements");
            var chosen = WorldLevels.Get(8, 0); string expectedName = chosen.displayName;
            chosen.rules.requiredSpheres = 6;
            chosen.modifiers.Clear();
            foreach (var modifier in WorldCatalog.Current.proceduralModifiers) chosen.modifiers.Add(modifier.id);
            system.RefreshSystemHeading();
            if (system.planets[0].transform.Find("Sphere Requirement/Sphere Count").GetComponent<TMP_Text>().text != "6")
                throw new Exception("Variable requirement badge was clamped");
            system.OpenPlanet(0);
            if (!system.Dialog || !system.Dialog.IsOpen || system.Dialog.Message != SystemDialog.Insufficient)
                throw new Exception("Six-sphere world did not block an insufficient collection");
            system.Dialog.ContinueButton.onClick.Invoke(); yield return null;
            // Inspect the future six-slot layout independently of the normal collection gate.
            LevelPrepView.SystemNumber = 8; LevelPrepView.PlanetIndex = 0;
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelPrep"); yield return null; yield return null;
            var prep = FindAnyObjectByType<LevelPrepView>();
            if (prep.Title.text != expectedName || LevelPrepView.RequiredSpheres != 6 || prep.Begin.interactable)
                throw new Exception("Procedural preparation did not use its saved name and six-sphere requirement");
            var slots = GameObject.Find("Your Spheres").transform;
            for (int i = 1; i <= 6; i++) if (!slots.Find("Slot " + i)) throw new Exception("Missing variable loadout slot");
            var conditions = GameObject.Find("World Conditions").transform.Find("Modifier viewport/Modifier list");
            foreach (string modifier in chosen.modifiers)
                if (!conditions.Find(modifier)) throw new Exception("Missing per-world modifier in preparation");
            var camera = GameObject.Find("Prep Camera").GetComponent<Camera>();
            foreach (var size in new[] { new Vector2Int(900,1600), new Vector2Int(1600,900) })
            {
                var target = new RenderTexture(size.x,size.y,24); target.Create(); camera.targetTexture = target;
                prep.Reflow(); Canvas.ForceUpdateCanvases();
                foreach (var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None)) if (preview.PreviewCamera) preview.PreviewCamera.Render();
                camera.Render(); var old = RenderTexture.active; RenderTexture.active = target;
                var pixels = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/WorldPrep-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                RenderTexture.active = old; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(pixels);
            }
            Debug.Log("ECOSPHERE_WORLD_PREPARATION_SMOKE_PASSED: Genesis names, data-driven badges, later-system 4+ minimum, six selection slots and variable modifier list");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
