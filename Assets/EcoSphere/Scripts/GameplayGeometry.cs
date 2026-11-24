using UnityEngine;
namespace EcoSphere
{
    // One source for sphere size, grid spacing and the bounds used by gameplay and validation.
    public static class GameplayGeometry
    {
        public const float SphereScale=.96f; // 1.6 times the original .6 scale.
        public const float SphereRadius=.44f*SphereScale;
        public const float CellSpacing=.9f*SphereScale;
        public const int DefaultClusterRings=3;
        public const float Left=-4.35f,Right=4.35f,Top=6.5f,Bottom=-5.9f;
        public static readonly Vector2 ClusterCenter=new Vector2(0,1.6f);
        // The cannon artwork's origin sits at CannonBase; the loaded sphere rests CannonBodyLift above it (at full scale).
        // Its pivot, size and aim limit are authored on AmmoChamberStyle.
        public static readonly Vector2 CannonBase=new Vector2(0,-5.45f);
        public const float CannonBodyLift=.57f*SphereScale;
        public static Vector2 ShotOrigin(Vector2 direction)=>AmmoChamberStyle.Current.ShotOrigin(direction);
        public static float MaxAimAngle=>AmmoChamberStyle.Current.maxAimAngle;
        // Horizontal clearance includes the entire fired sphere alongside the outermost sphere.
        public static int MaximumSafeRings=>Mathf.FloorToInt(Mathf.Min((Right-Left)*.5f-SphereRadius*3-.15f,
            Top-ClusterCenter.y-SphereRadius-.15f,ClusterCenter.y-Bottom-SphereRadius-.15f)/CellSpacing);
        public static int CellCount(int rings)=>3*rings*(rings+1);
    }
}
