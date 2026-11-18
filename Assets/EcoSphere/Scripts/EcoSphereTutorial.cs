using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        public Sprite tutorialFrame;
        public GameObject tutorialButtonPrefab;
        static readonly Vector2Int TutorialStoneCell = new Vector2Int(0, -3);
        // 2: Sunlight matching; 3: Ice and Stone release; 4: finish normally.
        int tutorialStage = 2, pendingTutorial = -1, tutorialMessage;
        bool tutorialPaused;
        float tutorialPreviousScale;
        int tutorialResumeFrame = -1;
        Canvas tutorialCanvas;
        RectTransform tutorialRoot, tutorialBox, tutorialLeader, tutorialTip;
        TMP_Text tutorialText;
        Button tutorialContinue;
        Texture2D tutorialBackingTexture, tutorialSolidTexture;
        bool TutorialTesting => Array.IndexOf(Environment.GetCommandLineArgs(), "-tutorialSmoke") >= 0;
        readonly string[] tutorialMessages = {
            "Welcome to EcoSphere. Your goal is to release elements to form new planets.",
            "This shows which sphere you have loaded in your cannon. Aim the cannon and launch the sphere at the center cluster. Connect 3 or more matching spheres to clear them.",
            "Some levels contain naturally occurring elements. Clear the spheres around them to release them.",
            "Clear the rest of the spheres from the core to complete the level."
        };

        void AdvanceTutorial(int matchCount, HashSet<Vector2Int> released)
        {
            if (!StoneTutorial || LaserTesting) return;
            if (tutorialStage == 2 && matchCount >= 3)
            {
                tutorialStage = 3;
                pendingTutorial = 2;
            }
            if (tutorialStage == 3 && released.Contains(TutorialStoneCell))
            {
                tutorialStage = 4;
                pendingTutorial = 3;
            }
        }

        void BuildTutorialInterface()
        {
            tutorialCanvas = new GameObject("Tutorial overlay", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster)).GetComponent<Canvas>();
            tutorialCanvas.transform.SetParent(transform, false);
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            tutorialCanvas.worldCamera = view;
            tutorialCanvas.planeDistance = 2;
            tutorialCanvas.sortingOrder = 100;
            var scaler = tutorialCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600);
            tutorialRoot = Panel("Modal input shield", tutorialCanvas.transform, Vector2.one * .5f,
                Vector2.zero, Vector2.zero, new Color(0, .008f, .02f, .32f));
            tutorialRoot.anchorMin = Vector2.zero; tutorialRoot.anchorMax = Vector2.one;
            tutorialSolidTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) tutorialSolidTexture.SetPixel(x, y, Color.white);
            tutorialSolidTexture.Apply();
            var solid = Sprite.Create(tutorialSolidTexture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            generatedSprites.Add(solid);
            tutorialRoot.GetComponent<Image>().sprite = solid;
            tutorialRoot.GetComponent<Image>().raycastTarget = true;
            tutorialLeader = Panel("Gold pointer", tutorialRoot, Vector2.one * .5f,
                Vector2.zero, Vector2.zero, new Color(1, .79f, .36f));
            tutorialLeader.pivot = new Vector2(0, .5f);
            tutorialLeader.GetComponent<Image>().sprite = solid;
            tutorialTip = Panel("Pointer endpoint", tutorialRoot, Vector2.one * .5f,
                Vector2.zero, Vector2.one * 12, new Color(1, .85f, .48f));
            tutorialTip.localRotation = Quaternion.Euler(0, 0, 45);
            tutorialTip.GetComponent<Image>().sprite = solid;
            tutorialBox = Panel("Tutorial rounded frame", tutorialRoot, Vector2.one * .5f,
                Vector2.zero, new Vector2(740, 450), Color.clear);
            // A solid interior keeps the text readable beneath the transparent frame artwork.
            var fill = Panel("Rounded interior", tutorialBox, Vector2.one * .5f,
                Vector2.zero, tutorialBox.sizeDelta - Vector2.one * 125, new Color(.015f, .025f, .045f, .98f)).GetComponent<Image>();
            tutorialBackingTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                var nearest = new Vector2(Mathf.Clamp(x, 10, 53), Mathf.Clamp(y, 10, 53));
                float alpha = Mathf.Clamp01(10 - Vector2.Distance(new Vector2(x, y), nearest));
                tutorialBackingTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            tutorialBackingTexture.Apply();
            var rounded = Sprite.Create(tutorialBackingTexture, new Rect(0, 0, 64, 64), Vector2.one * .5f,
                25, 0, SpriteMeshType.FullRect, Vector4.one * 12);
            generatedSprites.Add(rounded);
            fill.sprite = rounded; fill.type = Image.Type.Sliced;
            var border = Panel("Frame artwork", tutorialBox, Vector2.one * .5f,
                Vector2.zero, tutorialBox.sizeDelta, Color.white).GetComponent<Image>();
            border.sprite = tutorialFrame; border.type = Image.Type.Sliced;
            border.pixelsPerUnitMultiplier = 2;
            tutorialText = Label("Tutorial message", tutorialBox, Vector2.one * .5f,
                new Vector2(0, 50), new Vector2(590, 245), "", 30);
            tutorialText.color = new Color(1, .96f, .85f);
            var button = Instantiate(tutorialButtonPrefab, tutorialBox);
            button.name = "Continue";
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
            rect.anchoredPosition = new Vector2(0, -125);
            rect.sizeDelta = new Vector2(360, 120);
            var buttonLabel = button.GetComponent<CelestialButton>().label;
            buttonLabel.text = "Continue";
            buttonLabel.rectTransform.sizeDelta = new Vector2(-70, -40);
            tutorialContinue = button.GetComponent<Button>();
            tutorialContinue.onClick.AddListener(ContinueTutorial);
            if (!FindAnyObjectByType<EventSystem>())
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        void ShowTutorial(int message)
        {
            if (!tutorialCanvas) BuildTutorialInterface();
            pendingTutorial = -1;
            tutorialMessage = message;
            tutorialText.text = tutorialMessages[message];
            tutorialRoot.gameObject.SetActive(true);
            if (!tutorialPaused) tutorialPreviousScale = Time.timeScale;
            tutorialPaused = true;
            Time.timeScale = 0;
            aim.enabled = false;
            UpdateTutorialLayout();
        }

        void ContinueTutorial()
        {
            if (!tutorialPaused) return;
            if (tutorialMessage == 0) { ShowTutorial(1); return; }
            tutorialRoot.gameObject.SetActive(false);
            tutorialPaused = false;
            Time.timeScale = tutorialPreviousScale;
            // The same click must never fire a shot through the dismissed modal.
            tutorialResumeFrame = Time.frameCount;
        }

        void LateUpdate()
        {
            if (tutorialPaused) UpdateTutorialLayout();
        }

        void UpdateTutorialLayout()
        {
            var scaler = tutorialCanvas.GetComponent<CanvasScaler>();
            scaler.matchWidthOrHeight = (float)Screen.width / Screen.height < 900f / 1600 ? 0 : 1;
            Canvas.ForceUpdateCanvases();
            // Keep a little breathing room on narrow phones while using a livelier offset on wider screens.
            float offset = Mathf.Min(160, Mathf.Max(0, (tutorialRoot.rect.width - tutorialBox.sizeDelta.x) * .5f - 24));
            float x = tutorialMessage == 0 ? -offset : tutorialMessage == 1 ? offset : 0;
            tutorialBox.anchoredPosition = new Vector2(x, tutorialMessage == 2 ? -240 : 0);
            Transform target = tutorialMessage == 1 && loaded ? loaded.transform :
                tutorialMessage == 2 && visuals.TryGetValue(TutorialStoneCell, out var stone) ? stone.transform : null;
            tutorialLeader.gameObject.SetActive(target);
            tutorialTip.gameObject.SetActive(target);
            if (!target) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tutorialRoot,
                view.WorldToScreenPoint(target.position), view, out var end);
            Vector2 delta = end - tutorialBox.anchoredPosition;
            // The frame artwork has transparent padding inside the box rectangle.
            float edge = Mathf.Min(306 / Mathf.Max(.001f, Mathf.Abs(delta.x)),
                161 / Mathf.Max(.001f, Mathf.Abs(delta.y)));
            Vector2 start = tutorialBox.anchoredPosition + delta * edge;
            tutorialLeader.anchoredPosition = start;
            tutorialLeader.sizeDelta = new Vector2(Vector2.Distance(start, end), 7);
            tutorialLeader.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            tutorialTip.anchoredPosition = end;
        }

        IEnumerator TutorialSmokeRun()
        {
            yield return new WaitForSecondsRealtime(.3f);
            if (!tutorialFrame || !tutorialButtonPrefab || !stoneSurface || !stoneIcon)
                throw new Exception("Tutorial artwork is missing");
            foreach (var cell in board.Cells)
            {
                int ring = Mathf.Max(Mathf.Abs(cell.Key.x), Mathf.Abs(cell.Key.y), Mathf.Abs(cell.Key.x + cell.Key.y));
                int expected = cell.Key == TutorialStoneCell ? StoneType : ring == 4 ? 1 : ring >= 2 ? 0 : cell.Value;
                if (cell.Value != expected) throw new Exception("Incorrect tutorial ring arrangement");
            }
            if (!tutorialPaused || Time.timeScale != 0 || loadedType != 1) throw new Exception("Welcome did not pause with Sunlight loaded");
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(view, tutorialContinue.transform.position)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(tutorialContinue.transform))
                throw new Exception("Continue is not reachable through the modal input shield");
            CaptureTutorialPreview("welcome");
            yield return new WaitForSecondsRealtime(.2f);
            int beforeShots = shots;
            Fire();
            if (shots != beforeShots) throw new Exception("Modal allowed firing");
            ClickTutorialForSmoke();
            if (!tutorialPaused || tutorialMessage != 1) throw new Exception("Cannon lesson missing");
            yield return new WaitForSecondsRealtime(.1f);
            CaptureTutorialPreview("cannon");
            yield return new WaitForSecondsRealtime(.2f);
            ClickTutorialForSmoke();
            if (tutorialPaused || Time.timeScale != 1) throw new Exception("Continue did not resume gameplay");
            yield return null;
            // A lost shot must retain Sunlight and must not advance the lesson.
            direction = Vector2.down; Fire();
            float deadline = Time.realtimeSinceStartup + 10;
            while (!ready && Time.realtimeSinceStartup < deadline) yield return null;
            if (!ready || loadedType != 1 || tutorialStage != 2) throw new Exception("Miss did not reload Sunlight");
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            deadline = Time.realtimeSinceStartup + 15;
            while (!tutorialPaused && Time.realtimeSinceStartup < deadline) yield return null;
            if (!tutorialPaused || tutorialMessage != 2 || loadedType != 0 || !visuals.ContainsKey(TutorialStoneCell))
                throw new Exception("First match did not show Stone lesson with Ice loaded");
            CaptureTutorialPreview("stone");
            yield return new WaitForSecondsRealtime(.2f);
            ClickTutorialForSmoke();
            yield return null;
            direction = Vector2.down; Fire();
            deadline = Time.realtimeSinceStartup + 10;
            while (!ready && Time.realtimeSinceStartup < deadline) yield return null;
            if (!ready || loadedType != 0 || tutorialStage != 3) throw new Exception("Ice lesson did not retain Ice after a miss");
            int iceBefore = totals[0], sunBefore = totals[1];
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            deadline = Time.realtimeSinceStartup + 15;
            while (!tutorialPaused && Time.realtimeSinceStartup < deadline) yield return null;
            if (!tutorialPaused || tutorialMessage != 3 || board.Cells.ContainsValue(StoneType) || tutorialStage != 4)
                throw new Exception("Stone release did not trigger final lesson");
            if (totals[0] < iceBefore || totals[1] < sunBefore) throw new Exception("Release damaged resource totals");
            ClickTutorialForSmoke();
            if (Time.timeScale != 1) throw new Exception("Final lesson did not resume");
            Debug.Log("ECOSPHERE_TUTORIAL_SMOKE_PASSED: pause, Continue, missed shot, Sunlight match, Ice layers, Stone release, final lesson");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        void ClickTutorialForSmoke()
        {
            int before = UIClickAudio.PlayCount;
            tutorialContinue.onClick.Invoke();
            if (UIClickAudio.PlayCount != before + 1)
                throw new Exception("Tutorial Continue did not play exactly one click sound");
        }
        void CaptureTutorialPreview(string name)
        {
            var target = new RenderTexture(Screen.width, Screen.height, 24);
            var previousTarget = view.targetTexture;
            var previousActive = RenderTexture.active;
            view.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            view.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/tutorial-" + name + ".png"), pixels.EncodeToPNG());
            view.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release(); Destroy(target); Destroy(pixels);
        }
    }

}
