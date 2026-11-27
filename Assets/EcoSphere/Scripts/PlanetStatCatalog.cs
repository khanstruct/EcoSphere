using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    [Serializable] public struct PlanetStatEffect
    {
        public PlanetStat stat;[Range(-1,1)] public float amount;
        public PlanetStatEffect(PlanetStat stat,float amount){this.stat=stat;this.amount=amount;}
    }
    // An element's pull on one planet color. Strength weights both the color blend and how strongly the trait shows.
    [Serializable] public struct PlanetColorEffect
    {
        public PlanetColorTrait trait;public Color color;[Range(0,1)] public float strength;
        public PlanetColorEffect(PlanetColorTrait trait,Color color,float strength){this.trait=trait;this.color=color;this.strength=strength;}
    }
    [Serializable] public sealed class PlanetColorResponse
    {
        public PlanetColorTrait trait;
        [Min(.001f),Tooltip("Combined weighted color strength that reaches Maximum Amount.")]
        public float contributionForFullEffect=.3f;
        [Range(0,1),Tooltip("Largest share of the base color the tint may replace.")]
        public float maximumAmount=.85f;
        public float Evaluate(float contribution)=>Mathf.Clamp01(contribution/Mathf.Max(.001f,contributionForFullEffect))*maximumAmount;
    }
    [Serializable] public sealed class ElementPlanetProfile
    {
        public ElementId element;
        public List<PlanetStatEffect> effects=new List<PlanetStatEffect>();
        [Tooltip("Planet colors this element tints (water, vegetation, land). Blended by weighted contribution like the atmosphere.")]
        public List<PlanetColorEffect> colors=new List<PlanetColorEffect>();
        public Color atmosphereColor=Color.white;
        [Range(0,1)] public float atmosphereStrength;
    }
    [Serializable] public sealed class PlanetStatResponse
    {
        public PlanetStat stat;
        [Min(.001f),Tooltip("Combined weighted contribution that produces full visual strength. Temperature and atmosphere expansion also support negative values.")]
        public float contributionForFullEffect=.5f;
        [Min(.001f)] public float negativeContributionForFullEffect=.5f;
        public float Evaluate(float contribution)
        {
            float full=contribution<0 ? negativeContributionForFullEffect : contributionForFullEffect;
            return Mathf.Clamp(contribution/Mathf.Max(.001f,full),-1,1);
        }
    }
    [CreateAssetMenu(menuName="EcoSphere/Planet Stat Catalog")]
    public sealed class PlanetStatCatalog : ScriptableObject
    {
        public const string ResourceName="PlanetStatCatalog";
        static PlanetStatCatalog current;
        public static PlanetStatCatalog Current
        {
            get
            {
                if(!current)current=Resources.Load<PlanetStatCatalog>(ResourceName);
                return current ? current : throw new InvalidOperationException("Missing PlanetStatCatalog resource");
            }
        }
        public int Revision {get;private set;}
        void OnEnable()=>NotifyChanged();
        void OnValidate()=>NotifyChanged();
        void OnDisable(){if(current==this)current=null;}
        // Runtime tools that edit nested profile lists should notify once after their batch of edits.
        public void NotifyChanged()=>Revision++;
        [Min(1)] public float pointsForFullInfluence=250;
        [Min(1), Tooltip("Temperature buildup uses the whole planet total, not each element separately.")]
        public float temperaturePointsForFullInfluence=100;
        public float neutralTemperatureFahrenheit=59;
        public float coldTemperatureFahrenheit=-60;
        public float hotTemperatureFahrenheit=250;
        [Tooltip("Vegetation temperature: cold limit, cool comfort, warm comfort, hot limit (F).")]
        public Vector4 vegetationTemperatureRangeFahrenheit=new Vector4(-25,45,85,180);
        [Range(0,1)] public float dryVegetationGrowth=.55f;
        [Range(0,.5f)] public float dominanceBonus=.25f;
        [Range(-1,0)] public float freezingStartsAt=-.02f;
        [Range(-1,0)] public float fullyFrozenAt=-.45f;
        [Range(.01f,.12f)] public float minimumAtmosphereWidth=.02f;
        [Range(.01f,.12f)] public float baseAtmosphereWidth=.05f;
        [Range(.01f,.12f)] public float maximumAtmosphereWidth=.12f;
        public float ClampAtmosphereWidth(float width)=>Mathf.Clamp(width,minimumAtmosphereWidth,maximumAtmosphereWidth);
        public List<ElementPlanetProfile> elements=new List<ElementPlanetProfile>();
        public List<PlanetStatResponse> responses=DefaultResponses();
        public static List<PlanetStatResponse> DefaultResponses()
        {
            var result=new List<PlanetStatResponse>();
            foreach(PlanetStat stat in Enum.GetValues(typeof(PlanetStat)))
            {
                float full=stat==PlanetStat.Temperature ? 1 : stat==PlanetStat.CloudCover ? .45f :
                    stat==PlanetStat.BiologicalCoverage ? .3f : stat==PlanetStat.AtmosphereDensity ? .3f : stat==PlanetStat.CloudThickness ? .35f :
                    stat==PlanetStat.AtmosphereExpansion ? .6f : .45f;
                result.Add(new PlanetStatResponse{stat=stat,contributionForFullEffect=full,negativeContributionForFullEffect=full});
            }
            return result;
        }
        public float Respond(PlanetStat stat,float contribution)
        {
            if(responses!=null)foreach(var response in responses)if(response.stat==stat)return response.Evaluate(contribution);
            return Mathf.Clamp(contribution,-1,1);
        }
        public List<PlanetColorResponse> colorResponses=DefaultColorResponses();
        public static List<PlanetColorResponse> DefaultColorResponses()
        {
            var result=new List<PlanetColorResponse>();
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))result.Add(new PlanetColorResponse{trait=trait});
            return result;
        }
        public float RespondColor(PlanetColorTrait trait,float contribution)
        {
            if(colorResponses!=null)foreach(var response in colorResponses)if(response.trait==trait)return response.Evaluate(contribution);
            return Mathf.Clamp01(contribution);
        }
        public void Validate()
        {
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))
                if(colorResponses==null || colorResponses.FindAll(x=>x.trait==trait).Count!=1)throw new InvalidOperationException("Planet color trait "+trait+" needs exactly one response");
            foreach(var profile in elements)
            {
                var seen=new HashSet<PlanetColorTrait>();
                foreach(var tint in profile.colors)
                    if(!Enum.IsDefined(typeof(PlanetColorTrait),tint.trait) || !seen.Add(tint.trait) || tint.strength<=0 || tint.strength>1)
                        throw new InvalidOperationException(profile.element+": color traits must be distinct with strength in (0, 1]");
            }
        }
        // Default element tints. The one authored list: PopulateDefaults and PlanetColorTools.Configure both apply it.
        public void SeedColorTraits()
        {
            foreach(var profile in elements)profile.colors.Clear();
            Tint(ElementId.Plant,PlanetColorTrait.Water,new Color(.04f,.23f,.25f).gamma,.026f);     // same hue as the former WaterGreenness, kept equally faint
            Tint(ElementId.Algae,PlanetColorTrait.Water,new Color(.05f,.3f,.2f),.8f);
            Tint(ElementId.Algae,PlanetColorTrait.Vegetation,new Color(.1f,.45f,.4f),.6f);
            Tint(ElementId.Forest,PlanetColorTrait.Vegetation,new Color(.04f,.24f,.07f),.9f);   // deep, dense green
            Tint(ElementId.Mud,PlanetColorTrait.Water,new Color(.2f,.14f,.07f),.45f);
            Tint(ElementId.Swamp,PlanetColorTrait.Water,new Color(.12f,.15f,.06f),.6f);
            Tint(ElementId.Poison,PlanetColorTrait.Water,new Color(.25f,.32f,.05f),.7f);
            Tint(ElementId.Poison,PlanetColorTrait.Vegetation,new Color(.55f,.55f,.12f),.6f);
            Tint(ElementId.Oil,PlanetColorTrait.Water,new Color(.06f,.05f,.04f),.7f);
            Tint(ElementId.Smoke,PlanetColorTrait.Water,new Color(.18f,.17f,.14f),.35f);
            Tint(ElementId.Magic,PlanetColorTrait.Vegetation,new Color(.5f,.2f,.75f),.8f);
            Tint(ElementId.Crystal,PlanetColorTrait.Land,new Color(.89f,.42f,.95f),.7f);
            Tint(ElementId.Crystal,PlanetColorTrait.Vegetation,new Color(.75f,.45f,.85f),.25f);
            Tint(ElementId.Fungus,PlanetColorTrait.Vegetation,new Color(.55f,.42f,.5f),.6f);
            Tint(ElementId.Flower,PlanetColorTrait.Vegetation,new Color(.75f,.45f,.3f),.3f);
            Tint(ElementId.Salt,PlanetColorTrait.Land,new Color(.92f,.9f,.86f),.6f);
            Tint(ElementId.Clay,PlanetColorTrait.Land,new Color(.8f,.38f,.2f),.6f);
            Tint(ElementId.Coal,PlanetColorTrait.Land,new Color(.12f,.11f,.11f),.6f);
            Tint(ElementId.Ash,PlanetColorTrait.Land,new Color(.3f,.29f,.28f),.4f);
            Tint(ElementId.Metal,PlanetColorTrait.Land,new Color(.55f,.6f,.66f),.4f);
            Tint(ElementId.Glass,PlanetColorTrait.Land,new Color(.6f,.78f,.8f),.35f);
            // City lights default to the warm color on the planet material; these shift it.
            Tint(ElementId.Technology,PlanetColorTrait.CityLights,new Color(.8f,.9f,1f),.5f);
            Tint(ElementId.Magic,PlanetColorTrait.CityLights,new Color(.85f,.6f,1f),.8f);
            NotifyChanged();
        }
        // City lights: Civilization (population sprawl) spreads them; Technology, City and Energy brighten them.
        // Without brightness they stay dim (the material's base brightness). The one authored list:
        // PopulateDefaults and CityLightTools.Configure both apply it without touching other effects.
        // Every element except Core and Laser needs a profile, even one with no planet effect (it still counts in the totals).
        // New elements appended to ElementId get an empty profile here until their effects are authored.
        public void EnsureProfiles()
        {
            // Retired elements have no profile: drop any left over from before they were retired.
            elements.RemoveAll(x=>ElementIds.IsRetired(x.element));
            foreach(ElementId element in Enum.GetValues(typeof(ElementId)))
                if(element!=ElementId.Core && element!=ElementId.Laser && !ElementIds.IsRetired(element) && !elements.Exists(x=>x.element==element))
                    elements.Add(new ElementPlanetProfile{element=element});
            NotifyChanged();
        }
        public void SeedCityLights()
        {
            SetEffect(ElementId.Civilization,PlanetStat.Civilization,1);
            SetEffect(ElementId.Human,PlanetStat.CityLightIntensity,0);
            SetEffect(ElementId.City,PlanetStat.CityLightIntensity,.6f);
            SetEffect(ElementId.Technology,PlanetStat.CityLightIntensity,1);
            SetEffect(ElementId.Energy,PlanetStat.CityLightIntensity,.4f);
            NotifyChanged();
        }
        // Forest: denser vegetation than Plant (1 vs 0.8 coverage), some soil, moisture and cloud, and a stronger
        // blue-green atmosphere. Its deep green vegetation tint lives in SeedColorTraits. PopulateDefaults and
        // SphereIntroTools.Configure both apply it without touching other profiles.
        public void SeedForest()
        {
            SetEffect(ElementId.Forest,PlanetStat.BiologicalCoverage,1);
            SetEffect(ElementId.Forest,PlanetStat.Soil,.2f);
            SetEffect(ElementId.Forest,PlanetStat.Moisture,.35f);
            SetEffect(ElementId.Forest,PlanetStat.CloudCover,.15f);
            SetEffect(ElementId.Forest,PlanetStat.AtmosphereExpansion,.18f);
            var forest=elements.Find(x=>x.element==ElementId.Forest);
            forest.atmosphereColor=new Color(.5f,.82f,.78f,1);forest.atmosphereStrength=.12f;
            NotifyChanged();
        }
        void SetEffect(ElementId element,PlanetStat stat,float amount)
        {
            var profile=elements.Find(x=>x.element==element);
            // Amount 0 removes the effect. A new element (appended to ElementId) gets its profile here.
            if(profile==null){profile=new ElementPlanetProfile{element=element};elements.Add(profile);}
            profile.effects.RemoveAll(effect=>effect.stat==stat);if(amount!=0)profile.effects.Add(new PlanetStatEffect(stat,amount));
        }
        void Tint(ElementId element,PlanetColorTrait trait,Color color,float strength)
        {
            var profile=elements.Find(x=>x.element==element);
            if(profile==null){profile=new ElementPlanetProfile{element=element};elements.Add(profile);}
            profile.colors.Add(new PlanetColorEffect(trait,color,strength));
        }
        public void PopulateDefaults()
        {
            elements.Clear();
            Add(ElementId.Fire,(PlanetStat.Temperature,.8f),(PlanetStat.GeologicalActivity,.7f),(PlanetStat.AtmosphereExpansion,.2f),(PlanetStat.CloudDarkness,.6f));
            Add(ElementId.Ice,(PlanetStat.Temperature,-1),(PlanetStat.SurfaceWater,.8f),(PlanetStat.Moisture,.3f),(PlanetStat.AtmosphereExpansion,-.5f));
            Add(ElementId.Water,(PlanetStat.SurfaceWater,1),(PlanetStat.Moisture,.8f),(PlanetStat.AtmosphereExpansion,.85f),(PlanetStat.CloudThickness,.6f),(PlanetStat.CloudCover,.2f));
            Add(ElementId.Stone,(PlanetStat.TerrainRuggedness,1),(PlanetStat.Mineralization,.25f));
            Add(ElementId.Sunlight,(PlanetStat.Temperature,1));
            Add(ElementId.Void);
            Add(ElementId.Air,(PlanetStat.WeatherActivity,.2f),(PlanetStat.AtmosphereExpansion,.6f),(PlanetStat.CloudCover,.75f));
            Add(ElementId.Soil,(PlanetStat.Soil,1),(PlanetStat.Moisture,.15f));
            Add(ElementId.Sand,(PlanetStat.Sediment,1),(PlanetStat.Soil,.1f));
            Add(ElementId.Life,(PlanetStat.BiologicalCoverage,.5f),(PlanetStat.Soil,.2f));
            Add(ElementId.Steam,(PlanetStat.Temperature,.25f),(PlanetStat.Moisture,.8f),(PlanetStat.CloudCover,.6f),(PlanetStat.CloudThickness,.5f),(PlanetStat.AtmosphereExpansion,.4f));
            Add(ElementId.Cloud,(PlanetStat.CloudCover,1),(PlanetStat.CloudThickness,.8f),(PlanetStat.Moisture,.4f));
            Add(ElementId.Rain,(PlanetStat.Temperature,-.15f),(PlanetStat.Moisture,1),(PlanetStat.SurfaceWater,.25f),(PlanetStat.CloudCover,.6f),(PlanetStat.WeatherActivity,.6f));
            Add(ElementId.Snow,(PlanetStat.Temperature,-.8f),(PlanetStat.SurfaceWater,.6f),(PlanetStat.Moisture,.4f));
            Add(ElementId.Wind,(PlanetStat.WeatherActivity,1));
            Add(ElementId.Lightning,(PlanetStat.ElectricalActivity,1),(PlanetStat.WeatherActivity,.5f));
            Add(ElementId.Lava,(PlanetStat.Temperature,1),(PlanetStat.GeologicalActivity,1),(PlanetStat.TerrainRuggedness,.4f));
            Add(ElementId.Smoke,(PlanetStat.Pollution,.8f),(PlanetStat.CloudThickness,.3f),(PlanetStat.AtmosphereExpansion,.3f));
            Add(ElementId.Ash,(PlanetStat.Sediment,.6f),(PlanetStat.Soil,.35f),(PlanetStat.Pollution,.25f));
            Add(ElementId.Dust,(PlanetStat.Sediment,.8f),(PlanetStat.Pollution,.3f));
            Add(ElementId.Poison,(PlanetStat.Pollution,1),(PlanetStat.BiologicalCoverage,-.35f));
            Add(ElementId.Clay,(PlanetStat.Soil,.7f),(PlanetStat.Sediment,.4f),(PlanetStat.Moisture,.3f));
            Add(ElementId.Salt,(PlanetStat.Mineralization,.6f),(PlanetStat.Sediment,.35f));
            Add(ElementId.Metal,(PlanetStat.Mineralization,.8f));
            Add(ElementId.Crystal,(PlanetStat.Mineralization,1),(PlanetStat.TerrainRuggedness,.2f));
            Add(ElementId.Glass,(PlanetStat.Mineralization,.6f));
            Add(ElementId.Coal,(PlanetStat.Mineralization,.4f));Add(ElementId.Oil,(PlanetStat.Pollution,.3f));
            Add(ElementId.Wood,(PlanetStat.BiologicalCoverage,.4f));Add(ElementId.Gravity);
            Add(ElementId.Plant,(PlanetStat.BiologicalCoverage,.8f),(PlanetStat.Soil,.25f),(PlanetStat.AtmosphereExpansion,.12f));
            Add(ElementId.Algae,(PlanetStat.BiologicalCoverage,.6f),(PlanetStat.Moisture,.4f));
            Add(ElementId.Moss,(PlanetStat.BiologicalCoverage,.5f),(PlanetStat.Moisture,.35f));
            Add(ElementId.Fungus,(PlanetStat.BiologicalCoverage,.5f),(PlanetStat.Moisture,.3f));
            Add(ElementId.Flower,(PlanetStat.BiologicalCoverage,.6f));
            Add(ElementId.Bacteria,(PlanetStat.BiologicalCoverage,.2f));
            foreach(var id in new[]{ElementId.Insect,ElementId.Fish,ElementId.Bird,ElementId.Reptile,ElementId.Mammal})Add(id);
            Add(ElementId.Human,(PlanetStat.Civilization,.25f));
            Add(ElementId.Ocean,(PlanetStat.SurfaceWater,1),(PlanetStat.Moisture,1));
            Add(ElementId.Swamp,(PlanetStat.SurfaceWater,.4f),(PlanetStat.Moisture,1),(PlanetStat.Soil,.8f),(PlanetStat.BiologicalCoverage,.6f));
            Add(ElementId.Energy,(PlanetStat.GeologicalActivity,.25f),(PlanetStat.ElectricalActivity,.5f));
            Add(ElementId.Magic,(PlanetStat.ExoticActivity,1));
            Add(ElementId.Technology,(PlanetStat.Civilization,.7f),(PlanetStat.ElectricalActivity,.3f),(PlanetStat.Pollution,.15f));
            Add(ElementId.Sound);
            Add(ElementId.City,(PlanetStat.Civilization,1),(PlanetStat.Pollution,.3f));
            Add(ElementId.Mud,(PlanetStat.Soil,1));
            Atmosphere(ElementId.Fire,new Color(1,.25f,.035f,1),.12f);
            Atmosphere(ElementId.Ice,new Color(.88f,.95f,1,1),.18f);
            Atmosphere(ElementId.Water,new Color(.72f,.87f,1,1),.14f);
            Atmosphere(ElementId.Plant,new Color(.56f,.85f,.85f,1),.08f);
            Atmosphere(ElementId.Air,new Color(.45f,.72f,1,1),.5f);
            Atmosphere(ElementId.Steam,new Color(.9f,.94f,1,1),.3f);
            Atmosphere(ElementId.Smoke,new Color(.38f,.34f,.3f,1),.35f);
            Atmosphere(ElementId.Dust,new Color(.78f,.64f,.45f,1),.2f);
            Atmosphere(ElementId.Poison,new Color(.65f,.8f,.4f,1),.25f);
            SeedColorTraits();
            SeedCityLights();
            SeedForest();
            EnsureProfiles();
            NotifyChanged();
        }
        void Add(ElementId element,params (PlanetStat stat,float amount)[] effects)
        {
            var profile=new ElementPlanetProfile{element=element};
            foreach(var effect in effects)profile.effects.Add(new PlanetStatEffect(effect.stat,effect.amount));
            elements.Add(profile);
        }
        void Atmosphere(ElementId element,Color color,float strength)
        {
            var profile=elements.Find(x=>x.element==element);profile.atmosphereColor=color;profile.atmosphereStrength=strength;
        }
    }
}
