using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed class TutorialTwoNavigationSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tutorialTwoNavigationSmoke") >= 0)
                new GameObject("Tutorial 2 navigation checks").AddComponent<TutorialTwoNavigationSmoke>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            // Simulate the successful first world, so setup exposes exactly the three earned types.
            var resources = new double[Enum.GetValues(typeof(ElementId)).Length];
            resources[(int)ElementId.Fire] = resources[(int)ElementId.Void] = 620;
            WorldProgress.Complete(1, 0, -.02f, 0, false, resources);
            WorldProgress.ClearPending();
            SceneManager.LoadScene("System"); yield return null; yield return null;
            var system = FindAnyObjectByType<SystemView>();
            system.DismissIntroductionForSmoke(); yield return null;
            system.SetSystemNumber(1); system.OpenPlanet(1);
            yield return null; yield return null;
            var prep = FindAnyObjectByType<LevelPrepView>();
            if (!prep || prep.Title.text != "Genesis Beta" || LevelPrepView.RequiredSpheres != 3 || prep.Begin.interactable)
                throw new Exception("Tutorial 2 did not reach its three-slot setup");
            // Deliberately differ from the direct-launch default order.
            prep.AddSphere(SphereIds.Sunlight); prep.AddSphere(SphereIds.Fire);
            prep.Begin.onClick.Invoke();
            if (SceneManager.GetActiveScene().name != "LevelPrep" || prep.Begin.interactable)
                throw new Exception("Tutorial 2 launched with an incomplete loadout");
            prep.AddSphere(SphereIds.Void);
            if (!prep.Begin.interactable || prep.Selected.Count != 3) throw new Exception("Tutorial 2 loadout did not enable Begin");
            prep.Begin.onClick.Invoke(); yield return null; yield return null;
            var game = FindAnyObjectByType<EcoSpherePrototype>();
            if (SceneManager.GetActiveScene().name != "FirstLight" || !game || game.CellCount != 60 ||
                game.levelLayout != EcoSpherePrototype.LevelLayout.StoneTutorial || Time.timeScale != 0 || !game.transform.Find("Tutorial overlay"))
                throw new Exception("Tutorial 2 did not launch the guided Stone layout");
            if (!GameObject.Find("Title").GetComponent<TMPro.TMP_Text>().text.Contains("GENESIS BETA") || game.resourceGoal != WorldLevels.Get(1,1).rules.inclusionGoal)
                throw new Exception("Genesis Beta did not apply its authored name and rules");
            Debug.Log("ECOSPHERE_TUTORIAL_TWO_NAVIGATION_PASSED: System planet 2, three-slot gating, Sunlight/Fire/Void selection and guided gameplay launch");
            Destroy(gameObject);
            // The gameplay tutorial smoke continues through both lessons and exits Unity.
        }
    }
}
