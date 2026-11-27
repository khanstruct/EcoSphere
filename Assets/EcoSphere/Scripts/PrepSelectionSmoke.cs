using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed partial class LevelPrepView
    {
        // Real pointer clicks on sphere tiles: the list keeps its scroll position, a chosen tile shows its checkbox,
        // clicking a chosen tile removes it, and switching category returns the list to the top.
        internal IEnumerator SelectionSmoke()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Setup selection: "+message);}
            DismissSetupTutorialForSmoke();
            yield return new WaitForSeconds(.5f);
            var mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var scroll=root.Find("Discovered Spheres").GetComponent<ScrollRect>();var list=scroll.content;var viewport=scroll.viewport;
            Expect(list.sizeDelta.y>viewport.rect.height+50,"needs enough spheres to scroll (DEV mode)");
            // Alphabetical by name, laid out left to right then down the rows.
            for(int i=1;i<collectionTypes.Count;i++)
            {
                Expect(SphereCatalog.Current.CompareForDisplay(collectionTypes[i-1],collectionTypes[i])<0,
                    "spheres must be alphabetical: "+SphereCatalog.Current.Get(collectionTypes[i-1]).displayName+" before "+SphereCatalog.Current.Get(collectionTypes[i]).displayName);
                Vector2 previous=tiles[i-1].rectTransform.anchoredPosition,current=tiles[i].rectTransform.anchoredPosition;
                Expect(current.y<previous.y-1 || (Mathf.Abs(current.y-previous.y)<1 && current.x>previous.x),"tiles must be laid out in list order");
            }
            IEnumerator Click(Image tile)
            {
                Canvas.ForceUpdateCanvases();
                Vector2 point=RectTransformUtility.WorldToScreenPoint(view,tile.transform.position);
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                Expect(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(tile.transform),"tile is blocked from pointer input");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            }
            // Scroll partway down and pick a tile whose center is inside the viewport.
            scroll.verticalNormalizedPosition=.5f;scroll.velocity=Vector2.zero;yield return null;
            float scrolled=list.anchoredPosition.y;Expect(scrolled>50,"list did not scroll");
            Image target=null;int type=0;
            for(int i=0;i<tiles.Count && !target;i++)
            {
                if(!tiles[i].gameObject.activeInHierarchy)continue;
                var local=viewport.InverseTransformPoint(tiles[i].transform.position);
                if(viewport.rect.Contains(local) && Mathf.Abs(local.y)<viewport.rect.height*.3f){target=tiles[i];type=collectionTypes[i];}
            }
            Expect(target,"no tile visible after scrolling");
            var check=target.transform.Find("Selected check");
            Expect(check && !check.gameObject.activeSelf && check.GetComponent<Image>().sprite==selectedCheck && selectedCheck,"unselected tile must not show its checkbox");
            yield return Click(target);
            Expect(Selected.Contains(type) && check.gameObject.activeSelf,"click must choose the sphere and show its checkbox");
            Expect(Mathf.Abs(list.anchoredPosition.y-scrolled)<1,"choosing a sphere must not scroll the list ("+scrolled+" -> "+list.anchoredPosition.y+")");
            var corner=((RectTransform)check).anchoredPosition;var size=target.rectTransform.sizeDelta;
            Expect(corner.x>size.x*.25f && corner.y>size.y*.25f,"checkbox must sit in the tile's upper-right corner");

            yield return Click(target);
            Expect(!Selected.Contains(type) && !check.gameObject.activeSelf,"clicking a chosen sphere must remove it and its checkbox (selected: "+string.Join(",",Selected)+", check "+check.gameObject.activeSelf+")");
            Expect(Mathf.Abs(list.anchoredPosition.y-scrolled)<1,"removing a sphere must not scroll the list");
            Expect(Selected.Count==0 && slots.TrueForAll(slot=>!slot.Find("Selected sphere")),"removed sphere must leave its slot");
            SelectCategory(SphereCategory.Basic);yield return null;
            Expect(Mathf.Abs(list.anchoredPosition.y)<1,"switching category must return the list to the top");
            SelectCategory(null);
            // Review render last: capturing re-lays out the canvas at the capture size.
            ToggleSphere(SphereCatalog.Current.openingSpheres[0]);ToggleSphere(type);yield return new WaitForSeconds(.3f);Capture("LevelPrep-selected");
            Debug.Log("ECOSPHERE_PREP_SELECTION_PASSED: scroll kept on choose/remove, checkbox in the upper-right, click-to-remove, category resets to top");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        void Capture(string name)
        {
            var target=new RenderTexture(900,1600,24);view.targetTexture=target;float aspect=view.aspect;view.aspect=900f/1600;
            Canvas.ForceUpdateCanvases();
            foreach(var preview in FindObjectsByType<SpherePreview>())if(preview.PreviewCamera)preview.PreviewCamera.Render();
            view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(900,1600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,1600),0,0);image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/"+name+".png"),image.EncodeToPNG());
            RenderTexture.active=previous;view.targetTexture=null;view.aspect=aspect;target.Release();Destroy(target);Destroy(image);
        }
    }

    public sealed class PrepSelectionSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-prepSelectionSmoke"))new GameObject("Prep selection checks").AddComponent<PrepSelectionSmoke>();}
        IEnumerator Start()
        {
            LevelPrepView prep=null;
            while(!(prep=FindAnyObjectByType<LevelPrepView>()))yield return null;
            yield return null;yield return prep.SelectionSmoke();
        }
    }
}
