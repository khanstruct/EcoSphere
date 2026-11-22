using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class PlanetStreakTools
    {
        public static void Configure()
        {
            MatchWaveTools.Configure();
            var shader=Shader.Find("EcoSphere/Energy Flash");
            if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Energy flash shader failed");
            const string path="Assets/EcoSphere/Resources/EnergyFlash.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(path))AssetDatabase.CreateAsset(new Material(shader),path);
            AssetDatabase.SaveAssets();
            var random=new System.Random(3187);Vector3 center=new Vector3(0,11.5f,3);float minX=10,maxX=-10;
            for(int i=0;i<500;i++)
            {
                var start=new Vector3((float)random.NextDouble()*8-4,(float)random.NextDouble()*10-5,0);
                var flight=new PlanetStreakPath(start,center,5,random);
                if((flight.Position(0)-start).sqrMagnitude>.000001f || (flight.Position(1)-flight.destination).sqrMagnitude>.000001f || Mathf.Abs((flight.destination-center).magnitude-5)>.0001f || flight.normal.y>=0 || flight.normal.z>=0)
                    throw new Exception("Streak endpoint must be on the visible lower hemisphere");
                if(flight.Position(.08f).y>=start.y || flight.duration>.81f)throw new Exception("Streak must drop before a quick capture");
                float early=(flight.Position(.55f)-flight.Position(.5f)).magnitude;
                float late=(flight.Position(.95f)-flight.Position(.9f)).magnitude;
                if(late<=early*1.5f)throw new Exception("Streak must accelerate toward impact");
                minX=Mathf.Min(minX,flight.destination.x);maxX=Mathf.Max(maxX,flight.destination.x);
            }
            if(maxX-minX<4)throw new Exception("Impacts insufficiently varied");
            Debug.Log("ECOSPHERE_PLANET_STREAK_DATA_PASSED: 500 falling/curving/accelerating paths; actual lower-hemisphere surface endpoints; independent randomness and varied impacts");
        }
        public static void Smoke(){EditorSceneManager.OpenScene(TestLevelTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
