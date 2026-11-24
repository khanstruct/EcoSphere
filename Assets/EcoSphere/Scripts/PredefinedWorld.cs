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
        [Min(1)] public int inclusionGoal = 500, abundanceGoal = 1500;
        [Range(0,1)] public float clustering=.5f;
        public float rotationDegrees = 24;
        [Min(.01f)] public float rotationDuration = .65f;
        [Min(0)] public int oneStarScore = 500, twoStarScore = 1000, threeStarScore = 2500;
        public int StarsForScore(double score) => score >= threeStarScore ? 3 : score >= twoStarScore ? 2 : score >= oneStarScore ? 1 : 0;
        public WorldRules Copy() => new WorldRules { requiredSpheres = requiredSpheres, clusterRings=clusterRings, inclusionGoal = inclusionGoal,
            abundanceGoal = abundanceGoal, clustering=clustering, rotationDegrees = rotationDegrees, rotationDuration = rotationDuration,
            oneStarScore=oneStarScore, twoStarScore=twoStarScore, threeStarScore=threeStarScore };
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
