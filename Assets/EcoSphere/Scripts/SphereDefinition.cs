using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // Sphere IDs remain stable so existing boards and discovery saves keep their meaning.
    public static class SphereIds
    {
        public const int Core = -1, Ice = 0, Sunlight = 1, Laser = 2, Stone = 3, Fire = 4, Void = 5, Water = 6, Plant = 7, Life = 8, Mud = 9, Air = 10, Crystal = 11, Civilization = 12, Magic = 13, Technology = 14, Spirit = 15, Forest = 16, Fish = 17, Sand = 18, Bird = 19, Metal = 20, Wind = 21, Lava = 22, Mountain = 23, Obsidian = 24, Volcano = 25, Lightning = 26, Tools = 27, Moon = 28;
    }

    // Append new IDs only: existing sphere assets and saves serialize these numeric values.
    // To drop an element, retire it with [Obsolete] instead of deleting it, so later IDs keep their numbers.
    // Retired elements are hidden from inspector dropdowns, get no planet profile and are rejected by sphere/recipe validation.
    public enum ElementId { Ice, Sunlight, Stone, Fire, Void, Water, Laser, Core,
        Air, Soil, Sand, Life, Steam, Cloud, Rain, Snow, Wind, Lightning, Lava, Smoke,
        Ash, Dust, Poison, Clay, Salt, Metal, Crystal, Glass, Coal, Oil, Wood, Gravity,
        Plant, Algae, Moss, Fungus, Flower,
        [Obsolete("Retired: unused (Forest covers it). The slot keeps later element IDs stable.")] RetiredTree,
        Bacteria, Insect, Fish, Bird, Reptile,
        Mammal, Human, Ocean, Swamp, Energy, Magic, Technology, Sound, City, Mud, Civilization, Spirit, Forest, Mountain, Volcano, Tools, Moon }
    public static class ElementIds
    {
        static readonly HashSet<ElementId> retired = Retired();
        static HashSet<ElementId> Retired()
        {
            var result = new HashSet<ElementId>();
            foreach (var field in typeof(ElementId).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (Attribute.IsDefined(field, typeof(ObsoleteAttribute))) result.Add((ElementId)field.GetValue(null));
            return result;
        }
        // Retired elements keep their numeric slot but take no part in spheres, recipes or planet profiles.
        public static bool IsRetired(ElementId element) => retired.Contains(element);
        public static bool IsActive(ElementId element) => Enum.IsDefined(typeof(ElementId), element) && !IsRetired(element);
    }
    public enum SphereRole { Element, Laser, Core }
    // Append categories only: sphere definitions serialize these values. All is a UI filter, not a category.
    public enum SphereCategory { Basic = 0, Forces = 1, Weather = 2, Life = 3, Technology = 4, Beyond = 5 }

    [Serializable]
    public struct ElementContribution
    {
        public ElementId element;
        [Range(1,4)] public int amount;
        [Tooltip("Caps the total this sphere type has added of this element at the planet's total of Limited By (times Limit Ratio). Yield past the cap is lost; score is unaffected.")]
        public bool limited;
        public ElementId limitedBy;
        [Min(0)] public float limitRatio;
        public ElementContribution(ElementId element, int amount) { this.element = element; this.amount = amount; limited = false; limitedBy = default; limitRatio = 0; }
        public ElementContribution(ElementId element, int amount, ElementId limitedBy, float limitRatio = 1) : this(element, amount)
        { limited = true; this.limitedBy = limitedBy; this.limitRatio = limitRatio; }
        // The planet-wide amount that caps this line's accumulated yield. Unlimited lines have no cap.
        public double Cap(double[] totals) => limited ? totals[(int)limitedBy] * limitRatio : double.PositiveInfinity;
    }

    [CreateAssetMenu(menuName = "EcoSphere/Sphere Definition")]
    public sealed class SphereDefinition : ScriptableObject
    {
        internal static int IdentityRevision {get;private set;}
        void OnEnable()=>IdentityRevision++;
        void OnValidate()=>IdentityRevision++;
        public int id;
        [Tooltip("The element this sphere represents, if any (None for spheres like Forest that only add other elements). Independent of the elements it contributes.")]
        public const ElementId NoNamesake=(ElementId)(-1);
        public ElementId namesakeElement=NoNamesake;
        // A sphere without a namesake (e.g. Forest, which adds Plant) never supplies an element's color, art or voice.
        public bool HasNamesake => namesakeElement!=NoNamesake;
        public string displayName;
        [TextArea(2,4), Tooltip("Short player-facing text explaining how this sphere behaves in play. Don't list element yields here: ContributionSummary builds them from Contributions.")]
        public string description;
        public const int MaximumDescriptionLength=160;
        public SphereCategory category;
        public Texture2D surface;
        [Tooltip("Multiplied over the surface texture wherever the sphere is drawn (board, ammo, previews). White leaves it unchanged; " +
            "lets a sphere reuse another's texture in its own color, e.g. Mountain on Stone's.")]
        public Color surfaceTint = Color.white;
        public Sprite icon;
        public Color color = Color.white;
        [Tooltip("Visual body only. Collision, matching and scoring always use the round sphere footprint.")]
        public SphereShape shape;
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
        [Header("Shot deflection")]
        [Range(0,10), Tooltip("Hits from other sphere types this sphere bounces off like a wall (counting as ricochets). 0 = shots attach normally. " +
            "The hit after these breaks it: it is lost without points or elements and the shot takes its cell. Same-type shots always attach.")]
        public int deflectedHits;
        [Tooltip("Played when this sphere deflects a shot.")] public AudioClip deflectSound;
        [Tooltip("Played when a shot breaks this sphere.")] public AudioClip breakSound;
        [Tooltip("Drawn over the sphere once it has taken a hit and not yet broken (e.g. Crystal's cracks).")] public Sprite damagedOverlay;
        [Header("Erosion")]
        [Tooltip("After a shot that makes no match, the eroding sphere farthest from the core disappears (no points or elements). " +
            "Hazarded spheres (e.g. infested) don't count. Anything it was holding falls away as a normal breakaway.")]
        public bool erodesOnMiss;
        [Tooltip("Played when this sphere erodes.")] public AudioClip erodeSound;
        [Header("Freed from the cluster")]
        [Tooltip("When a breakaway cuts this sphere off from the core it keeps its cell instead of falling (no points or elements), " +
            "unless it is hazarded (e.g. infested). It can still be matched or reconnected by later shots.")]
        public bool staysWhenFreed;
        [Tooltip("Glow shown only while this sphere is on the board and cut off from the core; never on ammo or connected spheres.")]
        public Color freedGlowColor=Color.white;
        [Range(0,1)] public float freedGlowStrength;
        [Header("Natural occurrence")]
        [Min(0), UnityEngine.Serialization.FormerlySerializedAs("naturalChance"),
         Tooltip("Relative weight of this sphere when a system's natural occurrence picks one of the spheres the player doesn't own yet " +
            "(the per-world chance is WorldCatalog.naturalOccurrenceChance). Releasing it in a breakaway discovers it. 0 = never occurs naturally.")]
        public float naturalWeight;
        public bool OccursNaturally => naturalWeight > 0 && AmmoEligible;
        [Header("Lost without points (eroded, broken)")]
        [Tooltip("Particle burst where the sphere was: Crumble falls away, Shatter scatters. Profiles live in SphereShapeStyle.")]
        public SphereBurst lostBurst;
        [Tooltip("Burst color; leave fully transparent to use the sphere color.")] public Color lostBurstColor = Color.clear;
        public Color LostBurstColor => lostBurstColor.a > 0 ? lostBurstColor : color;
        public int TotalContributionUnits => ContributionUnits+unallocatedWeight;
        public bool Matchable => role == SphereRole.Element;
        public bool AmmoEligible => selectable && Matchable;

        public void Validate()
        {
            if(!Enum.IsDefined(typeof(SphereCategory),category))throw new InvalidOperationException(name+": invalid sphere category");
            if(!Enum.IsDefined(typeof(SphereShape),shape))throw new InvalidOperationException(name+": invalid sphere shape");
            if(HasNamesake && !ElementIds.IsActive(namesakeElement))throw new InvalidOperationException(name+": namesake must be an active element or none");
            if (string.IsNullOrWhiteSpace(displayName) || !surface || !icon)
                throw new InvalidOperationException(name + ": name, texture and icon are required");
            if (contributions == null || contributions.Count < 1 || contributions.Count > 4)
                throw new InvalidOperationException(name + ": assign 1 to 4 element contributions");
            if (unallocatedWeight < 0 || unallocatedWeight > 3) throw new InvalidOperationException(name + ": unallocated weight must be between zero and three");
            var seen = new HashSet<ElementId>();
            foreach (var entry in contributions)
                if (!ElementIds.IsActive(entry.element) || entry.amount < 1 || entry.amount > 4 || !seen.Add(entry.element))
                    throw new InvalidOperationException(name + ": contributions must be positive and use distinct elements");
                else if (entry.limited && (!ElementIds.IsActive(entry.limitedBy) || entry.limitedBy == entry.element || !(entry.limitRatio > 0)))
                    throw new InvalidOperationException(name + ": a limited contribution needs a different, valid limiting element and a positive ratio");
            if (ContributionUnits + unallocatedWeight > 4) throw new InvalidOperationException(name + ": at most four contribution units including unallocated units");
            if (deflectedHits < 0 || deflectedHits > 10 || (deflectedHits > 0 && (!Matchable || !deflectSound || !breakSound)))
                throw new InvalidOperationException(name + ": deflecting spheres must be elements with deflect and break sounds (0 to 10 hits)");
            if (!Enum.IsDefined(typeof(SphereBurst), lostBurst)) throw new InvalidOperationException(name + ": invalid lost-sphere burst");
            if (erodesOnMiss && (!Matchable || !erodeSound))
                throw new InvalidOperationException(name + ": eroding spheres must be elements with an erode sound");
            if (color.a <= 0) throw new InvalidOperationException(name + ": the streak/visualizer color must be visible");
            if (string.IsNullOrWhiteSpace(description) || description.Length > MaximumDescriptionLength)
                throw new InvalidOperationException(name + ": a description of 1 to " + MaximumDescriptionLength + " characters is required");
        }

        // Share of each earned point that reaches an element (0-1). Unallocated units take their share without reaching any element.
        public float ContributionShare(ElementContribution entry) => TotalContributionUnits > 0 ? entry.amount / (float)TotalContributionUnits : 0;
        // Uncapped yield of one contribution line for `rate` earned points. ElementLedger applies limits on top of this.
        public double ContributionYield(ElementContribution entry, double rate) => TotalContributionUnits > 0 ? rate * (entry.amount / (double)TotalContributionUnits) : 0;
        // Core is structural; Laser is collected when consumed but still never forms matches.
        public bool Contributes => role != SphereRole.Core;

        // Readable element yield, built from Contributions so the text can never disagree with scoring,
        // e.g. "Ice 50% · Water 25%". Largest share first; ties keep authored order. Core contributes nothing.
        public string ContributionSummary()
        {
            if (role == SphereRole.Core) return "";
            var ordered = new List<ElementContribution>(contributions);
            for (int i = 1; i < ordered.Count; i++)
                for (int j = i; j > 0 && ordered[j].amount > ordered[j - 1].amount; j--)
                    (ordered[j], ordered[j - 1]) = (ordered[j - 1], ordered[j]);
            var parts = new List<string>();
            foreach (var entry in ordered)
                parts.Add(SphereCatalog.Current.ElementName(entry.element) + " " + Mathf.RoundToInt(ContributionShare(entry) * 100) + "%");
            return string.Join(" · ", parts);
        }

        // Raw yield, ignoring contribution limits (those depend on planet state; gameplay adds through ElementLedger).
        public void AddContributions(double[] totals, int rate)
        {
            if (!Contributes) return;
            foreach (var entry in contributions)
                totals[(int)entry.element] += ContributionYield(entry, rate);
        }
    }
}
