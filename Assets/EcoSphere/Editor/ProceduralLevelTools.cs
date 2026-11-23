using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class ProceduralLevelTools
    {
        public const string ScenePath="Assets/EcoSphere/Scenes/Procedural.unity";
        [MenuItem("EcoSphere/Configure Procedural Levels")]
        public static void Configure()
        {
            SphereCatalogTools.Configure(); WorldCatalogTools.Configure();
            if(!System.IO.File.Exists(ScenePath) && !AssetDatabase.CopyAsset(TestLevelTools.ScenePath,ScenePath))
                throw new Exception("Could not create shared procedural gameplay scene");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var game=UnityEngine.Object.FindAnyObjectByType<EcoSpherePrototype>();
            game.levelLayout=EcoSpherePrototype.LevelLayout.Procedural; game.startingSphereTypes=Array.Empty<int>();
            game.resourceGoal=ElementScoring.Inclusion; game.abundanceGoal=ElementScoring.Abundance;
            EditorUtility.SetDirty(game); EditorSceneManager.SaveScene(scene);
            var catalog=WorldCatalog.Current;
            catalog.minimumProceduralSpheres=catalog.maximumProceduralSpheres=4;
            catalog.proceduralRules.requiredSpheres=4;
            catalog.proceduralGameplayScene=WorldLevels.ProceduralSceneName;
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Exists(item=>item.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();
            Validate();
        }
        [MenuItem("EcoSphere/Validate Procedural Levels")]
        public static void Validate()
        {
            var types=new[] {SphereIds.Fire,SphereIds.Ice,SphereIds.Water,SphereIds.Stone};
            double low=0,high=0;
            for(int seed=0;seed<64;seed++)foreach(float clustering in new[] {0f,.5f,1f})
            {
                var first=new BoardRules(); var repeated=new BoardRules();
                ProceduralCluster.Populate(first,types,4,seed,clustering);
                ProceduralCluster.Populate(repeated,types,4,seed,clustering);
                if(first.Cells.Count!=60 || first.Disconnected().Count!=0)throw new Exception("Procedural board must start full and attached to the core");
                foreach(int type in types)
                    if(first.Cells.Count(cell=>cell.Value==type)!=15)throw new Exception("Clustering changed the selected sphere mix");
                foreach(var cell in first.Cells)
                    if(!repeated.Cells.TryGetValue(cell.Key,out int other) || other!=cell.Value)throw new Exception("Seeded layout changed between identical generations");
                if(clustering==0)low+=ProceduralCluster.NeighborSimilarity(first);
                if(clustering==1)high+=ProceduralCluster.NeighborSimilarity(first);
            }
            if(high/64<=low/64+.12)throw new Exception("Clustering control did not meaningfully increase same-type neighbors");
            var catalog=WorldCatalog.Current; catalog.Validate();
            var save=new PlayerProfile {universeSeed=1491};
            var system=WorldGeneration.GetOrCreate(save,catalog,1);
            var restored=JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(save));
            for(int i=2;i<system.worlds.Count;i++)
            {
                var world=system.worlds[i];
                if(!world.IsProcedural || world.RequiredSpheres!=4 || world.GameplayScene!=WorldLevels.ProceduralSceneName ||
                    world.rules.clustering<catalog.minimumClustering || world.rules.clustering>catalog.maximumClustering ||
                    Math.Abs(world.rules.clustering-restored.systems[0].worlds[i].rules.clustering)>.000001)
                    throw new Exception("Procedural world scene, four-sphere rule, or saved clustering invalid");
            }
            if(!SphereCatalog.Current.recipeBook.TryCombine(ElementId.Ice,ElementId.Fire,out var output) || output.id!=SphereIds.Water)
                throw new Exception("Ice + Fire must unlock Water");
            Debug.Log($"ECOSPHERE_PROCEDURAL_DATA_PASSED: 192 balanced connected seeded boards; same-type neighbors {low/64:P0} scattered vs {high/64:P0} clustered; saved clustering, four slots and Water recipe");
        }
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath); EditorApplication.isPlaying=true;
        }
    }
}
