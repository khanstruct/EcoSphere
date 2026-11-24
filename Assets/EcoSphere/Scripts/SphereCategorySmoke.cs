using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EcoSphere
{
    public sealed partial class LevelPrepView
    {
        void ExpectCategory(bool condition,string message){if(!condition)throw new Exception("Categories: "+message);}
        void CheckCategoryLight()
        {
            int lit=0;
            foreach(var tab in categoryTabs)
            {
                bool selected=tab.category==SelectedCategory;
                ExpectCategory(tab.light.gameObject.activeSelf==selected,"selection light attached to wrong tab");
                if(selected)
                {
                    lit++;ExpectCategory(tab.light.anchoredPosition.x<tab.label.rectTransform.anchoredPosition.x-tab.label.preferredWidth*.5f,"light must precede the name");
                }
                else ExpectCategory(Mathf.Max(tab.label.color.r,tab.label.color.g,tab.label.color.b)<.6f && Mathf.Abs(tab.label.color.r-tab.label.color.b)<.05f,"inactive label must be dim/desaturated");
            }
            ExpectCategory(lit==1,"exactly one active category light required");
        }
        internal void CheckInitialCategories()
        {
            ExpectCategory(!SelectedCategory.HasValue && visibleTileIndices.Count==collectionTypes.Count,"All must be the default and show every owned sphere");
            CheckCategoryLight();
            var expected=new HashSet<SphereCategory>();foreach(int type in collectionTypes)expected.Add(SphereCatalog.Current.Get(type).category);
            ExpectCategory(categoryTabs.Count==expected.Count+1,"tabs must contain only categories represented by owned selectable spheres");
            int previous=-1;
            for(int i=1;i<categoryTabs.Count;i++)
            {int current=(int)categoryTabs[i].category.Value;ExpectCategory(current>previous && expected.Contains(categoryTabs[i].category.Value),"category order/ownership");previous=current;}
            SelectCategory(SphereCategory.Basic);
            CheckCategoryLight();
            foreach(int index in visibleTileIndices)ExpectCategory(SphereCatalog.Current.Get(collectionTypes[index]).category==SphereCategory.Basic,"Basic filter leaked another category");
            AddSphere(SphereIds.Fire);SelectCategory(SphereCategory.Forces);AddSphere(SphereIds.Void);
            ExpectCategory(visibleTileIndices.Count==1 && collectionTypes[visibleTileIndices[0]]==SphereIds.Void,"Forces must show Void only");
            SelectCategory(null);
            CheckCategoryLight();
            ExpectCategory(Selected.Count==2 && Selected[0]==SphereIds.Fire && Selected[1]==SphereIds.Void && Begin.interactable,"loadout or Begin gating lost across filters");
            Remove(0);Remove(0);
            foreach(var slot in slots)ExpectCategory(!slot.Find("Selected sphere"),"rapid cross-tab removal left a stale slot preview");
        }
        internal void CheckLifeFilter(int expectedCount)
        {
            ExpectCategory(categoryTabs.Exists(tab=>tab.category==SphereCategory.Life),"Life tab missing after ownership");
            SelectCategory(SphereCategory.Life);
            CheckCategoryLight();
            ExpectCategory(visibleTileIndices.Count==expectedCount,"Life filter has wrong owned count");
            foreach(int index in visibleTileIndices)ExpectCategory(SphereCatalog.Current.Get(collectionTypes[index]).category==SphereCategory.Life,"Life filter leaked");
            int hidden=collectionTypes.IndexOf(SphereIds.Void);
            var preview=tiles[hidden].GetComponentInChildren<SpherePreview>(true);
            ExpectCategory(!tiles[hidden].gameObject.activeSelf && (!preview.PreviewCamera || !preview.PreviewCamera.gameObject.activeInHierarchy),"hidden previews still render");
            AddSphere(SphereIds.Life);SelectCategory(null);
            ExpectCategory(Selected.Contains(SphereIds.Life) && visibleTileIndices.Count==collectionTypes.Count,"hidden selection lost or All omitted owned spheres");
            Remove(0);
        }
        internal void CheckCategoryAbsent(SphereCategory category)
        {
            ExpectCategory(!categoryTabs.Exists(tab=>tab.category==category),"locked category tab shown: "+category);
            var before=SelectedCategory;SelectCategory(category);ExpectCategory(before==SelectedCategory,"unavailable tab selected");
        }
        internal void CheckAllCategoryTabs()
        {
            ExpectCategory(categoryTabs.Count==7,"all seven tabs must fit or scroll when categories are owned");
            categoryScroll.horizontalNormalizedPosition=1;Canvas.ForceUpdateCanvases();
            SelectCategory(SphereCategory.Beyond);
            ExpectCategory(visibleTileIndices.Count==1,"future category must filter without special code");
        }
        internal IEnumerator CaptureCategoryLayout(string suffix)
        {
            SelectCategory(null);
            yield return new WaitForSecondsRealtime(.2f);
            foreach(var size in new[]{new Vector2Int(900,1600),new Vector2Int(1600,900)})
            {
                var target=new RenderTexture(size.x,size.y,24);target.Create();view.targetTexture=target;Reflow();
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                var viewport=(RectTransform)root.Find("Discovered Spheres/Collection viewport");
                if(categoryTabs.Count==7 && size.x<size.y)
                {
                    ExpectCategory(nextCategoryPage.gameObject.activeSelf,"overflow category navigation missing");
                    nextCategoryPage.onClick.Invoke();nextCategoryPage.onClick.Invoke();
                    yield return null;
                    var last=new Vector3[4];categoryTabs[categoryTabs.Count-1].image.rectTransform.GetWorldCorners(last);
                    var bounds=new Vector3[4];categoryViewport.GetWorldCorners(bounds);
                    ExpectCategory(last[3].x<=bounds[3].x+.01f && last[0].x>=bounds[0].x-.01f,"Beyond tab unreachable in narrow layout");
                }
                var a=new Vector3[4];var b=new Vector3[4];categoryViewport.GetWorldCorners(a);viewport.GetWorldCorners(b);
                ExpectCategory(a[0].y>b[1].y,"category tabs overlap collection grid");
                foreach(int index in visibleTileIndices)
                {
                    var tile=tiles[index].rectTransform;
                    ExpectCategory(tile.rect.xMin>=-viewport.sizeDelta.x*.5f-1 && tile.sizeDelta.x<=viewport.sizeDelta.x,"tile too wide");
                }
                foreach(var graphic in layoutCanvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                {
                    graphic.SetVerticesDirty();graphic.SetMaterialDirty();
                    if(graphic is UnityEngine.UI.MaskableGraphic maskable)maskable.RecalculateClipping();
                }
                foreach(var label in layoutCanvas.GetComponentsInChildren<TMPro.TMP_Text>())label.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                foreach(var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None))if(preview.PreviewCamera)preview.PreviewCamera.Render();
                view.Render();var old=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();RenderTexture.active=old;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/categories-"+suffix+"-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());
                view.targetTexture=null;target.Release();Destroy(target);Destroy(image);
            }
            Reflow();
        }
    }
    public sealed class SphereCategorySmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-sphereCategorySmoke"))new GameObject("Category checks").AddComponent<SphereCategorySmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();prep.DismissSetupTutorialForSmoke();yield return null;
            prep.CheckInitialCategories();
            foreach(var category in new[]{SphereCategory.Weather,SphereCategory.Technology,SphereCategory.Beyond})prep.CheckCategoryAbsent(category);
            if(!DevelopmentMode.Enabled)
            {
                prep.CheckCategoryAbsent(SphereCategory.Life);yield return prep.CaptureCategoryLayout("normal");
                var totals=new double[ElementComposition.Capacity];totals[(int)ElementId.Water]=500;totals[(int)ElementId.Mud]=500;
                GameServices.Progress.Complete(7,0,-.02f,0,null,totals);
                SceneManager.LoadScene("LevelPrep");yield return null;yield return null;
                prep=FindAnyObjectByType<LevelPrepView>();prep.DismissSetupTutorialForSmoke();yield return null;
                if(prep.SelectedCategory.HasValue)throw new Exception("Reopened setup must default to All");
                prep.CheckLifeFilter(1);yield return prep.CaptureCategoryLayout("unlocked");
            }
            else
            {
                prep.CheckLifeFilter(2);yield return prep.CaptureCategoryLayout("dev");
                int[] types={SphereIds.Fire,SphereIds.Sunlight,SphereIds.Ice};
                var previous=new SphereCategory[3];var future=new[]{SphereCategory.Weather,SphereCategory.Technology,SphereCategory.Beyond};
                try
                {
                    for(int i=0;i<types.Length;i++){var sphere=SphereCatalog.Current.Get(types[i]);previous[i]=sphere.category;sphere.category=future[i];}
                    SceneManager.LoadScene("LevelPrep");yield return null;yield return null;
                    prep=FindAnyObjectByType<LevelPrepView>();prep.CheckAllCategoryTabs();yield return prep.CaptureCategoryLayout("future");
                }
                finally{for(int i=0;i<types.Length;i++)SphereCatalog.Current.Get(types[i]).category=previous[i];}
            }
            Debug.Log("ECOSPHERE_SPHERE_CATEGORY_GAMEPLAY_PASSED: "+(DevelopmentMode.Enabled?"DEV collection and all seven future tabs":"normal locked categories and earned Life tab")+"; All default, ordered filters, cross-tab loadout/Begin, hidden preview cameras, portrait/wide layouts");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
