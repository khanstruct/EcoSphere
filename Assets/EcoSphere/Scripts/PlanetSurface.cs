using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed class PlanetSurface : MonoBehaviour
    {
        [Range(-.02f, 1.02f)] public float seaLevel = -.02f;
        public float rotationSpeed = 2f;
        [Min(1)] public float waterPointsForFullInfluence = 250;
        [Range(0, .5f)] public float dominantWaterBonus = .25f;
        public float TargetSeaLevel { get; private set; } = -.02f;
        public float WaterCoverage { get; private set; }
        Renderer surface;
        MaterialPropertyBlock properties;
        float[] elevationCdf;
        bool compositionDriven;
        static readonly Dictionary<Texture, float[]> distributions = new Dictionary<Texture, float[]>();

        void Awake()
        {
            surface = GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
        }

        public static float Coverage(IReadOnlyList<double> elements, float fullInfluence = 250, float dominanceBonus = .25f)
        {
            double total = 0;
            for (int i=0;i<elements.Count;i++) total += Math.Max(0,elements[i]);
            double water = elements.Count>(int)ElementId.Water ? Math.Max(0,elements[(int)ElementId.Water]) : 0;
            if(total<=0 || water<=0)return 0;
            float share=(float)(water/total);
            float excess=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,share));
            return Mathf.Clamp01((share + dominanceBonus*excess)*Mathf.Clamp01((float)(water/Math.Max(1,fullInfluence))));
        }

        public void ApplyComposition(IReadOnlyList<double> elements)
        {
            WaterCoverage=Coverage(elements,waterPointsForFullInfluence,dominantWaterBonus);
            if(elevationCdf==null) elevationCdf=ElevationDistribution(surface.sharedMaterial);
            TargetSeaLevel=LevelForCoverage(WaterCoverage);
            compositionDriven=true;
        }

        float LevelForCoverage(float coverage)
        {
            if(coverage<=0)return -.02f;
            if(coverage>=1)return 1.02f;
            for(int i=0;i<elevationCdf.Length;i++)
                if(elevationCdf[i]>=coverage)
                {
                    float previous=i==0?0:elevationCdf[i-1];
                    return (i+Mathf.InverseLerp(previous,elevationCdf[i],coverage))/elevationCdf.Length;
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
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            if(compositionDriven)seaLevel=Mathf.MoveTowards(seaLevel,TargetSeaLevel,Time.deltaTime*.45f);
            surface.GetPropertyBlock(properties);
            properties.SetFloat("_SeaLevel", seaLevel);
            surface.SetPropertyBlock(properties);
        }
    }
}
