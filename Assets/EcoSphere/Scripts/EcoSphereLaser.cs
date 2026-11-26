using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        const int LaserType = 2;

        void RotateLaserContact(Vector2Int cell)
        {
            if (visuals.TryGetValue(cell, out var visual) && visual.TryGetComponent<LaserSphere>(out var laser))
                laser.RotateClockwise();
        }
        bool LaserTesting => LaunchFlags.Has("-ecosphereLaserSmoke");

        IEnumerator LaserSmokeRun()
        {
            void Expect(bool value, string message) { if (!value) throw new System.Exception("Laser: " + message); }
            Expect(loadedType == LaserType && loaded.GetComponent<LaserSphere>(), "First shot must be a laser");
            yield return new WaitForSeconds(.5f);
            CapturePreview();
            System.IO.File.Copy(System.IO.Path.Combine(Application.dataPath, "../Logs/EcoSphere-test-level.png"),
                System.IO.Path.Combine(Application.dataPath, "../Logs/Laser-preview.png"), true);
            yield return new WaitForSeconds(.4f);
            foreach (var visual in visuals.Values) Destroy(visual);
            visuals.Clear(); board.Cells.Clear();
            cluster.rotation = Quaternion.identity;
            var cell = new Vector2Int(1, 0);
            var powerup = MakeSphere("Test laser", LaserType, cluster, BoardRules.Position(cell));
            visuals.Add(cell, powerup); board.Cells.Add(cell, LaserType);
            var laser = powerup.GetComponent<LaserSphere>();
            shots = 1; laser.placedTurn = 1;
            yield return ResolveLasers();
            Expect(laser.turns == 3, "Placement turn must not decrement timer");
            laser.RotateClockwise();
            Expect(laser.directionIndex == 1 && laser.LocalDirection.y < 0, "Hit rotates clockwise 60 degrees");
            for (int i = 0; i < 5; i++) laser.RotateClockwise();
            Expect(laser.directionIndex == 0, "Six rotations wrap");
            // A glancing shot can snap beside a laser after colliding with another sphere.
            for (int shot = 0; shot < 3; shot++)
            {
                var attached = cell + BoardRules.Directions[shot];
                foreach (var offset in BoardRules.Directions) RotateLaserContact(attached + offset);
                Expect(laser.directionIndex == shot + 1, "Each adjacent attachment must rotate exactly once");
            }
            RotateLaserContact(new Vector2Int(20, 20));
            Expect(laser.directionIndex == 3, "Unrelated attachment must not rotate laser");
            for (int i = 0; i < 3; i++) laser.RotateClockwise();
            Expect(Mathf.Abs(LaserDistance(powerup.transform.position, Vector2.left) - (BoardRules.Spacing - Radius)) < .001f,
                "Beam must stop at core surface");
            Expect(Mathf.Abs(LaserDistance(powerup.transform.position, Vector2.right) - (Right - BoardRules.Spacing)) < .001f,
                "Beam must stop at wall");
            for (int i = 2; i <= 4; i++)
            {
                var target = new Vector2Int(i, 0);
                board.Cells.Add(target, 0);
                visuals.Add(target, MakeSphere("Beam target", 0, cluster, BoardRules.Position(target)));
            }
            Expect(board.MatchFrom(cell).Count == 0, "Laser must not match");
            shots++; yield return ResolveLasers(); Expect(laser.turns == 2, "First subsequent turn");
            shots++; yield return ResolveLasers(); Expect(laser.turns == 1, "Second subsequent turn");
            var expected=(double[])totals.Clone();int expectedScore=score;
            foreach(var target in board.Cells)
            {
                Definition(target.Value).AddContributions(expected,ElementScoring.Breakaway);
                if(target.Value!=LaserType)expectedScore+=ElementScoring.Breakaway;
            }
            shots++; yield return ResolveLasers();
            Expect(board.Cells.Count == 0 && score==expectedScore,"Beam consumes laser, pierces all targets and scores");
            for(int i=0;i<totals.Length;i++)Expect(totals[i]==expected[i],"Beam uses the target definitions' weighted elemental yields");
            Debug.Log("ECOSPHERE_LASER_SMOKE_PASSED: first shot, placement grace, six aiming steps, core/wall stop, countdown, piercing, consumption, scoring.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
            #endif
        }

        IEnumerable<int> ElementTypes()
        {
            bool found = false;
            foreach (int type in board.Cells.Values)
                if (Definition(type).AmmoEligible) { found = true; yield return type; }
            // A lone armed laser still needs ordinary turns to finish counting down.
            if (!found && board.Cells.Count > 0) { foreach (int type in activeSphereTypes) yield return type; }
        }

        void MakeLaserDisplay(GameObject root)
        {
            var laser = root.AddComponent<LaserSphere>();
            Sprite icon = Definition(LaserType).icon;
            if (icon)
            {
                // Pivot is the socket center, rather than the asymmetric artwork bounds.
                icon = Sprite.Create(icon.texture, icon.rect, new Vector2(.5f, .44f), icon.pixelsPerUnit);
                generatedSprites.Add(icon);
            }
            laser.mechanism = SpritePart("Laser socket and channel", icon, root.transform,
                new Vector3(0, 0, -Radius - .02f), Radius * 2, 5).transform;
            laser.mechanism.localRotation = Quaternion.Euler(0, 0, -90);
            var digits = new GameObject("Countdown");
            digits.transform.SetParent(root.transform, false);
            digits.transform.localPosition = new Vector3(0, 0, -Radius - .04f);
            laser.countdown = digits.AddComponent<TextMeshPro>();
            laser.countdown.font = uiFont;
            laser.countdown.fontSize = 1.6f;


            laser.countdown.alignment = TextAlignmentOptions.Center;
            laser.countdown.rectTransform.sizeDelta = new Vector2(.22f, .22f);
            laser.countdown.color = new Color(1, .65f, .75f);
            laser.countdown.text = "3";
            var renderer = digits.GetComponent<MeshRenderer>();

            renderer.sortingOrder = 6;
        }

        IEnumerator ResolveLasers()
        {
            var due = new List<Vector2Int>();
            foreach (var entry in visuals)
                if (entry.Value.TryGetComponent<LaserSphere>(out var laser) && laser.placedTurn != shots)
                {
                    laser.turns--;
                    if (laser.turns <= 0) due.Add(entry.Key);
                }
            foreach (var cell in due)
            {
                // Another laser or its breakaway may already have removed this powerup.
                if (!visuals.TryGetValue(cell, out var visual)) continue;
                var laser = visual.GetComponent<LaserSphere>();
                Vector2 origin = visual.transform.position;
                Vector2 direction = cluster.TransformDirection(laser.LocalDirection).normalized;
                float distance = LaserDistance(origin, direction);
                var hit = new HashSet<Vector2Int>();
                foreach (var entry in board.Cells)
                {
                    if (entry.Key == cell) continue;
                    Vector2 point = cluster.TransformPoint(BoardRules.Position(entry.Key));
                    if (BoardRules.Sweep(origin, direction, point, Radius, distance, out _)) hit.Add(entry.Key);
                }
                hit.Add(cell);
                status.text = "LASER FIRING";
                using (Elements.Batch())
                {
                    RemoveCells(hit, ElementScoring.Breakaway, true);
                    RemoveCells(board.Disconnected(), ElementScoring.Breakaway, true);
                }
                yield return LaserBeam(origin, origin + direction * distance);
            }
        }

        float LaserDistance(Vector2 origin, Vector2 direction)
        {
            float distance = float.PositiveInfinity;
            if (direction.x > .00001f) distance = Mathf.Min(distance, (Right - origin.x) / direction.x);
            if (direction.x < -.00001f) distance = Mathf.Min(distance, (Left - origin.x) / direction.x);
            if (direction.y > .00001f) distance = Mathf.Min(distance, (Top - origin.y) / direction.y);
            if (direction.y < -.00001f) distance = Mathf.Min(distance, (Bottom - origin.y) / direction.y);
            if (BoardRules.Sweep(origin, direction, center, Radius, distance, out float coreHit)) distance = coreHit;
            return Mathf.Max(0, distance);
        }

        IEnumerator LaserBeam(Vector2 origin, Vector2 end)
        {
            if (levelAudio) levelAudio.FireLaser();
            var beam = Line("Laser beam", transform, Color.red, Radius, 12);
            beam.positionCount = 2;
            beam.SetPosition(0, new Vector3(origin.x, origin.y, -1));
            beam.SetPosition(1, new Vector3(end.x, end.y, -1));
            const float duration = .28f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float remaining = 1 - t / duration;
                beam.startWidth = beam.endWidth = Radius * remaining * remaining;
                beam.startColor = beam.endColor = new Color(1, 0, 0, remaining);
                yield return null;
            }
            Destroy(beam.gameObject);
        }
    }
}
