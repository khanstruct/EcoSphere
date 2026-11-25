using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    [Serializable]
    public sealed class WorldRules
    {
        [Min(1)] public int requiredSpheres = 4;
        [Min(2)] public int clusterRings=GameplayGeometry.DefaultClusterRings;
        [Min(1)] public int abundanceGoal = 1500;
        [Tooltip("Whether completing this world can award recipe discoveries. Recipes qualify on element shares alone (see ScoringSettings).")]
        public bool recipeDiscoveries = true;
        [Range(0,1)] public float clustering=.5f;
        // Reserve chambers beside the cannon; zero disables the ammo ring (the Genesis tutorials).
        [Range(0,AmmoRing.MaximumChambers)] public int ammoChambers;
        public float rotationDegrees = 24;
        [Min(.01f)] public float rotationDuration = .65f;
        // Star thresholds are shared fractions of par (starting cluster cells x breakaway points; see ScoringSettings),
        // so they follow board size and point values instead of fixed numbers per world.
        public double Par => ScoringSettings.Current.Par(GameplayGeometry.CellCount(clusterRings));
        public double StarScore(int stars)
        {
            var scoring = ScoringSettings.Current;
            return Math.Round(Par * (stars <= 1 ? scoring.oneStarPar : stars == 2 ? scoring.twoStarPar : scoring.threeStarPar));
        }
        public int StarsForScore(double score) => score >= StarScore(3) ? 3 : score >= StarScore(2) ? 2 : score >= StarScore(1) ? 1 : 0;
        public WorldRules Copy() => new WorldRules { requiredSpheres = requiredSpheres, clusterRings=clusterRings, recipeDiscoveries = recipeDiscoveries,
            abundanceGoal = abundanceGoal, clustering=clustering, ammoChambers=ammoChambers, rotationDegrees = rotationDegrees, rotationDuration = rotationDuration };
    }

    [CreateAssetMenu(menuName = "EcoSphere/Predefined World")]
    public sealed class PredefinedWorld : ScriptableObject
    {
        public string id, displayName, gameplayScene;
        public List<SetupTutorialStep> setupTutorial = new List<SetupTutorialStep>();
        [Header("Discovery and next-world lessons (optional)")]
        public string discoveryTutorialMessage;
        public string nextWorldTutorialMessage, nextWorldTutorialTarget;
        public WorldRules rules = new WorldRules();
        public List<WorldModifierDefinition> modifiers = new List<WorldModifierDefinition>();
        public List<NaturalSpherePlacement> naturalSpheres=new List<NaturalSpherePlacement>();
        [Header("Guaranteed placement (system 0 disables)")]
        [Min(0)] public int guaranteedSystem;
        [Min(0)] public int guaranteedSlot;
        [Header("Conditional appearance (when not guaranteed)")]
        [Min(1)] public int minimumSystem = 1;
        [Min(0)] public int maximumSystem;
        [Range(0, 1)] public float appearanceChance = 1;
        public List<int> requiredUnlockedSpheres = new List<int>();
        public List<string> requiredCompletedWorlds = new List<string>();
    }
}
