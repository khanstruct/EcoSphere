using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class ArchitectureTools
    {
        [MenuItem("EcoSphere/Configure Shared Architecture")]
        public static void Configure()
        {
            var catalog=WorldCatalog.Current;
            if(catalog.Sun(catalog.defaultSunTypeId)==null)catalog.sunTypes.Add(new SunTypeDefinition{id=catalog.defaultSunTypeId,displayName="Yellow Star",sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EcoSphere/Art/Suns/yellow-sun.png")});
            EditorUtility.SetDirty(catalog);
            var style=Resources.Load<PlanetVisualStyle>("PlanetVisualStyle");
            if(!style.terrains.Exists(t=>t.id==catalog.defaultTerrainStyleId))style.terrains.Add(new PlanetTerrainStyle{id=catalog.defaultTerrainStyleId,material=AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoSphere/Art/Planets/barren-planet-v1.mat")});
            EditorUtility.SetDirty(style);AssetDatabase.SaveAssets();Validate();
        }
        static void Expect(bool value,string message){if(!value)throw new Exception("Architecture: "+message);}
        [Serializable] sealed class LegacyFixture
        {
            public int version=1,universeSeed=314,currentSystem=1;
            public bool stoneUnlocked=true;
            public List<SystemRecord> systems;
            public List<CompletedWorld> worlds;
            public List<int> unlockedSpheres=new List<int>();
            public List<string> placedPredefinedWorlds=new List<string>();
        }
        public static void Validate()
        {
            var dev=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previous=dev.devModeEnabled;
            try
            {
                dev.devModeEnabled=false;GameServices.Reset();
                Expect(GameServices.Progress.Find(99,0)==null && GameServices.Profile.Active.systems.Count==0,"read-only completion query generated a system");
                var beta=WorldLevels.Get(1,1);Expect(beta.naturalSpheres.Count==1,"Beta natural placement must be authored data");
                var natural=GameServices.Progress.Complete(1,1,.3f,44,new[]{SphereIds.Stone});
                Expect(natural.discoveries.Count==1 && natural.discoveries[0]==SphereIds.Stone && beta.completion==natural.world,"nested natural completion");
                Expect(GameServices.Profile.OwnsSphere(SphereIds.Stone),"unified natural ownership");
                var air=WorldLevels.Get(1,2);air.naturalSpheres.Add(new NaturalSpherePlacement{sphereId=SphereIds.Air,cell=Vector2Int.up});
                Expect(GameServices.Progress.Complete(1,2,0,0,new[]{SphereIds.Air}).discoveries.Contains(SphereIds.Air),"generic non-Stone natural discovery");
                var amounts=new double[ElementComposition.Capacity];amounts[(int)ElementId.Fire]=amounts[(int)ElementId.Void]=600;
                var alpha=GameServices.Progress.Complete(1,0,.2f,0,null,amounts);
                Expect(alpha.discoveries.Contains(SphereIds.Sunlight),"recipe ownership uses same discovery operation");
                Expect(GameServices.Progress.Complete(1,1,0,0,new[]{SphereIds.Stone}).discoveries.Count==0,"revisit rewarded twice");
                var profile=GameServices.Profile.Active;var system=profile.systems.Find(s=>s.number==1);
                Expect(!system.IsComplete,"partially completed system marked complete");
                for(int i=3;i<system.worlds.Count;i++)GameServices.Progress.Complete(1,i,0,0,null);
                Expect(system.IsComplete,"system completion must derive from planets");
                var loaded=WorldSaveStore.Deserialize(JsonUtility.ToJson(profile));
                Expect(loaded.systems[0].worlds[1].completion.discoveries.Contains(SphereIds.Stone) && loaded.systems[0].IsComplete,"nested profile round trip");
                var legacy=new LegacyFixture{systems=profile.systems,worlds=new List<CompletedWorld>{natural.world}};
                foreach(var planet in legacy.systems[0].worlds)planet.completion=null;
                var migrated=WorldSaveStore.Deserialize(JsonUtility.ToJson(legacy));
                Expect(migrated.version==2 && migrated.unlockedSpheres.Contains(SphereIds.Stone) && migrated.systems[0].worlds[1].completion!=null,"v1 migration");
                dev.devModeEnabled=true;GameServices.Reset();
                beta=WorldLevels.Get(1,1);
                Expect(GameServices.Progress.Complete(1,1,0,0,new[]{SphereIds.Stone}).discoveries.Count==0,"DEV rediscovered owned natural sphere");
                Expect(GameServices.Progress.Complete(1,0,0,0,null,amounts).discoveries.Count==0,"DEV rediscovered owned recipe sphere");
                var a=new object();var b=new object();GameServices.Pause.Acquire(a);GameServices.Pause.Acquire(b);GameServices.Pause.Release(a);
                Expect(GameServices.Pause.IsPaused && Time.timeScale==0,"closing one modal unpaused another");GameServices.Pause.Release(b);Expect(Time.timeScale==1,"last pause release did not resume");
                GameServices.Profile.Active.audio.master=.5f;GameServices.Profile.Active.audio.voice=.4f;
                Expect(Mathf.Approximately(GameServices.Audio.Gain(AudioChannel.Voice),.2f),"profile audio preferences");
                Expect(WorldCatalog.Current.Sun(WorldCatalog.Current.defaultSunTypeId)!=null,"shared sun definition");
                Debug.Log("ECOSPHERE_ARCHITECTURE_PASSED: nested profile/system/planet/completion, derived completion, generic natural/recipe ownership, DEV suppression, v1 migration, pause requests and audio preferences");
            }
            finally{dev.devModeEnabled=previous;GameServices.Reset();}
        }
    }
}
