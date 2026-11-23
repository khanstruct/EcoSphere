using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CheckPlantGameplay()
        {
            var cells=new List<Vector2Int>();foreach(var cell in board.Cells)if(cell.Value==SphereIds.Plant)cells.Add(cell.Key);
            if(cells.Count!=15 || !displayedElements.Contains(ElementId.Life) || !displayedElements.Contains(ElementId.Plant))throw new Exception("Plant sphere did not enter generated board/composition");
            cells.RemoveAll(cell=>board.Infested.Contains(cell));
            int scoreBefore=score;
            RemoveCells(new HashSet<Vector2Int>{cells[0]},12);
            var visual=visuals[cells[1]];
            RemoveCells(new HashSet<Vector2Int>{cells[1]},50,true);UpdateVisualizer();
            if(totals[(int)ElementId.Life]!=15.5 || totals[(int)ElementId.Plant]!=31 || score!=scoreBefore+62)
                throw new Exception("Plant gameplay failed weighted match/breakaway yields");
            var plant=Definition(SphereIds.Plant);
            if(visual.GetComponent<TrailRenderer>().startColor!=plant.color || LevelAudio.MatchVoice(SphereIds.Plant,30)!=plant.resourceVoice)
                throw new Exception("Plant streak/voice ignored its definition");
            int units=0;
            int lifeBar=displayedElements.IndexOf(ElementId.Life);
            if(lifeBar<0 || energyBars[lifeBar].color!=Definition(SphereIds.Life).color)throw new Exception("Life supplied by Plant must use the Life sphere color");
            for(int i=0;i<displayedElements.Count;i++)if(displayedElements[i]==ElementId.Plant){units++;if(energyBars[i].color!=plant.color)throw new Exception("Plant visualizer color mismatch");}
            if(units!=1 || planet.GetComponent<PlanetSurface>().Stats[PlanetStat.BiologicalCoverage]<=0)throw new Exception("Plant element bars or existing base-stat wiring missing");
        }
    }
    public sealed class PlantSphereSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-plantSphereSmoke")>=0)new GameObject("Plant sphere checks").AddComponent<PlantSphereSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            if(!DevelopmentMode.Enabled || !GameServices.Progress.IsSphereAvailable(SphereIds.Plant))throw new Exception("Plant missing from DEV collection");
            var configuration=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previous=configuration.devModeEnabled;
            try{configuration.devModeEnabled=false;if(GameServices.Progress.IsSphereAvailable(SphereIds.Plant))throw new Exception("Plant was accidentally added to normal starting collection");}
            finally{configuration.devModeEnabled=previous;}
            FindAnyObjectByType<SystemView>().OpenPlanet(2);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            int[] selected={SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Plant};
            foreach(int id in selected)prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Plant cannot be selected in setup");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();game.CheckProceduralBoard(WorldLevels.Get(1,2),selected);game.CheckPlantGameplay();
            yield return new WaitForSeconds(.9f);
            foreach(var size in new[]{new Vector2Int(900,1600),new Vector2Int(1600,900)})
            {
                var target=new RenderTexture(size.x,size.y,24);target.Create();game.SetPreviewTarget(target);game.RenderPresentation();
                var old=RenderTexture.active;RenderTexture.active=target;
                var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Plant-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                RenderTexture.active=old;game.SetPreviewTarget(null);target.Release();Destroy(target);Destroy(pixels);
            }
            Debug.Log("ECOSPHERE_PLANT_GAMEPLAY_PASSED: DEV versus normal collection, setup, generated board, weighted matches/breakaways, unique element bars with namesake colors, green streaks, florasphere voice, base stats, portrait/wide previews");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
