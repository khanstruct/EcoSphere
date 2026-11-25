using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // Shapes of the starting groups in procedural clusters. Shared data on the world catalog.
    [Serializable] public sealed class ClusterGroupSettings
    {
        [Tooltip("Relative frequency of starting patches of 1, 2, 3, 4... matching spheres (index 0 = singles).")]
        public float[] sizeWeights = { 40, 32, 20, 8 };
        [Tooltip("How strongly a world's clustering value shifts patch sizes: low clustering favors singles, high favors larger patches.")]
        [Range(0, 3)] public float clusteringTilt = 1;
        [Tooltip("Chance that a patch may take the same type as a neighboring patch (merging into a larger group), at clustering 0 and 1.")]
        [Range(0, 1)] public float minimumMergeChance = .03f, maximumMergeChance = .25f;

        public void Validate()
        {
            if (sizeWeights == null || sizeWeights.Length == 0) throw new InvalidOperationException("Cluster group sizes need at least one weight");
            double sum = 0;
            foreach (float weight in sizeWeights) { if (weight < 0 || float.IsNaN(weight)) throw new InvalidOperationException("Cluster group weights must be non-negative"); sum += weight; }
            if (sum <= 0) throw new InvalidOperationException("Cluster group weights must not all be zero");
            if (minimumMergeChance > maximumMergeChance) throw new InvalidOperationException("Cluster merge chance range is inverted");
        }
    }

    public static class ProceduralCluster
    {
        // Splits the cluster into small connected patches (sizes drawn from the settings, tilted by clustering), then
        // gives each patch a type, avoiding the types of neighboring patches except for an occasional deliberate merge.
        // Every type receives an exact equal share; patches are split when a type's remaining share is too small.
        public static void Populate(BoardRules board, IReadOnlyList<int> types, int rings, int seed, float clustering, ClusterGroupSettings settings = null)
        {
            if (types == null || types.Count < 2 || new HashSet<int>(types).Count != types.Count || rings < 2)
                throw new ArgumentException("A procedural cluster needs distinct sphere types and at least two rings");
            settings = settings ?? new ClusterGroupSettings();
            settings.Validate();
            var random = new System.Random(seed);
            var cells = new List<Vector2Int>();
            for (int q = -rings; q <= rings; q++) for (int r = -rings; r <= rings; r++)
                if (Mathf.Max(Mathf.Abs(q), Mathf.Abs(r), Mathf.Abs(q + r)) <= rings && (q != 0 || r != 0)) cells.Add(new Vector2Int(q, r));
            if (types.Count > cells.Count) throw new ArgumentException("Too many types for this cluster");
            int total = cells.Count;
            var remaining = new int[types.Count];
            for (int i = 0; i < types.Count; i++) remaining[i] = total / types.Count + (i < total % types.Count ? 1 : 0);
            clustering = Mathf.Clamp01(clustering);
            board.Cells.Clear(); board.Infested.Clear();

            var patches = BuildPatches(cells, random, TiltedWeights(settings, clustering));
            double mergeChance = Mathf.Lerp(settings.minimumMergeChance, settings.maximumMergeChance, clustering);
            var pending = new List<List<Vector2Int>>(patches);
            while (pending.Count > 0)
            {
                int index = random.Next(pending.Count); var patch = pending[index]; pending.RemoveAt(index);
                int chosen = ChooseType(board, types, remaining, patch, random, random.NextDouble() < mergeChance);
                // Assign the connected growth-order prefix the type can still take; any rest becomes a new patch.
                int take = Mathf.Min(remaining[chosen], patch.Count);
                for (int i = 0; i < take; i++) board.Cells.Add(patch[i], types[chosen]);
                remaining[chosen] -= take;
                if (take < patch.Count) pending.Add(patch.GetRange(take, patch.Count - take));
            }
            if (board.Cells.Count != total) throw new InvalidOperationException("Procedural fill did not reach the entire cluster");
        }

        static double[] TiltedWeights(ClusterGroupSettings settings, float clustering)
        {
            var weights = new double[settings.sizeWeights.Length];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = settings.sizeWeights[i] * Math.Exp(settings.clusteringTilt * (clustering - .5f) * 2 * i);
            return weights;
        }

        static List<List<Vector2Int>> BuildPatches(List<Vector2Int> cells, System.Random random, double[] sizeWeights)
        {
            var free = new HashSet<Vector2Int>(cells);
            var order = new List<Vector2Int>(cells);
            for (int i = order.Count - 1; i > 0; i--) { int j = random.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            var patches = new List<List<Vector2Int>>();
            foreach (var start in order)
            {
                if (!free.Contains(start)) continue;
                int target = DrawSize(sizeWeights, random);
                var patch = new List<Vector2Int> { start }; free.Remove(start);
                var candidates = new List<Vector2Int>();
                while (patch.Count < target)
                {
                    candidates.Clear();
                    foreach (var cell in patch) foreach (var direction in BoardRules.Directions)
                        if (free.Contains(cell + direction) && !candidates.Contains(cell + direction)) candidates.Add(cell + direction);
                    if (candidates.Count == 0) break;
                    var next = candidates[random.Next(candidates.Count)];
                    patch.Add(next); free.Remove(next);
                }
                patches.Add(patch);
            }
            return patches;
        }

        static int DrawSize(double[] weights, System.Random random)
        {
            double sum = 0; foreach (double weight in weights) sum += weight;
            double choice = random.NextDouble() * sum;
            for (int i = 0; i < weights.Length; i++) if ((choice -= weights[i]) < 0) return i + 1;
            return weights.Length;
        }

        // Prefers types with share left that no neighboring placed sphere already uses; a merge, or no other option,
        // allows any type with share left. Choices are weighted by remaining share to keep the mix even.
        static int ChooseType(BoardRules board, IReadOnlyList<int> types, int[] remaining, List<Vector2Int> patch, System.Random random, bool allowMerge)
        {
            var neighborTypes = new HashSet<int>();
            foreach (var cell in patch) foreach (var direction in BoardRules.Directions)
                if (board.Cells.TryGetValue(cell + direction, out int type)) neighborTypes.Add(type);
            for (int pass = allowMerge ? 1 : 0; pass < 2; pass++)
            {
                double sum = 0;
                for (int i = 0; i < types.Count; i++) if (remaining[i] > 0 && (pass == 1 || !neighborTypes.Contains(types[i]))) sum += remaining[i];
                if (sum <= 0) continue;
                double choice = random.NextDouble() * sum;
                for (int i = 0; i < types.Count; i++)
                {
                    if (remaining[i] <= 0 || (pass == 0 && neighborTypes.Contains(types[i]))) continue;
                    if ((choice -= remaining[i]) < 0) return i;
                }
            }
            for (int i = 0; i < types.Count; i++) if (remaining[i] > 0) return i;
            throw new InvalidOperationException("No sphere share left for a patch");
        }

        public static double NeighborSimilarity(BoardRules board)
        {
            int same = 0, total = 0;
            foreach (var cell in board.Cells) foreach (var direction in BoardRules.Directions)
                if (board.Cells.TryGetValue(cell.Key + direction, out int other)) { total++; if (other == cell.Value) same++; }
            return total == 0 ? 0 : (double)same / total;
        }

        // Sizes of the connected same-type groups on a board (for validation and tuning).
        public static List<int> GroupSizes(BoardRules board)
        {
            var sizes = new List<int>(); var seen = new HashSet<Vector2Int>(); var stack = new Stack<Vector2Int>();
            foreach (var start in board.Cells)
            {
                if (!seen.Add(start.Key)) continue;
                int size = 0; stack.Push(start.Key);
                while (stack.Count > 0)
                {
                    var cell = stack.Pop(); size++;
                    foreach (var direction in BoardRules.Directions)
                        if (board.Cells.TryGetValue(cell + direction, out int other) && other == start.Value && seen.Add(cell + direction)) stack.Push(cell + direction);
                }
                sizes.Add(size);
            }
            return sizes;
        }
    }
}
