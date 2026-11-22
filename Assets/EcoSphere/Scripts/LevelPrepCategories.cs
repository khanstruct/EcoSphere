using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class LevelPrepView
    {
        sealed class CategoryTab
        {
            public SphereCategory? category;
            public Image image;
            public TMP_Text label;
            public RectTransform underline;
            public RectTransform light;
        }
        readonly List<CategoryTab> categoryTabs=new List<CategoryTab>();
        readonly List<int> visibleTileIndices=new List<int>();
        RectTransform categoryViewport,categoryContent;
        ScrollRect categoryScroll;
        Button previousCategoryPage,nextCategoryPage;
        public SphereCategory? SelectedCategory {get;private set;} // null = All

        void BuildCategoryTabs(Transform panel)
        {
            categoryViewport=Rect("Category tabs",panel,new Vector2(500,58),Vector2.zero);
            categoryViewport.gameObject.AddComponent<RectMask2D>();
            categoryViewport.gameObject.AddComponent<Image>().color=Color.clear;
            categoryContent=Rect("Tab list",categoryViewport,new Vector2(500,58),Vector2.zero);
            categoryContent.anchorMin=categoryContent.anchorMax=categoryContent.pivot=new Vector2(0,.5f);
            categoryScroll=categoryViewport.gameObject.AddComponent<ScrollRect>();
            categoryScroll.viewport=categoryViewport;categoryScroll.content=categoryContent;
            categoryScroll.horizontal=true;categoryScroll.vertical=false;categoryScroll.movementType=ScrollRect.MovementType.Clamped;
            categoryScroll.scrollSensitivity=24;
            previousCategoryPage=CategoryArrow(panel,"Previous categories","<",-.55f);
            nextCategoryPage=CategoryArrow(panel,"More categories",">",.55f);
            categoryScroll.onValueChanged.AddListener(_=>UpdateCategoryArrows());
            AddCategoryTab(null);
            var ownedCategories=new HashSet<SphereCategory>();
            foreach(int type in collectionTypes)ownedCategories.Add(SphereCatalog.Current.Get(type).category);
            foreach(SphereCategory category in Enum.GetValues(typeof(SphereCategory)))
                if(ownedCategories.Contains(category))AddCategoryTab(category);
        }
        Button CategoryArrow(Transform panel,string name,string glyph,float offset)
        {
            var label=Text(name,panel,glyph,30,new Vector2(32,54),Vector2.zero);label.raycastTarget=true;
            var button=label.gameObject.AddComponent<Button>();button.targetGraphic=label;ButtonClickSound.Attach(button);
            button.onClick.AddListener(()=>
            {
                if(SetupInputBlocked)return;
                categoryScroll.StopMovement();categoryScroll.horizontalNormalizedPosition=Mathf.Clamp01(categoryScroll.horizontalNormalizedPosition+offset);
                UpdateCategoryArrows();
            });
            return button;
        }
        void UpdateCategoryArrows()
        {
            if(!previousCategoryPage || !nextCategoryPage)return;
            float position=categoryScroll.horizontalNormalizedPosition;
            previousCategoryPage.interactable=position>.001f;
            nextCategoryPage.interactable=position<.999f;
        }
        void AddCategoryTab(SphereCategory? category)
        {
            string title=category?.ToString() ?? "All";
            var image=Frame(title+" tab",categoryContent,new Vector2(120,54),Vector2.zero);
            image.raycastTarget=true;
            var label=Text("Label",image.transform,title,22,new Vector2(100,42),Vector2.zero);
            var light=Rect("Selected light",image.transform,new Vector2(26,26),Vector2.zero);
            light.gameObject.AddComponent<TabSelectionLight>().raycastTarget=false;
            var underline=Rect("Selected underline",image.transform,new Vector2(92,3),new Vector2(0,-22));
            var line=underline.gameObject.AddComponent<Image>();line.color=new Color(1,.78f,.33f);line.raycastTarget=false;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            ButtonClickSound.Attach(button);button.onClick.AddListener(()=>SelectCategory(category));
            categoryTabs.Add(new CategoryTab{category=category,image=image,label=label,underline=underline,light=light});
        }
        public void SelectCategory(SphereCategory? category)
        {
            if(SetupInputBlocked || !categoryTabs.Exists(tab=>tab.category==category))return;
            SelectedCategory=category;ApplyCategoryFilter();Reflow();
        }
        void ApplyCategoryFilter()
        {
            visibleTileIndices.Clear();
            for(int i=0;i<collectionTypes.Count;i++)
            {
                bool visible=!SelectedCategory.HasValue || SphereCatalog.Current.Get(collectionTypes[i]).category==SelectedCategory.Value;
                tiles[i].gameObject.SetActive(visible);if(visible)visibleTileIndices.Add(i);
            }
            foreach(var tab in categoryTabs)
            {
                bool selected=tab.category==SelectedCategory;
                tab.image.color=selected ? new Color(1,.8f,.4f,1) : new Color(.38f,.39f,.41f,.35f);
                tab.label.color=selected ? new Color(1,.92f,.7f) : new Color(.48f,.49f,.51f);
                tab.underline.gameObject.SetActive(selected);
                tab.light.gameObject.SetActive(selected);
            }
        }
        void LayoutCategoryTabs(float width,float y)
        {
            float x=0;
            foreach(var tab in categoryTabs)
            {
                float tabWidth=Mathf.Max(70,tab.label.preferredWidth+50);
                var rect=tab.image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);
                rect.sizeDelta=new Vector2(tabWidth,54);rect.anchoredPosition=new Vector2(x+tabWidth*.5f,0);
                ((RectTransform)rect.Find("Midnight backing")).sizeDelta=rect.sizeDelta-new Vector2(22,18);
                tab.label.rectTransform.sizeDelta=new Vector2(tabWidth-36,42);
                tab.label.rectTransform.anchoredPosition=new Vector2(8,0);
                tab.light.anchoredPosition=new Vector2(8-tab.label.preferredWidth*.5f-14,0);
                tab.underline.sizeDelta=new Vector2(tabWidth-22,3);
                x+=tabWidth+8;
            }
            bool overflow=x-8>width;
            float viewportWidth=overflow ? width-64 : width;
            ScreenLayout.Place(categoryViewport,new Vector2(viewportWidth,58),new Vector2(0,y));
            categoryContent.sizeDelta=new Vector2(Mathf.Max(viewportWidth,x-8),58);
            previousCategoryPage.gameObject.SetActive(overflow);nextCategoryPage.gameObject.SetActive(overflow);
            ScreenLayout.Place(previousCategoryPage.GetComponent<RectTransform>(),new Vector2(32,54),new Vector2(-width*.5f+16,y));
            ScreenLayout.Place(nextCategoryPage.GetComponent<RectTransform>(),new Vector2(32,54),new Vector2(width*.5f-16,y));
            if(!overflow)categoryScroll.horizontalNormalizedPosition=0;
            UpdateCategoryArrows();
        }
    }
}
