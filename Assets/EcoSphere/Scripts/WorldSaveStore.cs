using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public static class WorldSaveStore
    {
        // Only migration knows the obsolete flat completion list and Stone flag.
        [Serializable] sealed class LegacySave { public bool stoneUnlocked; public List<LegacyCompletion> worlds; }
        [Serializable] sealed class LegacyCompletion
        {
            public int system=1,planet;
            public float seaLevel,rotation,stoneRuggedness,iceCoverage,fireHeat,atmosphereStrength,atmosphereWidth;
            public Color atmosphereColor;
            public PlanetStats planetStats;
            public List<int> discoveries;
            public List<DiscoveryRecord> discoveryDetails;
            public bool discoveryTutorialSeen,nextWorldTutorialSeen;
            public CompletedWorld Convert()=>new CompletedWorld{system=system,planet=planet,seaLevel=seaLevel,rotation=rotation,
                planetStats=planetStats ?? PlanetStats.Legacy(stoneRuggedness,iceCoverage,fireHeat,atmosphereColor,atmosphereStrength,atmosphereWidth),
                discoveries=discoveries ?? new List<int>(),discoveryDetails=discoveryDetails ?? new List<DiscoveryRecord>(),
                discoveryTutorialSeen=discoveryTutorialSeen,nextWorldTutorialSeen=nextWorldTutorialSeen};
        }
        public static PlayerProfile Load(string path)
        {
            if(!File.Exists(path))return new PlayerProfile{universeSeed=Guid.NewGuid().GetHashCode()&int.MaxValue};
            return Deserialize(File.ReadAllText(path));
        }
        public static PlayerProfile Deserialize(string json)
        {
            var save=JsonUtility.FromJson<PlayerProfile>(json);
            if(save==null || save.systems==null || save.placedPredefinedWorlds==null || save.unlockedSpheres==null || save.version<1 || save.version>2)
                throw new InvalidDataException("Invalid EcoSphere player profile");
            if(save.version==1)
            {
                var legacy=JsonUtility.FromJson<LegacySave>(json);
                if(legacy.stoneUnlocked && !save.unlockedSpheres.Contains(SphereIds.Stone))save.unlockedSpheres.Add(SphereIds.Stone);
                if(legacy.worlds!=null)foreach(var old in legacy.worlds)
                {
                    var system=save.systems.Find(s=>s.number==old.system);
                    if(system==null || old.planet<0 || old.planet>=system.worlds.Count)throw new InvalidDataException("Legacy completion has no planet");
                    var planet=system.worlds[old.planet];planet.completion=old.Convert();
                    planet.completion.score=planet.highestScore;planet.completion.terrainSeed=planet.seed;
                }
                foreach(var system in save.systems)foreach(var planet in system.worlds)
                {
                    var definition=WorldCatalog.Current.Find(planet.predefinedId);
                    if(definition && (planet.naturalSpheres==null || planet.naturalSpheres.Count==0))
                    {
                        planet.naturalSpheres=new List<NaturalSpherePlacement>();
                        foreach(var natural in definition.naturalSpheres)planet.naturalSpheres.Add(natural.Copy());
                    }
                }
                save.version=2;
            }
            if(string.IsNullOrEmpty(save.id))save.id=Guid.NewGuid().ToString("N");
            if(save.audio==null)save.audio=new AudioPreferences();
            var numbers=new HashSet<int>();var ids=new HashSet<string>();
            foreach(var system in save.systems)
            {
                if(system==null || system.number<1 || !numbers.Add(system.number) || system.worlds==null)throw new InvalidDataException("Invalid or duplicate system");
                if(string.IsNullOrEmpty(system.id))system.id="system:"+system.number;
                if(string.IsNullOrEmpty(system.displayName))system.displayName="System "+system.number.ToString("D2");
                for(int i=0;i<system.worlds.Count;i++)
                {
                    var planet=system.worlds[i];
                    if(planet==null || string.IsNullOrEmpty(planet.id) || !ids.Add(planet.id))throw new InvalidDataException("Invalid or duplicate planet");
                    if(planet.naturalSpheres==null)planet.naturalSpheres=new List<NaturalSpherePlacement>();
                    var completion=planet.completion;if(completion==null)continue;
                    completion.system=system.number;completion.planet=i;
                    completion.discoveries=completion.discoveries ?? new List<int>();completion.discoveryDetails=completion.discoveryDetails ?? new List<DiscoveryRecord>();
                }
            }
            return save;
        }
        public static void Save(string path,PlayerProfile data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temporary=path+".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(data,true));
            if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
        }
    }
}
