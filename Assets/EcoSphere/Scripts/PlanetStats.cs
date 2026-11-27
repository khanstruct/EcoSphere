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
        Mineralization, ExoticActivity, AtmosphereExpansion,
        [Obsolete("Superseded by the Water color trait; kept so old snapshots keep their index and tint.")] WaterGreenness,
        CloudDarkness,
        CityLightIntensity,
        // Molten rock at the surface: draws the glowing orange fissures. Separate from Temperature and GeologicalActivity.
        Magma }

    // Colors that elements tint, blended by weighted contribution. Append only: snapshots store this order.
    // Each maps to the planet shader property "_<Trait>Tint" (rgb = blended color, a = amount).
    public enum PlanetColorTrait { Water, Vegetation, Land, CityLights }

    [Serializable]
    public sealed class PlanetStats
    {
        static readonly int ChannelCount=Enum.GetValues(typeof(PlanetStat)).Length;
        static readonly int ColorTraitCount=Enum.GetValues(typeof(PlanetColorTrait)).Length;
        static readonly int[] ColorTraitProperties=ColorTraitPropertyIds();
        static int[] ColorTraitPropertyIds()
        {
            var ids=new int[ColorTraitCount];
            for(int i=0;i<ids.Length;i++)ids[i]=Shader.PropertyToID("_"+(PlanetColorTrait)i+"Tint");
            return ids;
        }
        // Tint applied to snapshots saved before color traits, which only had a green-water amount.
        // The old shader constant was linear; colors pass through SetColor as sRGB, so store its sRGB equivalent.
        static readonly Color LegacyWaterGreen=new Color(.04f,.23f,.25f).gamma;
        public int version=1;
        public float[] values=new float[ChannelCount];
        // rgb = blended trait color, a = tint amount (0 = untinted). Missing in old snapshots, which read as untinted.
        public Color[] colorTraits=new Color[ColorTraitCount];
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
        public Color this[PlanetColorTrait trait] {
            get => colorTraits!=null && (int)trait<colorTraits.Length ? colorTraits[(int)trait] : Color.clear;
            set { EnsureSize();colorTraits[(int)trait]=value; }
        }
        // What the shader draws for a trait. Old snapshots without a Water trait keep their legacy green-water amount.
        public Color AppliedTint(PlanetColorTrait trait)
        {
            var tint=this[trait];
#pragma warning disable CS0618
            float legacyGreen=this[PlanetStat.WaterGreenness];
#pragma warning restore CS0618
            if(trait==PlanetColorTrait.Water && tint.a<=0 && legacyGreen>0){tint=LegacyWaterGreen;tint.a=Mathf.Clamp01(legacyGreen);}
            return tint;
        }
        void EnsureSize()
        {
            if(values==null || values.Length!=ChannelCount)Array.Resize(ref values,ChannelCount);
            if(colorTraits==null || colorTraits.Length!=ColorTraitCount)Array.Resize(ref colorTraits,ColorTraitCount);
        }
        public void Clear() {EnsureSize();Array.Clear(values,0,values.Length);Array.Clear(colorTraits,0,colorTraits.Length);atmosphereColor=Color.clear;atmosphereWidth=0;}
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
            if(other.colorTraits!=null)Array.Copy(other.colorTraits,colorTraits,Math.Min(colorTraits.Length,other.colorTraits.Length));
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
        // Glowing fissures follow Magma only, never Temperature: cold worlds can have lava and hot deserts have none.
        public float Magma => this[PlanetStat.Magma];
        public float VolcanicPotential => Magma;
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
            for(int i=0;i<colorTraits.Length;i++)
            {
                var goal=target[(PlanetColorTrait)i];var now=colorTraits[i];
                // A trait fading in starts from its own hue rather than from black.
                if(now.a<=.001f){now.r=goal.r;now.g=goal.g;now.b=goal.b;}
                colorTraits[i]=Color.Lerp(now,goal,blend);
            }
            atmosphereColor=Color.Lerp(atmosphereColor,target.atmosphereColor,blend);
            atmosphereWidth=Mathf.Lerp(atmosphereWidth,target.atmosphereWidth,blend);
        }
        public static PlanetStats Legacy(float stone,float ice,float fire,Color color,float strength,float width)
        {
            var result=new PlanetStats();result[PlanetStat.Temperature]=Mathf.Clamp(fire-ice,-1,1);
            result[PlanetStat.TerrainRuggedness]=stone;result[PlanetStat.GeologicalActivity]=fire;result[PlanetStat.Magma]=fire;   // legacy saves drew lava from their fire value
            result[PlanetStat.AtmosphereDensity]=strength;result.atmosphereColor=color;result.atmosphereWidth=width;return result;
        }
        public void Apply(MaterialPropertyBlock properties)
        {
            properties.SetFloat("_Temperature",Temperature);
            properties.SetFloat("_FreezeStart",freezingStartsAt);properties.SetFloat("_FullyFrozen",fullyFrozenAt);
            properties.SetFloat("_StoneRuggedness",Ruggedness);
            properties.SetFloat("_GeologicalActivity",Geology);
            properties.SetFloat("_Magma",Magma);
            properties.SetFloat("_Sediment",this[PlanetStat.Sediment]);
            properties.SetFloat("_Soil",this[PlanetStat.Soil]);
            properties.SetFloat("_Moisture",this[PlanetStat.Moisture]);
            properties.SetFloat("_Vegetation",VegetationPotential);
            properties.SetFloat("_Civilization",this[PlanetStat.Civilization]);properties.SetFloat("_CityLightIntensity",this[PlanetStat.CityLightIntensity]);
            for(int i=0;i<ColorTraitProperties.Length;i++)properties.SetColor(ColorTraitProperties[i],AppliedTint((PlanetColorTrait)i));
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
            var traitWeights=new float[Enum.GetValues(typeof(PlanetColorTrait)).Length];var traitColors=new Color[traitWeights.Length];
            foreach(var profile in catalog.elements)
            {
                float influence=composition.Influence(profile.element,catalog.pointsForFullInfluence,catalog.dominanceBonus);
                if(influence<=0)continue;
                float temperatureInfluence=Mathf.Clamp01(composition.BoostedShare(profile.element,catalog.dominanceBonus)*temperatureBuildup);
                foreach(var effect in profile.effects)
                    result[effect.stat]+=effect.amount*(effect.stat==PlanetStat.Temperature ? temperatureInfluence : influence);
                float weight=profile.atmosphereStrength*influence;
                atmosphereWeight+=weight;color+=profile.atmosphereColor*weight;
                foreach(var tint in profile.colors)
                {
                    float tintWeight=tint.strength*influence;
                    traitWeights[(int)tint.trait]+=tintWeight;traitColors[(int)tint.trait]+=tint.color*tintWeight;
                }
            }
            // Color traits: weighted-average hue, with the amount set by total weighted strength through the trait's response.
            for(int i=0;i<traitWeights.Length;i++)
            {
                if(traitWeights[i]<=0)continue;
                var blended=traitColors[i]/traitWeights[i];blended.a=catalog.RespondColor((PlanetColorTrait)i,traitWeights[i]);
                result[(PlanetColorTrait)i]=blended;
            }
            // Combine all element effects before applying the visual response: opposite effects still cancel.
            result[PlanetStat.AtmosphereDensity]+=atmosphereWeight;
            for(int i=0;i<result.values.Length;i++)
            {
                var stat=(PlanetStat)i;float value=catalog.Respond(stat,result.values[i]);
                result.values[i]=Mathf.Clamp(value,stat==PlanetStat.Temperature || stat==PlanetStat.AtmosphereExpansion?-1:0,1);
            }
            if(atmosphereWeight>0){result.atmosphereColor=color/atmosphereWeight;result.atmosphereColor.a=1;}
            else if(result.AtmosphereDensity>0)result.atmosphereColor=Color.white;
            float expansion=result[PlanetStat.AtmosphereExpansion];
            result.atmosphereWidth=catalog.ClampAtmosphereWidth(catalog.baseAtmosphereWidth+expansion*
                (expansion>=0 ? catalog.maximumAtmosphereWidth-catalog.baseAtmosphereWidth : catalog.baseAtmosphereWidth-catalog.minimumAtmosphereWidth));
        }
    }
}
