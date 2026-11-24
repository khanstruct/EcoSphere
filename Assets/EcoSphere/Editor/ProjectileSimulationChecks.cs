using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class ProjectileSimulationChecks
    {
        static void Expect(bool value,string message){if(!value)throw new Exception("Projectile simulation: "+message);}
        public static void Validate()
        {
            var bounds=new ShotBounds(-4,4,7,-7);var empty=new List<Vector2>();
            var exit=ProjectileSimulation.Sweep(Vector2.zero,Vector2.down,10,bounds,empty,.5f);
            Expect(exit.Kind==ShotContactKind.Bottom && exit.Distance==7,"bottom exit");
            var wall=ProjectileSimulation.Sweep(Vector2.zero,Vector2.right,10,bounds,empty,.5f);
            Expect(wall.Kind==ShotContactKind.Wall && wall.Distance==4 && wall.Normal==Vector2.left,"wall contact");
            var cells=new List<Vector2Int>{new Vector2Int(1,0),new Vector2Int(0,2)};
            var snapshot=new BoardCollisionSnapshot();
            var matrix=Matrix4x4.TRS(new Vector3(1,2,0),Quaternion.Euler(0,0,37),Vector3.one);
            snapshot.Refresh(cells,matrix,new Vector2(1,2));int transformed=snapshot.TransformCount;
            snapshot.Refresh(cells,matrix,new Vector2(1,2));Expect(snapshot.TransformCount==transformed,"stationary board transformed twice");
            cells[0]=new Vector2Int(-1,0);snapshot.Refresh(cells,matrix,new Vector2(1,2));
            Expect(snapshot.CellAt(1)==cells[0] && Vector2.Distance(snapshot.Centers[1],matrix.MultiplyPoint3x4(BoardRules.Position(cells[0])))<.0001f,"same-count membership change");
            var random=new System.Random(829);
            for(int i=0;i<500;i++)
            {
                Vector2 origin=new Vector2((float)random.NextDouble()*6-3,-6);
                Vector2 direction=new Vector2((float)random.NextDouble()*2-1,1).normalized;
                var hit=ProjectileSimulation.Sweep(origin,direction,15,bounds,snapshot.Centers,.5f);
                // Independent analytic nearest-circle oracle checks ordering against the shared wall result.
                var expected=ProjectileSimulation.Sweep(origin,direction,15,bounds,empty,.5f);float distance=expected.Distance;int sphere=-1;
                for(int j=0;j<snapshot.Centers.Count;j++)
                    if(BoardRules.Sweep(origin,direction,snapshot.Centers[j],.5f,distance,out float d)){distance=d;sphere=j;}
                Expect(Mathf.Abs(hit.Distance-distance)<.0001f && hit.Kind==(sphere>=0 ? ShotContactKind.Sphere : expected.Kind) && hit.SphereIndex==sphere,"nearest contact at rotated cluster");
                var points=ProjectileSimulation.Trace(origin,direction,15,bounds,snapshot.Centers,.5f,new List<Vector2>());
                Expect(Vector2.Distance(points[1],origin+direction*hit.Distance)<.0001f,"aim and projectile disagree on first contact");
            }
            var rotated=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,90),Vector3.one);
            snapshot.Refresh(cells,rotated,Vector2.zero);Expect(snapshot.TransformCount>transformed,"rotation did not invalidate positions");
            Debug.Log("ECOSPHERE_PROJECTILE_SIMULATION_PASSED: bottom/walls, cached centers, same-count board edits, rotated nearest contacts and 500 aim/live comparisons");
        }
        public static void CheckAll(){Validate();RefactorChecks.Validate();}
    }
}
