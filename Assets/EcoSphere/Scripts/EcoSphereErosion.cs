using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal int ErosionCount { get; private set; }
        internal Vector2Int? LastEroded { get; private set; }

        // A hazarded cell (currently: infested by an alien) doesn't count as its sphere type for type-based rules.
        // Future hazards join here.
        bool IsHazarded(Vector2Int cell) => board.Infested.Contains(cell);

        // The eroding sphere farthest from the core; ties go to the lowest, then leftmost cell so the choice is stable.
        internal Vector2Int? ErosionTarget()
        {
            Vector2Int? best = null; float bestDistance = -1;
            foreach (var entry in board.Cells)
            {
                if (!Definition(entry.Value).erodesOnMiss || IsHazarded(entry.Key)) continue;
                float distance = BoardRules.Position(entry.Key).sqrMagnitude;
                var cell = entry.Key;
                if (best == null || distance > bestDistance + 1e-5f ||
                    (Mathf.Abs(distance - bestDistance) <= 1e-5f && (cell.y < best.Value.y || (cell.y == best.Value.y && cell.x < best.Value.x))))
                { best = cell; bestDistance = distance; }
            }
            return best;
        }

        // After a shot of a matchable sphere that made no match: one eroding sphere disappears (no points or elements).
        // Callers resolve the breakaway afterwards, so anything it held falls away normally and the ammo check sees the result.
        bool ErodeAfterMiss(int shotType)
        {
            if (!Definition(shotType).Matchable) return false;
            var target = ErosionTarget();
            if (target == null) return false;
            var definition = Definition(board.Cells[target.Value]);
            LoseSphere(target.Value, definition.erodeSound);
            ErosionCount++; LastEroded = target;
            return true;
        }

        // A shot lost out the bottom makes no match: erode, then let anything freed fall away as a normal breakaway.
        void ErodeAfterLostShot(int shotType)
        {
            if (!ErodeAfterMiss(shotType)) return;
            resolutionSpherePoints.Clear();
            using (Elements.Batch())
            {
                var released = board.Disconnected(); lastRelease = released.Count;
                RecordResolution(0, lastRelease, 0);
                RemoveCells(released, ElementScoring.Breakaway, true, voicePoints: resolutionSpherePoints);
            }
            if (levelAudio) { levelAudio.Resolve(0, lastRelease); levelAudio.AnnounceResolution(resolutionSpherePoints); }
            complete = board.Cells.Count == 0;
        }

        // Removes a sphere without points or elements (a broken deflector, an eroded sphere): it dissolves, plays its sound
        // and bursts into its definition's particles (Sand crumbles, Crystal shatters).
        void LoseSphere(Vector2Int cell, AudioClip sound)
        {
            if (board.Infested.Contains(cell)) KnockOffAlien(cell, false);
            var definition = Definition(board.Cells[cell]);
            var visual = visuals[cell]; visuals.Remove(cell); board.Cells.Remove(cell);
            SphereBursts.Spawn(definition.lostBurst, visual.transform.position, Radius * 2 * cluster.lossyScale.x, definition.LostBurstColor, cluster.gameObject.layer);
            StartCoroutine(DissolveMatch(visual));
            if (levelAudio && sound) levelAudio.PlayEffect(sound);
        }
    }
}
