using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SystemMapGraphic : MaskableGraphic
    {
        public bool circle;
        public float orbitHeight = .5f;
        public float thickness = 2;
        public float planetX;
        public float planetGapRadius = 49;
        public RectTransform planetSlot;
        public bool selected;
        public static float OrbitY(float x, float height) => height - .11f * x * x;
        public float RingRadius => Mathf.Max(1, Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f - 3);
        float lastScale;

        protected override void OnEnable() { base.OnEnable(); lastScale = 0; }
        void Update()
        {
            float scale = canvas ? canvas.scaleFactor : 1;
            if (Mathf.Abs(scale - lastScale) > .001f || selected)
            { lastScale = scale; SetVerticesDirty(); }
        }
        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera)) return false;
            if (!circle) return true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local);
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            return (local - rectTransform.rect.center).sqrMagnitude <= radius * radius;
        }
        Vector2 OrbitPoint(float x)
        {
            var r = rectTransform.rect;
            float normalized = (x - r.center.x) / r.width;
            return new Vector2(x, r.yMin + OrbitY(normalized, orbitHeight) * r.height);
        }
        float Join(Vector2 center, float radius, int side)
        {
            float inside = center.x, outside = center.x + side * radius * 2;
            for (int i = 0; i < 24; i++)
            {
                float mid = (inside + outside) * .5f;
                if ((OrbitPoint(mid) - center).sqrMagnitude < radius * radius) inside = mid;
                else outside = mid;
            }
            return (inside + outside) * .5f;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float scale = Mathf.Max(.1f, canvas ? canvas.scaleFactor : 1);
            if (circle)
            {
                int count = Mathf.Clamp(Mathf.CeilToInt(2 * Mathf.PI * RingRadius * scale / 3), 48, 1024);
                Strip(vh, 0, 2 * Mathf.PI, count, scale);
            }
            else
            {
                Vector2 center = OrbitPoint(r.center.x + planetX * r.width);
                float radius = planetGapRadius;
                if (planetSlot)
                {
                    center = rectTransform.InverseTransformPoint(planetSlot.TransformPoint(planetSlot.rect.center));
                    var ring = planetSlot.GetComponent<SystemMapGraphic>();
                    // Meet the ring's centerline; its opaque stroke covers the join.
                    if (ring) radius = ring.RingRadius;
                }
                float left = r.xMin - r.width * .05f, right = r.xMax + r.width * .05f;
                float joinLeft = Join(center, radius, -1), joinRight = Join(center, radius, 1);
                Strip(vh, left, joinLeft, Mathf.Clamp(Mathf.CeilToInt((joinLeft - left) * scale / 5), 8, 1024), scale);
                Strip(vh, joinRight, right, Mathf.Clamp(Mathf.CeilToInt((right - joinRight) * scale / 5), 8, 1024), scale);
            }
        }
        void Strip(VertexHelper vh, float from, float to, int segments, float scale)
        {
            float aa = 1 / scale;
            float half = thickness * .5f;
            float pulse = selected ? .9f + .1f * Mathf.Sin(Time.unscaledTime * 2) : 1;
            int start = vh.currentVertCount;
            // Shared vertices and analytic normals prevent seams between segments.
            for (int i = 0; i <= segments; i++)
            {
                float t = Mathf.Lerp(from, to, (float)i / segments);
                Vector2 point, normal;
                if (circle)
                {
                    normal = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                    point = rectTransform.rect.center + normal * RingRadius;
                }
                else
                {
                    point = OrbitPoint(t);
                    var r = rectTransform.rect;
                    float slope = -.22f * (t - r.center.x) * r.height / (r.width * r.width);
                    normal = new Vector2(-slope, 1).normalized;
                }
                for (int band = 0; band < 6; band++)
                {
                    float offset = band == 0 ? -half - 3 * aa : band == 1 ? -half - aa : band == 2 ? -half : band == 3 ? half : band == 4 ? half + aa : half + 3 * aa;
                    Color tint = color;
                    tint.a *= (band == 0 || band == 5 ? 0 : band == 1 || band == 4 ? .06f : 1) * pulse;
                    vh.AddVert(point + normal * offset, tint, Vector2.zero);
                }
                if (i == 0) continue;
                int previous = start + (i - 1) * 6, current = start + i * 6;
                for (int band = 0; band < 5; band++)
                {
                    vh.AddTriangle(previous + band, previous + band + 1, current + band + 1);
                    vh.AddTriangle(previous + band, current + band + 1, current + band);
                }
            }
        }
    }
}
