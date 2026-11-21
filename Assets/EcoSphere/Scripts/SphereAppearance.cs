using UnityEngine;

namespace EcoSphere
{
    public sealed class SphereAppearance : MonoBehaviour
    {
        public Transform surface;
        public Transform icon;
        public Transform glow;
        public float spin = 12;
        float longitude;
        void LateUpdate()
        {
            // Only the mesh turns: neither travel nor the board imparts rolling to the sphere.
            longitude += spin * Time.deltaTime;
            if (surface) surface.rotation = Quaternion.Euler(0, longitude, 0);
            if (icon) icon.rotation = Quaternion.identity;
            if (glow) glow.rotation = Quaternion.identity;
        }
    }
}
