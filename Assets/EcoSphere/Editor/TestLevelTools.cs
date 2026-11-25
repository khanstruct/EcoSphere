using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere.Editor
{
    public static class TestLevelTools
    {
        public const string ScenePath = "Assets/EcoSphere/Scenes/FirstLight.unity";
        const string Art = "Assets/EcoSphere/Art/";

        [MenuItem("EcoSphere/Open Test Level")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("EcoSphere/Rebuild Test Level")]
        public static void Build()
        {
            SphereCatalogTools.Configure();
            GameAudioTools.Configure();
            Directory.CreateDirectory("Assets/EcoSphere/Scenes");
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(normalized);
                bool surface = normalized.Contains("surface") || normalized.Contains("heightmap");
                importer.textureType = surface ? TextureImporterType.Default : TextureImporterType.Sprite;
                importer.alphaIsTransparency = !surface;
                importer.mipmapEnabled = surface;
                importer.wrapMode = surface ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.maxTextureSize = normalized.Contains("/Backgrounds/starfield-") ? 2048 : 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (!surface) { importer.spriteImportMode = SpriteImportMode.Single; }
                importer.SaveAndReimport();
            }
            var previous = SceneManager.GetActiveScene();
            if (previous.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool replaceEmpty = string.IsNullOrEmpty(previous.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceEmpty ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("EcoSphere Test Level").AddComponent<EcoSpherePrototype>();
            root.abundanceGoal = 3000000;
            root.uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTools.FontAssetPath);
            root.receivingPlanetMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoSphere/Art/Planets/barren-planet-v1.mat");
            root.tutorialFrame = Sprite("UI/Frames/panel-frame-v1.png");
            root.tutorialButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoSphere/Prefabs/UI/CelestialButton.prefab");
            root.leftArm = Sprite("Cannon/left-arm-v1.png");
            root.rightArm = Sprite("Cannon/right-arm-v1.png");
            root.foreground = Sprite("Cannon/main-foreground-v1.png");
            root.starfield = Sprite("Backgrounds/starfield-v2.png");
            root.twinkleStar = Sprite("Backgrounds/twinkle-star-v1.png");
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceEmpty)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var levelEntry = scenes.Find(entry => entry.path == ScenePath);
            if (levelEntry == null) scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            else levelEntry.enabled = true;
            var systemEntry = scenes.Find(entry => entry.path == SystemSceneTools.ScenePath);
            if (systemEntry != null) { scenes.Remove(systemEntry); scenes.Insert(0,systemEntry); }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ECOSPHERE_SCENE_CREATED " + ScenePath);
        }

        static Texture2D Texture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + path);
            if (!texture) throw new Exception("Missing texture: " + path);
            return texture;
        }

        static Sprite Sprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
            if (!sprite) throw new Exception("Missing sprite: " + path);
            return sprite;
        }

        public static void BuildAndValidate() { Validate(); Build(); }

        public static void SetupBackgroundAndSmoke()
        {
            AssetDatabase.Refresh();
            foreach (string name in new[] { "starfield-v2.png", "twinkle-star-v1.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "Backgrounds/" + name);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = name.Contains("twinkle");
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = name.Contains("starfield") ? 2048 : 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            EditorSceneManager.OpenScene(ScenePath);
            var root = UnityEngine.Object.FindFirstObjectByType<EcoSpherePrototype>();
            root.starfield = Sprite("Backgrounds/starfield-v2.png");
            root.twinkleStar = Sprite("Backgrounds/twinkle-star-v1.png");
            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(root.gameObject.scene);
            RunSmoke();
        }

        public static void RunSmoke()
        {
            Validate();
            if (!File.Exists(ScenePath)) BuildAndValidate();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("EcoSphere/Validate Board Rules")]
        public static void Validate()
        {
            int checks = 0;
            void Expect(bool condition, string message)
            {
                if (!condition) throw new Exception("EcoSphere validation: " + message);
                checks++;
            }
            var board = new BoardRules();
            var a = new Vector2Int(1, 0); var b = new Vector2Int(2, 0); var c = new Vector2Int(3, 0);
            board.Cells[a] = board.Cells[b] = board.Cells[c] = 0;
            var isolatedShot = new Vector2Int(-1, 0); board.Cells[isolatedShot] = 1;
            Expect(board.MatchFrom(isolatedShot).Count == 0, "An unrelated shot must not clear a pre-existing triple");
            Expect(board.Cells.Count == 4, "Detection must not mutate the board");
            var fired = new Vector2Int(2, -1); board.Cells[fired] = 0;
            Expect(board.MatchFrom(fired).Count == 4, "A shot joining an existing group must clear the whole same-type component");
            board.Cells.Clear(); board.Cells[a] = 0; board.Cells[b] = 1; board.Cells[c] = 1;
            Expect(board.Disconnected().Count == 0, "Mixed types connect through the core");
            board.Cells.Remove(a);
            Expect(board.Disconnected().SetEquals(new[] { b, c }), "Removing a bridge releases all unsupported spheres");
            board.Cells.Clear();
            foreach (var d in BoardRules.Directions) board.Cells[d] = 0;
            Expect(board.Cells.Count == 6 && board.Disconnected().Count == 0, "Core must support all six attachment slots");
            foreach (var d in BoardRules.Directions)
                Expect(Mathf.Abs(BoardRules.Position(d).magnitude - BoardRules.Spacing) < .0001f, "Six grid neighbors must be equally spaced");
            Expect(BoardRules.Sweep(new Vector2(0, -10), Vector2.up, Vector2.zero, .88f, 20, out float hit)
                && Mathf.Abs(hit - 9.12f) < .001f, "Fast shot must find its first contact without tunneling");
            Expect(!BoardRules.Sweep(new Vector2(2, -10), Vector2.up, Vector2.zero, .88f, 20, out _), "A clear miss must remain a miss");
            Expect(!BoardRules.Sweep(new Vector2(0, -10), Vector2.down, Vector2.zero, .88f, 20, out _), "A sphere behind the projectile cannot be hit");
            Vector2 reflected = BoardRules.Reflect(new Vector2(.6f, .8f), Vector2.left);
            Expect((reflected - new Vector2(-.6f, .8f)).sqrMagnitude < .0001f, "Side ricochet must preserve vertical travel");
            Expect((BoardRules.Reflect(Vector2.up, Vector2.down) - Vector2.down).sqrMagnitude < .0001f, "Top ricochet must return downward");
            Debug.Log("ECOSPHERE_RULE_CHECKS_PASSED " + checks);
            float radius = EcoSpherePrototype.Radius;
            var path = BoardRules.AimPath(new Vector2(0, -5), new Vector2(.97f, .26f), 4.35f,
                -4.35f + radius, 4.35f - radius, 6.5f - radius, -5.9f);
            Expect(path.Count == 3, "Half-width guide must show a first side ricochet at shallow angles");
            Expect(Mathf.Abs(path[1].x - (4.35f - radius)) < .001f && path[2].x < path[1].x,
                "Guide must reflect at the projectile center's wall boundary");
            float pathLength = 0;
            for (int i = 1; i < path.Count; i++) pathLength += Vector2.Distance(path[i - 1], path[i]);
            Expect(Mathf.Abs(pathLength - 4.35f) < .001f, "Reflected guide must preserve its total half-width length");
            path = BoardRules.AimPath(new Vector2(0, 6), Vector2.up, 2,
                -4.35f + radius, 4.35f - radius, 6.5f - radius, -5.9f);
            Expect(path.Count == 3 && path[2].y < path[1].y, "Guide must reflect downward off the top");
            Debug.Log("ECOSPHERE_GUIDE_CHECKS_PASSED 4");
            path = BoardRules.AimPath(new Vector2(0, -3), Vector2.up, 8,
                -4, 4, 6, -6, new[] { new Vector2(0, 2), Vector2.zero }, radius * 2);
            Expect(path.Count == 2 && Mathf.Abs(path[1].y + radius * 2) < .001f,
                "Guide must stop at the nearest sphere's swept contact, regardless of collection order");
            path = BoardRules.AimPath(Vector2.zero, new Vector2(1, 1), 12,
                -4, 4, 8, -6, new[] { new Vector2(2, 6) }, radius * 2);
            Expect(path.Count == 3 && Vector2.Distance(path[2], new Vector2(2, 6)) <= radius * 2 + .001f,
                "Guide must stop at a sphere encountered after a wall ricochet");
            Debug.Log("ECOSPHERE_SPHERE_GUIDE_CHECKS_PASSED 2");
            var selector = new ShotSelector(new System.Random(17));
            for (int i = 0; i < 3; i++)
                Expect(selector.TryNext(new[] { 0 }, out int only) && only == 0, "Single remaining type must stay playable");
            Expect(selector.TryNext(new[] { 0, 0, 0, 1 }, out int forced) && forced == 1,
                "After three identical loads an available alternative must be selected");
            Expect(selector.TryNext(new[] { 0 }, out int remaining) && remaining == 0,
                "An eliminated type must never be selected");
            Expect(!selector.TryNext(new int[0], out _), "Empty board must not generate a shot");
            int previous = -1, streak = 0;
            for (int i = 0; i < 200; i++)
            {
                selector.TryNext(new[] { 0, 0, 1 }, out int selected);
                streak = selected == previous ? streak + 1 : 1; previous = selected;
                Expect(streak <= 3, "Mixed board must never load four identical shots consecutively");
            }
            Debug.Log("ECOSPHERE_SHOT_SELECTION_CHECKS_PASSED");
            Expect(BoardRules.TouchesEdge(new Vector2(4 - radius, 0), radius, -4, 4, 6, -6),
                "Exact edge tangency must lose, not only overlap");
            Expect(!BoardRules.TouchesEdge(Vector2.zero, radius, -4, 4, 6, -6), "Interior cluster must remain safe");
            Expect(BoardRules.TouchesEdge(new Vector2(0, -6 + radius), radius, -4, 4, 6, -6), "Bottom contact must lose");
            Expect(BoardRules.CircleTouchesTriangle(new Vector2(.25f, .25f), .1f, Vector2.zero, Vector2.right, Vector2.up),
                "Sphere within cannon triangle must touch");
            Expect(BoardRules.CircleTouchesTriangle(new Vector2(-.1f, 0), .1f, Vector2.zero, Vector2.right, Vector2.up),
                "Cannon corner tangency must touch");
            Expect(!BoardRules.CircleTouchesTriangle(new Vector2(2, 2), .1f, Vector2.zero, Vector2.right, Vector2.up),
                "Empty space outside cannon geometry must not touch");
            Debug.Log("ECOSPHERE_CONTACT_CHECKS_PASSED 6");
            Expect(LevelAudio.ResourceCallout(new[] { LevelAudio.SphereVoiceMinimumPoints - 1, LevelAudio.SphereVoiceMinimumPoints - 1 }) == -1, "Subthreshold gains must not announce");
            Expect(LevelAudio.ResourceCallout(new[] { LevelAudio.SphereVoiceMinimumPoints, 0 }) == 0, "Exactly the audio threshold of Ice must announce Cryosphere");
            Expect(LevelAudio.ResourceCallout(new[] { LevelAudio.SphereVoiceMinimumPoints, LevelAudio.SphereVoiceMinimumPoints + 50 }) == LevelAudio.MixedResourceCallout, "Two unequal qualifying gains must announce EcoSphere");
            Expect(LevelAudio.ResourceCallout(new[] { LevelAudio.SphereVoiceMinimumPoints + 50, LevelAudio.SphereVoiceMinimumPoints + 50 }) == LevelAudio.MixedResourceCallout, "Equal qualifying gains must announce EcoSphere");
            Debug.Log("ECOSPHERE_RESOURCE_VOICE_CHECKS_PASSED 4");
            int tied = LevelAudio.ResourceCallout(new[] { LevelAudio.SphereVoiceMinimumPoints - 1, LevelAudio.SphereVoiceMinimumPoints + 100, LevelAudio.SphereVoiceMinimumPoints + 100, LevelAudio.SphereVoiceMinimumPoints });
            Expect(tied == LevelAudio.MixedResourceCallout, "Any two or more qualifying resources must announce EcoSphere");
            Expect(LevelAudio.ImpressiveChance(0, 5, false) == 0, "Bank shots without matches must never announce Impressive");
            Expect(LevelAudio.ImpressiveChance(3, 0, false) == 0, "Direct shots must never announce Impressive");
            Expect(LevelAudio.ImpressiveChance(3, 4, true) == 0, "Other selected voices must suppress Impressive");
            Expect(Mathf.Abs(LevelAudio.ImpressiveChance(3, 1, false) - .2f) < .0001f, "One bank must give 20 percent chance");
            Expect(Mathf.Abs(LevelAudio.ImpressiveChance(3, 2, false) - .36f) < .0001f, "Two banks must give two independent opportunities, capped at one callout");
            Debug.Log("ECOSPHERE_IMPRESSIVE_CHECKS_PASSED 5");
        }
    }
}
