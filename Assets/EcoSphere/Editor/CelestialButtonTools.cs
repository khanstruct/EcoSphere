using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere.Editor
{
    public static class CelestialButtonTools
    {
        const string Art = "Assets/EcoSphere/Art/UI/Buttons/";
        const string PrefabPath = "Assets/EcoSphere/Prefabs/UI/CelestialButton.prefab";

        static Image Layer(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 6;
            image.raycastTarget = false;
            return image;
        }

        static GameObject CreatePrefab()
        {
            foreach (var path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/'));
                importer.spriteBorder = new Vector4(400f * texture.width / 2172, 300f * texture.height / 724,
                    400f * texture.width / 2172, 300f * texture.height / 724);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 256; importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var root = new GameObject("CelestialButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CelestialButton));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 120);
            var body = root.GetComponent<Image>();
            body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "capsule-body-v1.png");
            body.type = Image.Type.Sliced; body.pixelsPerUnitMultiplier = 6;
            var button = root.GetComponent<Button>(); button.targetGraphic = body;
            var colors = button.colors;
            colors.highlightedColor = new Color(1, .96f, .85f);
            colors.pressedColor = new Color(.65f, .65f, .75f);
            colors.disabledColor = new Color(.4f, .43f, .5f);
            button.colors = colors;
            var style = root.GetComponent<CelestialButton>();
            style.glow = Layer("Edge glow", root.transform, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "capsule-glow-v1.png"));
            style.highlight = Layer("Highlight", root.transform, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "capsule-highlight-v1.png"));
            style.glow.color = new Color(1, 1, 1, .3f);
            style.highlight.color = new Color(1, 1, 1, .5f);
            var moving = new GameObject("Traveling edge highlight", typeof(RectTransform), typeof(ButtonEdgeHighlight));
            moving.transform.SetParent(root.transform, false);
            var movingRect = moving.GetComponent<RectTransform>();
            movingRect.anchorMin = Vector2.zero; movingRect.anchorMax = Vector2.one; movingRect.sizeDelta = Vector2.zero;
            style.movingHighlight = moving.GetComponent<ButtonEdgeHighlight>();
            style.movingHighlight.raycastTarget = false;
            style.highlight.enabled = false;
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(root.transform, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = new Vector2(-90, -64);
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            label.fontSize = 25; label.alignment = TextAlignmentOptions.Center;
            label.text = "Button"; label.raycastTarget = false;
            label.color = new Color(1, .97f, .88f); style.label = label;
            Directory.CreateDirectory("Assets/EcoSphere/Prefabs/UI");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void AddToCanvas(Transform canvas)
        {
            var prefab = CreatePrefab();
            for (int i = 0; i < 2; i++)
            {
                string name = i == 0 ? "Previous" : "Next";
                var existing = canvas.Find(name + " System");
                if (existing) Object.DestroyImmediate(existing.gameObject);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
                go.name = name + " System";
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(i, 0);
                rect.anchoredPosition = new Vector2(i == 0 ? 180 : -180, 62);
                var style = go.GetComponent<CelestialButton>();
                style.label.text = name; style.phase = i * 2.1f;
                go.GetComponent<Button>().interactable = true;
            }
        }

        [MenuItem("EcoSphere/Setup System Navigation Buttons")]
        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            AddToCanvas(Object.FindAnyObjectByType<SystemView>().transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_NAV_BUTTONS_CREATED: Previous and Next active, no navigation callbacks.");
            SystemSceneTools.ValidateAndRender();
        }
    }
}
