using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        bool FirstTutorialTesting => Array.IndexOf(Environment.GetCommandLineArgs(), "-firstTutorialSmoke") >= 0;

        IEnumerator FirstTutorialSmokeRun()
        {
            yield return new WaitForSecondsRealtime(.3f);
            if (StoneTutorial) throw new Exception("Genesis Alpha must use its introductory layout");
            yield return IntroTutorialSmokeRun();
            int first = activeSphereTypes[0], second = activeSphereTypes[1];
            int firstElement = (int)Definition(first).contributions[0].element, secondElement = (int)Definition(second).contributions[0].element;
            int ice = 0, sunlight = 0;
            foreach (var cell in board.Cells)
            {
                if (cell.Value == first) ice++;
                else if (cell.Value == second) sunlight++;
                else throw new Exception("Tutorial 1 must contain only the configured opening pair");
                if (!board.Cells.TryGetValue(-cell.Key, out int opposite) || opposite == cell.Value)
                    throw new Exception("Tutorial 1 halves are not balanced about the core");
            }
            if (ice != 30 || sunlight != 30 || board.MatchFrom(new Vector2Int(-4, 0)).Count != 30 ||
                board.MatchFrom(new Vector2Int(4, 0)).Count != 30)
                throw new Exception("Each tutorial half must be one connected group of 30 spheres");
            CapturePreview();
            File.Copy(Path.Combine(Application.dataPath, "../Logs/EcoSphere-test-level.png"),
                Path.Combine(Application.dataPath, "../Logs/Tutorial1-preview.png"), true);
            int firstAmmo = loadedType;
            var expectedFirst=new double[totals.Length];Definition(firstAmmo).AddContributions(expectedFirst,620);
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            float deadline = Time.realtimeSinceStartup + 15;
            while (!ready && !complete && Time.realtimeSinceStartup < deadline) yield return null;
            if (!ready || complete || board.Cells.Count != 30 || loadedType == firstAmmo || totals[(int)Definition(firstAmmo).contributions[0].element] != expectedFirst[(int)Definition(firstAmmo).contributions[0].element] || tutorialPaused)
                throw new Exception("First tutorial shot did not clear its half and load the remaining element");
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            deadline = Time.realtimeSinceStartup + 15;
            while (!complete && Time.realtimeSinceStartup < deadline) yield return null;
            var expected=new double[totals.Length];Definition(first).AddContributions(expected,620);Definition(second).AddContributions(expected,620);
            if (!complete || board.Cells.Count != 0 || totals[firstElement] != expected[firstElement] || totals[secondElement] != expected[secondElement] || score!=1240 || stoneReleased)
                throw new Exception($"Tutorial weighted totals: first={totals[firstElement]}, second={totals[secondElement]}, score={score}");
            UpdateComposition();
            var reward=GameServices.Progress.Complete(GameServices.Session.SystemNumber,GameServices.Session.PlanetIndex,-.02f,0,null,selectedElementTotals,composition.Ranked);
            if(!reward.discoveries.Contains(SphereIds.Sunlight))throw new Exception("Genesis Alpha's weighted opening clear must unlock Sunlight");
            Debug.Log($"ECOSPHERE_TUTORIAL_ONE_SMOKE_PASSED: two actual shots, unchanged 1240 score, weighted Fire={totals[(int)ElementId.Fire]}, Void={totals[(int)ElementId.Void]}, recipe minimum={resourceGoal}; reduced yields may require tutorial balancing");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
