using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    // Authored topology; all ordinary sphere identities still come from the chosen loadout.
    public static class StoneTutorialLayout
    {
        public static Vector2Int StoneCell(int rings) => new Vector2Int(0,-rings);
        public static Vector2Int SupportCell(int rings) => StoneCell(rings)+Vector2Int.up;
        public static int HolderType(IReadOnlyList<int> types)
        {
            foreach(int type in types) if(type!=SphereIds.Stone)return type;
            throw new InvalidOperationException("Stone tutorial needs an ordinary supporting sphere");
        }
        public static int Ring(Vector2Int cell) => Mathf.Max(Mathf.Abs(cell.x),Mathf.Abs(cell.y),Mathf.Abs(cell.x+cell.y));
        public static void Populate(BoardRules board,IReadOnlyList<int> types,int rings)
        {
            if(rings<3 || types.Count<2) throw new InvalidOperationException("Stone tutorial needs an outer ring and a mixed interior");
            board.Cells.Clear();
            int holder=HolderType(types);
            var interior=new List<Vector2Int>();
            var coreNeighbors=new List<Vector2Int>(BoardRules.Directions);
            Shuffle(coreNeighbors);
            for(int q=-rings;q<=rings;q++) for(int r=-rings;r<=rings;r++)
            {
                var cell=new Vector2Int(q,r); int ring=Ring(cell);
                if(ring==0 || ring>rings)continue;
                if(cell==StoneCell(rings)) board.Cells.Add(cell,SphereIds.Stone);
                else if(ring==rings || cell==SupportCell(rings))board.Cells.Add(cell,holder);
                else if(ring==1)board.Cells.Add(cell,types[coreNeighbors.IndexOf(cell)%types.Count]);
                else interior.Add(cell);
            }
            Shuffle(interior);
            for(int i=0;i<interior.Count;i++)board.Cells.Add(interior[i],types[i%types.Count]);
            // Stone's three occupied neighbors all belong to the connected outer match.
            // Clearing it cuts every route from Stone to the core; mixed core neighbors survive.
        }
        static void Shuffle(List<Vector2Int> cells)
        {
            for(int i=cells.Count-1;i>0;i--)
            {
                int j=UnityEngine.Random.Range(0,i+1);
                var swap=cells[i]; cells[i]=cells[j]; cells[j]=swap;
            }
        }
    }
}
