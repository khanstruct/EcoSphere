using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // A rolled natural sphere sits in its cell, is never offered as ammo, and counts as released when it falls away.
        internal void CheckNaturalPlacement(int sphereId,Vector2Int cell)
        {
            if(!board.Cells.TryGetValue(cell,out int type) || type!=sphereId || !visuals.ContainsKey(cell))throw new Exception("Natural occurrence: the rolled sphere is not on the board");
            if(new List<int>(ElementTypes()).Contains(sphereId) || loadedType==sphereId || new List<int>(ammoRing.Chambers).Contains(sphereId))
                throw new Exception("Natural occurrence: a natural sphere was offered as ammo");
        }
        internal bool ReleasedNatural(int sphereId)=>releasedNaturalSpheres.Contains(sphereId);
    }

    // Run with SphereIntroTools.Smoke and -naturalSmoke (DEV mode; DEV is switched off where ownership matters).
    public sealed class NaturalOccurrenceSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-naturalSmoke"))new GameObject("Natural occurrence checks").AddComponent<NaturalOccurrenceSmoke>();}
        static void Expect(bool value,string message){if(!value)throw new Exception("Natural occurrence: "+message);}

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            var catalog=SphereCatalog.Current;var spirit=catalog.Get(SphereIds.Spirit);var bird=catalog.Get(SphereIds.Bird);var worlds=WorldCatalog.Current;
            var settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previousDev=settings.devModeEnabled;
            float authoredChance=worlds.naturalOccurrenceChance,birdWeight=bird.naturalWeight;
            Expect(spirit.OccursNaturally && authoredChance>0 && authoredChance<1,"Spirit must occur naturally and the per-world chance must be authored");

            // 1. Generation, on throwaway profiles (the real save is untouched).
            List<(WorldRecord world,NaturalSpherePlacement natural)> Rolled(SystemRecord system)
            {
                var found=new List<(WorldRecord,NaturalSpherePlacement)>();
                foreach(var world in system.worlds)if(world.IsProcedural)foreach(var natural in world.naturalSpheres)found.Add((world,natural));
                return found;
            }
            SystemRecord Generate(PlayerProfile profile,int number)=>WorldGeneration.GetOrCreate(profile,worlds,number);
            try
            {
                settings.devModeEnabled=false;
                // At chance 1 the first procedural world in slot order takes it, and no other world in the system gets one.
                worlds.naturalOccurrenceChance=1;
                var system=Generate(new PlayerProfile{universeSeed=1234},3);var rolled=Rolled(system);
                Expect(rolled.Count==1 && rolled[0].world==system.worlds.Find(w=>w.IsProcedural) && rolled[0].natural.sphereId==SphereIds.Spirit,"chance 1 must give exactly the first procedural world one natural Spirit");
                foreach(var world in system.worlds)if(!world.IsProcedural)Expect(!world.naturalSpheres.Exists(n=>n.sphereId==SphereIds.Spirit),"predefined worlds keep their authored placements only");
                // At the authored chance: never more than one per system, never touching the core, inside the cluster.
                worlds.naturalOccurrenceChance=authoredChance;bird.naturalWeight=1;
                var profile=new PlayerProfile{universeSeed=99};int systemsWithOne=0,spirits=0,birds=0,outerRing=0;const int Systems=200;
                for(int number=1;number<=Systems;number++)
                {
                    var found=Rolled(Generate(profile,number));
                    Expect(found.Count<=1,"system "+number+" has "+found.Count+" natural spheres");
                    if(found.Count==0)continue;
                    systemsWithOne++;var cell=found[0].natural.cell;if(NaturalOccurrence.Ring(cell)==found[0].world.rules.clusterRings)outerRing++;
                    Expect(NaturalOccurrence.IsAllowedCell(cell,found[0].world.rules.clusterRings) && NaturalOccurrence.Ring(cell)>=2,"a natural sphere touches the core or leaves the cluster ("+cell+")");
                    if(found[0].natural.sphereId==SphereIds.Spirit)spirits++;else if(found[0].natural.sphereId==SphereIds.Bird)birds++;
                    else Expect(false,"picked a sphere that doesn't occur naturally");
                }
                int procedural=Generate(new PlayerProfile{universeSeed=1234},3).worlds.FindAll(w=>w.IsProcedural).Count;
                double expected=Systems*(1-Math.Pow(1-authoredChance,procedural));
                Expect(Math.Abs(systemsWithOne-expected)<expected*.5+5,"systems with a natural sphere: "+systemsWithOne+", expected about "+expected.ToString("0"));
                Expect(outerRing>0,"natural spheres may sit on the outer ring too");
                Expect(spirits>0 && birds>0,"the pick must draw from every unowned candidate ("+spirits+" Spirit, "+birds+" Bird)");
                var owner=new PlayerProfile{universeSeed=99};owner.unlockedSpheres.Add(SphereIds.Spirit);
                for(int number=1;number<=Systems;number++)foreach(var found in Rolled(Generate(owner,number)))Expect(found.natural.sphereId==SphereIds.Bird,"an owned sphere must not occur naturally");
                owner.unlockedSpheres.Add(SphereIds.Bird);worlds.naturalOccurrenceChance=1;
                Expect(Rolled(Generate(owner,Systems+1)).Count==0,"with every candidate owned nothing occurs naturally");
                bird.naturalWeight=birdWeight;
                // Deterministic, and the roll never reshuffles the rest of generation.
                var again=Generate(new PlayerProfile{universeSeed=1234},3);
                Expect(Rolled(again).Count==1 && Rolled(again)[0].natural.cell==rolled[0].natural.cell && Rolled(again)[0].world.id==rolled[0].world.id,"rolls must be deterministic per system");
                worlds.naturalOccurrenceChance=0;
                var none=Generate(new PlayerProfile{universeSeed=1234},3);
                Expect(Rolled(none).Count==0,"chance 0 must never place a natural sphere");
                for(int i=0;i<system.worlds.Count;i++)Expect(none.worlds[i].displayName==system.worlds[i].displayName && none.worlds[i].seed==system.worlds[i].seed,"natural rolls must not reshuffle world generation");
                worlds.naturalOccurrenceChance=1;settings.devModeEnabled=true;
                Expect(Rolled(Generate(new PlayerProfile{universeSeed=1234},3)).Count==0,"DEV owns every collectable sphere, so nothing occurs naturally");
            }
            finally{settings.devModeEnabled=previousDev;worlds.naturalOccurrenceChance=authoredChance;bird.naturalWeight=birdWeight;}

            // 2. Gameplay on a real world record: roll it in, play it through setup, a real shot and results.
            var record=WorldLevels.Get(1,2);record.rules.requiredSpheres=5;record.naturalSpheres.Clear();
            Vector2Int naturalCell;
            try{settings.devModeEnabled=false;NaturalOccurrence.Roll(new SystemRecord{seed=7},new[]{record},GameServices.Profile.Active,catalog,1);}
            finally{settings.devModeEnabled=previousDev;}
            Expect(record.naturalSpheres.Count==1 && record.naturalSpheres[0].sphereId==SphereIds.Spirit,"the roll did not place Spirit on the test world");
            naturalCell=record.naturalSpheres[0].cell;
            var view=FindAnyObjectByType<SystemView>();view.RefreshSystemHeading();view.OpenPlanet(2);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            foreach(int id in new[]{SphereIds.Fire,SphereIds.Ice,SphereIds.Water,SphereIds.Stone,SphereIds.Void})prep.AddSphere(id);
            Expect(prep.Begin.interactable,"setup cannot begin");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            for(float t=0;t<1f;t+=Time.deltaTime)yield return null;
            game.CheckNaturalPlacement(SphereIds.Spirit,naturalCell);
            yield return game.ExerciseProceduralShot();
            if(!game.ReleasedNatural(SphereIds.Spirit))game.CheckNaturalPlacement(SphereIds.Spirit,naturalCell);   // ammo stays clear after a real reload
            // Normal ownership for the reward: DEV would treat Spirit as already owned and suppress the discovery.
            settings.devModeEnabled=false;
            try
            {
                Expect(!GameServices.Profile.OwnsSphere(SphereIds.Spirit),"the test profile must not own Spirit yet");
                game.FinishProceduralForSmoke();
                Expect(game.ReleasedNatural(SphereIds.Spirit),"the natural Spirit did not count as released");
                float deadline=Time.realtimeSinceStartup+25;
                while(SceneManager.GetActiveScene().name!="Results" && Time.realtimeSinceStartup<deadline)
                { var rating=FindAnyObjectByType<StarRatingPresentation>(); if(rating && rating.RevealComplete)rating.ContinueButton.onClick.Invoke(); yield return null; }
                var result=GameServices.Progress.Find(1,2);
                Expect(SceneManager.GetActiveScene().name=="Results" && result!=null,"results did not open");
                int index=result.discoveries.IndexOf(SphereIds.Spirit);
                Expect(index>=0 && result.discoveryDetails[index].kind==DiscoveryKind.Natural,"releasing the natural Spirit must discover it as Natural");
                Expect(GameServices.Profile.OwnsSphere(SphereIds.Spirit),"the discovery must grant Spirit");
            }
            finally{settings.devModeEnabled=previousDev;}
            Debug.Log("ECOSPHERE_NATURAL_OCCURRENCE_PASSED: at most one per system (first procedural world to roll), weighted pick among unowned candidates, never touching the core, deterministic, no reshuffle, none when owned or in DEV, on the board but never ammo through a real shot, released and discovered as Natural on results");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
