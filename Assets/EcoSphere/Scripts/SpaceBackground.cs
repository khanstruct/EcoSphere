using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed class SpaceBackground : MonoBehaviour
    {
        sealed class Star
        {
            public SpriteRenderer renderer;
            public Vector2 viewport;
            public float width, next, duration, idle;
        }
        readonly List<Star> stars = new List<Star>();
        readonly System.Random random = new System.Random(3187);
        Camera cameraView;
        SpriteRenderer field;
        float Between(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        public void Initialize(Camera camera, Sprite starfield, Sprite sparkle)
        {
            cameraView = camera;
            if (starfield) field = Create("Static starfield", starfield, 20, -100);
            if (sparkle)
                for (int i = 0; i < 24; i++)
                {
                    Vector2 uv = new Vector2(Between(.03f, .97f), Between(.04f, .96f));
                    // Most animated stars stay near the sides; a few remain scattered through the center.
                    if (i < 16) uv.x = i % 2 == 0 ? Between(.03f, .24f) : Between(.76f, .97f);
                    var star = new Star {
                        renderer = Create("Twinkle " + (i + 1), sparkle, 19, -99), viewport = uv,
                        width = Between(.19f, .34f), next = Time.time + Between(.3f, 14),
                        duration = Between(.8f, 1.7f), idle = Between(.08f, .2f)
                    };
                    star.renderer.color = new Color(.88f, .95f, 1, star.idle);
                    stars.Add(star);
                }
            RefreshLayout();
        }

        SpriteRenderer Create(string label, Sprite sprite, float depth, int order)
        {
            var item = new GameObject(label); item.transform.SetParent(transform, false);
            item.transform.localPosition = new Vector3(0, 0, depth);
            var renderer = item.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        public void RefreshLayout()
        {
            if (!cameraView) return;
            float height = cameraView.orthographicSize * 2, width = height * cameraView.aspect;
            Vector2 center = cameraView.transform.position;
            if (field)
            {
                field.transform.position = new Vector3(center.x, center.y, 20);
                float scale = Mathf.Max(width / field.sprite.bounds.size.x, height / field.sprite.bounds.size.y);
                field.transform.localScale = Vector3.one * scale;
            }
            foreach (var star in stars)
                star.renderer.transform.position = new Vector3(center.x + (star.viewport.x - .5f) * width,
                    center.y + (star.viewport.y - .5f) * height, 19);
        }

        void LateUpdate()
        {
            RefreshLayout();
            foreach (var star in stars)
            {
                float p = (Time.time - star.next) / star.duration;
                float pulse = p >= 0 && p <= 1 ? Mathf.Pow(Mathf.Sin(p * Mathf.PI), 3) : 0;
                star.renderer.color = new Color(.88f, .95f, 1, Mathf.Lerp(star.idle, .9f, pulse));
                star.renderer.transform.localScale = Vector3.one *
                    (star.width / star.renderer.sprite.bounds.size.x) * (1 + .16f * pulse);
                if (p > 1)
                {
                    star.next = Time.time + Between(5, 16);
                    star.duration = Between(.8f, 1.7f);
                }
            }
        }
    }
}
