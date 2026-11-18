using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ButtonEdgeHighlight : MaskableGraphic
    {
        public float phase;
        public float cycleSeconds = 9;
        public float intensity = 1;

        void Update() { SetVerticesDirty(); }

        static Vector2 Point(float distance, float straight, float radius)
        {
            float horizontal = straight * 2;
            float arc = Mathf.PI * radius;
            float length = horizontal * 2 + arc * 2;
            distance = Mathf.Repeat(distance, length);
            if (distance < horizontal) return new Vector2(-straight + distance, radius);
            distance -= horizontal;
            if (distance < arc)
            {
                float angle = Mathf.PI * .5f - distance / radius;
                return new Vector2(straight + Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            distance -= arc;
            if (distance < horizontal) return new Vector2(straight - distance, -radius);
            distance -= horizontal;
            float theta = -Mathf.PI * .5f - distance / radius;
            return new Vector2(-straight + Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            // Match the artwork's capsule inside its transparent canvas padding.
            float radius = rect.height * .245f;
            float straight = Mathf.Max(0, rect.width * .42f - radius);
            float perimeter = straight * 4 + Mathf.PI * radius * 2;
            float head = (Time.unscaledTime / Mathf.Max(1, cycleSeconds) + phase) * perimeter;
            float trail = perimeter * .22f;
            const int segments = 72;
            for (int layer = 0; layer < 4; layer++)
            {
                float width = layer == 0 ? 12 : layer == 1 ? 7 : layer == 2 ? 3 : 1.4f;
                float opacity = layer == 0 ? .035f : layer == 1 ? .09f : layer == 2 ? .35f : .95f;
                Color tint = layer == 3 ? new Color(1, .98f, .8f) : new Color(1, .72f, .17f);
                for (int i = 0; i < segments; i++)
                {
                    float t = (float)i / segments, next = (float)(i + 1) / segments;
                    Vector2 a = rect.center + Point(head - trail * (1 - t), straight, radius);
                    Vector2 b = rect.center + Point(head - trail * (1 - next), straight, radius);
                    Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
                    Color ca = tint, cb = tint;
                    ca.a = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2) * opacity * intensity;
                    cb.a = Mathf.Pow(Mathf.Sin(next * Mathf.PI), 2) * opacity * intensity;
                    int start = vh.currentVertCount;
                    vh.AddVert(a - normal, ca, Vector2.zero); vh.AddVert(a + normal, ca, Vector2.zero);
                    vh.AddVert(b + normal, cb, Vector2.zero); vh.AddVert(b - normal, cb, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                }
            }
        }
    }
}
