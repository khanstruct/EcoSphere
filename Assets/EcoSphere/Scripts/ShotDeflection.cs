using UnityEngine;

namespace EcoSphere
{
    public enum ShotImpact { Attach, Deflect, Break }

    // Shared rule for what a shot does when it touches a board sphere. Data comes from SphereDefinition.deflectedHits,
    // so any sphere can become a deflector without scene code naming it.
    public static class ShotDeflection
    {
        public static ShotImpact Resolve(SphereDefinition target, int shotType, int hitsTaken)
        {
            if (!target || target.deflectedHits <= 0 || target.id == shotType) return ShotImpact.Attach;
            return hitsTaken < target.deflectedHits ? ShotImpact.Deflect : ShotImpact.Break;
        }
    }
}
