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
            yield return new WaitForSeconds(.3f);
            if (StoneTutorial || tutorialPaused || tutorialCanvas || Time.timeScale != 1)
                throw new Exception("Tutorial 1 must start without the Stone tutorial's message windows");
            int ice = 0, sunlight = 0;
            foreach (var cell in board.Cells)
            {
                if (cell.Value == 0) ice++;
                else if (cell.Value == 1) sunlight++;
                else throw new Exception("Tutorial 1 must contain only Ice and Sunlight");
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
            if (!ready || complete || board.Cells.Count != 30 || loadedType == firstAmmo || totals[firstAmmo] != 620 || tutorialPaused)
                throw new Exception("First tutorial shot did not clear its half and load the remaining element");
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            deadline = Time.realtimeSinceStartup + 15;
            while (!complete && Time.realtimeSinceStartup < deadline) yield return null;
            if (!complete || board.Cells.Count != 0 || totals[0] != 620 || totals[1] != 620 ||
                totals[0] < resourceGoal || totals[1] < resourceGoal || totals[0] >= abundanceGoal || stoneReleased)
                throw new Exception("Both tutorial elements must reach inclusion without reaching excess or releasing Stone");
            Debug.Log("ECOSPHERE_TUTORIAL_ONE_SMOKE_PASSED: 30 Ice / 30 Sunlight, no messages or Stone, two actual shots, both elements 620/500, below 1500 excess");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
