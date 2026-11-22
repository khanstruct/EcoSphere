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
    [Serializable] public sealed class ElementPlanetProfile
    {
        public ElementId element;
        public List<PlanetStatEffect> effects=new List<PlanetStatEffect>();
        public Color atmosphereColor=Color.white;
        [Range(0,1)] public float atmosphereStrength;
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
        public void PopulateDefaults()
        {
            elements.Clear();
            Add(ElementId.Fire,(PlanetStat.Temperature,.8f),(PlanetStat.GeologicalActivity,.7f),(PlanetStat.AtmosphereExpansion,.2f));
            Add(ElementId.Ice,(PlanetStat.Temperature,-1),(PlanetStat.SurfaceWater,.8f),(PlanetStat.Moisture,.3f),(PlanetStat.AtmosphereExpansion,-.5f));
            Add(ElementId.Water,(PlanetStat.SurfaceWater,1),(PlanetStat.Moisture,.8f),(PlanetStat.AtmosphereExpansion,.85f));
            Add(ElementId.Stone,(PlanetStat.TerrainRuggedness,1),(PlanetStat.Mineralization,.25f));
            Add(ElementId.Sunlight,(PlanetStat.Temperature,1));
            Add(ElementId.Void);
            Add(ElementId.Air,(PlanetStat.WeatherActivity,.2f),(PlanetStat.AtmosphereExpansion,.6f));
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
            Add(ElementId.Plant,(PlanetStat.BiologicalCoverage,.8f),(PlanetStat.Soil,.25f),(PlanetStat.WaterGreenness,.04f),(PlanetStat.AtmosphereExpansion,.12f));
            Add(ElementId.Algae,(PlanetStat.BiologicalCoverage,.6f),(PlanetStat.Moisture,.4f));
            Add(ElementId.Moss,(PlanetStat.BiologicalCoverage,.5f),(PlanetStat.Moisture,.35f));
            Add(ElementId.Fungus,(PlanetStat.BiologicalCoverage,.5f),(PlanetStat.Moisture,.3f));
            Add(ElementId.Flower,(PlanetStat.BiologicalCoverage,.6f));
            Add(ElementId.Tree,(PlanetStat.BiologicalCoverage,1),(PlanetStat.Soil,.2f));
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
