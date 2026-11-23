using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CheckAirGameplay()
        {
            var cells=new List<Vector2Int>();foreach(var cell in board.Cells)if(cell.Value==SphereIds.Air)cells.Add(cell.Key);
            if(cells.Count!=15)throw new Exception("Air missing from selected procedural board");
            RemoveCells(new HashSet<Vector2Int>{cells[0]},12);
            var visual=visuals[cells[1]];RemoveCells(new HashSet<Vector2Int>{cells[1]},50,true);UpdateVisualizer();
            int index=displayedElements.IndexOf(ElementId.Air);var sphere=Definition(SphereIds.Air);
            if(totals[(int)ElementId.Air]!=46.5 || index<0 || energyBars.Count!=4 || energyBars[index].color!=sphere.color || resourceLabels[index].text!="Air" || visual.GetComponent<TrailRenderer>().startColor!=sphere.color || LevelAudio.MatchVoice(SphereIds.Air,30)!=sphere.resourceVoice)
                throw new Exception("Air weighted yield, single bar, namesake color, streak or audio incorrect");
        }
    }
    public sealed class AirSphereSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-airSphereSmoke")>=0)new GameObject("Air checks").AddComponent<AirSphereSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            if(!GameServices.Progress.IsSphereAvailable(SphereIds.Air))throw new Exception("Air unavailable in DEV collection");
            FindAnyObjectByType<SystemView>().OpenPlanet(3);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();foreach(int id in new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Air})prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Air not selectable in setup");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            FindAnyObjectByType<EcoSpherePrototype>().CheckAirGameplay();
            Debug.Log("ECOSPHERE_AIR_GAMEPLAY_PASSED: DEV collection, setup, generated board, weighted yields, one namesake-colored bar, streak and voice");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
