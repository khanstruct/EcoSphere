using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CheckLifeGameplay()
        {
            var cells=new List<Vector2Int>();foreach(var cell in board.Cells)if(cell.Value==SphereIds.Life)cells.Add(cell.Key);
            if(cells.Count!=15 || !displayedElements.Contains(ElementId.Life))throw new Exception("Life missing from generated board/composition");
            cells.RemoveAll(cell=>board.Infested.Contains(cell));
            int scoreBefore=score;
            RemoveCells(new HashSet<Vector2Int>{cells[0]},12);
            var visual=visuals[cells[1]];RemoveCells(new HashSet<Vector2Int>{cells[1]},50,true);UpdateVisualizer();
            if(totals[(int)ElementId.Life]!=46.5 || score!=scoreBefore+62)throw new Exception("Life weighted scoring incorrect");
            var sphere=Definition(SphereIds.Life);
            if(visual.GetComponent<TrailRenderer>().startColor!=sphere.color || LevelAudio.MatchVoice(SphereIds.Life,30)!=sphere.resourceVoice)
                throw new Exception("Life streak/voice ignored its definition");
            int units=0;for(int i=0;i<displayedElements.Count;i++)if(displayedElements[i]==ElementId.Life){units++;if(energyBars[i].color!=sphere.color)throw new Exception("Life visualizer color incorrect");}
            if(units!=1 || planet.GetComponent<PlanetSurface>().Stats[PlanetStat.BiologicalCoverage]<=0)throw new Exception("Life unit bars or planet stats missing");
        }
    }
    public sealed class LifeSphereSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-lifeSphereSmoke")>=0)new GameObject("Life sphere checks").AddComponent<LifeSphereSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            if(!DevelopmentMode.Enabled || !GameServices.Progress.IsSphereAvailable(SphereIds.Life))throw new Exception("Life missing from DEV collection");
            var settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previous=settings.devModeEnabled;
            try{settings.devModeEnabled=false;if(GameServices.Progress.IsSphereAvailable(SphereIds.Life))throw new Exception("Life must remain locked during normal progression");}
            finally{settings.devModeEnabled=previous;}
            FindAnyObjectByType<SystemView>().OpenPlanet(2);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();int[] selected={SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Life};
            foreach(int id in selected)prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Life cannot be selected in setup");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();game.CheckProceduralBoard(WorldLevels.Get(1,2),selected);game.CheckLifeGameplay();
            Debug.Log("ECOSPHERE_LIFE_GAMEPLAY_PASSED: DEV availability, normal locked, setup, procedural board, weighted matches/breakaways, pink streaks and three bars, biosphere voice, planet stats");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
