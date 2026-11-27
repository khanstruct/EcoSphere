using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // Append shapes only: sphere definitions serialize these values.
    public enum SphereShape { Sphere = 0, Crystal = 1 }

    // Particle burst when a sphere is lost without points (eroded, broken). Append only: definitions serialize these values.
    public enum SphereBurst { None = 0, Crumble = 1, Shatter = 2 }

    [Serializable]
    public sealed class SphereBurstProfile
    {
        [Range(4, 200)] public int particles = 60;
        [Range(.1f, 3)] public float lifetime = .9f;
        [Range(0, 5), Tooltip("Gravity, in multiples of world gravity.")] public float gravity = .6f;
        [Range(.005f, .2f), Tooltip("Particle size relative to a sphere's diameter.")] public float size = .05f;
        [Range(0, 5), Tooltip("Largest initial outward speed, in sphere diameters per second.")] public float speed = .5f;
        [Range(0, 1), Tooltip("Slowest particle's speed as a share of the largest; higher reads as a sharp scatter.")] public float minimumSpeedShare;
    }

    // Visual-only tuning for non-round sphere bodies. Collision, matching and scoring never read it.
    [CreateAssetMenu(menuName = "EcoSphere/Sphere Shape Style")]
    public sealed class SphereShapeStyle : ScriptableObject
    {
        [Header("Crystal mesh (unit diameter; corners touch the sphere footprint)")]
        [Range(.1f, .45f), Tooltip("Radius of the flat front table, as a fraction of the footprint radius.")]
        public float crystalTableRadius = .3f;
        [Range(.05f, .45f), Tooltip("How far the front table rises toward the camera. Keep below the icon depth (.47).")]
        public float crystalCrownDepth = .26f;
        [Range(0, .15f), Tooltip("Half thickness of the straight girdle band around the hexagon.")]
        public float crystalGirdle = .04f;
        [Range(.05f, .5f), Tooltip("Depth of the pointed back (pavilion).")]
        public float crystalPavilionDepth = .3f;
        [Range(0, .2f), Tooltip("Per-facet texture offset so neighboring facets don't read as one continuous surface.")]
        public float crystalFacetTextureJitter = .11f;

        [Header("Crystal material and motion")]
        [Range(0, 1)] public float crystalSmoothness = .75f;
        [Range(0, 1)] public float crystalMetallic = .15f;
        [Range(0, 30), Tooltip("Largest tilt away from facing the camera, in degrees. The outline stays steady instead of spinning edge-on.")]
        public float crystalWobbleDegrees = 13;
        [Range(0, 3), Tooltip("Wobble speed in radians per second.")]
        public float crystalWobbleSpeed = .7f;

        [Header("Bursts when a sphere is lost without points (each sphere picks one in its definition)")]
        [Tooltip("Saved particle material (URP Particles/Unlit) so builds include its shader.")]
        [UnityEngine.Serialization.FormerlySerializedAs("crumbleMaterial")] public Material burstMaterial;
        [Tooltip("Grains that sag and fall away (eroding sand).")]
        public SphereBurstProfile crumble = new SphereBurstProfile { particles = 60, lifetime = .9f, gravity = .6f, size = .05f, speed = .5f };
        [Tooltip("Shards that scatter outward quickly with little gravity (shattering crystal).")]
        public SphereBurstProfile shatter = new SphereBurstProfile { particles = 45, lifetime = .45f, gravity = .05f, size = .065f, speed = 2.2f, minimumSpeedShare = .6f };
        public SphereBurstProfile Profile(SphereBurst burst) =>
            burst == SphereBurst.Crumble ? crumble : burst == SphereBurst.Shatter ? shatter : null;

        static SphereShapeStyle current;
        public static SphereShapeStyle Current
        {
            get
            {
                if (!current) current = Resources.Load<SphereShapeStyle>("SphereShapeStyle");
                if (!current) throw new InvalidOperationException("Missing Resources/SphereShapeStyle asset");
                return current;
            }
        }
        void OnValidate() => SphereShapes.InvalidateMeshes();
    }

    // The one place that turns a SphereShape into a mesh, material finish and idle motion.
    // Gameplay spheres, setup/results previews and discovery reveals all build their bodies here.
    public static class SphereShapes
    {
        static Mesh crystal;
        internal static void InvalidateMeshes() { if (crystal) UnityEngine.Object.DestroyImmediate(crystal); crystal = null; }

        // Builds a body of unit diameter with no collider. Sphere keeps Unity's primitive so planets and existing effects are unchanged.
        public static GameObject CreateBody(SphereShape shape, string name)
        {
            GameObject body;
            if (shape == SphereShape.Sphere)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var collider = body.GetComponent<Collider>();
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider); else UnityEngine.Object.DestroyImmediate(collider);
            }
            else
            {
                body = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                body.GetComponent<MeshFilter>().sharedMesh = Mesh(shape);
            }
            body.name = name;
            return body;
        }

        public static Mesh Mesh(SphereShape shape)
        {
            if (shape != SphereShape.Crystal) throw new ArgumentException("No generated mesh for " + shape);
            if (!crystal) crystal = BuildCrystal(SphereShapeStyle.Current);
            return crystal;
        }

        public static void ApplyFinish(Material material, SphereShape shape)
        {
            if (shape != SphereShape.Crystal || !material.HasProperty("_Smoothness")) return;
            var style = SphereShapeStyle.Current;
            material.SetFloat("_Smoothness", style.crystalSmoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", style.crystalMetallic);
        }

        // Idle orientation. Spheres turn about Y at `spin` degrees per second; crystals keep facing the camera and gently tilt.
        public static Quaternion Orientation(SphereShape shape, float elapsed, float spin, float initialDegrees = 0)
        {
            if (shape != SphereShape.Crystal) return Quaternion.Euler(0, initialDegrees + elapsed * spin, 0);
            var style = SphereShapeStyle.Current;
            float t = elapsed * style.crystalWobbleSpeed, a = style.crystalWobbleDegrees;
            return Quaternion.Euler(a * Mathf.Sin(t * .83f + 1.3f), a * Mathf.Sin(t), 0);
        }

        // A pointy-top hexagonal gem: flat sides face the six board neighbors. The camera looks along +Z,
        // so the table (front) sits at -Z. Every facet has its own vertices for crisp flat shading.
        static Mesh BuildCrystal(SphereShapeStyle style)
        {
            const float radius = .5f;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            Vector3 Ring(float r, float degrees, float z) => new Vector3(r * Mathf.Cos(degrees * Mathf.Deg2Rad), r * Mathf.Sin(degrees * Mathf.Deg2Rad), z);
            int facet = 0;
            void Face(params Vector3[] corners)
            {
                var normal = Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]).normalized;
                var centroid = Vector3.zero; foreach (var c in corners) centroid += c; centroid /= corners.Length;
                // In Unity, Cross(b-a, c-a) of a front face points at the viewer: reverse any face whose normal points inward.
                bool flip = Vector3.Dot(normal, centroid) < 0;
                if (flip) normal = -normal;
                var jitter = new Vector2(Mathf.Sin(facet * 2.39f), Mathf.Cos(facet * 3.71f)) * style.crystalFacetTextureJitter; facet++;
                int start = vertices.Count;
                foreach (var c in corners)
                {
                    vertices.Add(c); normals.Add(normal);
                    uvs.Add(new Vector2(c.x * .5f + .5f, c.y + .5f) + jitter);
                }
                for (int i = 1; i + 1 < corners.Length; i++)
                    if (flip) { triangles.Add(start); triangles.Add(start + i + 1); triangles.Add(start + i); }
                    else { triangles.Add(start); triangles.Add(start + i); triangles.Add(start + i + 1); }
            }
            float g = style.crystalGirdle, crown = -style.crystalCrownDepth, pavilion = style.crystalPavilionDepth, table = style.crystalTableRadius;
            var tableCorners = new Vector3[6];
            for (int i = 0; i < 6; i++) tableCorners[i] = Ring(table, i * 60, crown);
            Face(tableCorners);
            for (int i = 0; i < 6; i++)
            {
                // Outer corners at 30 + 60k keep the hexagon pointy-top; the table is turned 30 degrees so crown facets are triangles.
                float a0 = 30 + i * 60, a1 = a0 + 60;
                Vector3 frontA = Ring(radius, a0, -g), frontB = Ring(radius, a1, -g), backA = Ring(radius, a0, g), backB = Ring(radius, a1, g);
                Vector3 tableMid = tableCorners[(i + 1) % 6], tablePrevious = tableCorners[i];
                Face(frontA, frontB, tableMid);
                Face(tablePrevious, frontA, tableMid);
                Face(frontA, backA, backB, frontB);
                Face(backA, new Vector3(0, 0, pavilion), backB);
            }
            var mesh = new Mesh { name = "Generated crystal" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }
    }
}
