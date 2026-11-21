using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoSphere.Editor
{
    public static class LevelPrepTools
    {
        public const string ScenePath="Assets/EcoSphere/Scenes/LevelPrep.unity";
        const string Art="Assets/EcoSphere/Art/";
        public static void Build()
        {
            SphereCatalogTools.Configure();
            foreach(var path in new[]{Art+"UI/Frames/panel-frame-v1.png",Art+"UI/Modifiers/close-to-sun-v1.png",Art+"UI/Modifiers/continuous-rotation-v1.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.wrapMode=TextureWrapMode.Clamp;
                importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=256;
                if(path.Contains("frame"))
                {
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    importer.spriteBorder=Vector4.one*(250f*texture.width/1254);
                }
                importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var prep=new GameObject("Level Prep").AddComponent<LevelPrepView>();
            prep.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            prep.frame=Sprite("UI/Frames/panel-frame-v1.png");
            prep.line=Sprite("UI/Headings/heading-line-v1.png"); prep.arch=Sprite("UI/Headings/heading-arch-v1.png");
            prep.starfield=Sprite("Backgrounds/starfield-v2.png");
            prep.buttonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoSphere/Prefabs/UI/CelestialButton.prefab");
            MenuMusicTools.Configure();
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_PREP_SCENE_CREATED");
        }
        static Sprite Sprite(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+path);
        public static void TutorialSmoke()
        {
            EditorSceneManager.OpenScene(ScenePath);
            LevelPrepView.SystemNumber=1; LevelPrepView.PlanetIndex=0;
            EditorApplication.isPlaying=true;
        }
        public static void Smoke()
        {
            EditorSceneManager.OpenScene(ScenePath);
            LevelPrepView.SystemNumber=1; LevelPrepView.PlanetIndex=0; LevelPrepView.RequiredSpheres=2;
            EditorApplication.isPlaying=true;
        }
    }
}
