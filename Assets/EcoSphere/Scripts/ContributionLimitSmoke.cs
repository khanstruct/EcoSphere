using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Fire is temporarily authored as a Fish-like sphere (Life limited by Water) so the shared limit
        // mechanism is exercised through real shots, RemoveCells, the ledger, scoring and the HUD notice.
        internal IEnumerator CheckContributionLimits()
        {
            void Expect(bool value, string message) { if (!value) throw new Exception("Contribution limit: " + message); }
            var fire = Definition(SphereIds.Fire); var water = Definition(SphereIds.Water);
            var originalContributions = fire.contributions; int originalUnallocated = fire.unallocatedWeight;
            try
            {
                fire.contributions = new List<ElementContribution> { new ElementContribution(ElementId.Fire, 2), new ElementContribution(ElementId.Life, 2, ElementId.Water) };
                fire.unallocatedWeight = 0; fire.Validate();
                var invalid = ScriptableObject.CreateInstance<SphereDefinition>();
                invalid.displayName = "Invalid"; invalid.surface = fire.surface; invalid.icon = fire.icon; invalid.description = "x";
                invalid.contributions = new List<ElementContribution> { new ElementContribution(ElementId.Life, 2, ElementId.Life) };
                bool rejected = false; try { invalid.Validate(); } catch (InvalidOperationException) { rejected = true; }
                Destroy(invalid);
                Expect(rejected, "a line limited by its own element must fail validation");
                IncludeElements(fire); IncludeElements(water); UpdateVisualizer();

                // 1. Real shot: a three-Fire match plus a Water breakaway in one resolution. The match is removed
                // first, but the breakaway's Water must raise the cap before the matched Life is checked.
                var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                while (!ready || projectile || armBusy) yield return null;
                rotationDegrees = 0;
                foreach (var model in visuals.Values) Destroy(model); visuals.Clear(); board.Cells.Clear();
                foreach (var alien in aliens.Values) Destroy(alien); aliens.Clear(); board.Infested.Clear();
                Elements.Clear();
                void Add(Vector2Int cell, int type) { board.Cells.Add(cell, type); visuals.Add(cell, MakeSphere("Limit test " + cell, type, cluster, BoardRules.Position(cell))); }
                var anchor = new Vector2Int(0, -1); var target = new Vector2Int(0, -2); var hanging = new Vector2Int(-1, -1);
                // A Void keeps the board non-empty so the shot does not complete the level.
                Add(new Vector2Int(1, 0), SphereIds.Void); Add(anchor, SphereIds.Fire); Add(target, SphereIds.Fire); Add(hanging, SphereIds.Water);
                if (loaded) Destroy(loaded); loadedType = SphereIds.Fire; loaded = MakeSphere("Loaded Fire", SphereIds.Fire, gun, LoadedLocal);
                yield return null;
                int scoreBefore = score; var shortages = new List<ElementShortage>(); Elements.ShortageOccurred += shortages.Add;
                Vector2 point = view.WorldToScreenPoint(visuals[target].transform.position);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 }); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null;
                Expect(!ready, "real click did not fire");
                while (projectile) yield return null;
                int rate = ElementScoring.MatchRate(3), breakaway = ElementScoring.Breakaway;
                double waterGain = water.ContributionYield(water.contributions[0], breakaway), lifeGain = 3 * rate * .5;
                Expect(lastMatch == 3 && lastRelease == 1 && board.Cells.Count == 1, "shot did not produce the 3-match + Water breakaway");
                Expect(lifeGain <= waterGain, "test layout must stay under the cap");
                Expect(Math.Abs(totals[(int)ElementId.Water] - waterGain) < 1e-9 && Math.Abs(totals[(int)ElementId.Life] - lifeGain) < 1e-9,
                    "Water from the same shot must raise the cap before Life is checked (Life " + totals[(int)ElementId.Life] + ", Water " + totals[(int)ElementId.Water] + ")");
                Expect(score == scoreBefore + 3 * rate + breakaway && shortages.Count == 0, "uncapped shot changed score or reported a shortage");

                // 2. Clipping: the next Fire may add only what is left under the cap; the rest is lost, points are not.
                var cell = new Vector2Int(3, 0);
                double life = totals[(int)ElementId.Life], fireTotal = totals[(int)ElementId.Fire];
                double perSphere = fire.ContributionYield(fire.contributions[1], breakaway);
                int bursts = 0;
                while (totals[(int)ElementId.Life] + perSphere <= waterGain + 1e-9)
                {
                    board.Cells[cell] = SphereIds.Fire; visuals[cell] = MakeSphere("Limit fill", SphereIds.Fire, cluster, BoardRules.Position(cell));
                    RemoveCells(new HashSet<Vector2Int> { cell }, breakaway, true); bursts++;
                }
                Expect(shortages.Count == 0, "a shortage was reported before reaching the cap");
                scoreBefore = score; double room = waterGain - totals[(int)ElementId.Life];
                board.Cells[cell] = SphereIds.Fire; visuals[cell] = MakeSphere("Limit overflow", SphereIds.Fire, cluster, BoardRules.Position(cell));
                RemoveCells(new HashSet<Vector2Int> { cell }, breakaway, true);
                Expect(Math.Abs(totals[(int)ElementId.Life] - waterGain) < 1e-9, "Life must stop exactly at the Water total");
                Expect(Math.Abs(totals[(int)ElementId.Fire] - (fireTotal + (bursts + 1) * perSphere)) < 1e-9, "unlimited Fire yield must be unaffected by the cap");
                Expect(score == scoreBefore + breakaway, "the cap must not change points");
                Expect(shortages.Count == 1 && shortages[0].limitedBy == ElementId.Water && shortages[0].element == ElementId.Life && shortages[0].sphere == fire &&
                    Math.Abs(shortages[0].lost - (perSphere - room)) < 1e-9, "shortage must name Water, Life, the sphere and the lost amount");

                // 3. The notice: "Not enough Water" in Water's visualizer color, just above the visualizer, spanning its width.
                yield return null;
                Expect(ShortageNotices.TryGetValue(ElementId.Water, out var notice) && notice, "no shortage notice shown");
                var waterColor = Catalog.ElementColor(ElementId.Water);
                Expect(notice.text == string.Format(visualizerStyle.shortageFormat, "Water") && notice.text == "Not enough Water", "wrong notice text: " + notice.text);
                Expect(Mathf.Approximately(notice.color.r, waterColor.r) && Mathf.Approximately(notice.color.g, waterColor.g) && Mathf.Approximately(notice.color.b, waterColor.b),
                    "notice must use the Water element color");
                var noticeMaterial = notice.fontSharedMaterial;
                Expect(noticeMaterial.IsKeywordEnabled(TMPro.ShaderUtilities.Keyword_Outline) && noticeMaterial.IsKeywordEnabled(TMPro.ShaderUtilities.Keyword_Underlay) &&
                    noticeMaterial.GetFloat(TMPro.ShaderUtilities.ID_OutlineWidth) > 0, "notice needs its dark outline and drop shadow");
                Expect(noticeMaterial == visualizerStyle.shortageMaterial && ShortagePrewarmed && !resourcePanel.Find("Shortage notice prewarm"),
                    "notice must use the saved style material, prewarmed at level start, with no prewarm label left behind");
                var noticeLayer = notice.GetComponent<Canvas>();
                Expect(noticeLayer && noticeLayer.overrideSorting && noticeLayer.sortingOrder > resourcePanel.GetComponentInParent<Canvas>().rootCanvas.sortingOrder, "notice must draw above the HUD text around it");
                // Checked while rising and again in the capture layout: the notice follows the visualizer as it re-lays out.
                void ExpectAboveVisualizer(string when)
                {
                    Expect(notice, "notice vanished " + when);
                    Canvas.ForceUpdateCanvases(); notice.ForceMeshUpdate();
                    var corners = new Vector3[4]; notice.rectTransform.GetWorldCorners(corners); var panel = new Vector3[4]; resourcePanel.GetWorldCorners(panel);
                    Expect(Mathf.Abs((corners[2].x - corners[0].x) - (panel[2].x - panel[0].x)) < .01f && Mathf.Abs((corners[0].x + corners[2].x) - (panel[0].x + panel[2].x)) < .01f,
                        "notice must span the visualizer's full width " + when);
                    Expect(corners[0].y >= panel[1].y - .001f, "notice must sit above the visualizer, not on it " + when);
                    Expect(notice.fontSize > 40, "notice text must be large (" + notice.fontSize + ") " + when);
                }
                for (float t = 0; t < .3f; t += Time.deltaTime) yield return null;
                ExpectAboveVisualizer("while rising");
                // Portrait capture: lay out at 900x1600, let the notice follow the re-laid-out visualizer for a frame, then render.
                var capture = new RenderTexture(900, 1600, 24); capture.Create();
                SetPreviewTarget(capture); yield return null; yield return null;
                ExpectAboveVisualizer("after re-layout");
                RenderPresentation();
                var previous = RenderTexture.active; RenderTexture.active = capture;
                var pixels = new Texture2D(900, 1600, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 900, 1600), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/ContributionLimit-notice.png"), pixels.EncodeToPNG());
                RenderTexture.active = previous; SetPreviewTarget(null); capture.Release(); Destroy(capture); Destroy(pixels);

                // 4. Only this sphere's own Life counts against its cap (Life from other spheres does not).
                totals[(int)ElementId.Life] += 1000; shortages.Clear();
                var extraWater = new Vector2Int(4, 0); board.Cells[extraWater] = SphereIds.Water; visuals[extraWater] = MakeSphere("More water", SphereIds.Water, cluster, BoardRules.Position(extraWater));
                RemoveCells(new HashSet<Vector2Int> { extraWater }, breakaway, true);
                life = totals[(int)ElementId.Life];
                board.Cells[cell] = SphereIds.Fire; visuals[cell] = MakeSphere("Own budget", SphereIds.Fire, cluster, BoardRules.Position(cell));
                RemoveCells(new HashSet<Vector2Int> { cell }, breakaway, true);
                Expect(Math.Abs(totals[(int)ElementId.Life] - life - perSphere) < 1e-9 && shortages.Count == 0, "Life from other spheres must not use up this sphere's cap");
                Expect(Math.Abs(Elements.LimitedYield(SphereIds.Fire, ElementId.Life) - (waterGain + perSphere)) < 1e-9, "ledger must track this sphere type's limited Life");

                // 5. Reset on failure clears limited yield along with totals.
                Elements.Clear();
                Expect(Elements.LimitedYield(SphereIds.Fire, ElementId.Life) == 0 && totals[(int)ElementId.Life] == 0, "ledger reset left stale limited yield");
                Elements.ShortageOccurred -= shortages.Add;
            }
            finally { fire.contributions = originalContributions; fire.unallocatedWeight = originalUnallocated; }
        }

    }

    public sealed class ContributionLimitSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch() { if (LaunchFlags.Has("-contributionLimitSmoke")) new GameObject("Contribution limit checks").AddComponent<ContributionLimitSmoke>(); }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null; yield return null;
            FindAnyObjectByType<SystemView>().OpenPlanet(3); yield return null; yield return null;
            var prep = FindAnyObjectByType<LevelPrepView>(); foreach (int id in new[] { SphereIds.Fire, SphereIds.Void, SphereIds.Stone, SphereIds.Water }) prep.AddSphere(id);
            if (!prep.Begin.interactable) throw new Exception("Contribution limit: setup cannot begin");
            prep.Begin.onClick.Invoke(); yield return null; yield return null;
            var game = FindAnyObjectByType<EcoSpherePrototype>();
            for (float t = 0; t < 1.5f; t += Time.deltaTime) yield return null;
            yield return game.CheckContributionLimits();
            Debug.Log("ECOSPHERE_CONTRIBUTION_LIMIT_PASSED: real-shot resolution raises the cap before checking, Life clipped at Water with excess lost, score and unlimited yield unchanged, per-sphere budget, colored, outlined 'Not enough Water' notice spanning the visualizer just above it, validation, reset");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
