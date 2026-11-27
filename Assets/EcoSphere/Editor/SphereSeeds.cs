using System;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    // The authored starting data for each sphere asset. SphereCatalogTools seeds a new asset from its entry; existing
    // assets keep their Inspector edits except where a migration (ReseedSphere, ReapplyArt, SeedNaturalWeight) re-applies one.
    // Art paths are not here: AssignArt uses the default folder files or SphereCatalogTools.ArtOverrides.
    sealed class SphereSeed
    {
        public int id;
        public string label;               // asset name and Art/Spheres/<label> folder
        public string displayName;         // defaults to label
        public ElementId element;
        public Color32 color;
        public SphereRole role = SphereRole.Element;
        public bool starter;
        public SphereCategory category;    // sorts the setup list; Laser and Core never appear there
        public string voice;               // Audio/Voice/<voice>.mp3
        public string description;
        public ElementContribution[] contributions;   // null: one unit of the namesake element
        public int unallocated;
        public float naturalWeight;               // relative pick weight for natural occurrence; 0 = never
        public Action<SphereDefinition> extra;        // anything beyond the common fields (shape, deflection, erosion, glow)

        public void ApplyContributions(SphereDefinition sphere)
        {
            sphere.contributions.Clear();
            if (contributions == null) { sphere.contributions.Add(new ElementContribution(element, 1)); return; }
            sphere.contributions.AddRange(contributions); sphere.unallocatedWeight = unallocated;
        }
    }

    static class SphereSeeds
    {
        static ElementContribution C(ElementId element, int amount) => new ElementContribution(element, amount);
        // A contribution capped by the planet's amount of another element (ratio 1).
        static ElementContribution Capped(ElementId element, int amount, ElementId limitedBy) => new ElementContribution(element, amount, limitedBy);
        static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/" + path);

        // Catalog order: new spheres are appended, so existing list positions stay put.
        public static readonly SphereSeed[] All =
        {
            new SphereSeed { id = SphereIds.Ice, label = "Ice", element = ElementId.Ice, color = new Color32(55,198,255,255), voice = "cryosphere",
                contributions = new[] { C(ElementId.Water,1), C(ElementId.Ice,2) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Sunlight, label = "Sunlight", element = ElementId.Sunlight, color = new Color32(255,194,40,255), voice = "heliosphere",
                contributions = new[] { C(ElementId.Fire,1), C(ElementId.Sunlight,2) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Laser, label = "Laser", element = ElementId.Laser, color = new Color32(255,0,0,255), role = SphereRole.Laser },
            new SphereSeed { id = SphereIds.Stone, label = "Stone", element = ElementId.Stone, color = new Color32(208,211,216,255), voice = "lithosphere",
                contributions = new[] { C(ElementId.Stone,3) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Fire, label = "Fire", element = ElementId.Fire, color = new Color32(255,86,24,255), starter = true, voice = "pyrosphere",
                contributions = new[] { C(ElementId.Fire,3) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Void, label = "Void", element = ElementId.Void, color = new Color32(232,238,255,255), starter = true, category = SphereCategory.Forces, voice = "shadowsphere",
                contributions = new[] { C(ElementId.Void,2) }, unallocated = 2,
                extra = d => { d.unlit = true; d.glowColor = Color.white; d.glowStrength = .42f; } },
            new SphereSeed { id = SphereIds.Water, label = "Water", element = ElementId.Water, color = new Color32(20,100,225,255), voice = "hydrosphere",
                contributions = new[] { C(ElementId.Water,3) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Plant, label = "Plant", displayName = "Plant Life", element = ElementId.Plant, color = new Color32(62,160,74,255), category = SphereCategory.Life, voice = "florasphere",
                contributions = new[] { C(ElementId.Life,1), C(ElementId.Plant,2) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Life, label = "Life", element = ElementId.Life, color = new Color32(246,183,205,255), category = SphereCategory.Life, voice = "biosphere",
                contributions = new[] { C(ElementId.Life,3) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Mud, label = "Mud", element = ElementId.Mud, color = new Color32(202,156,107,255), voice = "pelitosphere",
                contributions = new[] { C(ElementId.Water,1), C(ElementId.Stone,1), C(ElementId.Mud,2) } },
            new SphereSeed { id = SphereIds.Air, label = "Air", element = ElementId.Air, color = new Color32(232,245,250,255), voice = "atmosphere",
                contributions = new[] { C(ElementId.Air,3) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Crystal, label = "Crystal", element = ElementId.Crystal, color = new Color32(227,108,242,255), voice = "latisphere",
                description = "Adds a bit of magic to the world, while reflecting other elements.",
                contributions = new[] { C(ElementId.Stone,1), C(ElementId.Magic,1), C(ElementId.Crystal,2) },
                extra = d => { d.shape = SphereShape.Crystal; d.deflectedHits = 1; d.damagedOverlay = AssetDatabase.LoadAssetAtPath<Sprite>(SphereCatalogTools.CrystalCracksArt);
                    d.deflectSound = Clip("crystal-hit.mp3"); d.breakSound = Clip("crystal-breaking.mp3"); } },
            new SphereSeed { id = SphereIds.Civilization, label = "Civilization", element = ElementId.Civilization, color = new Color32(232,183,114,255), category = SphereCategory.Technology, voice = "politosphere",
                description = "Spreads settlements across the land. Their lights glow brighter as technology advances.",
                contributions = new[] { C(ElementId.Human,1), Capped(ElementId.Civilization,2,ElementId.Human) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Magic, label = "Magic", element = ElementId.Magic, color = new Color32(53,203,182,255), category = SphereCategory.Beyond, voice = "arcanosphere",
                description = "Fills the world with wonder, tinting its plant life and city lights in strange colors.",
                contributions = new[] { C(ElementId.Magic,3), C(ElementId.Void,1) } },
            new SphereSeed { id = SphereIds.Technology, label = "Technology", element = ElementId.Technology, color = new Color32(13,152,237,255), category = SphereCategory.Technology, voice = "technosphere",
                description = "Powers civilization, making its city lights shine brighter.",
                contributions = new[] { C(ElementId.Civilization,1), C(ElementId.Technology,2) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Spirit, label = "Spirit", element = ElementId.Spirit, color = new Color32(168,230,194,255), category = SphereCategory.Beyond, voice = "ectosphere",
                description = "Fills the world with an unseen presence, a quiet bridge between life and magic.",
                contributions = new[] { C(ElementId.Magic,1), C(ElementId.Spirit,2), C(ElementId.Void,1) }, naturalWeight = 1 },
            new SphereSeed { id = SphereIds.Forest, label = "Forest", element = ElementId.Forest, color = new Color32(31,107,42,255), category = SphereCategory.Life, voice = "florasphere",
                description = "Blankets the land in dense woodland, adding a wealth of plant life.",
                contributions = new[] { C(ElementId.Forest,2), C(ElementId.Plant,1), C(ElementId.Water,1) } },
            new SphereSeed { id = SphereIds.Fish, label = "Fish", element = ElementId.Fish, color = new Color32(247,107,13,255), category = SphereCategory.Life, voice = "biosphere",
                description = "Fills the seas with life, though that life can only flourish as far as the planet's water allows.",
                contributions = new[] { Capped(ElementId.Life,2,ElementId.Water), C(ElementId.Water,1), C(ElementId.Fish,1) } },
            new SphereSeed { id = SphereIds.Sand, label = "Sand", element = ElementId.Sand, color = new Color32(250,222,155,255), voice = "lithosphere",
                description = "Shifting and restless: whenever a shot makes no match, the outermost sand slips away.",
                contributions = new[] { C(ElementId.Sunlight,1), C(ElementId.Sand,2) }, unallocated = 1,
                extra = d => { d.erodesOnMiss = true; d.erodeSound = Clip("sandfall.mp3"); } },
            new SphereSeed { id = SphereIds.Bird, label = "Bird", element = ElementId.Bird, color = new Color32(120,190,245,255), category = SphereCategory.Life, voice = "biosphere",
                description = "Fills the skies with birdsong, though that life can only flourish as far as the planet's air allows.",
                contributions = new[] { C(ElementId.Bird,2), Capped(ElementId.Life,1,ElementId.Air), C(ElementId.Air,1) } },
            new SphereSeed { id = SphereIds.Metal, label = "Metal", element = ElementId.Metal, color = new Color32(90,97,112,255), voice = "ferrosphere",
                description = "Veins of ore run through the world, the raw strength that tools and industry are built from.",
                contributions = new[] { C(ElementId.Metal,2) }, unallocated = 2, naturalWeight = 1 },
            new SphereSeed { id = SphereIds.Wind, label = "Wind", element = ElementId.Wind, color = new Color32(220,214,245,255), category = SphereCategory.Weather, voice = "atmosphere",
                description = "Sets the skies in motion, stirring up the planet's weather.",
                contributions = new[] { C(ElementId.Wind,1), C(ElementId.Air,2) }, unallocated = 1 },
            new SphereSeed { id = SphereIds.Lava, label = "Lava", element = ElementId.Lava, color = new Color32(204,42,20,255), voice = "pyrosphere",
                description = "Molten rock wells up from below, heating the world and shaking its ground awake.",
                contributions = new[] { C(ElementId.Lava,2), C(ElementId.Fire,1) }, unallocated = 1,
                extra = d => { d.unlit = true; d.glowColor = new Color32(255,120,30,255); d.glowStrength = .35f; } },   // molten: lights itself
            new SphereSeed { id = SphereIds.Mountain, label = "Mountain", element = ElementId.Mountain, color = new Color32(168,147,214,255), voice = "lithosphere",
                description = "Thrusts the land up into towering peaks.",
                contributions = new[] { C(ElementId.Mountain,1), C(ElementId.Stone,2) }, unallocated = 1,
                extra = d => d.surfaceTint = new Color32(255,222,255,255) },   // Stone's surface in pale purple
            new SphereSeed { id = SphereIds.Core, label = "Core", element = ElementId.Core, color = new Color32(163,64,255,255), role = SphereRole.Core, extra = d => d.unlit = true },
        };

        public static SphereSeed Get(int id) => Array.Find(All, seed => seed.id == id) ?? throw new ArgumentException("No seed for sphere " + id);
    }
}
