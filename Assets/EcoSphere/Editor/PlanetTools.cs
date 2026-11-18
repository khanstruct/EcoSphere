using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class PlanetTools
    {
        const string Folder = "Assets/EcoSphere/Art/Planets/";
        public static void Setup()
        {
            AssetDatabase.Refresh();
            foreach (string filename in new[] { "barren-surface-v1.png", "barren-heightmap-v1.png" })
                AssetDatabase.ImportAsset(Folder + filename, ImportAssetOptions.ForceUpdate);
            var shader = Shader.Find("EcoSphere/Barren Planet");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Planet shader failed to compile");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "barren-planet-v1.mat");
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Folder + "barren-planet-v1.mat");
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "barren-surface-v1.png"));
            material.SetTexture("_HeightMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "barren-heightmap-v1.png"));
            material.SetFloat("_SeaLevel", -.02f);
            EditorUtility.SetDirty(material);
            var scene = EditorSceneManager.OpenScene(TestLevelTools.ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<EcoSpherePrototype>();
            game.receivingPlanetMaterial = material;
            EditorUtility.SetDirty(game);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            foreach (string filename in new[] { "barren-surface-v1.png", "barren-heightmap-v1.png" })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + filename);
                if (!texture || texture.width != 256 || texture.height != 128)
                    throw new Exception("Planet textures must be 256 by 128");
            }
            Debug.Log("ECOSPHERE_PLANET_SETUP_PASSED");
        }
    }
}
