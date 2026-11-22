using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public static class PlanetTemperatureDisplay
    {
        public static string Average(float fahrenheit)=>"Average Temperature: "+Mathf.RoundToInt(fahrenheit).ToString(System.Globalization.CultureInfo.InvariantCulture)+"°F";
    }
    // Append channels only; completed-world snapshots store this stable order.
    public enum PlanetStat { Temperature, TerrainRuggedness, SurfaceWater, Sediment, Soil,
        Moisture, BiologicalCoverage, GeologicalActivity, AtmosphereDensity, CloudCover,
        CloudThickness, WeatherActivity, ElectricalActivity, Civilization, Pollution,
        Mineralization, ExoticActivity, AtmosphereExpansion, WaterGreenness }

    [Serializable]
    public sealed class PlanetStats
    {
        static readonly int ChannelCount=Enum.GetValues(typeof(PlanetStat)).Length;
        public int version=1;
        public float[] values=new float[ChannelCount];
        public Color atmosphereColor;
        public float atmosphereWidth;
        // Saved with the stats so later tuning does not rewrite a completed world's climate.
        // Defaults retain the old linear response for snapshots written before these fields existed.
        public float freezingStartsAt=0, fullyFrozenAt=-1;
        public float neutralTemperatureFahrenheit=59, coldTemperatureFahrenheit=-60, hotTemperatureFahrenheit=250;
        public Vector4 vegetationTemperatureRangeFahrenheit=new Vector4(-25,45,85,180);
        public float dryVegetationGrowth=.55f;
        public float this[PlanetStat stat] {
            get => values!=null && (int)stat<values.Length ? values[(int)stat] : 0;
            set { EnsureSize();values[(int)stat]=value; }
        }
        void EnsureSize() { if(values==null || values.Length!=ChannelCount)Array.Resize(ref values,ChannelCount); }
        public void Clear() {EnsureSize();Array.Clear(values,0,values.Length);atmosphereColor=Color.clear;atmosphereWidth=0;}
        void CopyClimateFrom(PlanetStats other)
        {
            freezingStartsAt=other.freezingStartsAt;fullyFrozenAt=other.fullyFrozenAt;
            neutralTemperatureFahrenheit=other.neutralTemperatureFahrenheit;
            coldTemperatureFahrenheit=other.coldTemperatureFahrenheit;hotTemperatureFahrenheit=other.hotTemperatureFahrenheit;
            vegetationTemperatureRangeFahrenheit=other.vegetationTemperatureRangeFahrenheit;dryVegetationGrowth=other.dryVegetationGrowth;
        }
        public void CopyFrom(PlanetStats other)
        {
            if(ReferenceEquals(this,other))return;
            Clear();
            if(other.values!=null)Array.Copy(other.values,values,Math.Min(values.Length,other.values.Length));
            atmosphereColor=other.atmosphereColor;atmosphereWidth=other.atmosphereWidth;CopyClimateFrom(other);
        }
        public PlanetStats Copy() {var result=new PlanetStats();result.CopyFrom(this);return result;}
        public float Temperature => this[PlanetStat.Temperature]; // Abstract -1 frozen .. 0 neutral .. +1 hot.
        public float Heat => Mathf.Clamp01(Temperature);
        public float TemperatureFahrenheit => neutralTemperatureFahrenheit+Temperature*(Temperature<0 ? neutralTemperatureFahrenheit-coldTemperatureFahrenheit : hotTemperatureFahrenheit-neutralTemperatureFahrenheit);
        public float IceCoverage => Mathf.InverseLerp(freezingStartsAt,fullyFrozenAt,Temperature);
        public float Ruggedness => this[PlanetStat.TerrainRuggedness];
        public float SurfaceWater => this[PlanetStat.SurfaceWater];
        public float Geology => this[PlanetStat.GeologicalActivity];
        public float AtmosphereDensity => this[PlanetStat.AtmosphereDensity];
        public float DesertPotential => this[PlanetStat.Sediment]*(1-this[PlanetStat.Moisture])*(1-this[PlanetStat.BiologicalCoverage])*(1-IceCoverage);
        public float WetlandPotential => this[PlanetStat.Moisture]*this[PlanetStat.Soil]*(1-IceCoverage);
        public float VegetationTemperatureSuitability => Mathf.SmoothStep(0,1,Mathf.InverseLerp(vegetationTemperatureRangeFahrenheit.x,vegetationTemperatureRangeFahrenheit.y,TemperatureFahrenheit))
            *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(vegetationTemperatureRangeFahrenheit.z,vegetationTemperatureRangeFahrenheit.w,TemperatureFahrenheit)));
        public float VegetationPotential => this[PlanetStat.BiologicalCoverage]*Mathf.Lerp(dryVegetationGrowth,1,this[PlanetStat.Moisture])*(1-IceCoverage)*VegetationTemperatureSuitability;
        public float VolcanicPotential => Geology;
        public float FrozenWater => SurfaceWater*IceCoverage;
        public float LiquidWater => SurfaceWater*(1-IceCoverage);
        public float StormPotential => this[PlanetStat.CloudCover]*this[PlanetStat.WeatherActivity];
        public float CloudLightning => StormPotential*this[PlanetStat.ElectricalActivity];
        public void Approach(PlanetStats target,float dt)
        {
            EnsureSize();
            CopyClimateFrom(target);
            for(int i=0;i<values.Length;i++)values[i]=Mathf.MoveTowards(values[i],target[(PlanetStat)i],dt*.6f);
            float blend=1-Mathf.Exp(-dt*4);
            atmosphereColor=Color.Lerp(atmosphereColor,target.atmosphereColor,blend);
            atmosphereWidth=Mathf.Lerp(atmosphereWidth,target.atmosphereWidth,blend);
        }
        public static PlanetStats Legacy(float stone,float ice,float fire,Color color,float strength,float width)
        {
            var result=new PlanetStats();result[PlanetStat.Temperature]=Mathf.Clamp(fire-ice,-1,1);
            result[PlanetStat.TerrainRuggedness]=stone;result[PlanetStat.GeologicalActivity]=fire;
            result[PlanetStat.AtmosphereDensity]=strength;result.atmosphereColor=color;result.atmosphereWidth=width;return result;
        }
        public void Apply(MaterialPropertyBlock properties)
        {
            properties.SetFloat("_Temperature",Temperature);
            properties.SetFloat("_FreezeStart",freezingStartsAt);properties.SetFloat("_FullyFrozen",fullyFrozenAt);
            properties.SetFloat("_StoneRuggedness",Ruggedness);
            properties.SetFloat("_GeologicalActivity",Geology);
            properties.SetFloat("_Sediment",this[PlanetStat.Sediment]);
            properties.SetFloat("_Soil",this[PlanetStat.Soil]);
            properties.SetFloat("_Moisture",this[PlanetStat.Moisture]);
            properties.SetFloat("_Vegetation",VegetationPotential);
            properties.SetFloat("_WaterGreenness",this[PlanetStat.WaterGreenness]);
            // Legacy names are also set for existing tooling; shader logic uses shared temperature.
            properties.SetFloat("_FireHeat",Heat);properties.SetFloat("_IceCoverage",IceCoverage);
        }
    }

    public static class PlanetStatCalculator
    {
        public static float Influence(IReadOnlyList<double> elements,ElementId element,float fullInfluence=250,float dominanceBonus=.25f)
            =>new ElementComposition(elements).Influence(element,fullInfluence,dominanceBonus);
        public static PlanetStats Evaluate(IReadOnlyList<double> elements) {var result=new PlanetStats();Evaluate(elements,PlanetStatCatalog.Current,result);return result;}
        public static void Evaluate(IReadOnlyList<double> elements,PlanetStatCatalog catalog,PlanetStats result)
        {
            result.Clear();result.atmosphereWidth=catalog.baseAtmosphereWidth;
            result.freezingStartsAt=catalog.freezingStartsAt;result.fullyFrozenAt=catalog.fullyFrozenAt;
            result.neutralTemperatureFahrenheit=catalog.neutralTemperatureFahrenheit;result.coldTemperatureFahrenheit=catalog.coldTemperatureFahrenheit;result.hotTemperatureFahrenheit=catalog.hotTemperatureFahrenheit;
            result.vegetationTemperatureRangeFahrenheit=catalog.vegetationTemperatureRangeFahrenheit;result.dryVegetationGrowth=catalog.dryVegetationGrowth;
            var composition=new ElementComposition(elements);
            float temperatureBuildup=Mathf.Clamp01((float)(composition.Total/Math.Max(1,catalog.temperaturePointsForFullInfluence)));
            float atmosphereWeight=0;Color color=Color.clear;
            foreach(var profile in catalog.elements)
            {
                float influence=composition.Influence(profile.element,catalog.pointsForFullInfluence,catalog.dominanceBonus);
                if(influence<=0)continue;
                float temperatureInfluence=Mathf.Clamp01(composition.BoostedShare(profile.element,catalog.dominanceBonus)*temperatureBuildup);
                foreach(var effect in profile.effects)
                    result[effect.stat]+=effect.amount*(effect.stat==PlanetStat.Temperature ? temperatureInfluence : influence);
                float weight=profile.atmosphereStrength*influence;
                atmosphereWeight+=weight;color+=profile.atmosphereColor*weight;
            }
            for(int i=0;i<result.values.Length;i++)result.values[i]=Mathf.Clamp(result.values[i],i==(int)PlanetStat.Temperature || i==(int)PlanetStat.AtmosphereExpansion?-1:0,1);
            result[PlanetStat.AtmosphereDensity]=Mathf.Clamp01(result.AtmosphereDensity+atmosphereWeight);
            if(atmosphereWeight>0){result.atmosphereColor=color/atmosphereWeight;result.atmosphereColor.a=1;}
            else if(result.AtmosphereDensity>0)result.atmosphereColor=Color.white;
            float expansion=result[PlanetStat.AtmosphereExpansion];
            result.atmosphereWidth=catalog.ClampAtmosphereWidth(catalog.baseAtmosphereWidth+expansion*
                (expansion>=0 ? catalog.maximumAtmosphereWidth-catalog.baseAtmosphereWidth : catalog.baseAtmosphereWidth-catalog.minimumAtmosphereWidth));
        }
    }
}
