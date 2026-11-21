using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed class SystemView : MonoBehaviour
    {
        public Button[] planets;
        public TMP_Text selection;
        [Min(1)] public int systemNumber = 1;
        public TMP_Text systemLabel;
        public RectTransform leftAdornment, rightAdornment;
        public Material completedWorldMaterial;
        [Min(0), Tooltip("Completed planet rotation in degrees per second.")]
        public float completedPlanetRotationSpeed = 8f;
        public Sprite tutorialFrame;
        public GameObject tutorialButtonPrefab;
        public SystemDialog Dialog { get; private set; }
        int dialogDismissFrame = -1;
        int displayedSystem = -1;
        public Color orbitColor = new Color(1, .76f, .24f);
        public int SelectedPlanet { get; private set; } = -1;

        void Awake()
        {
            if (Camera.main) Camera.main.cullingMask &= ~(1 << 30);
            if (!GetComponent<SystemResponsiveLayout>()) gameObject.AddComponent<SystemResponsiveLayout>();
            if (LevelPrepView.ReturningToSystem) { systemNumber = LevelPrepView.SystemNumber; LevelPrepView.ReturningToSystem = false; }
            else systemNumber = WorldProgress.CurrentSystem;
            RefreshSystemHeading();
            for (int i = 0; i < planets.Length; i++)
            {
                int index = i;
                ButtonClickSound.Attach(planets[i]);
                planets[i].onClick.AddListener(() => OpenPlanet(index));
            }
        }

        void Start()
        {
            RefreshCompletedWorlds();
            if (systemNumber == 1 && !WorldProgress.SystemIntroductionSeen && WorldProgress.Find(1,0) == null && WorldProgress.AvailableSphereCount == 2)
                EnsureDialog().ShowIntroduction();
            else if(WorldProgress.TryGetNextWorldTutorial(systemNumber,out var source,out var definition,out int target) && target<planets.Length)
                EnsureDialog().ShowNextWorld(source,planets[target].transform,definition.nextWorldTutorialMessage);
        }

        SystemDialog EnsureDialog()
        {
            if (!Dialog)
            {
                Dialog = new GameObject("System dialog overlay").AddComponent<SystemDialog>();
                Dialog.Initialize(this, tutorialFrame, tutorialButtonPrefab, selection.font);
            }
            return Dialog;
        }
        internal void DialogDismissed() { dialogDismissFrame = Time.frameCount; }
        internal void DismissIntroductionForSmoke()
        {
            while (Dialog && Dialog.IsOpen && Dialog.IsIntroduction) Dialog.ContinueButton.onClick.Invoke();
        }
        void OnDestroy() { if (Dialog) Destroy(Dialog.gameObject); }

        void RefreshCompletedWorlds()
        {
            bool any = false;
            for (int i = 0; i < planets.Length; i++)
            {
                var previous = planets[i].transform.Find("Completed world");
                if (previous) { previous.gameObject.SetActive(false); Destroy(previous.gameObject); }
                var world = WorldProgress.Find(systemNumber, i);
                if (world == null || !completedWorldMaterial) continue;
                any = true;
                var image = new GameObject("Completed world", typeof(RectTransform), typeof(RawImage));
                var rect = (RectTransform)image.transform; rect.SetParent(planets[i].transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.one * -12;
                image.GetComponent<RawImage>().raycastTarget = false;
                var preview = image.AddComponent<SpherePreview>();
                preview.surfaceMaterial = completedWorldMaterial;
                preview.rotationSpeed = completedPlanetRotationSpeed; preview.seaLevel = world.seaLevel; preview.initialRotation = world.rotation;
            }
            if (any && !GameObject.Find("Completed world light"))
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.48f, .5f, .6f);
                var light = new GameObject("Completed world light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.35f;
                light.transform.rotation = Quaternion.Euler(35, -30, 0);
            }
        }

        public void OpenPlanet(int index)
        {
            if (index < 0 || index >= planets.Length || (Dialog && Dialog.IsOpen) || Time.frameCount <= dialogDismissFrame) return;
            SelectPlanet(index);
            var world = WorldLevels.Get(systemNumber,index);
            if (WorldProgress.Find(systemNumber,index) == null && WorldProgress.AvailableSphereCount < world.RequiredSpheres)
            {
                EnsureDialog().ShowInsufficientSpheres();
                return;
            }
            LevelPrepView.SystemNumber = systemNumber;
            LevelPrepView.PlanetIndex = index;
            LevelPrepView.RequiredSpheres = world.RequiredSpheres;
            SceneManager.LoadScene(WorldProgress.TryOpenCompletedResults(systemNumber, index) ? "Results" : "LevelPrep");
        }

        void LateUpdate()
        {
            if (displayedSystem != systemNumber)
            {
                WorldProgress.VisitSystem(systemNumber);
                RefreshSystemHeading();
                RefreshCompletedWorlds();
            }
        }

        public void SetSystemNumber(int number)
        {
            systemNumber = Mathf.Max(1, number);
            WorldProgress.VisitSystem(systemNumber);
            RefreshSystemHeading();
            RefreshCompletedWorlds();
        }

        public void RefreshSystemHeading()
        {
            systemNumber = Mathf.Max(1, systemNumber);
            displayedSystem = systemNumber;
            var generated = WorldProgress.GetSystem(systemNumber);
            for (int i = 0; i < planets.Length; i++)
            {
                planets[i].gameObject.SetActive(i < generated.worlds.Count);
                if (i >= generated.worlds.Count) continue;
                var world = generated.worlds[i];
                planets[i].name = world.displayName;
                var count = planets[i].transform.Find("Sphere Requirement/Sphere Count");
                if (count) count.GetComponent<TMP_Text>().text = world.RequiredSpheres.ToString();
            }
            if (selection && SelectedPlanet >= 0 && SelectedPlanet < generated.worlds.Count)
                selection.text = generated.worlds[SelectedPlanet].displayName;
            if (!systemLabel) return;
            systemLabel.text = "SYSTEM " + systemNumber.ToString("D2");
            float offset = systemLabel.preferredWidth * .5f + 68;
            if (leftAdornment) leftAdornment.anchoredPosition = new Vector2(-offset, 50);
            if (rightAdornment) rightAdornment.anchoredPosition = new Vector2(offset, 50);
        }

        public void SelectPlanet(int index)
        {
            if (index < 0 || index >= planets.Length) return;
            SelectedPlanet = index;
            for (int i = 0; i < planets.Length; i++)
            {
                var ring = planets[i].GetComponent<SystemMapGraphic>();
                ring.selected = i == index;
                ring.color = i == index ? Color.white : orbitColor;
                ring.thickness = i == index ? 3 : 2;
                ring.SetVerticesDirty();
            }
            selection.text = WorldLevels.Name(systemNumber, index);
        }
    }
}
