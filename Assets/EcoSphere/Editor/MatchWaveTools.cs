using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class MatchWaveTools
    {
        public static void Configure()
        {
            var shader=Shader.Find("EcoSphere/Match Flare");
            if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Match flare shader failed");
            const string path="Assets/EcoSphere/Resources/MatchFlare.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            AssetDatabase.SaveAssets();
            var cells=new HashSet<Vector2Int>{new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0),new Vector2Int(3,0),new Vector2Int(2,1),new Vector2Int(1,1)};
            var delays=MatchWave.Delays(cells,Vector2Int.zero);
            if(delays[Vector2Int.zero]!=0 || delays[new Vector2Int(1,0)]<=0 || delays[new Vector2Int(2,0)]!=delays[new Vector2Int(1,1)] || delays[new Vector2Int(3,0)]<=delays[new Vector2Int(2,0)])
                throw new Exception("Match wave must follow adjacency and spread across equal-depth branches");
            cells.Clear();for(int i=0;i<200;i++)cells.Add(new Vector2Int(i,0));
            delays=MatchWave.Delays(cells,Vector2Int.zero);
            if(delays.Count!=200 || delays[new Vector2Int(199,0)]+MatchWave.DissolveDuration>.421f)throw new Exception("Long chains exceed wave time budget");
            float last=-1;for(int i=0;i<200;i++){if(delays[new Vector2Int(i,0)]<=last)throw new Exception("Wave skipped chain order");last=delays[new Vector2Int(i,0)];}
            Debug.Log("ECOSPHERE_MATCH_WAVE_DATA_PASSED: adjacency branches, ordered 200-sphere chain, .22s maximum spread + .20s dissolve, shared flare shader");
        }
        public static void Smoke(){EditorSceneManager.OpenScene(TestLevelTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
