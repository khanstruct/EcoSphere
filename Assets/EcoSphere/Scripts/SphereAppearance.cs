using UnityEngine;

namespace EcoSphere
{
    public sealed class SphereAppearance : MonoBehaviour
    {
        public Transform surface;
        public Transform icon;
        public Transform glow;
        public float spin = 12;
        public SphereShape shape;
        float elapsed;
        void Start() { if (shape != SphereShape.Sphere) elapsed = Random.Range(0, 100f); } // De-synchronize wobbling neighbors.
        void LateUpdate()
        {
            // Only the body moves: neither travel nor the board imparts rolling to the sphere.
            elapsed += Time.deltaTime;
            if (surface) surface.rotation = SphereShapes.Orientation(shape, elapsed, spin);
            if (icon) icon.rotation = Quaternion.identity;
            if (glow) glow.rotation = Quaternion.identity;
        }
    }
}
