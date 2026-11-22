using System;
using System.Collections.Generic;

namespace EcoSphere
{
    public sealed class ShotSelector
    {
        readonly Random random;
        int previousType, consecutive;
        readonly List<int> candidates=new List<int>();
        readonly HashSet<int> unique=new HashSet<int>();

        public ShotSelector() : this(new Random()) { }
        public ShotSelector(Random random) { this.random = random; }

        public bool TryNext(IEnumerable<int> remainingTypes, out int type)
        {
            // Sample types, not individual spheres: abundance must not increase a type's odds.
            candidates.Clear();unique.Clear();
            foreach(int candidate in remainingTypes)if(unique.Add(candidate))candidates.Add(candidate);
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
