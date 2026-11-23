using System;
using System.Collections.Generic;
namespace EcoSphere
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int version=2;
        public string id=Guid.NewGuid().ToString("N"),displayName="Player";
        public int universeSeed,currentSystem=1;
        public AudioPreferences audio=new AudioPreferences();
        public List<SystemRecord> systems=new List<SystemRecord>();
        public List<int> unlockedSpheres=new List<int>();
        public List<string> placedPredefinedWorlds=new List<string>();
        public bool systemIntroductionSeen;
        public List<SetupTutorialProgress> setupTutorials=new List<SetupTutorialProgress>();
        public IEnumerable<CompletedWorld> CompletedPlanets
        {
            get{foreach(var system in systems)foreach(var planet in system.worlds)if(planet.completion!=null)yield return planet.completion;}
        }
    }
    [Serializable]
    public sealed class NaturalSpherePlacement
    {
        public int sphereId;
        public UnityEngine.Vector2Int cell;
        public NaturalSpherePlacement Copy()=>new NaturalSpherePlacement{sphereId=sphereId,cell=cell};
    }
}
