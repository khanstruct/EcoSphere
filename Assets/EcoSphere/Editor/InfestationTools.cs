using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class InfestationTools
    {
        public static InfestationModifierDefinition EnsureModifier(WorldCatalog catalog)
        {
            const string spritePath="Assets/EcoSphere/Art/Creatures/alien-infestation-v2.png";
            const string alternatePath="Assets/EcoSphere/Art/Creatures/alien-infestation-v3.png";
            foreach(string framePath in new[]{spritePath,alternatePath})
            {
            var importer=(TextureImporter)AssetImporter.GetAtPath(framePath);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            if(settings.spriteAlignment!=(int)SpriteAlignment.Custom || settings.spritePivot!=new Vector2(.5f,.4f) || settings.spriteMeshType!=SpriteMeshType.FullRect)
            {
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.4f);settings.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);importer.maxTextureSize=256;importer.SaveAndReimport();
            }
            }
            var shader=Shader.Find("EcoSphere/Alien Overlay");
            if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Alien overlay shader failed");
            const string materialPath="Assets/EcoSphere/Art/Creatures/alien-overlay.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            const string path="Assets/EcoSphere/Data/Worlds/Modifiers/alien-infestation.asset";
            var modifier=AssetDatabase.LoadAssetAtPath<InfestationModifierDefinition>(path);
            if(!modifier)
            {
                modifier=ScriptableObject.CreateInstance<InfestationModifierDefinition>();
                modifier.id="alien-infestation";modifier.displayName="Alien Infestation";
                modifier.description="Attach beside aliens to remove them. Infested spheres block matches.";
                modifier.icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EcoSphere/Art/UI/Modifiers/alien-infestation-v2.png");
                modifier.alienSprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);modifier.overlayMaterial=material;
                modifier.alienAlternateSprite=AssetDatabase.LoadAssetAtPath<Sprite>(alternatePath);
                modifier.hitAudio=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/alien-hit.mp3");
                modifier.minimumInfested=8;modifier.maximumInfested=12;modifier.proceduralChance=0;
                modifier.guaranteedSystem=1;modifier.guaranteedSlot=2;
                AssetDatabase.CreateAsset(modifier,path);
            }
            if(!modifier.alienAlternateSprite){modifier.alienAlternateSprite=AssetDatabase.LoadAssetAtPath<Sprite>(alternatePath);EditorUtility.SetDirty(modifier);}
            if(!catalog.proceduralModifiers.Contains(modifier))catalog.proceduralModifiers.Add(modifier);
            return modifier;
        }
        public static void Configure()
        {
            WorldCatalogTools.Configure();Validate();
        }
        public static void Validate()
        {
            var catalog=WorldCatalog.Current;catalog.Validate();
            var modifier=(InfestationModifierDefinition)catalog.Modifier("alien-infestation");modifier.Validate();
            var animationObject=new GameObject("Alien animation data check",typeof(SpriteRenderer),typeof(InfestedSphereVisual));
            try
            {
                var appearance=animationObject.GetComponent<InfestedSphereVisual>();var random=new System.Random(3187);
                var startingFrames=new HashSet<int>();var speeds=new HashSet<float>();
                for(int i=0;i<64;i++)
                {
                    appearance.InitializeAnimation(modifier,random);startingFrames.Add(appearance.CurrentFrame);speeds.Add(appearance.FramesPerSecond);
                    if(appearance.FramesPerSecond<modifier.animationFramesPerSecond*(1-modifier.animationSpeedVariation) || appearance.FramesPerSecond>modifier.animationFramesPerSecond*(1+modifier.animationSpeedVariation))throw new Exception("Alien speed outside configured variation");
                }
                if(startingFrames.Count!=2 || (modifier.animationSpeedVariation>0 && speeds.Count<2))throw new Exception("Alien animation starts/speeds must vary independently");
            }
            finally{UnityEngine.Object.DestroyImmediate(animationObject);}
            var save=new PlayerProfile{universeSeed=3187};var system=WorldGeneration.GetOrCreate(save,catalog,1);
            if(!system.worlds[2].modifiers.Contains(modifier.id) || system.worlds[0].modifiers.Contains(modifier.id) || system.worlds[1].modifiers.Contains(modifier.id))throw new Exception("Infestation placement must target world three");
            var counts=new HashSet<int>();
            for(int seed=0;seed<100;seed++)
            {
                var board=new BoardRules();ProceduralCluster.Populate(board,new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Mud},4,seed,.5f,WorldCatalog.Current.clusterGroups);
                var before=new Dictionary<Vector2Int,int>(board.Cells);InfestationRules.Populate(board,modifier,seed);
                if(board.Infested.Count<8 || board.Infested.Count>12 || board.Infested.Contains(Vector2Int.zero))throw new Exception("Infestation count/core exclusion");
                counts.Add(board.Infested.Count);var original=new HashSet<Vector2Int>(board.Infested);InfestationRules.Populate(board,modifier,seed);
                if(!original.SetEquals(board.Infested))throw new Exception("Infestation must be seed deterministic");
                foreach(var pair in board.Cells)if(before[pair.Key]!=pair.Value)throw new Exception("Infestation changed sphere layout");
            }
            if(counts.Count!=5)throw new Exception("Infestation range must include every value from eight through twelve");
            var chain=new BoardRules();
            for(int i=1;i<=5;i++)chain.Cells.Add(new Vector2Int(i,0),SphereIds.Fire);
            chain.Infested.Add(new Vector2Int(3,0));
            if(chain.MatchFrom(new Vector2Int(3,0)).Count!=0 || chain.MatchFrom(new Vector2Int(1,0)).Count!=0 || chain.Disconnected().Count!=0)
                throw new Exception("Infested host must block matching but retain structural support");
            chain.Cells.Add(new Vector2Int(1,1),SphereIds.Fire);
            var match=chain.MatchFrom(new Vector2Int(1,0));
            if(match.Count!=3 || match.Contains(new Vector2Int(3,0)) || !chain.Infested.Contains(new Vector2Int(3,0)))throw new Exception("Neighboring matches must leave alien/host untouched");
            foreach(var cell in match)chain.Cells.Remove(cell);
            var release=chain.Disconnected();if(!release.Contains(new Vector2Int(3,0)))throw new Exception("Infested host must release when detached");
            chain.Infested.Clear();
            if(chain.MatchFrom(new Vector2Int(3,0)).Count!=3)throw new Exception("Cleansed spheres must match normally");
            var cleansedThisShot=new HashSet<Vector2Int>{new Vector2Int(3,0)};
            if(chain.MatchFrom(new Vector2Int(4,0),cleansedThisShot).Count!=0)throw new Exception("Cleansed hosts must not retroactively complete the same shot's chain");
            Debug.Log("ECOSPHERE_INFESTATION_DATA_PASSED: world-three modifier, 100 deterministic layouts, 8-12 counts, core exclusion, match-chain barrier, structural support, normal breakaway and cleansed matching");
        }
        public static void Smoke(){Configure();EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);EditorApplication.isPlaying=true;}
    }
}
