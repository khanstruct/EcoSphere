using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal IEnumerator CheckMatchWave()
        {
            enabled=false;gameplayCanvas.enabled=false;
            foreach(var visual in visuals.Values)Destroy(visual);visuals.Clear();board.Cells.Clear();
            yield return null;
            // A narrow stem reaches a fan: the wave must animate the fan's siblings together.
            var origin=new Vector2Int(0,-4);var stem=origin+new Vector2Int(1,0);
            var junction=origin+new Vector2Int(2,0);
            var upper=junction+new Vector2Int(0,1);var lower=junction+new Vector2Int(1,-1);
            var end=junction+new Vector2Int(2,0);
            var cells=new HashSet<Vector2Int>{origin,stem,junction,upper,lower,junction+new Vector2Int(1,0),end};
            var models=new Dictionary<Vector2Int,GameObject>();
            foreach(var cell in cells){board.Cells.Add(cell,SphereIds.Void);var model=MakeSphere("Wave test "+cell,SphereIds.Void,cluster,BoardRules.Position(cell));visuals.Add(cell,model);models.Add(cell,model);}
            var target=new RenderTexture(900,900,24);target.Create();view.targetTexture=target;
            view.transform.position=new Vector3(.7f,-1.8f,-25);view.orthographicSize=2;view.aspect=1;
            void Capture(string name)
            {
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/match-wave-"+name+".png"),image.EncodeToPNG());Destroy(image);
            }
            int before=score;float started=Time.time;
            RemoveCells(cells,12,matchOrigin:origin);
            if(board.Cells.Count!=0 || score!=before+cells.Count*12)throw new Exception("Visual wave delayed board/scoring resolution");
            if(models[origin].transform.parent!=transform || models[stem].transform.parent!=cluster)throw new Exception("Wave did not start at impact");
            var flash=models[origin].transform.Find("Rotating surface/Match flash");
            if(!flash)throw new Exception("Void flare missing");
            var properties=new MaterialPropertyBlock();flash.GetComponent<Renderer>().GetPropertyBlock(properties);
            if(properties.GetFloat("_Strength")<=0)throw new Exception("Void flare is not bright");
            Capture("impact");
            while(models[upper] && models[upper].transform.parent==cluster)yield return null;
            yield return null; // Allow all sibling coroutines scheduled for this frame to advance.
            if(!models[lower] || models[lower].transform.parent!=transform || !models[end] || models[end].transform.parent!=cluster)
                throw new Exception("Wave failed to expand through fan siblings before the distant end");
            Capture("fan");
            while(Time.time-started<.49f)yield return null;
            foreach(var model in models.Values)if(model)throw new Exception("Match wave exceeded half a second");
            view.targetTexture=null;target.Release();Destroy(target);
            Debug.Log("ECOSPHERE_MATCH_WAVE_GAMEPLAY_PASSED: impact-first Void flare, stem-to-fan simultaneous branching, unchanged immediate scoring/topology, all visuals gone under .5s");
        }
    }
    public sealed class MatchWaveSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-matchWaveSmoke"))new GameObject("Match wave checks").AddComponent<MatchWaveSmoke>();}
        IEnumerator Start()
        {
            // Synchronous screenshot readbacks must not turn the next simulated frame into a giant time step.
            float previous=Time.captureDeltaTime;Time.captureDeltaTime=1f/120;
            try{yield return null;yield return null;yield return FindAnyObjectByType<EcoSpherePrototype>().CheckMatchWave();}
            finally{Time.captureDeltaTime=previous;}
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
