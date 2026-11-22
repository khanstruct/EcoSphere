using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public static class InfestationRules
    {
        public static void Populate(BoardRules board,InfestationModifierDefinition modifier,int seed)
        {
            board.Infested.Clear();var candidates=new List<Vector2Int>();
            foreach(var cell in board.Cells)
                if(cell.Key!=Vector2Int.zero && SphereCatalog.Current.Get(cell.Value).role!=SphereRole.Core)candidates.Add(cell.Key);
            candidates.Sort((a,b)=>a.x!=b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var random=new System.Random(seed^0x41a17);
            int count=Mathf.Min(candidates.Count,random.Next(modifier.minimumInfested,modifier.maximumInfested+1));
            for(int i=0;i<count;i++)
            {
                int index=random.Next(i,candidates.Count);var cell=candidates[index];candidates[index]=candidates[i];candidates[i]=cell;
                board.Infested.Add(cell);
            }
        }
    }
}
