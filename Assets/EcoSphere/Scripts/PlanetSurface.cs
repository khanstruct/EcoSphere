using UnityEngine;

namespace EcoSphere
{
    // Height data and appearance remain separate so water can fill the terrain later.
    public sealed class PlanetSurface : MonoBehaviour
    {
        [Range(-.02f, 1.02f)] public float seaLevel = -.02f;
        public float rotationSpeed = 2f;
        Renderer surface;
        MaterialPropertyBlock properties;

        void Awake()
        {
            surface = GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            surface.GetPropertyBlock(properties);
            properties.SetFloat("_SeaLevel", seaLevel);
            surface.SetPropertyBlock(properties);
        }
    }
}
