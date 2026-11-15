using System;
using System.Collections.Generic;

namespace EcoSphere
{
    public sealed class ShotSelector
    {
        readonly Random random;
        int previousType, consecutive;

        public ShotSelector() : this(new Random()) { }
        public ShotSelector(Random random) { this.random = random; }

        public bool TryNext(IEnumerable<int> remainingTypes, out int type)
        {
            // Sample types, not individual spheres: abundance must not increase a type's odds.
            var candidates = new List<int>(new HashSet<int>(remainingTypes));
            candidates.Sort();
            if (candidates.Count == 0) { type = default; return false; }
            if (consecutive >= 3 && candidates.Count > 1) candidates.Remove(previousType);
            type = candidates[random.Next(candidates.Count)];
            consecutive = consecutive > 0 && type == previousType ? consecutive + 1 : 1;
            previousType = type;
            return true;
        }
    }
}
