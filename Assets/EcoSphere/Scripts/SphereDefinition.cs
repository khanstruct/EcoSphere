using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // Sphere IDs remain stable so existing boards and discovery saves keep their meaning.
    public static class SphereIds
    {
        public const int Core = -1, Ice = 0, Sunlight = 1, Laser = 2, Stone = 3, Fire = 4, Void = 5, Water = 6;
    }

    public enum ElementId { Ice, Sunlight, Stone, Fire, Void, Water, Laser, Core }
    public enum SphereRole { Element, Laser, Core }

    [Serializable]
    public struct ElementContribution
    {
        public ElementId element;
        [Range(1,4)] public int amount;
        public ElementContribution(ElementId element, int amount) { this.element = element; this.amount = amount; }
    }

    [CreateAssetMenu(menuName = "EcoSphere/Sphere Definition")]
    public sealed class SphereDefinition : ScriptableObject
    {
        public int id;
        public string displayName;
        public Texture2D surface;
        public Sprite icon;
        public Color color = Color.white;
        [Header("Optional sphere halo")]
        public Color glowColor=Color.white;
        [Range(0,1)] public float glowStrength;
        [Tooltip("Voice clip for a large match of this sphere (also used for single-element resource callouts).")]
        public AudioClip resourceVoice;
        public SphereRole role;
        public bool availableAtStart, selectable, unlit;
        [Tooltip("Contribution units: one bar per real unit. Real plus unallocated units must total at most four. Earned points are divided proportionally.")]
        public List<ElementContribution> contributions = new List<ElementContribution>();
        [InspectorName("Unallocated Units"), Range(0,3), Tooltip("Unused units consume part of the four-unit budget, reducing elemental yield without reducing earned score.")]
        public int unallocatedWeight;
        public int ContributionUnits { get { int sum=0; foreach(var entry in contributions) sum+=entry.amount; return sum; } }
        public bool Matchable => role == SphereRole.Element;
        public bool AmmoEligible => selectable && Matchable;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(displayName) || !surface || !icon)
                throw new InvalidOperationException(name + ": name, texture and icon are required");
            if (contributions == null || contributions.Count < 1 || contributions.Count > 4)
                throw new InvalidOperationException(name + ": assign 1 to 4 element contributions");
            if (unallocatedWeight < 0 || unallocatedWeight > 3) throw new InvalidOperationException(name + ": unallocated weight must be between zero and three");
            var seen = new HashSet<ElementId>();
            foreach (var entry in contributions)
                if (!Enum.IsDefined(typeof(ElementId), entry.element) || entry.amount < 1 || entry.amount > 4 || !seen.Add(entry.element))
                    throw new InvalidOperationException(name + ": contributions must be positive and use distinct elements");
            if (ContributionUnits + unallocatedWeight > 4) throw new InvalidOperationException(name + ": at most four contribution units including unallocated units");
            if (color.a <= 0) throw new InvalidOperationException(name + ": the streak/visualizer color must be visible");
        }

        public void AddContributions(double[] totals, int rate)
        {
            // Core is structural; Laser is collected when consumed but still never forms matches.
            if (role == SphereRole.Core) return;
            double weight = unallocatedWeight;
            foreach (var entry in contributions) weight += entry.amount;
            if (weight <= 0) return;
            foreach (var entry in contributions)
                totals[(int)entry.element] += rate * (entry.amount / weight);
        }
    }
}
