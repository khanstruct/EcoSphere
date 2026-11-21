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
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            float deadline = Time.realtimeSinceStartup + 15;
            while (!ready && !complete && Time.realtimeSinceStartup < deadline) yield return null;
            if (!ready || complete || board.Cells.Count != 30 || loadedType == firstAmmo || totals[(int)Definition(firstAmmo).contributions[0].element] != 620 || tutorialPaused)
                throw new Exception("First tutorial shot did not clear its half and load the remaining element");
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            deadline = Time.realtimeSinceStartup + 15;
            while (!complete && Time.realtimeSinceStartup < deadline) yield return null;
            if (!complete || board.Cells.Count != 0 || totals[firstElement] != 620 || totals[secondElement] != 620 ||
                totals[firstElement] < resourceGoal || totals[secondElement] < resourceGoal || totals[firstElement] >= abundanceGoal || stoneReleased)
                throw new Exception($"Both tutorial elements must reach inclusion without reaching excess or releasing Stone: complete={complete}, cells={board.Cells.Count}, first={totals[firstElement]}, second={totals[secondElement]}, goal={resourceGoal}, abundance={abundanceGoal}, stone={stoneReleased}, failed={failed}");
            Debug.Log("ECOSPHERE_TUTORIAL_ONE_SMOKE_PASSED: welcome and cannon lessons, pause/input shield, 30 Fire / 30 Void, two actual shots, both elements 620/500");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
