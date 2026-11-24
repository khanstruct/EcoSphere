using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // All coordinates are local to the rotating cluster. Visual transforms never define adjacency.
    public sealed class BoardRules
    {
        public static readonly Vector2Int[] Directions = {
            new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1)
        };
        public readonly Dictionary<Vector2Int, int> Cells = new Dictionary<Vector2Int, int>();
        public readonly HashSet<Vector2Int> Infested = new HashSet<Vector2Int>();
        public const float Spacing = GameplayGeometry.CellSpacing;
        public static Vector2 Position(Vector2Int c) =>
            new Vector2(Spacing * (c.x + c.y * .5f), Spacing * .8660254f * c.y);

        public HashSet<Vector2Int> MatchFrom(Vector2Int start,ISet<Vector2Int> excludedThisShot=null)
        {
            var result = new HashSet<Vector2Int>();
            if (Infested.Contains(start) || (excludedThisShot!=null && excludedThisShot.Contains(start)) || !Cells.TryGetValue(start, out int type)) return result;
            if (!SphereCatalog.Current.Get(type).Matchable) return result;
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start); result.Add(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var d in Directions)
                {
                    var next = cell + d;
                    if (!Infested.Contains(next) && (excludedThisShot==null || !excludedThisShot.Contains(next)) && Cells.TryGetValue(next, out int other) && other == type && result.Add(next))
                        queue.Enqueue(next);
                }
            }
            if (result.Count < 3) result.Clear();
            return result;
        }

        public HashSet<Vector2Int> Disconnected()
        {
            var connected = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var d in Directions)
                if (Cells.ContainsKey(d)) { connected.Add(d); queue.Enqueue(d); }
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var d in Directions)
                {
                    var next = cell + d;
                    if (Cells.ContainsKey(next) && connected.Add(next)) queue.Enqueue(next);
                }
            }
            var loose = new HashSet<Vector2Int>(Cells.Keys);
            loose.ExceptWith(connected);
            return loose;
        }

        // Swept circle versus stationary circle. Direction must be normalized.
        public static bool Sweep(Vector2 origin, Vector2 direction, Vector2 center,
            float combinedRadius, float maximum, out float distance)
        {
            Vector2 offset = origin - center;
            float b = Vector2.Dot(offset, direction);
            float c = offset.sqrMagnitude - combinedRadius * combinedRadius;
            float discriminant = b * b - c;
            distance = 0;
            if (c <= 0) return true;
            if (b >= 0 || discriminant < 0) return false;
            distance = -b - Mathf.Sqrt(discriminant);
            return distance >= 0 && distance <= maximum;
        }

        public static Vector2 Reflect(Vector2 direction, Vector2 normal) =>
            direction - 2 * Vector2.Dot(direction, normal) * normal;

        public static bool TouchesEdge(Vector2 point, float radius, float left, float right, float top, float bottom) =>
            point.x - radius <= left + .00001f || point.x + radius >= right - .00001f ||
            point.y - radius <= bottom + .00001f || point.y + radius >= top - .00001f;

        public static bool CircleTouchesTriangle(Vector2 point, float radius, Vector2 a, Vector2 b, Vector2 c)
        {
            float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
            float ab = Cross(b - a, point - a), bc = Cross(c - b, point - b), ca = Cross(a - c, point - c);
            if (Mathf.Abs(Cross(b - a, c - a)) > .000001f &&
                ((ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0))) return true;
            float DistanceSquared(Vector2 start, Vector2 end)
            {
                Vector2 edge = end - start;
                float t = edge.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - start, edge) / edge.sqrMagnitude) : 0;
                return (point - (start + edge * t)).sqrMagnitude;
            }
            float square = radius * radius + .000001f;
            return DistanceSquared(a, b) <= square || DistanceSquared(b, c) <= square || DistanceSquared(c, a) <= square;
        }

        // The guide follows the same center-of-sphere wall limits as the live projectile.
        public static List<Vector2> AimPath(Vector2 origin, Vector2 direction, float length,
            float left, float right, float top, float bottom,
            IReadOnlyList<Vector2> obstacles = null, float combinedRadius = 0)
        {
            return TraceAimPath(origin,direction,length,left,right,top,bottom,new List<Vector2>(13),obstacles,combinedRadius);
        }

        public static List<Vector2> TraceAimPath(Vector2 origin, Vector2 direction, float length,
            float left, float right, float top, float bottom, List<Vector2> points,
            IReadOnlyList<Vector2> obstacles = null, float combinedRadius = 0)
        {
            return ProjectileSimulation.Trace(origin,direction,length,new ShotBounds(left,right,top,bottom),obstacles,combinedRadius,points);
        }
    }
}
