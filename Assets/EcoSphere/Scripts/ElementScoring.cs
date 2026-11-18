using UnityEngine;

namespace EcoSphere
{
    public static class ElementScoring
    {
        public const int Breakaway = 50;
        public const int Inclusion = 500;
        public const int Abundance = 1500;
        public static int MatchRate(int count) => count < 3 ? 0 : Mathf.Min(20, 10 + 2 * (count - 3));
    }
}
