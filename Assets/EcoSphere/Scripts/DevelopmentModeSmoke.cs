using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CheckDevelopmentWater()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-devModeSmoke")<0)throw new InvalidOperationException();
            if(tutorialPaused || Time.timeScale!=1 || Array.IndexOf(activeSphereTypes,SphereIds.Water)<0 || !displayedElements.Contains(ElementId.Water))
                throw new Exception("DEV Water: chosen Water did not reach gameplay or tutorial paused it");
            var cells=new HashSet<Vector2Int>();
            foreach(var cell in board.Cells)if(cell.Value==SphereIds.Water)cells.Add(cell.Key);
            if(cells.Count==0 || LevelAudio.MatchVoice(SphereIds.Water,30)!=Definition(SphereIds.Water).resourceVoice)
                throw new Exception("DEV Water: no board spheres or incorrect large-match voice");
            RemoveCells(cells,ElementScoring.MatchRate(cells.Count)); UpdateVisualizer();
            if(totals[(int)ElementId.Water]!=600 || score!=600)throw new Exception("DEV Water: match points did not contribute to Water");
            foreach(var bar in energyBars)
                if(bar.color!=Definition(SphereIds.Water).color && bar.color!=Definition(SphereIds.Ice).color)
                    throw new Exception("DEV Water: visualizer used the wrong loadout colors");
        }
    }
    public sealed class DevelopmentModeSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-devModeSmoke")>=0)
                new GameObject("Development mode checks").AddComponent<DevelopmentModeSmoke>();
        }
        static void Expect(bool value,string message) { if(!value)throw new Exception("Development mode: "+message); }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null; yield return null;
            Expect(DevelopmentMode.Enabled,"default development toggle is off");
            int selectable=0;
            foreach(var sphere in SphereCatalog.Current.spheres)
                if(sphere.AmmoEligible) { selectable++; Expect(WorldProgress.IsSphereAvailable(sphere.id),"selectable sphere unavailable"); }
            Expect(WorldProgress.AvailableSphereCount==selectable && !WorldProgress.SystemIntroductionSeen,"collection bypass persisted an intro acknowledgment");
            var configuration=Resources.Load<DevelopmentSettings>("DevelopmentSettings");
            bool previous=configuration.devModeEnabled;
            try
            {
                configuration.devModeEnabled=false;
                Expect(!DevelopmentMode.Enabled && !WorldProgress.IsSphereAvailable(SphereIds.Water) && WorldProgress.AvailableSphereCount==2,"disabling DEV did not restore the actual collection");
            }
            finally { configuration.devModeEnabled=previous; }
            var system=FindAnyObjectByType<SystemView>();
            Expect((!system.Dialog || !system.Dialog.IsOpen) && Time.timeScale==1,"System tutorial opened in DEV mode");
            system.OpenPlanet(0); yield return null; yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            Expect(prep && (!prep.SetupPopup || !prep.SetupPopup.IsOpen) && Time.timeScale==1,"setup tutorial opened in DEV mode");
            string alphaId=WorldLevels.Get(1,0).id;
            Expect(WorldProgress.SetupTutorialStep(alphaId)==0,"DEV skipped lessons by marking them completed");
            prep.AddSphere(SphereIds.Water); prep.AddSphere(SphereIds.Ice);
            Expect(prep.Selected.Count==2 && prep.Begin.interactable,"Water could not be selected in setup");
            prep.Begin.onClick.Invoke(); yield return null; yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>(); game.CheckDevelopmentWater();
            // The temporary collection override must not prevent real recipe/reveal testing.
            var gains=new double[Enum.GetValues(typeof(ElementId)).Length]; gains[(int)ElementId.Fire]=gains[(int)ElementId.Void]=620;
            var completed=WorldProgress.Complete(1,0,-.02f,0,false,gains);
            Expect(completed.discoveries.Contains(SphereIds.Sunlight),"DEV availability incorrectly suppressed ordinary recipe awards");
            SceneManager.LoadScene("Results"); yield return null; yield return null;
            var results=FindAnyObjectByType<ResultsView>(); float deadline=Time.realtimeSinceStartup+10;
            while(results.RevealPhase!=DiscoveryRevealPhase.Ready && Time.realtimeSinceStartup<deadline)yield return null;
            Expect(results.RevealPhase==DiscoveryRevealPhase.Ready,"DEV recipe reveal failed");
            results.OkButton.onClick.Invoke(); deadline=Time.realtimeSinceStartup+5;
            while(!results.Settled && Time.realtimeSinceStartup<deadline)yield return null;
            Expect(results.Settled && !results.DiscoveryTutorial && !completed.world.discoveryTutorialSeen,"results tutorial opened or was marked seen in DEV mode");
            results.ContinueButton.onClick.Invoke(); yield return null; yield return null;
            system=FindAnyObjectByType<SystemView>();
            Expect(!system.Dialog || !system.Dialog.IsOpen,"next-world tutorial opened in DEV mode");
            system.OpenPlanet(1); yield return null; yield return null;
            prep=FindAnyObjectByType<LevelPrepView>();
            prep.AddSphere(SphereIds.Water); prep.AddSphere(SphereIds.Fire); prep.AddSphere(SphereIds.Void);
            prep.Begin.onClick.Invoke(); yield return null; yield return null;
            Expect(SceneManager.GetActiveScene().name=="FirstLight" && Time.timeScale==1 && !GameObject.Find("Tutorial overlay"),"Beta's natural-element lesson was not skipped");
            Debug.Log("ECOSPHERE_DEV_MODE_PASSED: complete selectable collection, Water setup/board/scoring/voice/colors, skipped System/setup/gameplay/results/followup lessons, preserved tutorial progress and normal recipe reveals");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
