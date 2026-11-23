using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public static class WorldGeneration
    {
        // Stable integer mixing; independent of platform-specific string hashing and Unity's global RNG.
        static int Seed(int universe, int system)
        {
            unchecked { uint value = (uint)universe ^ (uint)system * 0x9e3779b9u; value ^= value >> 16; value *= 0x85ebca6bu; value ^= value >> 13; return (int)(value & 0x7fffffff); }
        }

        public static SystemRecord GetOrCreate(PlayerProfile save, WorldCatalog catalog, int number)
        {
            if (number < 1) throw new ArgumentOutOfRangeException(nameof(number));
            var existing = save.systems.Find(system => system.number == number);
            if (existing != null) return existing;
            catalog.Validate();
            var system = new SystemRecord { number = number, seed = Seed(save.universeSeed, number), id="system:"+number, displayName="System "+number.ToString("D2") };
            system.sunTypeId=catalog.defaultSunTypeId;
            var random = new System.Random(system.seed);
            var slots = new WorldRecord[catalog.planetsPerSystem];
            var placed = new HashSet<string>(save.placedPredefinedWorlds);
            var newlyPlaced = new List<string>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Guaranteed worlds reserve their slots before conditional worlds are considered.
            foreach (var definition in catalog.predefinedWorlds)
                if (definition.guaranteedSystem == number && !placed.Contains(definition.id))
                    Place(definition, definition.guaranteedSlot);
            foreach (var definition in catalog.predefinedWorlds)
            {
                if (definition.guaranteedSystem > 0 || placed.Contains(definition.id) || number < definition.minimumSystem ||
                    (definition.maximumSystem > 0 && number > definition.maximumSystem) || !Eligible(save, definition)) continue;
                var available = new List<int>();
                for (int slot = 0; slot < slots.Length; slot++) if (slots[slot] == null) available.Add(slot);
                if (available.Count == 0) break;
                if (random.NextDouble() < definition.appearanceChance) Place(definition, available[random.Next(available.Count)]);
            }
            for (int slot = 0; slot < slots.Length; slot++)
            {
                if (slots[slot] != null) continue;
                var rules = catalog.proceduralRules.Copy();
                int minimum = Mathf.Max(4, catalog.minimumProceduralSpheres), maximum = Mathf.Max(minimum, catalog.maximumProceduralSpheres);
                rules.requiredSpheres = random.Next(minimum, maximum + 1);
                rules.clustering=Mathf.Lerp(catalog.minimumClustering,catalog.maximumClustering,(float)random.NextDouble());
                var world = new WorldRecord { id = number + ":" + slot, seed = random.Next(), rules = rules,
                    displayName = GenerateName(random, catalog, names), gameplayScene = catalog.proceduralGameplayScene, terrainStyleId=catalog.defaultTerrainStyleId };
                foreach (var modifier in catalog.proceduralModifiers)
                    if (modifier.proceduralChance>0 && random.NextDouble() < modifier.proceduralChance) world.modifiers.Add(modifier.id);
                slots[slot] = world;
            }
            foreach(var modifier in catalog.proceduralModifiers)
                if(modifier.guaranteedSystem==number && !slots[modifier.guaranteedSlot].modifiers.Contains(modifier.id))
                    slots[modifier.guaranteedSlot].modifiers.Add(modifier.id);
            system.worlds.AddRange(slots);
            // Commit placement and the finished system together; no reservation is made for incomplete generation.
            save.systems.Add(system); save.placedPredefinedWorlds.AddRange(newlyPlaced);
            return system;

            void Place(PredefinedWorld definition, int slot)
            {
                var world = new WorldRecord { id = number + ":" + slot, predefinedId = definition.id, displayName = definition.displayName,
                    gameplayScene = definition.gameplayScene, seed = random.Next(), rules = definition.rules.Copy(), terrainStyleId=catalog.defaultTerrainStyleId };
                foreach (var modifier in definition.modifiers) world.modifiers.Add(modifier.id);
                foreach(var natural in definition.naturalSpheres)world.naturalSpheres.Add(natural.Copy());
                slots[slot] = world; placed.Add(definition.id); newlyPlaced.Add(definition.id); names.Add(world.displayName);
            }
        }

        static bool Eligible(PlayerProfile save, PredefinedWorld definition)
        {
            foreach (int sphere in definition.requiredUnlockedSpheres)
                if (!save.unlockedSpheres.Contains(sphere) && !SphereCatalog.Current.Get(sphere).availableAtStart) return false;
            foreach (string required in definition.requiredCompletedWorlds)
            {
                bool found = false;
                foreach (var system in save.systems)
                    for (int slot = 0; slot < system.worlds.Count; slot++)
                        if (system.worlds[slot].predefinedId == required && system.worlds[slot].IsComplete) found = true;
                if (!found) return false;
            }
            return true;
        }

        static string GenerateName(System.Random random, WorldCatalog catalog, HashSet<string> used)
        {
            string candidate = "";
            for (int attempt = 0; attempt < 256; attempt++)
            {
                candidate = catalog.nameOpenings[random.Next(catalog.nameOpenings.Length)];
                if (random.Next(3) == 0) candidate += catalog.nameMiddles[random.Next(catalog.nameMiddles.Length)];
                candidate += catalog.nameEndings[random.Next(catalog.nameEndings.Length)];
                candidate = char.ToUpperInvariant(candidate[0]) + candidate.Substring(1).ToLowerInvariant();
                bool awkward = candidate.Length < 4 || candidate.Length > 12;
                for (int i = 2; i < candidate.Length; i++) if (candidate[i] == candidate[i - 1] && candidate[i] == candidate[i - 2]) awkward = true;
                if (!awkward && used.Add(candidate)) return candidate;
            }
            // A tiny custom fragment pool still cannot trap system creation in an endless retry loop.
            string root = candidate; int suffix = 2;
            while (!used.Add(candidate)) candidate = root + " " + suffix++;
            return candidate;
        }
    }
}
