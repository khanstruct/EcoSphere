using System;
using UnityEditor;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class StoneTutorialTools
    {
        [MenuItem("EcoSphere/Validate Stone Tutorial Layout")]
        public static void Validate()
        {
            var state=UnityEngine.Random.state;
            int layouts=0;
            try
            {
                foreach(var types in new[] {
                    new[] { SphereIds.Fire,SphereIds.Void,SphereIds.Sunlight },
                    new[] { SphereIds.Sunlight,SphereIds.Fire,SphereIds.Void },
                    new[] { SphereIds.Void,SphereIds.Ice,SphereIds.Sunlight } })
                for(int seed=0;seed<64;seed++)
                {
                    UnityEngine.Random.InitState(seed);
                    var board=new BoardRules(); StoneTutorialLayout.Populate(board,types,4);
                    int holder=StoneTutorialLayout.HolderType(types);
                    var stone=StoneTutorialLayout.StoneCell(4);
                    if(board.Cells.Count!=60 || board.Cells[stone]!=SphereIds.Stone || StoneTutorialLayout.Ring(stone)!=4)
                        throw new Exception("Stone must occupy the outer ring of the authored cluster");
                    foreach(int type in types)if(!board.Cells.ContainsValue(type))throw new Exception("Selected sphere missing from mixed interior");
                    foreach(var direction in BoardRules.Directions)
                        if(board.Cells.TryGetValue(stone+direction,out int neighbor) && neighbor!=holder)
                            throw new Exception("Stone has an uncontrolled connection to the cluster");
                    var matched=board.MatchFrom(new Vector2Int(4,0));
                    if(matched.Count<24 || !matched.Contains(StoneTutorialLayout.SupportCell(4)))
                        throw new Exception("Outer match omitted the Stone support");
                    foreach(var cell in matched)board.Cells.Remove(cell);
                    var loose=board.Disconnected();
                    if(!loose.Contains(stone))throw new Exception("Clearing the ring failed to release Stone");
                    foreach(var cell in loose)board.Cells.Remove(cell);
                    if(board.Cells.Count==0)throw new Exception("Mixed core-attached interior must survive the guided shot");
                    layouts++;
                }
            }
            finally { UnityEngine.Random.state=state; }
            Debug.Log("ECOSPHERE_STONE_TOPOLOGY_PASSED: "+layouts+" layouts, selected sphere identities, guaranteed single-match release and surviving interior");
        }
    }
}
