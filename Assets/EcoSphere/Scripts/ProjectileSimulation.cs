using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public enum ShotContactKind { None, Wall, Sphere, Bottom }
    public readonly struct ShotBounds
    {
        public readonly float Left,Right,Top,Bottom;
        public ShotBounds(float left,float right,float top,float bottom){Left=left;Right=right;Top=top;Bottom=bottom;}
    }
    public readonly struct ShotContact
    {
        public readonly ShotContactKind Kind;
        public readonly float Distance;
        public readonly Vector2 Normal;
        public readonly int SphereIndex;
        public ShotContact(ShotContactKind kind,float distance,Vector2 normal,int index=-1){Kind=kind;Distance=distance;Normal=normal;SphereIndex=index;}
    }
    // Scene-independent swept collision shared by the live projectile and its aim prediction.
    public static class ProjectileSimulation
    {
        public const int MaximumContacts=12;
        public static ShotContact Sweep(Vector2 origin,Vector2 direction,float maximum,ShotBounds bounds,IReadOnlyList<Vector2> spheres,float radius)
        {
            float distance=maximum;Vector2 normal=Vector2.zero;var kind=ShotContactKind.None;int index=-1;
            void Wall(float candidate,Vector2 candidateNormal,ShotContactKind candidateKind)
            {
                if(candidate<0 || candidate>distance)return;
                distance=candidate;normal=candidateNormal;kind=candidateKind;
            }
            if(direction.x>.00001f)Wall((bounds.Right-origin.x)/direction.x,Vector2.left,ShotContactKind.Wall);
            else if(direction.x<-.00001f)Wall((bounds.Left-origin.x)/direction.x,Vector2.right,ShotContactKind.Wall);
            if(direction.y>.00001f)Wall((bounds.Top-origin.y)/direction.y,Vector2.down,ShotContactKind.Wall);
            else if(direction.y<-.00001f)Wall((bounds.Bottom-origin.y)/direction.y,Vector2.up,ShotContactKind.Bottom);
            if(spheres!=null)for(int i=0;i<spheres.Count;i++)
                if(BoardRules.Sweep(origin,direction,spheres[i],radius,distance,out float hit))
                {distance=hit;index=i;kind=ShotContactKind.Sphere;}
            return new ShotContact(kind,distance,normal,index);
        }
        public static List<Vector2> Trace(Vector2 origin,Vector2 direction,float length,ShotBounds bounds,IReadOnlyList<Vector2> spheres,float radius,List<Vector2> points)
        {
            points.Clear();points.Add(origin);direction.Normalize();if(direction.sqrMagnitude<.5f)return points;
            for(int bounce=0;bounce<MaximumContacts && length>.00001f;bounce++)
            {
                var contact=Sweep(origin,direction,length,bounds,spheres,radius);
                origin+=direction*contact.Distance;points.Add(origin);length-=contact.Distance;
                if(contact.Kind!=ShotContactKind.Wall)break;
                direction=BoardRules.Reflect(direction,contact.Normal).normalized;
            }
            return points;
        }
    }
    // Cache transformed centers until the cluster moves. Membership is refreshed from board data,
    // including same-count edits, without relying on scene objects or mutation notifications.
    public sealed class BoardCollisionSnapshot
    {
        readonly Dictionary<Vector2Int,Vector2> positions=new Dictionary<Vector2Int,Vector2>();
        readonly List<Vector2> centers=new List<Vector2>();
        readonly List<Vector2Int> cells=new List<Vector2Int>();
        Matrix4x4 previousMatrix;
        public IReadOnlyList<Vector2> Centers=>centers;
        public Vector2Int CellAt(int index)=>cells[index];
        public int TransformCount {get;private set;}
        public void Refresh(Dictionary<Vector2Int,int>.KeyCollection occupied,Matrix4x4 localToWorld,Vector2 core)
        {
            Begin(localToWorld,core);foreach(var cell in occupied)Add(cell,localToWorld);
        }
        public void Refresh(IReadOnlyList<Vector2Int> occupied,Matrix4x4 localToWorld,Vector2 core)
        {
            Begin(localToWorld,core);for(int i=0;i<occupied.Count;i++)Add(occupied[i],localToWorld);
        }
        void Begin(Matrix4x4 localToWorld,Vector2 core)
        {
            if(!previousMatrix.Equals(localToWorld)){positions.Clear();previousMatrix=localToWorld;}
            centers.Clear();cells.Clear();centers.Add(core);cells.Add(Vector2Int.zero);
        }
        void Add(Vector2Int cell,Matrix4x4 localToWorld)
        {
            if(!positions.TryGetValue(cell,out var position))
            {position=localToWorld.MultiplyPoint3x4(BoardRules.Position(cell));positions.Add(cell,position);TransformCount++;}
            centers.Add(position);cells.Add(cell);
        }
    }
}
