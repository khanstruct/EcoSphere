using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public static class MatchWave
    {
        public const float StepDuration=.035f, MaximumSpread=.22f, DissolveDuration=.20f;
        public static Vector2Int LastCell(IReadOnlyDictionary<Vector2Int,float> delays)
        {
            Vector2Int last=default;float latest=-1;
            foreach(var pair in delays)
                if(pair.Value>latest || (pair.Value==latest && (pair.Key.x>last.x || (pair.Key.x==last.x && pair.Key.y>last.y))))
                {last=pair.Key;latest=pair.Value;}
            return last;
        }
        // Graph distance follows connected branches, rather than jumping across gaps in the cluster.
        public static Dictionary<Vector2Int,float> Delays(HashSet<Vector2Int> cells,Vector2Int origin)
        {
            var depths=new Dictionary<Vector2Int,int>(cells.Count);
            var result=new Dictionary<Vector2Int,float>(cells.Count);
            if(!cells.Contains(origin))return result;
            var queue=new Queue<Vector2Int>();queue.Enqueue(origin);depths.Add(origin,0);int maximum=0;
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();int nextDepth=depths[cell]+1;
                foreach(var direction in BoardRules.Directions)
                {
                    var next=cell+direction;
                    if(cells.Contains(next) && !depths.ContainsKey(next))
                    {depths.Add(next,nextDepth);maximum=Mathf.Max(maximum,nextDepth);queue.Enqueue(next);}
                }
            }
            float step=maximum>0 ? Mathf.Min(StepDuration,MaximumSpread/maximum) : 0;
            foreach(var pair in depths)result.Add(pair.Key,pair.Value*step);
            return result;
        }
    }
}
