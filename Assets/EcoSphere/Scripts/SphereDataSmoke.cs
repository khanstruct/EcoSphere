using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        bool SphereDataTesting => LaunchFlags.Has("-sphereDataSmoke");

        IEnumerator SphereDataSmokeRun()
        {
            yield return new WaitForSeconds(.3f);
            void Expect(bool value, string message) { if (!value) throw new Exception("Sphere data: " + message); }
            var glowTest=MakeSphere("Void halo test",SphereIds.Void,cluster,Vector3.zero);
            var halo=glowTest.transform.Find("Sphere glow");
            Expect(halo && halo.GetComponent<SpriteRenderer>().sprite.texture.width==256,"Void halo missing or exceeds the sprite limit");
            Expect(halo.GetComponent<SpriteRenderer>().color==new Color(1,1,1,Definition(SphereIds.Void).glowStrength),"Void halo must use its authored white strength");
            StartCoroutine(DissolveMatch(glowTest));
            Expect(halo.GetComponent<SpriteRenderer>().color.a<=Definition(SphereIds.Void).glowStrength,"match dissolve brightened the subtle halo");
            var fire = Definition(SphereIds.Fire);
            var original = fire.contributions;
            int originalUnallocated = fire.unallocatedWeight;
            try
            {
                // Exercise the actual removal path with a designer-authored, four-element sphere.
                fire.contributions = new List<ElementContribution> {
                    new ElementContribution(ElementId.Fire, 1), new ElementContribution(ElementId.Water, 1),
                    new ElementContribution(ElementId.Stone, 1), new ElementContribution(ElementId.Void, 1) };
                fire.unallocatedWeight=0;
                foreach (var visual in visuals.Values) Destroy(visual);
                visuals.Clear(); board.Cells.Clear(); Array.Clear(totals, 0, totals.Length);
                displayedElements.Clear();
                IncludeElements(fire); IncludeElements(Definition(SphereIds.Void));
                var cell = new Vector2Int(1, 0);
                board.Cells[cell] = SphereIds.Fire;
                visuals[cell] = MakeSphere("Mixed fire smoke", SphereIds.Fire, cluster, BoardRules.Position(cell));
                var released = visuals[cell];
                RemoveCells(new HashSet<Vector2Int> { cell }, ElementScoring.Breakaway, true);
                double quarter = ElementScoring.Breakaway / 4.0;   // four equal contribution units
                Expect(Math.Abs(totals[(int)ElementId.Fire] - quarter) < 1e-10 && Math.Abs(totals[(int)ElementId.Water] - quarter) < 1e-10 &&
                    Math.Abs(totals[(int)ElementId.Stone] - quarter) < 1e-10 && Math.Abs(totals[(int)ElementId.Void] - quarter) < 1e-10, "breakaway omitted an element or used a sphere ID as an element ID");
                Expect(released.GetComponent<TrailRenderer>().startColor == fire.color, "streak did not use the definition color");
                cell = new Vector2Int(2, 0); board.Cells[cell] = SphereIds.Void;
                visuals[cell] = MakeSphere("Void smoke", SphereIds.Void, cluster, BoardRules.Position(cell));
                RemoveCells(new HashSet<Vector2Int> { cell }, ElementScoring.StandardMatch);
                var voidSphere = Definition(SphereIds.Void);
                double voidFromMatch = 0; var voidOnly = new double[totals.Length]; voidSphere.AddContributions(voidOnly, ElementScoring.StandardMatch); voidFromMatch = voidOnly[(int)ElementId.Void];
                Expect(Math.Abs(totals[(int)ElementId.Void] - (voidFromMatch + quarter)) < 1e-10, "shared planet-wide element total did not accumulate");
                UpdateVisualizer();
                // One channel per distinct contributed element (Docs/Element-composition.md), in first-appearance order.
                var expectedElements = new List<ElementId>();
                foreach (var definition in new[] { fire, Definition(SphereIds.Void) })
                    foreach (var entry in definition.contributions)
                        if (entry.amount > 0 && !expectedElements.Contains(entry.element)) expectedElements.Add(entry.element);
                Expect(resourceLabels.Count == expectedElements.Count && System.Linq.Enumerable.SequenceEqual(displayedElements, expectedElements),
                    "each distinct contributed element must have exactly one channel");
                for (int i = 0; i < displayedElements.Count; i++)
                {
                    Expect(barTotals[i].text == Math.Truncate(totals[(int)displayedElements[i]]).ToString("0"), "channel did not show its element's truncated total");
                    Expect(energyBars[i].color == (Catalog.ElementAppearance(displayedElements[i])?.color ?? Color.white), "element bar must use its namesake sphere color");
                }
                Expect(barTotals[displayedElements.IndexOf(ElementId.Void)].text == Math.Truncate(voidFromMatch + quarter).ToString("0"), "Void contributions from both spheres must share one channel");
                yield return null;
                foreach (var size in new[] { new Vector2Int(900, 1600), new Vector2Int(1600, 900) })
                {
                    var target = new RenderTexture(size.x, size.y, 24); target.Create();
                    SetPreviewTarget(target); RenderPresentation();
                    var previous = RenderTexture.active; RenderTexture.active = target;
                    var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/SphereData-" + size.x + "x" + size.y + ".png"), pixels.EncodeToPNG());
                    RenderTexture.active = previous; SetPreviewTarget(null); target.Release(); Destroy(target); Destroy(pixels);
                }
                fire.contributions = new List<ElementContribution> { new ElementContribution(ElementId.Fire,1) };
                fire.unallocatedWeight = 1;
                Array.Clear(totals,0,totals.Length);
                int scoreBefore = score;
                cell = new Vector2Int(3,0); board.Cells[cell] = SphereIds.Fire;
                visuals[cell] = MakeSphere("Weighted match",SphereIds.Fire,cluster,BoardRules.Position(cell));
                RemoveCells(new HashSet<Vector2Int> { cell },12);
                Expect(totals[(int)ElementId.Fire]==6 && score==scoreBefore+12,"unused weight changed match score or failed to reduce element yield");
                cell = new Vector2Int(4,0); board.Cells[cell] = SphereIds.Fire;
                visuals[cell] = MakeSphere("Weighted breakaway",SphereIds.Fire,cluster,BoardRules.Position(cell));
                RemoveCells(new HashSet<Vector2Int> { cell },50,true);
                Expect(totals[(int)ElementId.Fire]==31 && score==scoreBefore+62,"unused weight changed breakaway score or yield");
                int channelsBefore = displayedElements.Count;
                cell = new Vector2Int(5,0); board.Cells[cell] = SphereIds.Laser;
                visuals[cell] = MakeSphere("Unselected natural powerup",SphereIds.Laser,cluster,BoardRules.Position(cell));
                RemoveCells(new HashSet<Vector2Int> { cell },50,true);
                UpdateVisualizer();
                Expect(displayedElements.Count==channelsBefore && !displayedElements.Contains(ElementId.Laser) && !composition.IsLeading(ElementId.Laser),"collecting an unselected sphere added a channel or leader");
                var result = GameServices.Progress.Complete(9, 0, -.02f, 0,null, new double[] { 0, 0, 0, 620, 620, 0, 0, 0 });
                Expect(result.discoveries.Contains(SphereIds.Sunlight), "Fire + Void must unlock the solar sphere after inclusion");
                Expect(GameServices.Progress.IsSphereAvailable(SphereIds.Sunlight), "recipe discovery did not enter the collection");
                result = GameServices.Progress.Complete(9, 1, -.02f, 0,null, new double[] { 0, 0, 0, 620, 620, 0, 0, 0 });
                Expect(result.discoveries.Count == 0, "recipe discovery awarded twice");
            }
            finally { fire.contributions = original; fire.unallocatedWeight = originalUnallocated; }
            Debug.Log("ECOSPHERE_SPHERE_DATA_SMOKE_PASSED: four-element breakaway, shared totals, streak/visualizer colors, dynamic portrait/wide channels, solar recipe and unique unlock");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
