using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        IEnumerator SmokeRun()
        {
            yield return new WaitForSeconds(.6f);
            if (CellCount != 3 * InitialRings * (InitialRings + 1))
                throw new Exception("Initial pre-existing groups were incorrectly resolved");
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            float deadline = Time.time + 15;
            while (!ready && !complete && Time.time < deadline) yield return null;
            if (Time.time >= deadline) throw new Exception("Smoke shot did not resolve in time");
            if (lastMatch < 3) throw new Exception("Known matching shot failed to clear its initial group");
            if (!complete && Mathf.Abs(Mathf.DeltaAngle(cluster.eulerAngles.z, rotationDegrees)) > .1f)
                throw new Exception("Next sphere loaded before cluster rotation completed");
            Debug.Log($"ECOSPHERE_SMOKE resolved: board={CellCount}, matched={lastMatch}, released={lastRelease}, lost={lost}");
            if (!complete)
            {
                int before = ricochets;
                direction = new Vector2(.97f, .26f).normalized; gun.up = direction; Fire();
                deadline = Time.time + 20;
                while (!ready && !complete && Time.time < deadline) yield return null;
                if (Time.time >= deadline || ricochets <= before) throw new Exception("Bank shot failed to ricochet and resolve");
                Debug.Log($"ECOSPHERE_SMOKE bank shot: ricochets={ricochets - before}, board={CellCount}");
            }
            if (!complete)
            {
                int previousLost = lost;
                // Exercise the bottom-return path directly, without depending on a particular board gap.
                direction = Vector2.down; Fire();
                deadline = Time.time + 5;
                while (!ready && Time.time < deadline) yield return null;
                if (lost != previousLost + 1 || !ready) throw new Exception("Bottom loss did not advance and reload the turn");
                Debug.Log("ECOSPHERE_SMOKE bottom loss passed");
                gun.rotation = Quaternion.identity; direction = Vector2.up;
            }
            yield return new WaitForSeconds(.3f);
            string output = System.IO.Path.Combine(Application.dataPath, "../Logs/EcoSphere-test-level.png");
            ScreenCapture.CaptureScreenshot(output);
            yield return new WaitForSeconds(1);
            Debug.Log("ECOSPHERE_SMOKE_COMPLETE");
            CapturePreview();
            SmokeScoringChecks();
            SmokeLossChecks();
            yield return SmokeVoiceChecks();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
        }

        void CapturePreview()
        {
            var target = new RenderTexture(900, 1600, 24);
            var previousTarget = view.targetTexture;
            var previousActive = RenderTexture.active;
            view.targetTexture = target; view.aspect = 900f / 1600; FitCamera();
            if (spaceBackground) spaceBackground.RefreshLayout();
            Canvas.ForceUpdateCanvases();
            view.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(900, 1600, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 900, 1600), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/EcoSphere-test-level.png"), pixels.EncodeToPNG());
            view.targetTexture = previousTarget; RenderTexture.active = previousActive;
            target.Release(); Destroy(target); Destroy(pixels);
        }

        void SmokeLossChecks()
        {
            // A matching attachment beyond the boundary must lose before it can clear or score.
            board.Cells.Clear(); cluster.rotation = Quaternion.identity;
            var hitCell = new Vector2Int(6, 2);
            board.Cells[hitCell] = board.Cells[new Vector2Int(6, 1)] = board.Cells[new Vector2Int(5, 2)] = 0;
            projectile = new GameObject("Boundary attachment test");
            projectile.transform.position = cluster.TransformPoint(BoardRules.Position(new Vector2Int(7, 2)));
            loadedType = 0; totals[0] = 8;
            Attach(hitCell, Vector2.left);
            if (!failed || board.Cells.Count != 4 || lastMatch != 0 || totals[0] != 0)
                throw new Exception("Boundary attachment was allowed to match before losing");
            failed = false;
            board.Cells.Clear();
            cluster.rotation = Quaternion.identity;
            // Derive the test cell from the current geometry: safe where it is, but touching a wall once rotated
            // onto the +x axis. A fixed cell goes stale whenever sphere size or spacing changes.
            Vector2Int cell = default; float losingAngle = 0; bool found = false;
            for (int q = -12; q <= 12 && !found; q++)
                for (int r = -12; r <= 12 && !found; r++)
                {
                    var candidate = new Vector2Int(q, r);
                    if (candidate == Vector2Int.zero) continue;
                    board.Cells.Clear(); board.Cells[candidate] = 0;
                    Vector2 candidateOffset = BoardRules.Position(candidate);
                    float angle = -Mathf.Atan2(candidateOffset.y, candidateOffset.x) * Mathf.Rad2Deg;
                    if (CheckGameOver(true, 0) || !CheckGameOver(true, angle)) continue;
                    cell = candidate; losingAngle = angle; found = true;
                }
            if (!found) throw new Exception("No cell is safe in place yet loses when rotated to a wall");
            board.Cells.Clear(); board.Cells[cell] = 0;
            if (CheckGameOver()) throw new Exception("Safe pre-rotation cluster incorrectly lost");
            if (!CheckGameOver(true, losingAngle) || failed)
                throw new Exception("Rotation preview must detect loss without ending the level early");
            cluster.rotation = Quaternion.Euler(0, 0, losingAngle);
            totals[0] = 8; matchedCount = 6;
            if (!CheckGameOver() || ready || totals[0] != 0 || matchedCount != 0)
                throw new Exception("Rotation-edge contact must end level and discard all rewards");
            Debug.Log("ECOSPHERE_LOSS_CHECKS_PASSED");
        }

        void SmokeScoringChecks()
        {
            int[] sizes = { 3, 4, 5, 6, 8, 10, 19 };
            int[] expected = { 30, 48, 70, 96, 160, 200, 380 };
            for (int i = 0; i < sizes.Length; i++)
                if (ElementScoring.MatchRate(sizes[i]) * sizes[i] != expected[i])
                    throw new Exception("Incorrect capped match bonus");
            board.Cells.Clear(); visuals.Clear();Array.Clear(totals,0,totals.Length);score=0;
            var matched = new HashSet<Vector2Int>();
            for (int i = 1; i <= 3; i++)
            {
                var cell = new Vector2Int(i, 0); matched.Add(cell);
                board.Cells[cell] = 0; visuals[cell] = new GameObject("Matched scoring test");
            }
            RemoveCells(matched, ElementScoring.MatchRate(matched.Count));
            if (totals[(int)ElementId.Ice] != 15 || totals[(int)ElementId.Water]!=7.5 || score != 30) throw new Exception("Three Ice matches must split 30 score into 15 Ice / 7.5 Water / Null");
            var sun = new Vector2Int(0, 1); var stone = new Vector2Int(0, 2);
            board.Cells[sun] = 1; board.Cells[stone] = StoneType;
            visuals[sun] = new GameObject("Sunlight scoring test"); visuals[stone] = new GameObject("Stone scoring test");
            RemoveCells(new HashSet<Vector2Int> { sun, stone }, ElementScoring.Breakaway, true);
            if (totals[(int)ElementId.Ice] != 15 || totals[(int)ElementId.Sunlight] != 25 || totals[(int)ElementId.Fire]!=12.5 || totals[(int)ElementId.Stone]!=37.5 || score != 130)
                throw new Exception("Breakaways must award 50 each; Stone affects score without adding a resource bar");
            board.Cells[sun] = 1; visuals[sun] = new GameObject("Threshold scoring test");
            RemoveCells(new HashSet<Vector2Int> { sun }, ElementScoring.Breakaway, true);
            if (totals[(int)ElementId.Sunlight] != 50 || LevelAudio.ResourceCallout(new[] { 0, totals[(int)ElementId.Sunlight] }) != -1)
                throw new Exception("Two Sunlight breakaways must remain below the inclusion threshold");
            UpdateVisualizer(); int initialGoal=resourceGoal, initialExcess=abundanceGoal;
            for (int i = 0; i < totals.Length; i++) totals[i] = abundanceGoal * 4;
            UpdateVisualizer();
            if (initialGoal!=resourceGoal || initialExcess!=abundanceGoal)
                throw new Exception("Visualizer scaling changed the gameplay thresholds");
            if (barTotals[0].text != (abundanceGoal*4).ToString() || barTotals[1].text != (abundanceGoal*4).ToString())
                throw new Exception("Full point totals must remain readable above the visual scale");
            Debug.Log("ECOSPHERE_SCORING_CHECKS_PASSED");
        }
        Vector2 SmokeMatchingDirection()
        {
            for (int step = -240; step <= 240; step++)
            {
                float degrees = step * .25f;
                Vector2 candidate = new Vector2(Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));
                Vector2 origin = GameplayGeometry.ShotOrigin(candidate);
                float nearest = 20; int hitType = -1; Vector2 hitCenter = default;
                foreach (var entry in board.Cells)
                    if (BoardRules.Sweep(origin, candidate, cluster.TransformPoint(BoardRules.Position(entry.Key)),
                        Radius * 2, nearest, out float distance))
                    { nearest = distance; hitType = entry.Value; hitCenter = cluster.TransformPoint(BoardRules.Position(entry.Key)); }
                // The test shot must reach its matching target before contacting the Core.
                if (BoardRules.Sweep(origin, candidate, center, Radius * 2, nearest, out _)) continue;
                Vector2 offset = hitCenter - origin;
                float perpendicular = Mathf.Abs(offset.x * candidate.y - offset.y * candidate.x);
                if (hitType == loadedType && perpendicular < .12f) return candidate;
            }
            throw new Exception("No direct matching smoke-test trajectory found");
        }

        IEnumerator SmokeVoiceChecks()
        {
            if (!levelAudio || !levelAudio.worldComplete || !levelAudio.creationFailed ||
                !Catalog.ElementVoice(ElementId.Ice) || !Catalog.ElementVoice(ElementId.Sunlight) || !levelAudio.ecosphere)
                throw new Exception("Level result voice clips are missing");
            var mainAudio = levelAudio;
            var lossSource = mainAudio.transform.Find("Level result announcer").GetComponent<AudioSource>();
            if (lossSource.clip != mainAudio.creationFailed)
                throw new Exception("Actual game-over checkpoint did not announce Creation Failed");
            var test = new GameObject("Win announcement test").AddComponent<LevelAudio>();
            test.voiceVolume = 0; test.musicVolume = 0; test.Initialize();
            test.AnnounceResources(ElementScoring.Inclusion, ElementScoring.Inclusion + 50);
            var source = test.transform.Find("Level result announcer").GetComponent<AudioSource>();
            if (source.clip != mainAudio.ecosphere)
                throw new Exception("Two qualifying resource gains did not announce EcoSphere");
            levelAudio = test; failed = false; complete = true;
            yield return FinishTurn();
            float deadline = Time.time + 15;
            while (source.clip != mainAudio.worldComplete && Time.time < deadline) yield return null;
            if (source.clip != mainAudio.worldComplete || source.clip.length <= 0)
                throw new Exception("Completion did not select World Complete");
            test.AnnounceResult(false);
            if (source.clip != mainAudio.worldComplete)
                throw new Exception("Level result was announced more than once");
            levelAudio = mainAudio; Destroy(test.gameObject);
            Debug.Log("ECOSPHERE_RESULT_VOICE_CHECKS_PASSED");
        }

    }
}
