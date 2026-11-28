using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public sealed class PlanetSurface : MonoBehaviour
    {
        [Range(-.02f,1.02f)] public float seaLevel=-.02f;
        public float rotationSpeed=2;
        public PlanetStats Stats {get;}=new PlanetStats();
        readonly PlanetStats shownStats=new PlanetStats();
        public float StoneRuggedness=>Stats.Ruggedness;
        public float IceCoverage=>Stats.IceCoverage;
        public float FireHeat=>Stats.Heat;
        public float DisplayedTemperatureFahrenheit=>shownStats.TemperatureFahrenheit;
        public float WaterCoverage=>Stats.SurfaceWater;
        public Color AtmosphereColor=>Stats.atmosphereColor;
        public float AtmosphereStrength=>Stats.AtmosphereDensity;
        public float AtmosphereWidth=>Stats.atmosphereWidth;
        public float TargetSeaLevel {get;private set;}=-.02f;
        Renderer surface;
        MaterialPropertyBlock properties;
        PlanetAtmosphere atmosphere;
        PlanetClouds clouds;
        PlanetMoons moons;
        internal PlanetMoons Moons => moons;
        float[] elevationCdf;
        Vector4 terrainBounds;
        bool compositionDriven;
        double[] previousComposition=Array.Empty<double>();
        PlanetStatCatalog appliedCatalog;
        int appliedCatalogRevision=-1;
        internal int EvaluationCount {get;private set;}
        static readonly Dictionary<Texture,float[]> distributions=new Dictionary<Texture,float[]>();
        void Awake()
        {
            surface=GetComponent<Renderer>();properties=new MaterialPropertyBlock();
            terrainBounds=TerrainBounds(surface.sharedMaterial);
            atmosphere=PlanetAtmosphere.Create(transform);
            clouds=PlanetClouds.Create(transform);
            moons=PlanetMoons.Create(transform);
        }
        public static float Coverage(IReadOnlyList<double> elements,float fullInfluence=250,float dominanceBonus=.25f)
            =>PlanetStatCalculator.Influence(elements,ElementId.Water,fullInfluence,dominanceBonus);
        public static float Influence(IReadOnlyList<double> elements,ElementId element,float fullInfluence=250,float dominanceBonus=.25f)
            =>PlanetStatCalculator.Influence(elements,element,fullInfluence,dominanceBonus);
        public void ApplyComposition(IReadOnlyList<double> elements)
        {
            var catalog=PlanetStatCatalog.Current;int count=elements?.Count ?? 0;
            bool unchanged=appliedCatalog==catalog && appliedCatalogRevision==catalog.Revision && previousComposition.Length==count;
            if(unchanged)for(int i=0;i<count;i++)if(previousComposition[i]!=ElementComposition.PositiveFinite(elements[i])){unchanged=false;break;}
            if(unchanged)return;
            if(previousComposition.Length!=count)previousComposition=new double[count];
            for(int i=0;i<count;i++)previousComposition[i]=ElementComposition.PositiveFinite(elements[i]);
            PlanetStatCalculator.Evaluate(elements,catalog,Stats);
            appliedCatalog=catalog;appliedCatalogRevision=catalog.Revision;EvaluationCount++;
            if(elevationCdf==null)elevationCdf=ElevationDistribution(surface.sharedMaterial);
            TargetSeaLevel=LevelForCoverage(WaterCoverage);compositionDriven=true;
        }
        float LevelForCoverage(float coverage)=>LevelForCoverage(elevationCdf,coverage);
        public static Vector4 TerrainBounds(Material material)
        {
            var distribution=ElevationDistribution(material);
            return new Vector4(LevelForCoverage(distribution,.02f),LevelForCoverage(distribution,.98f),0,0);
        }
        static float LevelForCoverage(float[] distribution,float coverage)
        {
            if(coverage<=0)return -.02f;
            if(coverage>=1)return 1.02f;
            for(int i=0;i<distribution.Length;i++)
                if(distribution[i]>=coverage)
                {
                    float previous=i==0?0:distribution[i-1];
                    return (i+Mathf.InverseLerp(previous,distribution[i],coverage))/distribution.Length;
                }
            return 1.02f;
        }

        // Convert requested surface area into elevation, rather than assuming uniformly distributed terrain heights.
        static float[] ElevationDistribution(Material material)
        {
            var height=material && material.HasProperty("_HeightMap") ? material.GetTexture("_HeightMap") : null;
            if(!height)return new[]{1f};
            if(distributions.TryGetValue(height,out var cached))return cached;
            var target=RenderTexture.GetTemporary(256,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;
            var sample=new Texture2D(256,128,TextureFormat.RGBA32,false,true);
            float[] histogram=new float[256];
            try
            {
                Graphics.Blit(height,target); RenderTexture.active=target;
                sample.ReadPixels(new Rect(0,0,256,128),0,0); sample.Apply();
                var pixels=sample.GetPixels32(); double area=0;
                for(int y=0;y<128;y++)
                {
                    float weight=Mathf.Sin(Mathf.PI*(y+.5f)/128);
                    for(int x=0;x<256;x++) {histogram[pixels[y*256+x].r]+=weight;area+=weight;}
                }
                float sum=0;
                for(int i=0;i<256;i++){sum+=histogram[i];histogram[i]=(float)(sum/area);}
                histogram[255]=1; distributions[height]=histogram;
                return histogram;
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(sample);}
        }

        void LateUpdate()
        {
            transform.Rotate(0,rotationSpeed*Time.deltaTime,0,Space.Self);
            if(compositionDriven)seaLevel=Mathf.MoveTowards(seaLevel,TargetSeaLevel,Time.deltaTime*.45f);
            shownStats.Approach(Stats,Time.deltaTime);
            surface.GetPropertyBlock(properties);properties.SetFloat("_SeaLevel",seaLevel);
            properties.SetVector("_TerrainBounds",terrainBounds);
            shownStats.Apply(properties);surface.SetPropertyBlock(properties);
            if(atmosphere)atmosphere.SetAppearance(shownStats.atmosphereColor,shownStats.AtmosphereDensity,shownStats.atmosphereWidth);
            if(clouds)clouds.SetAppearance(shownStats);
            if(moons)moons.SetAppearance(shownStats);
        }
    }
}
