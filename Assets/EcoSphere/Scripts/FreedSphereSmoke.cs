using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Real pointer shots (rotation held at 0): a match that cuts a Moon off from the core leaves it in place with its freed
        // glow, while what only it held and an infested Moon fall as a normal breakaway. Connected Moons and Moon ammo never glow.
        internal IEnumerator CheckFreedSpheres()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Freed spheres: "+message);}
            var moonDefinition=Definition(SphereIds.Moon);
            Expect(moonDefinition.staysWhenFreed && moonDefinition.freedGlowStrength>0,"Moon must stay when freed with a freed glow");
            bool Glows(Vector2Int cell)=>visuals.TryGetValue(cell,out var v) && v.transform.Find(FreedGlowName);
            while(!ready || projectile || armBusy)yield return null;

            // Core-(0,-1) Fire and (1,-1) Fire: a Fire shot matches them. The Moon at (0,-2) hangs only from (0,-1); the Stone at
            // (-1,-2) hangs only from that Moon; the infested Moon at (0,-3) hangs only from the free Moon and that Stone. The Moon at (-1,0)
            // touches the core directly.
            var freeMoon=new Vector2Int(0,-2);var heldStone=new Vector2Int(-1,-2);var infestedMoon=new Vector2Int(0,-3);var connectedMoon=new Vector2Int(-1,0);
            SetTestBoard("Freed sphere test",(0,-1,SphereIds.Fire,false),(1,-1,SphereIds.Fire,false),(0,-2,SphereIds.Moon,false),(-1,-2,SphereIds.Stone,false),
                (0,-3,SphereIds.Moon,true),(-1,0,SphereIds.Moon,false),(1,0,SphereIds.Void,false));
            Expect(!Glows(freeMoon) && !Glows(connectedMoon),"connected Moons must not glow");
            int scoreBefore=score;
            yield return RealShot(SphereIds.Fire,new Vector2Int(1,-1));
            Expect(lastMatch>=3,"the Fire shot must match ("+lastMatch+")");
            Expect(board.Cells.TryGetValue(freeMoon,out int stayed) && stayed==SphereIds.Moon,"a Moon cut off from the core must stay in its cell");
            Expect(Glows(freeMoon),"a freed Moon must show its freed glow");
            var glow=visuals[freeMoon].transform.Find(FreedGlowName).GetComponent<SpriteRenderer>();
            Expect(glow.color.b>glow.color.r && Mathf.Approximately(glow.color.a,moonDefinition.freedGlowColor.a*moonDefinition.freedGlowStrength),"the freed glow must use the Moon's pale blue freed glow");
            Expect(!board.Cells.ContainsKey(heldStone),"a sphere held only by a freed Moon must still fall");
            Expect(!board.Cells.ContainsKey(infestedMoon),"an infested (hazarded) Moon must fall instead of staying");
            Expect(lastRelease==2,"only the held Stone and the infested Moon fall ("+lastRelease+" released)");
            Expect(board.Cells.ContainsKey(connectedMoon) && !Glows(connectedMoon),"a Moon still connected to the core must not glow");
            Expect(score>scoreBefore,"the match and the fallen spheres must still score");
            yield return new WaitForSeconds(.3f);CaptureBurst("freed-moon",visuals[freeMoon].transform.position);

            // Ammo never glows, even while a freed Moon is on the board.
            while(!ready)yield return null;
            if(loaded)Destroy(loaded);loadedType=SphereIds.Moon;loaded=MakeSphere("Loaded Moon",SphereIds.Moon,gun,LoadedLocal);
            yield return null;
            Expect(!loaded.transform.Find(FreedGlowName) && !loaded.transform.Find("Sphere glow"),"Moon ammo must not glow");
            foreach(Transform child in gun)Expect(!child.Find(FreedGlowName),"cannon and chamber spheres must not show a freed glow");

            // Reconnection: once a sphere links the freed Moon back to the core, the shared refresh removes its glow.
            var bridge=new Vector2Int(0,-1);
            board.Cells.Add(bridge,SphereIds.Stone);visuals.Add(bridge,MakeSphere("Bridge",SphereIds.Stone,cluster,BoardRules.Position(bridge)));
            RefreshFreedGlows();yield return null;
            Expect(!FreedCells().Contains(freeMoon) && !Glows(freeMoon),"a Moon reconnected to the cluster must lose its glow");
            Debug.Log("ECOSPHERE_FREED_MOON: stayed with glow after a real match, lost it on reconnection");
        }
    }

    public sealed class FreedSphereSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-freedSphereSmoke"))new GameObject("Freed sphere checks").AddComponent<FreedSphereSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            FindAnyObjectByType<SystemView>().OpenPlanet(3);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();foreach(int id in new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Stone,SphereIds.Moon})prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Freed spheres: setup cannot begin");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            for(float t=0;t<1.5f;t+=Time.deltaTime)yield return null;
            yield return game.CheckFreedSpheres();
            Debug.Log("ECOSPHERE_FREED_SPHERES_PASSED: real-shot match leaves a cut-off Moon in place with its pale blue freed glow, what it held and an infested Moon fall and score normally, connected Moons and Moon ammo never glow, reconnection removes the glow");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
