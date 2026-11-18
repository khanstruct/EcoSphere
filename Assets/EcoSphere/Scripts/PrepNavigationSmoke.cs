using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EcoSphere
{
    public sealed class PrepNavigationSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.LoadScene("System"); yield return null; yield return null;
            var system=FindAnyObjectByType<SystemView>();
            system.SetSystemNumber(7); system.OpenPlanet(2);
            yield return null; yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            if(!prep || prep.Title.text!="Planet 3" || LevelPrepView.SystemNumber!=7 || LevelPrepView.RequiredSpheres!=4)
                throw new System.Exception("System to prep context failed");
            prep.AddSphere(0); prep.AddSphere(1);
            if(prep.Begin.interactable)throw new System.Exception("Four-slot Begin should remain disabled");
            var back=GameObject.Find("Back").GetComponent<UnityEngine.UI.Button>(); back.onClick.Invoke();
            yield return null; yield return null;
            system=FindAnyObjectByType<SystemView>();
            if(!system || system.systemNumber!=7)throw new System.Exception("Back did not restore system");
            system.SetSystemNumber(1);
            system.planets[0].onClick.Invoke(); yield return null; yield return null;
            prep=FindAnyObjectByType<LevelPrepView>();
            if(prep.Title.text!="Tutorial 1" || LevelPrepView.RequiredSpheres!=2 || LevelPrepView.PlanetIndex!=0 || LevelPrepView.SystemNumber!=1)
                throw new System.Exception("Tutorial 1 name or world context missing");
            prep.Begin.onClick.Invoke();
            if(SceneManager.GetActiveScene().name!="LevelPrep")throw new System.Exception("Incomplete selection must not launch gameplay");
            prep.AddSphere(0); prep.AddSphere(1);
            if(!prep.Begin.interactable)throw new System.Exception("Two-slot Begin should enable");
            prep.Begin.onClick.Invoke();
            yield return null; yield return null; yield return new WaitForSecondsRealtime(.3f);
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            if(SceneManager.GetActiveScene().name!="Tutorial1" || !game || game.levelLayout!=EcoSpherePrototype.LevelLayout.FirstTutorial || game.CellCount!=60 || Time.timeScale!=1)
                throw new System.Exception("Begin did not launch the playable first tutorial");
            if(FindAnyObjectByType<MenuMusic>())throw new System.Exception("Menu music continued into Tutorial 1 gameplay");
            Debug.Log("ECOSPHERE_PREP_NAVIGATION_PASSED: planet click, Tutorial 1 name, context, Back, selection gating, playable Tutorial1 and music transition.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
            #endif
        }
    }
}
