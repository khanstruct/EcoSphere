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
            // Starting group sizes per clustering level: singles most common, then pairs, threes, rare fours.
            var groupCounts=new Dictionary<float,int[]>{{0f,new int[6]},{.5f,new int[6]},{1f,new int[6]}};
            for(int seed=0;seed<64;seed++)foreach(float clustering in new[] {0f,.5f,1f})
            {
                var first=new BoardRules(); var repeated=new BoardRules();
                ProceduralCluster.Populate(first,types,GameplayGeometry.DefaultClusterRings,seed,clustering,WorldCatalog.Current.clusterGroups);
                ProceduralCluster.Populate(repeated,types,GameplayGeometry.DefaultClusterRings,seed,clustering,WorldCatalog.Current.clusterGroups);
                if(first.Cells.Count!=GameplayGeometry.CellCount(GameplayGeometry.DefaultClusterRings) || first.Disconnected().Count!=0)throw new Exception("Procedural board must start full and attached to the core");
                foreach(int type in types)
                    if(first.Cells.Count(cell=>cell.Value==type)!=GameplayGeometry.CellCount(GameplayGeometry.DefaultClusterRings)/types.Length)throw new Exception("Clustering changed the selected sphere mix");
                foreach(var cell in first.Cells)
                    if(!repeated.Cells.TryGetValue(cell.Key,out int other) || other!=cell.Value)throw new Exception("Seeded layout changed between identical generations");
                if(clustering==0)low+=ProceduralCluster.NeighborSimilarity(first);
                if(clustering==1)high+=ProceduralCluster.NeighborSimilarity(first);
                foreach(int size in ProceduralCluster.GroupSizes(first))groupCounts[clustering][Math.Min(size,5)]++;
            }
            // Clustering is a tilt within the capped group mix, so its effect on neighbors is deliberately modest.
            if(high/64<=low/64+.04)throw new Exception("Clustering control did not increase same-type neighbors");
            foreach(var entry in groupCounts)
            {
                var c=entry.Value; int groups=c.Sum();
                if(!(c[1]>=c[2] && c[2]>=c[3] && c[3]>=c[4]))throw new Exception($"Starting groups at clustering {entry.Key} must favor singles, then pairs, threes and fours: {string.Join("/",c.Skip(1))}");
                if(c[5]>groups*.12)throw new Exception($"Starting groups of five or more must stay uncommon at clustering {entry.Key}: {c[5]} of {groups}");
            }
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
            Debug.Log($"ECOSPHERE_PROCEDURAL_DATA_PASSED: 192 balanced connected seeded boards; same-type neighbors {low/64:P0} scattered vs {high/64:P0} clustered; groups 1/2/3/4/5+ at clustering .5: {string.Join("/",groupCounts[.5f].Skip(1))}; saved clustering, four slots and Water recipe");
        }
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(SystemSceneTools.ScenePath); EditorApplication.isPlaying=true;
        }
    }
}
