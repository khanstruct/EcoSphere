using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class WorldCatalogTools
    {
        const string Folder = "Assets/EcoSphere/Data/Worlds/";
        const string CatalogPath = "Assets/EcoSphere/Resources/WorldCatalog.asset";

        [MenuItem("EcoSphere/Configure Worlds")]
        public static void Configure()
        {
            Directory.CreateDirectory(Folder + "Modifiers");
            Directory.CreateDirectory("Assets/EcoSphere/Resources");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<WorldCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<WorldCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            AddWorld(catalog, "genesis-alpha", "Genesis Alpha", "Tutorial1", 0, 2, 175, 1000000);
            AddWorld(catalog, "genesis-beta", "Genesis Beta", "FirstLight", 1, 3, 1000000, 3000000);
            AddModifier(catalog, "close-to-sun", "Close to the Sun", "Sunlight and Fire are more effective.", "close-to-sun-v1.png");
            AddModifier(catalog, "continuous-rotation", "Continuous Rotation", "The cluster rotates throughout play.", "continuous-rotation-v1.png");
            InfestationTools.EnsureModifier(catalog);
            catalog.ratingStar=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EcoSphere/Art/UI/world-rating-star-v1.png");
            if(string.IsNullOrWhiteSpace(catalog.proceduralGameplayScene))catalog.proceduralGameplayScene=WorldLevels.ProceduralSceneName;
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            catalog.Validate();
            Debug.Log("ECOSPHERE_WORLD_CATALOG_CONFIGURED: Genesis Alpha/Beta, guaranteed slots, procedural requirements and modifier pool");
        }

        static void AddWorld(WorldCatalog catalog, string id, string label, string scene, int slot, int spheres, int goal, int excess)
        {
            string path = Folder + id + ".asset";
            var world = AssetDatabase.LoadAssetAtPath<PredefinedWorld>(path);
            if (!world)
            {
                world = ScriptableObject.CreateInstance<PredefinedWorld>();
                world.id = id; world.displayName = label; world.gameplayScene = scene;
                world.guaranteedSystem = 1; world.guaranteedSlot = slot;
                world.rules.requiredSpheres = spheres; world.rules.inclusionGoal = goal; world.rules.abundanceGoal = excess;
                if(scene=="FirstLight")world.naturalSpheres.Add(new NaturalSpherePlacement{sphereId=SphereIds.Stone,cell=StoneTutorialLayout.StoneCell(GameplayGeometry.DefaultClusterRings)});
                AssetDatabase.CreateAsset(world, path);
            }
            if (!catalog.predefinedWorlds.Contains(world)) catalog.predefinedWorlds.Add(world);
        }

        static void AddModifier(WorldCatalog catalog, string id, string label, string description, string icon)
        {
            string path = Folder + "Modifiers/" + id + ".asset";
            var modifier = AssetDatabase.LoadAssetAtPath<WorldModifierDefinition>(path);
            if (!modifier)
            {
                modifier = ScriptableObject.CreateInstance<WorldModifierDefinition>();
                modifier.id = id; modifier.displayName = label; modifier.description = description;
                modifier.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EcoSphere/Art/UI/Modifiers/" + icon);
                AssetDatabase.CreateAsset(modifier, path);
            }
            if (!catalog.proceduralModifiers.Contains(modifier)) catalog.proceduralModifiers.Add(modifier);
        }

        [MenuItem("EcoSphere/Validate World Generation")]
        public static void Validate()
        {
            int checks = 0;
            void Expect(bool value, string message) { if (!value) throw new Exception("World generation: " + message); checks++; }
            var catalog = WorldCatalog.Current; catalog.Validate();
            var save = new PlayerProfile { universeSeed = 7261 };
            var later = WorldGeneration.GetOrCreate(save, catalog, 12);
            Expect(later.worlds.TrueForAll(world => world.IsProcedural && world.RequiredSpheres >= 4), "later systems must not receive tutorials or low requirements");
            var first = WorldGeneration.GetOrCreate(save, catalog, 1);
            Expect(first.worlds[0].displayName == "Genesis Alpha" && first.worlds[0].RequiredSpheres == 2 && first.worlds[0].gameplayScene == "Tutorial1", "guaranteed Alpha slot");
            Expect(first.worlds[1].displayName == "Genesis Beta" && first.worlds[1].RequiredSpheres == 3 && first.worlds[1].gameplayScene == "FirstLight", "guaranteed Beta slot");
            Expect(save.placedPredefinedWorlds.Count == 2, "placement must claim tutorials before they are completed");
            Expect(ReferenceEquals(first, WorldGeneration.GetOrCreate(save, catalog, 1)), "revisit must reuse its saved system");
            var identical = WorldGeneration.GetOrCreate(new PlayerProfile { universeSeed = 7261 }, catalog, 1);
            for (int i = 0; i < first.worlds.Count; i++) Expect(first.worlds[i].displayName == identical.worlds[i].displayName, "deterministic creation");
            for (int number = 2; number <= 250; number++)
            {
                var system = WorldGeneration.GetOrCreate(save, catalog, number);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var world in system.worlds)
                {
                    Expect(world.IsProcedural && world.RequiredSpheres >= 4, "procedural minimum or authored uniqueness violated");
                    Expect(names.Add(world.displayName), "duplicate names within a system");
                }
            }
            var testCatalog = UnityEngine.Object.Instantiate(catalog);
            var conditional = ScriptableObject.CreateInstance<PredefinedWorld>();
            conditional.id = "conditional-test"; conditional.displayName = "Conditional Test"; conditional.gameplayScene = "Tutorial1";
            conditional.minimumSystem = 5; conditional.appearanceChance = 1;
            conditional.requiredUnlockedSpheres.Add(SphereIds.Sunlight); conditional.requiredCompletedWorlds.Add("genesis-alpha");
            testCatalog.predefinedWorlds.Add(conditional);
            testCatalog.minimumProceduralSpheres = 4; testCatalog.maximumProceduralSpheres = 6;
            var never = UnityEngine.Object.Instantiate(catalog.proceduralModifiers[0]); never.proceduralChance = 0;
            var always = UnityEngine.Object.Instantiate(catalog.proceduralModifiers[1]); always.proceduralChance = 1;
            testCatalog.proceduralModifiers = new List<WorldModifierDefinition> { never, always };
            var conditionalSave = new PlayerProfile { universeSeed = 17 };
            WorldGeneration.GetOrCreate(conditionalSave, testCatalog, 1);
            var blocked = WorldGeneration.GetOrCreate(conditionalSave, testCatalog, 5);
            Expect(blocked.worlds.TrueForAll(world => world.IsProcedural), "prerequisite condition must block appearance");
            conditionalSave.unlockedSpheres.Add(SphereIds.Sunlight);
            conditionalSave.systems.Find(system=>system.number==1).worlds[0].completion=new CompletedWorld { system = 1, planet = 0 };
            var eligible = WorldGeneration.GetOrCreate(conditionalSave, testCatalog, 6);
            Expect(eligible.worlds.FindAll(world => world.predefinedId == conditional.id).Count == 1, "eligible conditional world did not appear");
            var next = WorldGeneration.GetOrCreate(conditionalSave, testCatalog, 7);
            Expect(next.worlds.TrueForAll(world => world.IsProcedural), "placed conditional world reappeared before completion");
            foreach (var world in next.worlds)
            {
                Expect(world.RequiredSpheres >= 4 && world.RequiredSpheres <= 6, "configured requirement range");
                Expect(world.modifiers.Count == 1 && world.modifiers[0] == always.id, "per-world variable modifier list");
            }
            string originalName = first.worlds[2].displayName;
            testCatalog.nameOpenings = new[] { "Changed" };
            Expect(WorldGeneration.GetOrCreate(save, testCatalog, 1).worlds[2].displayName == originalName, "editing generation renamed an existing planet");
            var invalid = new WorldRecord(); invalid.rules.requiredSpheres = 1;
            Expect(invalid.RequiredSpheres == 4, "record-level minimum must protect procedural requirements");
            string path = Path.GetFullPath("Logs/WorldGeneration-roundtrip.json");
            WorldSaveStore.Save(path, conditionalSave);
            var restored = WorldSaveStore.Load(path);
            Expect(restored.systems.Count == conditionalSave.systems.Count && restored.placedPredefinedWorlds.Contains(conditional.id), "save round trip lost systems or reservations");
            Expect(WorldGeneration.GetOrCreate(restored, testCatalog, 8).worlds.TrueForAll(world => world.IsProcedural), "load permitted a duplicate predefined world");
            Expect(restored.systems.Find(system => system.number == 7).worlds[0].modifiers[0] == always.id, "save round trip lost modifiers");
            WorldSaveStore.Save(path, restored); // Also exercise atomic replacement of an existing save.
            UnityEngine.Object.DestroyImmediate(testCatalog); UnityEngine.Object.DestroyImmediate(conditional);
            UnityEngine.Object.DestroyImmediate(never); UnityEngine.Object.DestroyImmediate(always);
            Debug.Log("ECOSPHERE_WORLD_GENERATION_CHECKS_PASSED " + checks + ": guaranteed tutorials, 250 systems, deterministic names, unique placements, conditional appearances, 4+ spheres, modifiers, atomic save/load");
        }

        public static void ConfigureAndValidate()
        {
            SphereCatalogTools.Configure(); Configure(); Validate();
        }

        public static void PreparationSmoke()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void DevelopmentProgressCheck()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void SystemTutorialSmoke()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
