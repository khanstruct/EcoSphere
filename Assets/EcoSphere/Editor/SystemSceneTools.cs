using TMPro;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere.Editor
{
    public static class SystemSceneTools
    {
        public const string ScenePath = "Assets/EcoSphere/Scenes/System.unity";
        const string Art = "Assets/EcoSphere/Art/";

        [MenuItem("EcoSphere/Open System")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("EcoSphere/Rebuild System Scene")]
        public static void Build()
        {
            const string requirementPath = Art + "UI/sphere-requirement-v1.png";
            var requirementImporter = (TextureImporter)AssetImporter.GetAtPath(requirementPath);
            requirementImporter.textureType = TextureImporterType.Sprite;
            requirementImporter.spriteImportMode = SpriteImportMode.Single;
            requirementImporter.alphaIsTransparency = true;
            requirementImporter.mipmapEnabled = false;
            requirementImporter.wrapMode = TextureWrapMode.Clamp;
            requirementImporter.filterMode = FilterMode.Bilinear;
            requirementImporter.npotScale = TextureImporterNPOTScale.None;
            requirementImporter.maxTextureSize = 256;
            requirementImporter.textureCompression = TextureImporterCompression.Uncompressed;
            requirementImporter.SaveAndReimport();
            var requirementIcon = AssetDatabase.LoadAssetAtPath<Sprite>(requirementPath);
            foreach (var path in Directory.GetFiles(Art + "Suns", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);

                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var previous = SceneManager.GetActiveScene();
            if (previous.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool replaceEmpty = string.IsNullOrEmpty(previous.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceEmpty ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            ConfigureMusic(camera);
            var view = camera.GetComponent<Camera>();
            view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.015f, .02f, .045f);
            var canvas = new GameObject("System Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600);
            scaler.matchWidthOrHeight = .5f;
            var root = canvas.GetComponent<RectTransform>();
            var controller = canvas.AddComponent<SystemView>();
            controller.tutorialFrame = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "UI/Frames/panel-frame-v1.png");
            controller.tutorialButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoSphere/Prefabs/UI/CelestialButton.prefab");
            controller.completedWorldMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Planets/barren-planet-v1.mat");
            var backdrop = Rect("Starfield", root, Vector2.zero, Vector2.one, Vector2.zero);
            var bg = backdrop.gameObject.AddComponent<Image>();
            bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Backgrounds/starfield-v2.png");
            bg.color = new Color(.6f, .65f, .8f, 1);
            bg.raycastTarget = false;
            var sun = Rect("Yellow Sun", root, new Vector2(.5f, .18f), new Vector2(.5f, .18f), new Vector2(620, 620));
            var sunImage = sun.gameObject.AddComponent<Image>();
            sunImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Suns/yellow-sun.png");
            sunImage.preserveAspect = true;
            sunImage.raycastTarget = false;
            float[] heights = { .40f, .50f, .60f, .70f, .80f };
            // Bake subtle variation into the system, so revisiting it keeps the same layout.
            // Fixed inner/outer bounds preserve sun clearance and room below the heading.
            var orbitRandom = new System.Random(3187);
            for (int i = 1; i < heights.Length - 1; i++)
                heights[i] += (float)(orbitRandom.NextDouble() * 2 - 1) * .022f;
            float[] xs = { -.32f, .24f, -.12f, .28f, -.26f };
            controller.planets = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                var orbit = Rect("Orbit " + (i + 1), root, Vector2.zero, Vector2.one, Vector2.zero);
                var line = orbit.gameObject.AddComponent<SystemMapGraphic>();
                line.orbitHeight = heights[i];
                line.planetX = xs[i];
                line.planetGapRadius = 34;
                line.color = new Color(1, .76f, .24f, .23f);
                line.thickness = 1.2f;
                line.raycastTarget = false;
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 anchor = new Vector2(.5f + xs[i], SystemMapGraphic.OrbitY(xs[i], heights[i]));
                var slot = Rect("Planet " + (i + 1), root, anchor, anchor, new Vector2(70, 70));
                var graphic = slot.gameObject.AddComponent<SystemMapGraphic>();
                graphic.circle = true;
                graphic.thickness = 2;
                graphic.color = controller.orbitColor;
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = graphic;
                var colors = button.colors;
                colors.highlightedColor = new Color(1, 1, .75f);
                colors.pressedColor = new Color(1, .8f, .35f);
                button.colors = colors;
                controller.planets[i] = button;
                root.Find("Orbit " + (i + 1)).GetComponent<SystemMapGraphic>().planetSlot = slot;
                var requirement = Rect("Sphere Requirement", slot, new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f), new Vector2(100, 70));
                requirement.anchoredPosition = new Vector2(0, -62);
                requirement.localScale = Vector3.one * .7f;
                var iconRect = Rect("Sphere Icon", requirement, new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f), new Vector2(48, 48));
                iconRect.anchoredPosition = new Vector2(-20, 0);
                var icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = requirementIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var count = Label("Sphere Count", requirement, new Vector2(.5f, .5f),
                    WorldLevels.Get(controller.systemNumber,i).RequiredSpheres.ToString(), 30);
                count.rectTransform.sizeDelta = new Vector2(35, 50);
                count.rectTransform.anchoredPosition = new Vector2(28, 0);
                count.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.NumberAssetPath);
                count.fontStyle = FontStyles.Normal;
                count.color = Color.white;
                count.outlineColor = new Color32(0, 0, 6, 255);
                count.outlineWidth = .15f;
            }
            controller.selection = Label("Selection", root, new Vector2(.5f, .96f), "Choose a planet", 30);
            SystemHeadingTools.Add(controller);
            CelestialButtonTools.AddToCanvas(root);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceEmpty)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            var firstSystem = scenes.Find(s => s.path == ScenePath);
            scenes.Remove(firstSystem); scenes.Insert(0,firstSystem);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_SYSTEM_CREATED: 5 clickable planets; all sun sprites configured.");
        }

        public static void CompactLayout()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var system = UnityEngine.Object.FindAnyObjectByType<SystemView>();
            float[] heights = { .40f, .50f, .60f, .70f, .80f };
            float[] xs = { -.32f, .24f, -.12f, .28f, -.26f };
            var random = new System.Random(3187);
            for (int i = 1; i < heights.Length - 1; i++)
                heights[i] += (float)(random.NextDouble() * 2 - 1) * .022f;
            for (int i = 0; i < system.planets.Length; i++)
            {
                var slot = system.planets[i].GetComponent<RectTransform>();
                var anchor = new Vector2(.5f + xs[i], SystemMapGraphic.OrbitY(xs[i], heights[i]));
                slot.anchorMin = slot.anchorMax = anchor;
                slot.sizeDelta = new Vector2(70, 70);
                var requirement = (RectTransform)slot.Find("Sphere Requirement");
                requirement.localScale = Vector3.one * .7f;
                requirement.anchoredPosition = new Vector2(0, -62);
                var orbit = system.transform.Find("Orbit " + (i + 1)).GetComponent<SystemMapGraphic>();
                orbit.orbitHeight = heights[i]; orbit.planetX = xs[i]; orbit.planetGapRadius = 34;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("ECOSPHERE_COMPACT_LAYOUT_SAVED");
            ValidateAndRender();
        }

        public static void PolishGeometry()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var system = UnityEngine.Object.FindAnyObjectByType<SystemView>();
            for (int i = 0; i < system.planets.Length; i++)
            {
                var ring = system.planets[i].GetComponent<SystemMapGraphic>();
                ring.thickness = 2; ring.selected = false;
                var orbit = system.transform.Find("Orbit " + (i + 1)).GetComponent<SystemMapGraphic>();
                orbit.planetSlot = system.planets[i].GetComponent<RectTransform>();
                orbit.thickness = 1.2f;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            ValidateAndRender();
            Debug.Log("ECOSPHERE_GEOMETRY_POLISHED: adaptive curves, feathered edges, exact circle joins.");
        }

        static void ConfigureMusic(GameObject camera)
        {
            MenuMusicTools.Configure();
        }

        [MenuItem("EcoSphere/Configure System Music")]
        public static void SetupMusic()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).First();
            ConfigureMusic(camera.gameObject);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log("ECOSPHERE_SYSTEM_MUSIC_CONFIGURED: system-background, looping, 2D, play on awake.");
        }

        public static void ValidateAndRender()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var controller = UnityEngine.Object.FindAnyObjectByType<SystemView>();
            if (controller.planets.Length != 5) throw new Exception("Expected five planets");
            controller.SendMessage("Awake");
            for (int i = 0; i < 5; i++)
            {
                controller.SelectPlanet(i);
                if (controller.SelectedPlanet != i) throw new Exception("Planet selection failed");
            }
            controller.selection.text = "Choose a Planet";
            foreach (var button in controller.planets)
            {
                var ring = button.GetComponent<SystemMapGraphic>();
                ring.color = controller.orbitColor; ring.thickness = 2; ring.selected = false;
            }
            var camera = Camera.main;
            var canvas = controller.GetComponent<Canvas>();
            var target = new RenderTexture(900, 1600, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(900, 1600, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 900, 1600), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/System-preview.png", image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("ECOSPHERE_SYSTEM_VALIDATED: five button callbacks passed; portrait preview rendered.");
        }

        public static void BuildValidateAndRender() { Build(); ValidateAndRender(); }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        static TMP_Text Label(string name, Transform parent, Vector2 anchor, string value, int size)
        {
            var text = Rect(name, parent, anchor, anchor, new Vector2(700, 70)).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            text.fontSize = size; text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.9f, .91f, .96f);
            text.raycastTarget = false;
            return text;
        }
    }
}
