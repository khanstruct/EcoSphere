using UnityEngine;

namespace EcoSphere
{
    // A short particle burst where a sphere was lost: Crumble grains fall away, Shatter shards scatter outward.
    // Profiles and the shared material live in SphereShapeStyle; each sphere picks its burst and color in its definition.
    public static class SphereBursts
    {
        public static ParticleSystem Spawn(SphereBurst burst, Vector3 position, float diameter, Color color, int layer)
        {
            var style = SphereShapeStyle.Current; var profile = style.Profile(burst);
            if (profile == null) return null;
            var go = new GameObject("Sphere burst (" + burst + ")"); go.layer = layer; go.transform.position = position;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = .1f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(profile.lifetime * .6f, profile.lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(profile.speed * profile.minimumSpeedShare * diameter, profile.speed * diameter);
            main.startSize = new ParticleSystem.MinMaxCurve(profile.size * diameter * .6f, profile.size * diameter * 1.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(color * .8f, color);
            main.gravityModifier = profile.gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)profile.particles) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = diameter * .45f;
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = style.burstMaterial;
            particles.Play();
            return particles;
        }
    }
}
