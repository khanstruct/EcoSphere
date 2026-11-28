using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal const string FreedGlowName = "Freed glow";

        // A sphere that stays when freed (SphereDefinition.staysWhenFreed, e.g. Moon) keeps its cell after a breakaway cuts it
        // off from the core, unless it is hazarded. Every breakaway (shots, lost shots, the laser) asks this, not Disconnected.
        bool StaysWhenFreed(Vector2Int cell) => Definition(board.Cells[cell]).staysWhenFreed && !IsHazarded(cell);

        HashSet<Vector2Int> FallingAfterBreak()
        {
            var loose = board.Disconnected();
            loose.RemoveWhere(StaysWhenFreed);
            return loose;
        }

        // Board spheres cut off from the core that stayed: they show their freed glow until reconnected, matched or knocked loose.
        internal HashSet<Vector2Int> FreedCells()
        {
            var loose = board.Disconnected();
            loose.RemoveWhere(cell => !StaysWhenFreed(cell));
            return loose;
        }

        // Adds or removes each board sphere's freed glow to match the board. Ammo, chambers and connected spheres never have one.
        void RefreshFreedGlows()
        {
            var freed = FreedCells();
            foreach (var entry in visuals)
            {
                if (!entry.Value) continue;
                var glow = entry.Value.transform.Find(FreedGlowName);
                bool show = freed.Contains(entry.Key);
                if (show && !glow)
                {
                    var definition = Definition(board.Cells[entry.Key]);
                    SphereGlow.Create(entry.Value.transform, definition.freedGlowColor, definition.freedGlowStrength, Radius * 2, entry.Value.layer, FreedGlowName);
                }
                else if (!show && glow) Destroy(glow.gameObject);
            }
        }
    }
}
