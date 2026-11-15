using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // UI geometry stays sharp at any resolution, without extra texture assets.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SystemMapGraphic : MaskableGraphic
    {
        public bool circle;
        public float orbitHeight = .5f;
        public float thickness = 2;
        public float planetX;

        public static float OrbitY(float x, float height) => height - .11f * x * x;

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera)) return false;
            if (!circle) return true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local);
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            return (local - rectTransform.rect.center).sqrMagnitude <= radius * radius;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            const int segments = 160;
            for (int i = 0; i < segments; i++)
            {
                Vector2 a, b;
                if (circle)
                {
                    float radius = Mathf.Min(r.width, r.height) * .5f - thickness;
                    float t = i * Mathf.PI * 2 / segments;
                    float u = (i + 1) * Mathf.PI * 2 / segments;
                    a = r.center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius;
                    b = r.center + new Vector2(Mathf.Cos(u), Mathf.Sin(u)) * radius;
                }
                else
                {
                    float x = Mathf.Lerp(-.55f, .55f, (float)i / segments);
                    float next = Mathf.Lerp(-.55f, .55f, (float)(i + 1) / segments);
                    if (Mathf.Abs((x + next) * .5f - planetX) * r.width < 49) continue;
                    a = new Vector2(r.center.x + x * r.width, r.yMin + OrbitY(x, orbitHeight) * r.height);
                    b = new Vector2(r.center.x + next * r.width, r.yMin + OrbitY(next, orbitHeight) * r.height);
                }
                Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * thickness * .5f;
                int start = vh.currentVertCount;
                vh.AddVert(a - normal, color, Vector2.zero);
                vh.AddVert(a + normal, color, Vector2.zero);
                vh.AddVert(b + normal, color, Vector2.zero);
                vh.AddVert(b - normal, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
