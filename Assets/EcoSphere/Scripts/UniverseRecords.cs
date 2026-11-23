using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    [Serializable]
    public sealed class WorldRecord
    {
        public string id, predefinedId, displayName, gameplayScene;
        public int seed;
        public string terrainStyleId="barren-v1";
        public CompletedWorld completion;
        public bool IsComplete=>completion!=null;
        public List<NaturalSpherePlacement> naturalSpheres=new List<NaturalSpherePlacement>();
        public double highestScore;
        public int EarnedStars => rules.StarsForScore(highestScore);
        public WorldRules rules = new WorldRules();
        public List<string> modifiers = new List<string>();
        public bool IsProcedural => string.IsNullOrEmpty(predefinedId);
        public string GameplayScene => IsProcedural && string.IsNullOrWhiteSpace(gameplayScene) ? WorldCatalog.Current.proceduralGameplayScene : gameplayScene;
        public int RequiredSpheres => IsProcedural ? Mathf.Max(4, rules.requiredSpheres) : rules.requiredSpheres;
        public IEnumerable<WorldModifierDefinition> ModifierDefinitions
        {
            get{foreach(string id in modifiers)yield return WorldCatalog.Current.Modifier(id);}
        }
    }

    [Serializable]
    public sealed class SystemRecord
    {
        public int number, seed;
        public string id,displayName,sunTypeId="yellow-star";
        public bool IsComplete=>worlds.Count>0 && worlds.TrueForAll(planet=>planet.IsComplete);
        public List<WorldRecord> worlds = new List<WorldRecord>();
    }

}
