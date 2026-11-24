using System;
using System.Collections.Generic;

namespace EcoSphere
{
    // Reserve ammo held beside the cannon. Chamber 0 feeds the cannon; the ring turns one step at a time:
    // cannon -> last chamber, chamber 0 -> cannon, every other chamber moves one place toward the cannon.
    public sealed class AmmoRing
    {
        public const int MaximumChambers = 4;
        public delegate bool Source(out int type);

        readonly List<int> chambers = new List<int>(MaximumChambers);
        public int ChamberCount { get; }
        public IReadOnlyList<int> Chambers => chambers;

        public AmmoRing(int chamberCount)
        {
            if (chamberCount < 0 || chamberCount > MaximumChambers)
                throw new ArgumentOutOfRangeException(nameof(chamberCount));
            ChamberCount = chamberCount;
        }

        // Tops the ring up to its chamber count. Returns false if the source ran dry.
        public bool Fill(Source source)
        {
            while (chambers.Count < ChamberCount)
            {
                if (!source(out int type)) return false;
                chambers.Add(type);
            }
            return true;
        }

        // Supplies the next cannon type. Without chambers the source loads the cannon directly.
        public bool TryAdvance(Source source, out int cannon)
        {
            if (ChamberCount == 0) return source(out cannon);
            if (!Fill(source) || !source(out int refill)) { cannon = default; return false; }
            cannon = chambers[0];
            chambers.RemoveAt(0);
            chambers.Add(refill);
            return true;
        }

        // Turns the ring one step: the current cannon type goes to the last chamber.
        public int Rotate(int cannon)
        {
            if (ChamberCount == 0 || chambers.Count == 0) return cannon;
            int next = chambers[0];
            chambers.RemoveAt(0);
            chambers.Add(cannon);
            return next;
        }

        // Replaces held types the board no longer contains. Returns the indices that changed.
        public List<int> ReplaceWhere(Func<int, bool> absent, Source source)
        {
            var replaced = new List<int>();
            for (int i = 0; i < chambers.Count; i++)
            {
                if (!absent(chambers[i]) || !source(out int type)) continue;
                chambers[i] = type;
                replaced.Add(i);
            }
            return replaced;
        }
    }
}
