using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public static class ProceduralCluster
    {
        public static void Populate(BoardRules board,IReadOnlyList<int> types,int rings,int seed,float clustering)
        {
            if(types==null || types.Count<2 || new HashSet<int>(types).Count!=types.Count || rings<2)
                throw new ArgumentException("A procedural cluster needs distinct sphere types and at least two rings");
            var random=new System.Random(seed);
            var cells=new HashSet<Vector2Int>();
            for(int q=-rings;q<=rings;q++)for(int r=-rings;r<=rings;r++)
                if(Mathf.Max(Mathf.Abs(q),Mathf.Abs(r),Mathf.Abs(q+r))<=rings && (q!=0 || r!=0))cells.Add(new Vector2Int(q,r));
            if(types.Count>cells.Count)throw new ArgumentException("Too many types for this cluster");
            int total=cells.Count;
            var remaining=new int[types.Count];
            for(int i=0;i<types.Count;i++)remaining[i]=total/types.Count+(i<total%types.Count ? 1 : 0);
            var frontier=new List<Vector2Int> {BoardRules.Directions[random.Next(BoardRules.Directions.Length)]};
            var queued=new HashSet<Vector2Int>(frontier);
            board.Cells.Clear();board.Infested.Clear(); clustering=Mathf.Clamp01(clustering);
            while(frontier.Count>0)
            {
                int index=random.Next(frontier.Count); var cell=frontier[index]; frontier.RemoveAt(index);
                var weights=new double[types.Count]; double sum=0;
                for(int i=0;i<types.Count;i++)
                {
                    int adjacent=0;
                    foreach(var direction in BoardRules.Directions)
                        if(board.Cells.TryGetValue(cell+direction,out int neighbor) && neighbor==types[i])adjacent++;
                    // Quotas preserve the same mix at every difficulty; only spatial grouping changes.
                    weights[i]=remaining[i]*Math.Exp(clustering*5*adjacent); sum+=weights[i];
                }
                double choice=random.NextDouble()*sum; int chosen=types.Count-1;
                for(int i=0;i<types.Count;i++)if((choice-=weights[i])<0) {chosen=i;break;}
                board.Cells.Add(cell,types[chosen]); remaining[chosen]--;
                foreach(var direction in BoardRules.Directions)
                {
                    var next=cell+direction;
                    if(cells.Contains(next) && queued.Add(next))frontier.Add(next);
                }
            }
            if(board.Cells.Count!=total)throw new InvalidOperationException("Procedural fill did not reach the entire cluster");
        }
        public static double NeighborSimilarity(BoardRules board)
        {
            int same=0,total=0;
            foreach(var cell in board.Cells)foreach(var direction in BoardRules.Directions)
                if(board.Cells.TryGetValue(cell.Key+direction,out int other)) {total++;if(other==cell.Value)same++;}
            return total==0 ? 0 : (double)same/total;
        }
    }
}
