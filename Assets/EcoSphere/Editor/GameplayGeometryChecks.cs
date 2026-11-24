using System;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class GameplayGeometryChecks
    {
        public static void Validate()
        {
            WorldCatalog.Current.Validate();
            if(GameplayGeometry.CellSpacing<GameplayGeometry.SphereRadius*2 || GameplayGeometry.MaximumSafeRings<GameplayGeometry.DefaultClusterRings)
                throw new Exception("Sphere geometry overlaps neighbors or exceeds initial bounds");
            var board=new BoardRules();var types=new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Air};
            ProceduralCluster.Populate(board,types,GameplayGeometry.DefaultClusterRings,123,.5f);
            // A starting cluster must leave room for a whole projectile, not only fit itself.
            float sideGap=(GameplayGeometry.Right-GameplayGeometry.Left)*.5f-GameplayGeometry.DefaultClusterRings*GameplayGeometry.CellSpacing-GameplayGeometry.SphereRadius;
            if(sideGap<=GameplayGeometry.SphereRadius*2)throw new Exception("Starting cluster blocks the side bank-shot corridors");
            var snapshot=new BoardCollisionSnapshot();snapshot.Refresh(board.Cells.Keys,Matrix4x4.Translate(GameplayGeometry.ClusterCenter),GameplayGeometry.ClusterCenter);
            var bounds=new ShotBounds(GameplayGeometry.Left+GameplayGeometry.SphereRadius,GameplayGeometry.Right-GameplayGeometry.SphereRadius,GameplayGeometry.Top-GameplayGeometry.SphereRadius,GameplayGeometry.Bottom);
            foreach(int side in new[]{-1,1})
            {
                bool found=false;
                for(float angle=5;angle<=75 && !found;angle+=.2f)
                {
                    var direction=new Vector2(side*Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                    Vector2 origin=new Vector2(0,-5.45f)+direction*(.57f*GameplayGeometry.SphereScale);
                    var first=ProjectileSimulation.Sweep(origin,direction,30,bounds,snapshot.Centers,GameplayGeometry.SphereRadius*2);
                    if(first.Kind!=ShotContactKind.Wall || Mathf.Abs(first.Normal.x)<.5f)continue;
                    bool above=false;
                    for(int bounce=0;bounce<4;bounce++)
                    {
                        var hit=ProjectileSimulation.Sweep(origin,direction,30,bounds,snapshot.Centers,GameplayGeometry.SphereRadius*2);
                        origin+=direction*hit.Distance;above|=origin.y>GameplayGeometry.ClusterCenter.y;
                        if(hit.Kind==ShotContactKind.Sphere){found=above;break;}
                        if(hit.Kind!=ShotContactKind.Wall)break;
                        direction=BoardRules.Reflect(direction,hit.Normal).normalized;origin+=direction*.0001f;
                    }
                }
                if(!found)throw new Exception("No bank-shot route around the starting cluster on side "+side);
            }
            if(board.Cells.Count!=GameplayGeometry.CellCount(GameplayGeometry.DefaultClusterRings))throw new Exception("Configured cluster population");
            for(int degrees=0;degrees<360;degrees++)
            {
                var rotation=Quaternion.Euler(0,0,degrees);
                foreach(var cell in board.Cells)
                {
                    Vector2 position=GameplayGeometry.ClusterCenter+(Vector2)(rotation*(Vector3)BoardRules.Position(cell.Key));
                    if(BoardRules.TouchesEdge(position,GameplayGeometry.SphereRadius,GameplayGeometry.Left,GameplayGeometry.Right,GameplayGeometry.Top,GameplayGeometry.Bottom))
                        throw new Exception("Initial cluster crosses a border when rotated");
                }
            }
            foreach(var world in WorldCatalog.Current.predefinedWorlds)foreach(var natural in world.naturalSpheres)
                if(Mathf.Max(Mathf.Abs(natural.cell.x),Mathf.Abs(natural.cell.y),Mathf.Abs(natural.cell.x+natural.cell.y))>world.rules.clusterRings)
                    throw new Exception("Authored natural sphere lies outside its world's cluster");
            StoneTutorialTools.Validate();
            Debug.Log("ECOSPHERE_LARGE_SPHERES_PASSED: shared size/spacing, both side bank-shot routes, full 360-degree border clearance, natural placements and guided Stone support");
        }
    }
}
