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
        static Vector2Int TutorialStoneCell => StoneTutorialLayout.StoneCell(InitialRings);
        int TutorialOuterType => StoneTutorialLayout.HolderType(activeSphereTypes);
        // 2: release Stone by clearing its outer support; 4: finish normally.
        int tutorialStage = 2, pendingTutorial = -1, tutorialMessage;
        bool tutorialPaused;
        int tutorialResumeFrame = -1;
        Canvas tutorialCanvas;
        RectTransform tutorialRoot, tutorialBox, tutorialLeader, tutorialTip;
        TMP_Text tutorialText;
        Button tutorialContinue;
        Texture2D tutorialBackingTexture, tutorialSolidTexture;
        bool TutorialTesting => StoneTutorial && Array.IndexOf(Environment.GetCommandLineArgs(), "-tutorialSmoke") >= 0;
        readonly string[] tutorialMessages = {
            "Your goal is to match and release elements to create a new planet.",
            "In the center of the cannon, you can see the sphere that is currently loaded. Aim, then tap to shoot. Connect 3 or more matching spheres to clear them.",
            "Some levels contain naturally occurring elements. Clear the spheres around them to release them.",
            "Clear the rest of the spheres from the core to complete the level."
        };

        void AdvanceTutorial(HashSet<Vector2Int> released)
        {
            if (!StoneTutorial || LaserTesting) return;
            if (tutorialStage == 2 && released.Contains(TutorialStoneCell))
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
            UIInput.EnsureEventSystem();
        }

        void ShowTutorial(int message)
        {
            if(DevelopmentMode.Enabled) { pendingTutorial=-1; return; }
            if (!tutorialCanvas) BuildTutorialInterface();
            pendingTutorial = -1;
            tutorialMessage = message;
            tutorialText.text = tutorialMessages[message];
            tutorialRoot.gameObject.SetActive(true);
            if (!tutorialPaused) GameServices.Pause.Acquire(this);
            tutorialPaused = true;

            aim.enabled = false;
            UpdateTutorialLayout();
        }

        void ContinueTutorial()
        {
            if (!tutorialPaused) return;
            if (tutorialMessage == 0) { ShowTutorial(1); return; }
            tutorialRoot.gameObject.SetActive(false);
            tutorialPaused = false;
            GameServices.Pause.Release(this);
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
            if(tutorialMessage==1)
                tutorialBox.anchoredPosition=new Vector2(x,Mathf.Clamp(end.y+450,-tutorialRoot.rect.height*.5f+250,tutorialRoot.rect.height*.5f-250));
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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tutorialTwoNavigationSmoke") >= 0 &&
                (activeSphereTypes[0] != SphereIds.Sunlight || activeSphereTypes[1] != SphereIds.Fire || activeSphereTypes[2] != SphereIds.Void))
                throw new Exception("Tutorial 2 discarded the actual setup selection order");
            if (!tutorialFrame || !tutorialButtonPrefab || !Definition(StoneType).surface || !Definition(StoneType).icon)
                throw new Exception("Tutorial artwork is missing");
            foreach (var cell in board.Cells)
            {
                int ring = Mathf.Max(Mathf.Abs(cell.Key.x), Mathf.Abs(cell.Key.y), Mathf.Abs(cell.Key.x + cell.Key.y));
                int expected = cell.Key == TutorialStoneCell ? StoneType : ring == InitialRings || cell.Key==StoneTutorialLayout.SupportCell(InitialRings) ? TutorialOuterType : cell.Value;
                if (cell.Value != expected) throw new Exception("Incorrect tutorial ring arrangement");
                if (cell.Key != TutorialStoneCell && Array.IndexOf(activeSphereTypes, cell.Value) < 0)
                    throw new Exception("Tutorial used a sphere outside the selected loadout");
            }
            foreach (int type in activeSphereTypes)
                if (!board.Cells.ContainsValue(type)) throw new Exception("Selected type missing from tutorial cluster");
            foreach(var direction in BoardRules.Directions)
                if(board.Cells.TryGetValue(TutorialStoneCell+direction,out int neighbor) && neighbor!=TutorialOuterType)
                    throw new Exception("Stone has a connection outside its guided support match");
            if (!tutorialPaused || !tutorialCanvas || tutorialMessage!=2 || Time.timeScale!=0 || loadedType!=TutorialOuterType)
                throw new Exception("Genesis Beta must open with the natural-element lesson and supporting ammo");
            CaptureTutorialPreview("stone-opening");
            int beforeShots=shots; Fire();
            if(shots!=beforeShots)throw new Exception("Opening Stone modal allowed firing");
            if(displayedElements.Contains(ElementId.Stone))
                throw new Exception("Naturally occurring Stone added a visualizer channel");
            ClickTutorialForSmoke(); yield return null;
            for(int miss=0;miss<2;miss++)
            {
                direction=Vector2.down; Fire();
                float missedDeadline=Time.realtimeSinceStartup+10;
                while(!ready && Time.realtimeSinceStartup<missedDeadline)yield return null;
                if(!ready || loadedType!=TutorialOuterType || tutorialStage!=2 || !board.Cells.ContainsKey(TutorialStoneCell))
                    throw new Exception("Miss failed to retain the supporting ammo until Stone clears");
            }
            direction=SmokeMatchingDirection(); gun.up=direction; Fire();
            float deadline=Time.realtimeSinceStartup+15;
            while(!tutorialPaused && Time.realtimeSinceStartup<deadline)yield return null;
            if(!tutorialPaused || tutorialMessage!=3 || board.Cells.ContainsKey(TutorialStoneCell) || !stoneReleased || tutorialStage!=4 || board.Cells.Count==0)
                throw new Exception("One outer-ring match must release Stone and leave a mixed interior");
            CaptureTutorialPreview("stone-released");
            ClickTutorialForSmoke();
            if(Time.timeScale!=1)throw new Exception("Final lesson did not resume");
            // On the final stage, use the ordinary board-aware selection and its repeat cap.
            var remaining = new HashSet<int>(ElementTypes());
            int previous = -1, consecutive = 0;
            for (int i = 0; i < 30; i++)
            {
                if (loaded) Destroy(loaded);
                LoadNext();
                if (!remaining.Contains(loadedType)) throw new Exception("Post-lesson ammo used an absent type");
                consecutive = loadedType == previous ? consecutive + 1 : 1; previous = loadedType;
                if (remaining.Count > 1 && consecutive > 3) throw new Exception("Post-lesson ammo bypassed normal repeat cap");
            }
            Debug.Log("ECOSPHERE_TUTORIAL_SMOKE_PASSED: opening natural-element lesson, outer Stone/support topology, selected mixed interior, repeated supporting ammo after two misses, single-match release and normal remaining ammo");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        IEnumerator IntroTutorialSmokeRun()
        {
            if (!tutorialPaused || !tutorialCanvas || tutorialMessage != 0 || Time.timeScale != 0 ||
                tutorialText.text != tutorialMessages[0]) throw new Exception("Genesis Alpha did not open with the welcome lesson");
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
            if (!tutorialPaused || tutorialMessage != 1 || tutorialText.text != tutorialMessages[1] ||
                !tutorialLeader.gameObject.activeSelf || !tutorialTip.gameObject.activeSelf)
                throw new Exception("Cannon lesson or its loaded-ammo pointer is missing");
            if(tutorialLeader.sizeDelta.x<160)throw new Exception("Cannon lesson needs a clearly visible pointer line");
            yield return new WaitForSecondsRealtime(.1f);
            CaptureTutorialPreview("cannon");
            yield return new WaitForSecondsRealtime(.2f);
            ClickTutorialForSmoke();
            if (tutorialPaused || Time.timeScale != 1) throw new Exception("Continue did not resume gameplay");
            yield return null;
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
