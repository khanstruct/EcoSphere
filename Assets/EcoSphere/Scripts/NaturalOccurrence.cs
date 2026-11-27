using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // At most one naturally occurring sphere per system. When a system is created, each procedural world in slot order has
    // WorldCatalog.naturalOccurrenceChance to hold one; the first that rolls it gets a sphere the player doesn't own yet
    // (weighted by SphereDefinition.naturalWeight) and the rest of the system gets none. The sphere joins that world's
    // natural placements, so the board, the release check and the Natural discovery are the same ones authored natural
    // spheres (Genesis Beta's Stone) use.
    public static class NaturalOccurrence
    {
        // Its own stream from the system seed, so adding or tuning this never reshuffles names, rules or boards.
        const int StreamSalt = 0x4e415455;
        // Ring 1 touches the core: a natural sphere there could never break away, and it can't be matched (it is never ammo).
        public const int InnermostRing = 2;

        public static int Ring(Vector2Int cell) => Mathf.Max(Mathf.Abs(cell.x), Mathf.Abs(cell.y), Mathf.Abs(cell.x + cell.y));
        // Where any natural sphere, rolled or authored, may sit: inside the cluster and never attached to the core.
        public static bool IsAllowedCell(Vector2Int cell, int rings) => Ring(cell) >= InnermostRing && Ring(cell) <= rings;

        // Returns the world that received a natural sphere, or null.
        public static WorldRecord Roll(SystemRecord system, IReadOnlyList<WorldRecord> worlds, PlayerProfile profile, SphereCatalog catalog, float chance)
        {
            var random = new System.Random(system.seed ^ StreamSalt);
            foreach (var world in worlds)
            {
                if (!world.IsProcedural || random.NextDouble() >= chance) continue;
                // This world rolled it; whatever happens next, no other world in the system gets one.
                var sphere = PickUnowned(random, profile, catalog);
                if (sphere == null || !TryPickCell(world, random, out var cell)) return null;
                world.naturalSpheres.Add(new NaturalSpherePlacement { sphereId = sphere.id, cell = cell });
                return world;
            }
            return null;
        }

        static SphereDefinition PickUnowned(System.Random random, PlayerProfile profile, SphereCatalog catalog)
        {
            var candidates = new List<SphereDefinition>();
            double total = 0;
            foreach (var sphere in catalog.spheres)
                if (sphere.OccursNaturally && !ProfileService.Owns(profile, sphere.id)) { candidates.Add(sphere); total += sphere.naturalWeight; }
            if (candidates.Count == 0) return null;
            candidates.Sort((a, b) => a.id.CompareTo(b.id));   // stable regardless of catalog list order
            double pick = random.NextDouble() * total;
            foreach (var sphere in candidates) { pick -= sphere.naturalWeight; if (pick < 0) return sphere; }
            return candidates[candidates.Count - 1];
        }

        // Any allowed cell, the outer ring included: it can't be matched, so it always has to be broken away.
        static bool TryPickCell(WorldRecord world, System.Random random, out Vector2Int cell)
        {
            int rings = world.rules.clusterRings;
            var pool = new List<Vector2Int>();
            for (int q = -rings; q <= rings; q++) for (int r = -rings; r <= rings; r++)
            {
                var candidate = new Vector2Int(q, r);
                if (IsAllowedCell(candidate, rings) && !world.naturalSpheres.Exists(natural => natural.cell == candidate)) pool.Add(candidate);
            }
            cell = pool.Count > 0 ? pool[random.Next(pool.Count)] : default;
            return pool.Count > 0;
        }
    }
}
