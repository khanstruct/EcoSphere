using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere
{
    [Serializable] public sealed class DiscoveryArtwork
    {
        public int type;
        public string displayName;
        public Texture2D surface;
        public Sprite icon;
    }

    public sealed class ResultsView : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Sprite frame, line, arch, starfield;
        public GameObject buttonPrefab;
        public AudioClip elementDiscovered;
        public DiscoveryArtwork[] elements;
        public Button ContinueButton { get; private set; }
        public Button OkButton { get; private set; }
        public bool Settled { get; private set; }
        public int DiscoveryPlayCount { get; private set; }
        public Camera View { get; private set; }
        public Canvas LayoutCanvas { get; private set; }
        public RectTransform Root { get; private set; }
        public readonly List<RectTransform> DiscoverySpheres = new List<RectTransform>();
        readonly List<RectTransform> slots = new List<RectTransform>();
        readonly List<TMP_Text> names = new List<TMP_Text>();
        readonly List<TMP_Text> settledNames = new List<TMP_Text>();
        RectTransform imageFrame, descriptionFrame, collectionFrame, overlay, modal, header, planetLabel, flightLayer;
        CanvasGroup modalGroup;
        WorldResult result;
        Texture2D fillTexture;
        Sprite fillSprite;
        Vector2 previousSize;
        Rect previousSafe;

        RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            ScreenLayout.Place(rect, size, position);
            return rect;
        }
        Image Image(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position, Color color)
        {
            var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false;
            return image;
        }
        TMP_Text Text(string name, Transform parent, string value, int size, Vector2 bounds, Vector2 position)
        {
            var label = Rect(name, parent, bounds, position).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = value; label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, .97f, .86f);
            label.raycastTarget = false;
            return label;
        }
        RectTransform Frame(string name, Transform parent)
        {
            var image = Image(name, parent, frame, new Vector2(800, 300), Vector2.zero, Color.white);
            image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 6;
            var backing = Image("Backing", image.transform, fillSprite, new Vector2(750, 250), Vector2.zero,
                new Color(.012f, .025f, .05f, .98f));
            backing.transform.SetAsFirstSibling();
            return image.rectTransform;
        }
        Button Button(string name, Transform parent, Action action)
        {
            var go = Instantiate(buttonPrefab, parent); go.name = name;
            go.GetComponent<CelestialButton>().label.text = name;
            var button = go.GetComponent<Button>(); button.onClick.AddListener(() => action());
            return button;
        }

        void Start()
        {
            Time.timeScale = 1;
            result = WorldProgress.Pending ?? new WorldResult { world = new CompletedWorld {
                system = LevelPrepView.SystemNumber, planet = LevelPrepView.PlanetIndex } };
            fillTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) fillTexture.SetPixel(x, y, Color.white);
            fillTexture.Apply();
            fillSprite = Sprite.Create(fillTexture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            View = new GameObject("Results camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            View.transform.position = new Vector3(0, 0, -10); View.orthographic = true;
            View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(.01f, .02f, .04f);
            View.cullingMask = ~(1 << 30);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .5f, .6f);
            var light = new GameObject("Results light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0);
            LayoutCanvas = new GameObject("Results canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            LayoutCanvas.renderMode = RenderMode.ScreenSpaceCamera; LayoutCanvas.worldCamera = View; LayoutCanvas.planeDistance = 5;
            Root = Rect("Safe results layout", LayoutCanvas.transform, new Vector2(900, 1600), Vector2.zero);
            var bg = Image("Starfield", LayoutCanvas.transform, starfield, Vector2.zero, Vector2.zero, new Color(.6f, .65f, .8f));
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
            bg.transform.SetAsFirstSibling();
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio = starfield.rect.width / starfield.rect.height;
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            header = Text("System", Root, "SYSTEM " + result.world.system.ToString("D2"), 24, new Vector2(500, 40), Vector2.zero).rectTransform;
            planetLabel = Text("Planet", Root, WorldLevels.Name(result.world.system, result.world.planet) + " Created", 42, new Vector2(800, 65), Vector2.zero).rectTransform;
            float adornmentOffset = header.GetComponent<TMP_Text>().preferredWidth * .5f + 68;
            Image("Left ornament", header, line, new Vector2(170, 57), new Vector2(-adornmentOffset, 0), Color.white);
            Image("Right ornament", header, line, new Vector2(170, 57), new Vector2(adornmentOffset, 0), Color.white).transform.localScale = new Vector3(-1, 1, 1);
            Image("Arch", planetLabel, arch, new Vector2(500, 167), new Vector2(0, -47), Color.white);
            imageFrame = Frame("World image placeholder", Root);
            descriptionFrame = Frame("World description placeholder", Root);
            collectionFrame = Frame("Discovered elements", Root);
            Text("Collection heading", collectionFrame, "Elements Discovered", 28, new Vector2(700, 45), new Vector2(0, 85));
            ContinueButton = Button("Continue", Root, Continue);
            ContinueButton.interactable = result.discoveries.Count == 0;
            Settled = result.discoveries.Count == 0;
            BuildDiscovery();
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Reflow();
        }

        void BuildDiscovery()
        {
            int count = Mathf.Min(3, result.discoveries.Count);
            if (count == 0) return;
            overlay = Rect("Discovery overlay", LayoutCanvas.transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            var shield = overlay.gameObject.AddComponent<Image>(); shield.sprite = fillSprite;
            shield.color = new Color(0, .005f, .015f, .85f); shield.raycastTarget = true;
            modal = Frame("Discovery panel", overlay);
            modalGroup = modal.gameObject.AddComponent<CanvasGroup>();
            Text("Discovery title", modal, count == 1 ? "Element Discovered" : "Elements Discovered", 40,
                new Vector2(740, 70), Vector2.zero);
            for (int i = 0; i < count; i++)
            {
                var art = Array.Find(elements, e => e.type == result.discoveries[i]);
                if (art == null) throw new InvalidOperationException("Missing discovery artwork");
                var sphere = Rect(art.displayName + " discovered sphere", modal, Vector2.one * 170, Vector2.zero);
                sphere.gameObject.AddComponent<RawImage>().raycastTarget = false;
                var preview = sphere.gameObject.AddComponent<SpherePreview>(); preview.surface = art.surface; preview.icon = art.icon;
                DiscoverySpheres.Add(sphere);
                var name = Text(art.displayName + " name", modal, art.displayName, 36, new Vector2(430, 80), Vector2.zero);
                name.alignment = TextAlignmentOptions.Left; names.Add(name);
                var slot = Rect(art.displayName + " collected", collectionFrame, Vector2.one * 100, new Vector2((i - (count - 1) * .5f) * 190, -10));
                slots.Add(slot);
                var settledName = Text(art.displayName + " collected name", slot, art.displayName, 25, new Vector2(170, 40), new Vector2(0, -75));
                settledName.gameObject.SetActive(false); settledNames.Add(settledName);
            }
            OkButton = Button("Ok", modal, () => StartCoroutine(SettleDiscoveries()));
            flightLayer = Rect("Discovery animation", overlay, Vector2.zero, Vector2.zero);
            flightLayer.anchorMin = Vector2.zero; flightLayer.anchorMax = Vector2.one;
            var audio = gameObject.AddComponent<AudioSource>(); audio.playOnAwake = false;
            audio.spatialBlend = 0; audio.priority = 0; audio.ignoreListenerPause = true;
            if (elementDiscovered) { audio.PlayOneShot(elementDiscovered); DiscoveryPlayCount++; }
        }

        IEnumerator SettleDiscoveries()
        {
            if (!OkButton.interactable) yield break;
            OkButton.interactable = false;
            var starts = new List<Vector3>();
            foreach (var sphere in DiscoverySpheres)
            {
                starts.Add(sphere.position); sphere.SetParent(flightLayer, true);
            }
            const float duration = .8f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.SmoothStep(0, 1, t / duration);
                modalGroup.alpha = 1 - Mathf.Clamp01(t / .3f);
                for (int i = 0; i < DiscoverySpheres.Count; i++)
                {
                    DiscoverySpheres[i].position = Vector3.Lerp(starts[i], slots[i].position, p);
                    DiscoverySpheres[i].sizeDelta = Vector2.one * Mathf.Lerp(170, 100, p);
                }
                yield return null;
            }
            for (int i = 0; i < DiscoverySpheres.Count; i++)
            {
                var sphere = DiscoverySpheres[i]; sphere.SetParent(slots[i], false);
                sphere.anchoredPosition = Vector2.zero; sphere.sizeDelta = Vector2.one * 100;
                settledNames[i].gameObject.SetActive(true);
            }
            overlay.gameObject.SetActive(false);
            Settled = true; ContinueButton.interactable = true;
        }

        void Continue()
        {
            if (!Settled) return;
            LevelPrepView.SystemNumber = result.world.system;
            LevelPrepView.ReturningToSystem = true;
            WorldProgress.ClearPending();
            SceneManager.LoadScene("System");
        }

        void Update()
        {
            if (Root && (previousSize != new Vector2(Screen.width, Screen.height) || previousSafe != Screen.safeArea)) Reflow();
        }
        public void Reflow()
        {
            previousSize = new Vector2(Screen.width, Screen.height); previousSafe = Screen.safeArea;
            var m = ScreenLayout.Apply(LayoutCanvas, Root, View);
            ScreenLayout.Place(header, new Vector2(500, 40), new Vector2(0, m.height * .5f - 55));
            ScreenLayout.Place(planetLabel, new Vector2(800, 65), new Vector2(0, m.height * .5f - 110));
            void PlaceFrame(RectTransform rect, Vector2 size, Vector2 pos)
            {
                ScreenLayout.Place(rect, size, pos);
                ((RectTransform)rect.Find("Backing")).sizeDelta = size - Vector2.one * 42;
            }
            if (m.wide)
            {
                float width = (m.width - 140) * .5f;
                PlaceFrame(imageFrame, new Vector2(width, 520), new Vector2(-m.width * .25f, 0));
                PlaceFrame(descriptionFrame, new Vector2(width, 260), new Vector2(m.width * .25f, 130));
                PlaceFrame(collectionFrame, new Vector2(width, 260), new Vector2(m.width * .25f, -160));
            }
            else
            {
                float width = Mathf.Min(m.width - 60, 950);
                PlaceFrame(imageFrame, new Vector2(width, 480), new Vector2(0, 360));
                PlaceFrame(descriptionFrame, new Vector2(width, 280), new Vector2(0, -80));
                PlaceFrame(collectionFrame, new Vector2(width, 260), new Vector2(0, -420));
            }
            ScreenLayout.Place(ContinueButton.GetComponent<RectTransform>(), new Vector2(360, 120), new Vector2(m.width * .5f - 180, -m.height * .5f + 62));
            if (modal)
            {
                Canvas.ForceUpdateCanvases();
                float height = 260 + DiscoverySpheres.Count * 180;
                PlaceFrame(modal, new Vector2(Mathf.Min(820, m.width - 60), height), Vector2.zero);
                var title = (RectTransform)modal.Find("Discovery title");
                ScreenLayout.Place(title, new Vector2(modal.sizeDelta.x - 90, 70), new Vector2(0, height * .5f - 70));
                for (int i = 0; i < names.Count; i++)
                {
                    float y = height * .5f - 180 - i * 180;
                    if (DiscoverySpheres[i].parent == modal) DiscoverySpheres[i].anchoredPosition = new Vector2(-modal.sizeDelta.x * .28f, y);
                    ScreenLayout.Place(names[i].rectTransform, new Vector2(modal.sizeDelta.x * .5f, 80), new Vector2(modal.sizeDelta.x * .12f, y));
                }
                ScreenLayout.Place(OkButton.GetComponent<RectTransform>(), new Vector2(360, 120), new Vector2(0, -height * .5f + 70));
            }
        }

        void OnDestroy() { if (fillSprite) Destroy(fillSprite); if (fillTexture) Destroy(fillTexture); }
    }
}
