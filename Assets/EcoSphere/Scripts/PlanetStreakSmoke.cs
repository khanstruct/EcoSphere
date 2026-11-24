using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal IEnumerator CheckPlanetStreaks()
        {
            enabled=false;gameplayCanvas.enabled=false;
            foreach(var model in visuals.Values)Destroy(model);visuals.Clear();board.Cells.Clear();yield return null;
            var target=new RenderTexture(800,1200,24);target.Create();view.targetTexture=target;
            view.transform.position=new Vector3(0,2.5f,-25);view.orthographicSize=10;view.aspect=2f/3;
            Texture2D Capture(string name)
            {
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(800,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,1200),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/streak-"+name+".png"),image.EncodeToPNG());return image;
            }
            var released=new List<GameObject>();var starts=new List<Vector3>();var cells=new HashSet<Vector2Int>();
            for(int i=0;i<5;i++)
            {
                var cell=new Vector2Int(i-2,-2);cells.Add(cell);board.Cells.Add(cell,i%2==0?SphereIds.Water:SphereIds.Fire);
                var model=MakeSphere("Released flight "+i,board.Cells[cell],cluster,BoardRules.Position(cell));visuals.Add(cell,model);released.Add(model);starts.Add(model.transform.position);
            }
            int before=score;RemoveCells(cells,50,true);
            if(activeStreaks!=5 || score!=before+250)throw new Exception("Breakaways need one full streak each with unchanged scoring");
            yield return new WaitForSeconds(.1f);
            for(int i=0;i<released.Count;i++)if(released[i].transform.position.y>=starts[i].y)throw new Exception("Released sphere did not initially fall");
            Destroy(Capture("fall"));yield return new WaitForSeconds(.3f);Destroy(Capture("capture"));
            bool impactSeen=false;float deadline=Time.time+1;
            while(activeStreaks>0 && Time.time<deadline)
            {
                var flash=planet.Find("Planet streak impact");
                if(!impactSeen && flash)
                {
                    impactSeen=true;var lit=Capture("impact");var renderer=flash.GetComponent<Renderer>();renderer.enabled=false;
                    var plain=Capture("impact-no-flash");renderer.enabled=true;
                    var a=lit.GetPixels32();var b=plain.GetPixels32();int brighter=0;
                    for(int i=0;i<a.Length;i++)if(a[i].r+a[i].g+a[i].b>b[i].r+b[i].g+b[i].b+30)brighter++;
                    Destroy(lit);Destroy(plain);
                    if(brighter<150)throw new Exception("Impact flash must be clearly visible at game scale: "+brighter);
                    Debug.Log("ECOSPHERE_PLANET_IMPACT_VISIBLE_PASSED brightenedPixels="+brighter);
                }
                yield return null;
            }
            if(activeStreaks!=0 || !impactSeen)throw new Exception("Streaks did not finish with impact flashes");
            // A fan has several cells at the latest depth: exactly one must become the match streak.
            var origin=new Vector2Int(-2,-3);cells=new HashSet<Vector2Int>{origin,origin+Vector2Int.right,origin+new Vector2Int(2,0),origin+new Vector2Int(1,1)};
            var delays=MatchWave.Delays(cells,origin);var last=MatchWave.LastCell(delays);
            Vector3 expected=cluster.TransformPoint(BoardRules.Position(last));
            foreach(var cell in cells){board.Cells.Add(cell,SphereIds.Life);visuals.Add(cell,MakeSphere("Matched test",SphereIds.Life,cluster,BoardRules.Position(cell)));}
            before=score;RemoveCells(cells,12,matchOrigin:origin);
            float started=Time.time;
            while(activeStreaks==0 && Time.time-started<.49f)yield return null;
            var head=transform.Find("Matched energy streak");
            if(activeStreaks!=1 || !head || Vector3.Distance(head.position,expected)>.1f || score!=before+48)throw new Exception("Match representative must leave exactly once from a latest-clearing sphere");
            while(Time.time-started<.48f)yield return null;
            if(activeStreaks!=1 || transform.GetComponentsInChildren<TrailRenderer>().Length!=1)throw new Exception("Match emitted more than one streak");
            while(activeStreaks>0)yield return null;
            yield return new WaitForSeconds(.16f);
            if(planet.Find("Planet streak impact"))throw new Exception("Impact flash did not clear quickly");
            view.targetTexture=null;target.Release();Destroy(target);
            Debug.Log("ECOSPHERE_PLANET_STREAK_GAMEPLAY_PASSED: one flight per breakaway, initial fall/curved capture, surface flashes, single latest-cell match streak with tied branches, unchanged scores, clean effect completion");
        }
    }
    public sealed class PlanetStreakSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-planetStreakSmoke"))new GameObject("Planet streak checks").AddComponent<PlanetStreakSmoke>();}
        IEnumerator Start()
        {
            float previous=Time.captureDeltaTime;Time.captureDeltaTime=1f/120;
            try{yield return null;yield return null;yield return FindAnyObjectByType<EcoSpherePrototype>().CheckPlanetStreaks();}
            finally{Time.captureDeltaTime=previous;}
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
