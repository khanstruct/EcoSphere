using TMPro;
using System.Collections.Generic;
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
            GameServices.Session.Phase=GamePhase.System;
            if (Camera.main) Camera.main.cullingMask &= ~(1 << 30);
            if (!GetComponent<SystemResponsiveLayout>()) gameObject.AddComponent<SystemResponsiveLayout>();
            if (GameServices.Session.ReturningToSystem) { systemNumber = GameServices.Session.SystemNumber; GameServices.Session.ReturningToSystem = false; }
            else systemNumber = GameServices.Progress.CurrentSystem;
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
            if (systemNumber == 1 && !GameServices.Progress.SystemIntroductionSeen && GameServices.Progress.Find(1,0) == null && GameServices.Progress.AvailableSphereCount == 2)
                EnsureDialog().ShowIntroduction();
            else if(GameServices.Progress.TryGetNextWorldTutorial(systemNumber,out var source,out var definition,out int target) && target<planets.Length)
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
                var world = GameServices.Progress.Find(systemNumber, i);
                if (world == null || !completedWorldMaterial) continue;
                any = true;
                var image = new GameObject("Completed world", typeof(RectTransform), typeof(RawImage));
                var rect = (RectTransform)image.transform; rect.SetParent(planets[i].transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.one * -12;
                image.GetComponent<RawImage>().raycastTarget = false;
                var preview = image.AddComponent<SpherePreview>();
                preview.ConfigurePlanet(world,completedWorldMaterial,completedPlanetRotationSpeed);
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
            if (GameServices.Progress.Find(systemNumber,index) == null && GameServices.Progress.AvailableSphereCount < world.RequiredSpheres)
            {
                EnsureDialog().ShowInsufficientSpheres();
                return;
            }
            GameServices.Session.SystemNumber = systemNumber;
            GameServices.Session.PlanetIndex = index;
            if(GameServices.Progress.TryOpenCompletedResults(systemNumber,index))GameServices.UI.ShowResults();else GameServices.UI.ShowSetup();
        }

        void LateUpdate()
        {
            if (displayedSystem != systemNumber)
            {
                GameServices.Progress.VisitSystem(systemNumber);
                RefreshSystemHeading();
                RefreshCompletedWorlds();
            }
        }

        public void SetSystemNumber(int number)
        {
            systemNumber = Mathf.Max(1, number);
            GameServices.Progress.VisitSystem(systemNumber);
            RefreshSystemHeading();
            RefreshCompletedWorlds();
        }

        public void RefreshSystemHeading()
        {
            systemNumber = Mathf.Max(1, systemNumber);
            displayedSystem = systemNumber;
            var generated = GameServices.Progress.GetSystem(systemNumber);
            var sun=WorldCatalog.Current.Sun(generated.sunTypeId);
            if(sun!=null)
                foreach(var image in GetComponentsInChildren<Image>())
                    if(image.name=="Yellow Sun")
                    {
                        var view=image.GetComponent<SunView>();
                        if(!view)view=image.gameObject.AddComponent<SunView>();
                        view.Show(sun,WorldCatalog.Current.animatedSunMaterial);
                    }
            for (int i = 0; i < planets.Length; i++)
            {
                planets[i].gameObject.SetActive(i < generated.worlds.Count);
                if (i >= generated.worlds.Count) continue;
                var world = generated.worlds[i];
                planets[i].name = world.displayName;
                var count = planets[i].transform.Find("Sphere Requirement/Sphere Count");
                if (count) count.GetComponent<TMP_Text>().text = world.RequiredSpheres.ToString();
                RefreshWorldBadges(planets[i].transform,world,GameServices.Progress.Find(systemNumber,i)!=null);
            }
            if (selection && SelectedPlanet >= 0 && SelectedPlanet < generated.worlds.Count)
                selection.text = generated.worlds[SelectedPlanet].displayName;
            if (!systemLabel) return;
            systemLabel.text = generated.displayName.ToUpperInvariant();
            float offset = systemLabel.preferredWidth * .5f + 68;
            if (leftAdornment) leftAdornment.anchoredPosition = new Vector2(-offset, 50);
            if (rightAdornment) rightAdornment.anchoredPosition = new Vector2(offset, 50);
        }

        void RefreshWorldBadges(Transform planet,WorldRecord world,bool completed)
        {
            var requirement=planet.Find("Sphere Requirement") as RectTransform;
            if(!requirement)return;
            requirement.Find("Sphere Icon").gameObject.SetActive(!completed);
            requirement.Find("Sphere Count").gameObject.SetActive(!completed);
            var stars=requirement.Find("World Stars") as RectTransform;
            if(!stars)
            {
                stars=new GameObject("World Stars",typeof(RectTransform)).GetComponent<RectTransform>();
                stars.SetParent(requirement,false);stars.anchorMin=stars.anchorMax=new Vector2(.5f,.5f);stars.sizeDelta=Vector2.zero;
            }
            stars.gameObject.SetActive(completed);
            if(completed)
            {
                int count=world.EarnedStars;
                requirement.sizeDelta=new Vector2(Mathf.Max(1,count)*72-8,64);
                requirement.anchoredPosition=new Vector2(0,-62);
                var oldModifiers=requirement.Find("World Modifiers");if(oldModifiers)oldModifiers.gameObject.SetActive(false);
                for(int i=0;i<Mathf.Max(count,stars.childCount);i++)
                {
                    Image star;
                    if(i<stars.childCount)star=stars.GetChild(i).GetComponent<Image>();
                    else
                    {
                        star=new GameObject("Star "+(i+1),typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                        star.transform.SetParent(stars,false);
                    }
                    star.gameObject.SetActive(i<count);star.sprite=WorldCatalog.Current.ratingStar;
                    star.color=Color.white;star.preserveAspect=true;star.raycastTarget=false;
                    star.rectTransform.sizeDelta=Vector2.one*64;
                    star.rectTransform.anchoredPosition=new Vector2((i-(count-1)*.5f)*72,0);
                }
                return;
            }
            var modifiers=requirement.Find("World Modifiers") as RectTransform;
            if(!modifiers)
            {
                modifiers=new GameObject("World Modifiers",typeof(RectTransform)).GetComponent<RectTransform>();
                modifiers.SetParent(requirement,false);modifiers.anchorMin=modifiers.anchorMax=new Vector2(.5f,.5f);modifiers.sizeDelta=Vector2.zero;
            }
            modifiers.gameObject.SetActive(true);
            var definitions=new List<WorldModifierDefinition>();var seen=new HashSet<string>();
            foreach(var definition in world.ModifierDefinitions)if(seen.Add(definition.id))definitions.Add(definition);
            for(int i=0;i<modifiers.childCount;i++)modifiers.GetChild(i).gameObject.SetActive(i<definitions.Count);
            const float size=64,step=72;const int maximumColumns=4;
            int total=definitions.Count+1,columns=Mathf.Min(maximumColumns,total),rows=Mathf.CeilToInt(total/(float)maximumColumns);
            requirement.sizeDelta=new Vector2(columns*step-8,rows*step-8);
            requirement.anchoredPosition=new Vector2(0,-62-(rows-1)*step*.5f);
            Vector2 Position(int index)
            {
                int row=index/maximumColumns,rowCount=Mathf.Min(maximumColumns,total-row*maximumColumns);
                return new Vector2((index%maximumColumns-(rowCount-1)*.5f)*step,(rows-1)*step*.5f-row*step);
            }
            ((RectTransform)requirement.Find("Sphere Icon")).anchoredPosition=Position(0);
            ((RectTransform)requirement.Find("Sphere Count")).anchoredPosition=Position(0);
            for(int i=0;i<definitions.Count;i++)
            {
                Image image;
                if(i<modifiers.childCount)image=modifiers.GetChild(i).GetComponent<Image>();
                else
                {
                    image=new GameObject("Modifier",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(modifiers,false);image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(.5f,.5f);
                }
                image.gameObject.name=definitions[i].id;image.sprite=definitions[i].icon;image.color=Color.white;
                image.preserveAspect=true;image.raycastTarget=false;image.rectTransform.sizeDelta=Vector2.one*size;
                image.rectTransform.anchoredPosition=Position(i+1);
            }
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
