using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class FoliageTools
    {
        public static void Configure()
        {
            PlanetVisualTools.Configure();
            var catalog=PlanetStatCatalog.Current;
            var range=catalog.vegetationTemperatureRangeFahrenheit;
            if(!(range.x<range.y && range.y<=range.z && range.z<range.w))throw new Exception("Unordered vegetation climate preference");
            var input=new double[Enum.GetValues(typeof(ElementId)).Length];input[(int)ElementId.Plant]=600;input[(int)ElementId.Life]=300;input[(int)ElementId.Water]=300;
            var stats=PlanetStatCalculator.Evaluate(input);float temperate=stats.VegetationPotential;
            if(temperate<=0 || stats[PlanetStat.WaterGreenness]<=0 || stats[PlanetStat.WaterGreenness]>.04f || stats.AtmosphereDensity<=0 || stats.atmosphereColor.b<=stats.atmosphereColor.r)
                throw new Exception("Plant's foliage/water/atmosphere profile missing");
            var hot=stats.Copy();hot[PlanetStat.Temperature]=1;
            var cold=stats.Copy();cold[PlanetStat.Temperature]=-1;
            if(hot.VegetationPotential>=temperate*.1f || cold.VegetationPotential>=temperate*.1f)throw new Exception("Extreme temperature does not suppress foliage");
            var dry=stats.Copy();dry[PlanetStat.Moisture]=0;
            if(dry.VegetationPotential<=0 || dry.VegetationPotential>=temperate)throw new Exception("Moisture must support growth without being a hard prerequisite");
            var restored=JsonUtility.FromJson<PlanetStats>(JsonUtility.ToJson(stats.Copy()));
            if(restored.VegetationPotential!=temperate || restored[PlanetStat.WaterGreenness]!=stats[PlanetStat.WaterGreenness] || restored.atmosphereWidth!=stats.atmosphereWidth)
                throw new Exception("Foliage climate/tint/atmosphere did not survive snapshot copy");
            Debug.Log($"ECOSPHERE_FOLIAGE_DATA_PASSED temperate={temperate}, hot={hot.VegetationPotential}, cold={cold.VegetationPotential}, dry={dry.VegetationPotential}; Plant tint and atmosphere, snapshot settings");
        }
        public static void Smoke(){EditorSceneManager.OpenScene(TestLevelTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
