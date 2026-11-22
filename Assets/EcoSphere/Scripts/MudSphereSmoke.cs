using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CheckMudGameplay()
        {
            var cells=new List<Vector2Int>();foreach(var cell in board.Cells)if(cell.Value==SphereIds.Mud)cells.Add(cell.Key);
            if(cells.Count!=15 || !displayedElements.Contains(ElementId.Mud) || !displayedElements.Contains(ElementId.Water) || !displayedElements.Contains(ElementId.Stone))throw new Exception("Mud missing from board/composition");
            cells.RemoveAll(cell=>board.Infested.Contains(cell));
            int before=score;RemoveCells(new HashSet<Vector2Int>{cells[0]},12);
            var visual=visuals[cells[1]];RemoveCells(new HashSet<Vector2Int>{cells[1]},50,true);UpdateVisualizer();
            if(totals[(int)ElementId.Mud]!=31 || totals[(int)ElementId.Water]!=15.5 || totals[(int)ElementId.Stone]!=15.5 || score!=before+62)throw new Exception("Mud weighted scoring incorrect");
            var mud=Definition(SphereIds.Mud);
            if(visual.GetComponent<TrailRenderer>().startColor!=mud.color || LevelAudio.MatchVoice(SphereIds.Mud,30)!=mud.resourceVoice)throw new Exception("Mud streak/voice incorrect");
            int units=0;for(int i=0;i<channelSphereTypes.Count;i++)if(channelSphereTypes[i]==SphereIds.Mud){units++;if(energyBars[i].color!=mud.color)throw new Exception("Mud bar color incorrect");}
            if(units!=4 || planet.GetComponent<PlanetSurface>().Stats[PlanetStat.Soil]<=0)throw new Exception("Mud four-unit bars or soil stats missing");
        }
        internal IEnumerator CheckMudTint()
        {
            enabled=false;gameplayCanvas.enabled=false;
            var surface=planet.GetComponent<PlanetSurface>();surface.rotationSpeed=0;
            var input=new double[ElementComposition.Capacity];input[(int)ElementId.Mud]=500;input[(int)ElementId.Void]=500;
            surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
            var renderer=planet.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            var target=new RenderTexture(512,512,24);target.Create();view.targetTexture=target;
            view.transform.position=new Vector3(planet.position.x,planet.position.y,-25);view.orthographicSize=5.6f;view.aspect=1;
            Texture2D Capture(string name)
            {
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(512,512,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/mud-"+name+".png"),image.EncodeToPNG());return image;
            }
            var tinted=Capture("soil");float soil=block.GetFloat("_Soil");block.SetFloat("_Soil",0);renderer.SetPropertyBlock(block);var gray=Capture("no-soil");
            double warmth=0;int count=0;
            for(int y=70;y<442;y++)for(int x=70;x<442;x++)if((x-256)*(x-256)+(y-256)*(y-256)<180*180)
            {var a=tinted.GetPixel(x,y);var b=gray.GetPixel(x,y);warmth+=(a.r-a.b)-(b.r-b.b);count++;}
            block.SetFloat("_Soil",soil);renderer.SetPropertyBlock(block);
            Destroy(tinted);Destroy(gray);view.targetTexture=null;target.Release();Destroy(target);
            if(warmth/count<.03)throw new Exception("Mud tint must visibly warm the rocky ground: "+warmth/count);
            Debug.Log("ECOSPHERE_MUD_TINT_PASSED warmth delta="+warmth/count);
        }
    }
    public sealed class MudSphereSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mudSphereSmoke")>=0)new GameObject("Mud sphere checks").AddComponent<MudSphereSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            if(!DevelopmentMode.Enabled || !WorldProgress.IsSphereAvailable(SphereIds.Mud))throw new Exception("Mud missing from DEV collection");
            var settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previous=settings.devModeEnabled;
            try{settings.devModeEnabled=false;if(WorldProgress.IsSphereAvailable(SphereIds.Mud))throw new Exception("Mud must start locked normally");}
            finally{settings.devModeEnabled=previous;}
            FindAnyObjectByType<SystemView>().OpenPlanet(2);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();int[] selected={SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Mud};
            foreach(int id in selected)prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Mud missing from setup");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();game.CheckProceduralBoard(WorldLevels.Get(1,2),selected);game.CheckMudGameplay();
            var totals=new double[ElementComposition.Capacity];totals[(int)ElementId.Stone]=500;totals[(int)ElementId.Water]=500;
            var mud=WorldProgress.Complete(7,0,-.02f,0,false,totals,300);
            if(!mud.discoveries.Contains(SphereIds.Mud))throw new Exception("Stone + Water failed to award Mud");
            Array.Clear(totals,0,totals.Length);totals[(int)ElementId.Water]=500;totals[(int)ElementId.Mud]=500;
            var life=WorldProgress.Complete(8,0,-.02f,0,false,totals,300);
            if(!life.discoveries.Contains(SphereIds.Life) || life.world.discoveryDetails[0].kind!=DiscoveryKind.Recipe)throw new Exception("Water + Mud failed to award Life");
            if(!WorldProgress.TryOpenCompletedResults(8,0) || WorldProgress.Pending.discoveries.Count!=0 || !WorldProgress.Pending.world.discoveries.Contains(SphereIds.Life))throw new Exception("Historical Life discovery failed");
            yield return game.CheckMudTint();
            Debug.Log("ECOSPHERE_MUD_GAMEPLAY_PASSED: DEV/setup, mixed scoring, four bars, light brown streaks, voice, both recipe awards and historical discovery, rendered soil tint");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
