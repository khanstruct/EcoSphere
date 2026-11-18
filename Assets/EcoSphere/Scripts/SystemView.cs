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

        void Start() { RefreshCompletedWorlds(); }

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
                preview.rotationSpeed = 2; preview.seaLevel = world.seaLevel; preview.initialRotation = world.rotation;
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
            SelectPlanet(index);
            LevelPrepView.SystemNumber = systemNumber;
            LevelPrepView.PlanetIndex = index;
            LevelPrepView.RequiredSpheres = index == 0 ? 2 : index == 1 ? 3 : 4;
            SceneManager.LoadScene("LevelPrep");
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
            for (int i = 0; i < planets.Length; i++) planets[i].name = WorldLevels.Name(systemNumber, i);
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
