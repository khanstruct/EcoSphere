using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere.Editor
{
    public static class SystemHeadingTools
    {
        const string Art = "Assets/EcoSphere/Art/UI/Headings/";

        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }

        static RectTransform Ornament(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position)
        {
            var rect = Rect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return rect;
        }

        public static void Add(SystemView system)
        {
            foreach (string path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var old = system.transform.Find("System Heading");
            if (old)
            {
                if (system.selection) system.selection.transform.SetParent(system.transform, false);
                Object.DestroyImmediate(old.gameObject);
            }
            var heading = Rect("System Heading", system.transform, new Vector2(800, 160), new Vector2(0, -100));
            heading.anchorMin = heading.anchorMax = new Vector2(.5f, 1);
            var label = Rect("System Number", heading, new Vector2(500, 40), new Vector2(0, 50)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            label.fontSize = 22; label.characterSpacing = 6;
            label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, .97f, .86f);
            label.raycastTarget = false;
            system.systemLabel = label;
            var line = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "heading-line-v1.png");
            system.leftAdornment = Ornament("Left Ornament", heading, line, new Vector2(170, 57), Vector2.zero);
            system.rightAdornment = Ornament("Right Ornament", heading, line, new Vector2(170, 57), Vector2.zero);
            system.rightAdornment.localScale = new Vector3(-1, 1, 1);
            system.selection.transform.SetParent(heading, false);
            system.selection.rectTransform.anchorMin = system.selection.rectTransform.anchorMax = new Vector2(.5f, .5f);
            system.selection.rectTransform.anchoredPosition = Vector2.zero;
            system.selection.rectTransform.sizeDelta = new Vector2(760, 65);
            system.selection.fontSize = 44;
            system.selection.color = new Color(1, .98f, .9f);
            system.selection.text = "Choose a Planet";
            Ornament("Heading Arch", heading, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "heading-arch-v1.png"),
                new Vector2(500, 167), new Vector2(0, -47));
            system.RefreshSystemHeading();
        }

        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            var system = Object.FindAnyObjectByType<SystemView>();
            Add(system);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            SystemSceneTools.ValidateAndRender();
            system = Object.FindAnyObjectByType<SystemView>();
            int number = system.systemNumber;
            system.SetSystemNumber(125);
            if (system.systemLabel.text != "SYSTEM 125") throw new System.Exception("System number update failed");
            system.SetSystemNumber(number);
            for (int i = 0; i < system.planets.Length; i++)
            {
                system.SelectPlanet(i);
                if (system.selection.text != WorldLevels.Name(system.systemNumber,i)) throw new System.Exception("Planet title update failed");
            }
            Debug.Log("ECOSPHERE_HEADINGS_PASSED: dynamic system number and all five planet titles.");
        }
    }
}
