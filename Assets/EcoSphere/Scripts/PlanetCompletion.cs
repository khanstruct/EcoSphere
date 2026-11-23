using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    [Serializable]
    public sealed class CompletedWorld
    {
        public int system = 1, planet;
        public float seaLevel = -.02f, rotation;
        public double score;
        public int terrainSeed;
        public string terrainStyleId="barren-v1";
        public PlanetStats planetStats=new PlanetStats();
        public float stoneRuggedness=>AppearanceStats.Ruggedness;
        public float iceCoverage=>AppearanceStats.IceCoverage;
        public float fireHeat=>AppearanceStats.Heat;
        public float atmosphereStrength=>AppearanceStats.AtmosphereDensity;
        public float atmosphereWidth=>AppearanceStats.atmosphereWidth;
        public Color atmosphereColor=>AppearanceStats.atmosphereColor;
        public List<int> discoveries = new List<int>();
        public bool discoveryTutorialSeen, nextWorldTutorialSeen;
        public List<DiscoveryRecord> discoveryDetails = new List<DiscoveryRecord>();
        public PlanetStats AppearanceStats=>planetStats ?? (planetStats=new PlanetStats());
        public void SetStats(PlanetStats stats){planetStats=stats.Copy();}
    }
    public sealed class WorldResult
    {
        public CompletedWorld world;
        public bool revisiting;
        public IReadOnlyList<int> DisplayedDiscoveries => revisiting ? (IReadOnlyList<int>)world.discoveries : discoveries;
        public readonly List<int> discoveries = new List<int>();
        public readonly List<DiscoveryRecord> discoveryDetails = new List<DiscoveryRecord>();
    }

}
