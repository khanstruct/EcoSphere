using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Real pointer shots on small test boards (rotation held at 0): erosion on a miss, the farthest eligible sand,
        // infested sand exempt, a normal breakaway of what it held, ammo replaced after the erosion, and no erosion on a match.
        internal IEnumerator CheckErosion()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Erosion: "+message);}
            var sand=Definition(SphereIds.Sand);
            var mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var played=new List<AudioClip>();levelAudio.EffectPlayed+=played.Add;
            void Board(params (int x,int y,int type,bool infested)[] cells)
            {
                foreach(var model in visuals.Values)Destroy(model);visuals.Clear();board.Cells.Clear();
                foreach(var alien in aliens.Values)Destroy(alien);aliens.Clear();board.Infested.Clear();
                foreach(var c in cells)
                {
                    var cell=new Vector2Int(c.x,c.y);board.Cells.Add(cell,c.type);visuals.Add(cell,MakeSphere("Erosion test "+cell,c.type,cluster,BoardRules.Position(cell)));
                    if(c.infested)board.Infested.Add(cell);   // a hazard only; no alien art needed for the rule
                }
            }
            IEnumerator Shoot(int type,Vector2Int aimCell)
            {
                while(!ready || projectile || armBusy)yield return null;
                rotationDegrees=0;
                if(loaded)Destroy(loaded);loadedType=type;loaded=MakeSphere("Loaded "+Definition(type).displayName,type,gun,LoadedLocal);
                yield return null;
                Vector2 point=view.WorldToScreenPoint(visuals[aimCell].transform.position);
                played.Clear();
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                Expect(!ready,"real click did not fire");
                while(projectile)yield return null;
            }
            while(!ready || projectile || armBusy)yield return null;

            // 1. A miss erodes the farthest non-infested sand; what it held falls away as a normal, scoring breakaway.
            var nearSand=new Vector2Int(-1,0);var farSand=new Vector2Int(0,-2);var infestedSand=new Vector2Int(2,-3);var held=new Vector2Int(-1,-2);
            Board((0,-1,SphereIds.Void,false),(1,-1,SphereIds.Void,false),(-1,0,SphereIds.Sand,false),(0,-2,SphereIds.Sand,false),
                (1,-2,SphereIds.Stone,false),(2,-3,SphereIds.Sand,true),(-1,-2,SphereIds.Fire,false));
            Expect(ErosionTarget()==farSand,"target must be the farthest sand that isn't infested");
            int erosions=ErosionCount,scoreBefore=score;double sandBefore=totals[(int)ElementId.Sand];
            yield return Shoot(SphereIds.Stone,held);
            Expect(lastMatch==0 && ErosionCount==erosions+1 && LastEroded==farSand && !board.Cells.ContainsKey(farSand),"a missed shot must erode the farthest sand");
            Expect(board.Cells.ContainsKey(nearSand) && board.Cells.ContainsKey(infestedSand),"nearer sand and infested sand must stay");
            Expect(played.Contains(sand.erodeSound),"erosion must play sandfall");
            Expect(!board.Cells.ContainsKey(held) && lastRelease>=2 && score==scoreBefore+lastRelease*ElementScoring.Breakaway,
                "what the sand held must fall away as a normal breakaway ("+lastRelease+" released, score +"+(score-scoreBefore)+")");
            Expect(Math.Abs(totals[(int)ElementId.Sand]-sandBefore)<1e-9,"eroded sand must give no points or elements");
            var crumble=GameObject.Find("Sphere burst (Crumble)");
            Expect(crumble && crumble.GetComponent<ParticleSystemRenderer>().sharedMaterial==SphereShapeStyle.Current.burstMaterial && SphereShapeStyle.Current.burstMaterial && crumble.GetComponent<ParticleSystem>().main.gravityModifier.constant>SphereShapeStyle.Current.shatter.gravity,"eroded sand must crumble into falling grains");
            yield return new WaitForSeconds(.25f);CaptureBurst("crumble",crumble.transform.position);

            // 2. The ammo check runs after the erosion: eroding the last sand replaces Sand in the cannon and chambers.
            Board((0,-1,SphereIds.Void,false),(1,0,SphereIds.Fire,false),(0,-2,SphereIds.Sand,false));
            while(!ready || projectile || armBusy)yield return null;
            Expect(ammoRing.ChamberCount>0,"the test planet needs ammo chambers to check them");
            ammoRing.ReplaceWhere(t=>true,(out int t)=>{t=SphereIds.Sand;return true;});
            yield return Shoot(SphereIds.Stone,new Vector2Int(1,0));
            Expect(!board.Cells.ContainsValue(SphereIds.Sand),"the last sand must erode");
            while(!ready)yield return null;
            Expect(loadedType!=SphereIds.Sand && !new List<int>(ammoRing.Chambers).Contains(SphereIds.Sand),"ammo must drop Sand once the erosion removed the last one");

            // 3. A shot that makes a match erodes nothing.
            Board((0,-1,SphereIds.Void,false),(1,-1,SphereIds.Void,false),(2,-2,SphereIds.Void,false),(-1,0,SphereIds.Sand,false));
            erosions=ErosionCount;
            yield return Shoot(SphereIds.Void,new Vector2Int(1,-1));
            Expect(lastMatch>=3 && ErosionCount==erosions && !played.Contains(sand.erodeSound),"a matching shot must not erode sand");

            // 4. Infested sand doesn't count as sand: with no other sand, a miss erodes nothing.
            Board((0,-1,SphereIds.Void,false),(-1,-1,SphereIds.Sand,true),(1,0,SphereIds.Fire,false));
            erosions=ErosionCount;
            yield return Shoot(SphereIds.Stone,new Vector2Int(1,0));
            Expect(lastMatch==0 && ErosionCount==erosions && board.Cells.ContainsKey(new Vector2Int(-1,-1)),"infested sand must be exempt");

            // 5. A shot lost out the bottom is also a miss (exercised directly: the lost path calls this).
            while(!ready || projectile || armBusy)yield return null;
            Board((0,-1,SphereIds.Void,false),(0,-2,SphereIds.Sand,false),(-1,-2,SphereIds.Fire,false));
            erosions=ErosionCount;scoreBefore=score;
            ErodeAfterLostShot(SphereIds.Stone);
            Expect(ErosionCount==erosions+1 && !board.Cells.ContainsKey(new Vector2Int(-1,-2)) && score==scoreBefore+ElementScoring.Breakaway,"a lost shot must erode and drop what the sand held");
            levelAudio.EffectPlayed-=played.Add;
        }

        // Close-up render of a burst for review (Logs/burst-<name>.png).
        internal void CaptureBurst(string name,Vector3 at)
        {
            var camera=new GameObject("Burst capture").AddComponent<Camera>();camera.CopyFrom(view);
            camera.orthographic=true;camera.orthographicSize=Radius*cluster.lossyScale.x*3;camera.transform.position=new Vector3(at.x,at.y,view.transform.position.z);
            var target=new RenderTexture(400,400,24);target.Create();camera.targetTexture=target;camera.aspect=1;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(400,400,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,400,400),0,0);pixels.Apply();RenderTexture.active=previous;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/burst-"+name+".png"),pixels.EncodeToPNG());
            camera.targetTexture=null;target.Release();Destroy(target);Destroy(pixels);Destroy(camera.gameObject);
        }
    }

    public sealed class ErosionSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-erosionSmoke"))new GameObject("Erosion checks").AddComponent<ErosionSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            FindAnyObjectByType<SystemView>().OpenPlanet(3);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();foreach(int id in new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Stone,SphereIds.Sand})prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Erosion: setup cannot begin");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            for(float t=0;t<1.5f;t+=Time.deltaTime)yield return null;
            yield return game.CheckErosion();
            Debug.Log("ECOSPHERE_EROSION_PASSED: real-shot miss erodes the farthest non-infested sand with sandfall and crumble, normal breakaway of what it held, no points or elements from the sand, ammo replaced after erosion, no erosion on a match, infested sand exempt, lost shots erode");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
