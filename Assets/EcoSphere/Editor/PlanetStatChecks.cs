using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class PlanetStatChecks
    {
        static void Expect(bool ok,string message){if(!ok)throw new Exception("Planet stats: "+message);}
        static double[] Totals(params (ElementId element,double amount)[] parts)
        {
            var totals=new double[Enum.GetValues(typeof(ElementId)).Length];foreach(var part in parts)totals[(int)part.element]+=part.amount;return totals;
        }
        static PlanetStats Stats(params (ElementId element,double amount)[] parts)=>PlanetStatCalculator.Evaluate(Totals(parts));
        public static void Validate()
        {
            var catalog=PlanetStatCatalog.Current;var unique=new HashSet<ElementId>();
            Expect(catalog.coldTemperatureFahrenheit<catalog.neutralTemperatureFahrenheit && catalog.neutralTemperatureFahrenheit<catalog.hotTemperatureFahrenheit,"ordered Fahrenheit calibration");
            Expect(catalog.fullyFrozenAt<catalog.freezingStartsAt && catalog.freezingStartsAt<=0 && catalog.fullyFrozenAt>=-1,"ordered freezing response thresholds");
            Expect(catalog.minimumAtmosphereWidth>=.01f && catalog.minimumAtmosphereWidth<=catalog.baseAtmosphereWidth && catalog.baseAtmosphereWidth<=catalog.maximumAtmosphereWidth && catalog.maximumAtmosphereWidth<=.12f,"ordered atmosphere bounds");
            Expect(catalog.elements.Count==ElementComposition.Capacity-2,"one planet profile per element, excluding Core and Laser");
            foreach(var profile in catalog.elements)
            {
                Expect(unique.Add(profile.element),"duplicate element profile");
                Expect(profile.element!=ElementId.Core && profile.element!=ElementId.Laser,"special spheres must not be elemental profiles");
                foreach(var effect in profile.effects)Expect(Enum.IsDefined(typeof(PlanetStat),effect.stat) && !float.IsNaN(effect.amount) && Mathf.Abs(effect.amount)<=1,"invalid stat effect");
            }
            Expect((int)ElementId.Ice==0 && (int)ElementId.Fire==3 && (int)ElementId.Core==7 && SphereCatalog.Current.Get(SphereIds.Water).id==6,"existing IDs/artwork must remain unchanged");
            var neutral=Stats();Expect(neutral.Temperature==0 && neutral.SurfaceWater==0 && neutral.AtmosphereDensity==0,"neutral start");
            Expect(neutral.TemperatureFahrenheit==59 && Stats((ElementId.Ice,1000)).TemperatureFahrenheit==-60,"Fahrenheit baseline and cold endpoint");
            var fireTemperature=Stats((ElementId.Fire,1000));var sunTemperature=Stats((ElementId.Sunlight,1000));
            Expect(sunTemperature.TemperatureFahrenheit>fireTemperature.TemperatureFahrenheit && sunTemperature.Geology==0 && fireTemperature.Geology>0,"Sunlight hotter than Fire, without volcanism");
            var frozenVolcano=Stats((ElementId.Ice,700),(ElementId.Lava,300));
            Expect(frozenVolcano.IceCoverage==1 && frozenVolcano.Heat==0 && frozenVolcano.VolcanicPotential>0,"cold worlds can have active lava");
            var savedClimate=JsonUtility.FromJson<PlanetStats>(JsonUtility.ToJson(fireTemperature.Copy()));
            Expect(savedClimate.TemperatureFahrenheit==fireTemperature.TemperatureFahrenheit && PlanetTemperatureDisplay.Average(59)=="Average Temperature: 59°F","saved Fahrenheit calibration and formatting");
            Debug.Log($"ECOSPHERE_HEAT_LAVA_SEPARATION_PASSED neutral={neutral.TemperatureFahrenheit}F, Fire={fireTemperature.TemperatureFahrenheit}F, Sunlight={sunTemperature.TemperatureFahrenheit}F, frozen volcanic activity={frozenVolcano.VolcanicPotential}");
            var iceAir=Stats((ElementId.Ice,1000));var fireAir=Stats((ElementId.Fire,1000));var waterAir=Stats((ElementId.Water,1000));
            Expect(iceAir.atmosphereWidth<neutral.atmosphereWidth && fireAir.atmosphereWidth>neutral.atmosphereWidth && waterAir.atmosphereWidth>fireAir.atmosphereWidth,"Ice shrinks, Fire expands modestly, Water expands strongly");
            var minorWater=Stats((ElementId.Water,250),(ElementId.Void,750));
            Expect(minorWater.atmosphereWidth>neutral.atmosphereWidth && minorWater.atmosphereWidth<waterAir.atmosphereWidth,"size follows whole-planet share, not a fixed element width");
            Expect(Stats((ElementId.Water,1)).atmosphereWidth<minorWater.atmosphereWidth,"small contributions build up gradually");
            var mixedAir=Stats((ElementId.Water,500),(ElementId.Ice,500));
            Expect(mixedAir.atmosphereWidth>neutral.atmosphereWidth && mixedAir.atmosphereWidth<waterAir.atmosphereWidth,"opposing extent modifiers combine independently of opacity");
            var limits=ScriptableObject.CreateInstance<PlanetStatCatalog>();limits.PopulateDefaults();
            limits.elements.Find(x=>x.element==ElementId.Water).effects.Add(new PlanetStatEffect(PlanetStat.AtmosphereExpansion,1));
            limits.elements.Find(x=>x.element==ElementId.Ice).effects.Add(new PlanetStatEffect(PlanetStat.AtmosphereExpansion,-1));
            var bounded=new PlanetStats();PlanetStatCalculator.Evaluate(Totals((ElementId.Water,1000)),limits,bounded);
            Expect(bounded.atmosphereWidth==limits.maximumAtmosphereWidth,"maximum extent clamp");
            PlanetStatCalculator.Evaluate(Totals((ElementId.Ice,1000)),limits,bounded);
            Expect(bounded.atmosphereWidth==limits.minimumAtmosphereWidth,"minimum extent clamp");UnityEngine.Object.DestroyImmediate(limits);
            Debug.Log($"ECOSPHERE_ATMOSPHERE_EXTENT_PASSED neutral={neutral.atmosphereWidth}, Ice={iceAir.atmosphereWidth}, Fire={fireAir.atmosphereWidth}, Water={waterAir.atmosphereWidth}, minorityWater={minorWater.atmosphereWidth}");
            var hot=Stats((ElementId.Sunlight,500),(ElementId.Sand,500));
            Expect(hot.Temperature>0 && hot.DesertPotential>0 && hot.VolcanicPotential==0,"hot desert must not be volcanic");
            var volcano=Stats((ElementId.Fire,500),(ElementId.Stone,500));
            Expect(Mathf.Approximately(volcano.Temperature,.4f) && Mathf.Approximately(volcano.Ruggedness,catalog.Respond(PlanetStat.TerrainRuggedness,.5f)) && volcano.VolcanicPotential>0,"hot rugged volcano");
            var stone=Stats((ElementId.Stone,1000));Expect(stone.Temperature==0 && stone.Geology==0 && stone.Ruggedness==1,"Stone supplies relief, not temperature or active volcanism");
            var cold=Stats((ElementId.Ice,1000));Expect(cold.Temperature==-1 && cold.SurfaceWater>0 && cold.FrozenWater>0 && cold.LiquidWater==0,"frozen ocean state");
            var secondPlaceIce=Stats((ElementId.Ice,490),(ElementId.Void,510));
            var moderateIce=Stats((ElementId.Ice,300),(ElementId.Void,700));
            var frost=Stats((ElementId.Ice,100),(ElementId.Void,900));
            Expect(Mathf.Approximately(secondPlaceIce.Temperature,-.49f) && secondPlaceIce.IceCoverage==1,"second-place Ice can form a frozen world without altering whole-planet percentages");
            Expect(frost.IceCoverage<.25f && moderateIce.IceCoverage>.6f && moderateIce.IceCoverage<secondPlaceIce.IceCoverage,"gradual frost to extensive ice to full freezing");
            var warmed=Stats((ElementId.Ice,400),(ElementId.Fire,100),(ElementId.Void,500));
            Expect(warmed.IceCoverage<Stats((ElementId.Ice,400),(ElementId.Void,600)).IceCoverage,"Fire still counteracts cold through temperature");
            var savedCold=JsonUtility.FromJson<PlanetStats>(JsonUtility.ToJson(secondPlaceIce.Copy()));
            Expect(savedCold.IceCoverage==1 && savedCold.fullyFrozenAt==catalog.fullyFrozenAt,"saved freezing response");
            var smallLevel=Stats((ElementId.Ice,110),(ElementId.Void,100));
            var smallSecondPlace=Stats((ElementId.Ice,98),(ElementId.Void,102));
            Expect(smallLevel.IceCoverage==1 && smallSecondPlace.IceCoverage==1,"realistic low-score Ice/Void worlds freeze without 250 Ice points");
            Expect(Mathf.Approximately(smallSecondPlace.Temperature,-.49f),"temperature uses whole-planet percentages after total climate buildup");
            var firstMatch=Stats((ElementId.Ice,30));
            Expect(firstMatch.IceCoverage<1,"first match still builds toward established climate");
            Expect(Stats((ElementId.Ice,55),(ElementId.Fire,55)).Temperature<0,"signed contributions combine at low scores; Ice is stronger than Fire");
            Debug.Log($"ECOSPHERE_LOW_SCORE_FREEZING_PASSED 110 Ice/100 Void={smallLevel.IceCoverage}, 98 Ice/102 Void={smallSecondPlace.IceCoverage}, first match={firstMatch.IceCoverage}");
            Debug.Log($"ECOSPHERE_FREEZING_RESPONSE_PASSED 10% Ice coverage={frost.IceCoverage}, 30%={moderateIce.IceCoverage}, 49%={secondPlaceIce.IceCoverage}");
            var balanced=Stats((ElementId.Ice,500),(ElementId.Fire,500));Expect(Mathf.Approximately(balanced.Temperature,-.1f) && balanced.Heat==0 && balanced.SurfaceWater>0 && balanced.VolcanicPotential>0,"cold worlds retain volcanic activity and water");
            var minority=Stats((ElementId.Fire,600),(ElementId.Stone,1800),(ElementId.Void,300));
            Expect(Mathf.Approximately(minority.Temperature,600f/2700*.8f) && minority.VolcanicPotential>0,"whole-planet denominator, no ignored Stone");
            var scaled=Stats((ElementId.Fire,1200),(ElementId.Stone,3600),(ElementId.Void,600));
            Expect(Mathf.Approximately(minority.Temperature,scaled.Temperature),"same mature proportions must give same temperature");
            var tiny=Stats((ElementId.Fire,1),(ElementId.Stone,3));Expect(tiny.Heat<minority.Heat,"early buildup prevents instant intense visuals");
            var wet=Stats((ElementId.Swamp,1000));Expect(wet.WetlandPotential>0 && wet.DesertPotential==0 && wet.VegetationPotential>0,"wetlands differ from deserts");
            var weather=Stats((ElementId.Cloud,500),(ElementId.Wind,500),(ElementId.Lightning,500));
            Expect(weather.CloudLightning>0 && Stats((ElementId.Lightning,1000)).CloudLightning==0,"lightning inside actual storm clouds");
            Expect(Stats((ElementId.City,1000))[PlanetStat.Civilization]==1 && Stats((ElementId.Fish,1000))[PlanetStat.Civilization]==0,"optional globe effects");
            var json=JsonUtility.ToJson(minority);var copy=JsonUtility.FromJson<PlanetStats>(json);
            Expect(copy.Temperature==minority.Temperature && copy.Geology==minority.Geology && copy.Ruggedness==minority.Ruggedness,"snapshot serialization");
            var shortened=JsonUtility.FromJson<PlanetStats>("{\"values\":[0.3]}");Expect(shortened.Temperature==.3f && shortened.Geology==0,"future appended stat compatibility");
            var legacy=PlanetStats.Legacy(.6f,.2f,.7f,Color.red,.3f,.04f);Expect(Mathf.Approximately(legacy.Temperature,.5f) && legacy.Ruggedness==.6f,"legacy preview compatibility");
            var result=new PlanetStats();var dirty=Totals((ElementId.Fire,double.NaN),(ElementId.Water,double.PositiveInfinity),(ElementId.Stone,-1));PlanetStatCalculator.Evaluate(dirty,catalog,result);
            foreach(float value in result.values)Expect(!float.IsNaN(value) && !float.IsInfinity(value),"finite output");
            Debug.Log($"ECOSPHERE_PLANET_STATS_DATA_PASSED: {catalog.elements.Count} profiles; stable IDs; percentages/buildup; desert versus volcano; cold water; temperature cancellation; wetlands; storm lightning; snapshots/legacy");
        }
    }
}
