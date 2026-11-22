using System;
using UnityEngine;
namespace EcoSphere
{
    // Visual-only randomness is independent of the board and ammo generators.
    public readonly struct PlanetStreakPath
    {
        public readonly Vector3 start,firstControl,secondControl,destination,normal;
        public readonly float duration;
        public PlanetStreakPath(Vector3 start,Vector3 center,float radius,System.Random random)
        {
            float Between(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            this.start=start;
            float y=Between(-.98f,-.68f),yaw=Between(-1.1f,1.1f),horizontal=Mathf.Sqrt(1-y*y);
            normal=new Vector3(horizontal*Mathf.Sin(yaw),y,-horizontal*Mathf.Cos(yaw));
            destination=center+normal*radius;
            float side=Between(-.8f,.8f),drop=Between(.85f,1.35f);
            firstControl=start+new Vector3(side,-drop,0);
            secondControl=start+new Vector3(side*1.8f,-drop*1.4f,0);
            duration=Between(.68f,.8f);
        }
        public Vector3 Position(float progress)
        {
            float t=Mathf.Clamp01(progress),s=1-t;
            return s*s*s*start+3*s*s*t*firstControl+3*s*t*t*secondControl+t*t*t*destination;
        }
    }
}
