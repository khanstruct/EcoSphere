using UnityEngine;

namespace EcoSphere
{
    // Switches a planet view between day and night. Night dims the scene's sun and sets the planet shaders' shared
    // globals toward the PlanetVisualStyle night levels (never fully dark):
    //   _PlanetNight               0..1, luminescent effects (city lights) at full night strength everywhere
    //   _PlanetNightAmbientDim     how much the surface and cloud ambient term is dimmed
    //   _PlanetNightAtmosphereDim  how much the atmosphere glow is dimmed
    // All default to 0, so screens without this component are unaffected; they are reset when it is destroyed.
    // Lava and other emissive effects ignore lighting, so they stand out as the light falls.
    public sealed class PlanetDayNight : MonoBehaviour
    {
        static readonly int Night = Shader.PropertyToID("_PlanetNight");
        static readonly int AmbientDim = Shader.PropertyToID("_PlanetNightAmbientDim");
        static readonly int AtmosphereDim = Shader.PropertyToID("_PlanetNightAtmosphereDim");
        Light sun;
        float dayIntensity, current, target;

        public bool IsNight => target > 0;
        public float Blend => current;   // 0 = day, 1 = night

        public void Initialize(Light sceneSun) { sun = sceneSun; dayIntensity = sun.intensity; Apply(0); }

        public void Toggle() => target = IsNight ? 0 : 1;

        void Update()
        {
            if (!sun || Mathf.Approximately(current, target)) return;
            current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime / PlanetVisualStyle.Current.dayNightTransitionSeconds);
            Apply(current);
        }

        void Apply(float night)
        {
            var style = PlanetVisualStyle.Current;
            sun.intensity = dayIntensity * Mathf.Lerp(1, style.nightLightScale, night);
            Shader.SetGlobalFloat(Night, night);
            Shader.SetGlobalFloat(AmbientDim, (1 - style.nightAmbientScale) * night);
            Shader.SetGlobalFloat(AtmosphereDim, (1 - style.nightAtmosphereScale) * night);
        }

        void OnDestroy()
        {
            Shader.SetGlobalFloat(Night, 0); Shader.SetGlobalFloat(AmbientDim, 0); Shader.SetGlobalFloat(AtmosphereDim, 0);
        }
    }
}
