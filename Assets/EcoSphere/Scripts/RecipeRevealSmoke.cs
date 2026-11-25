using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed class RecipeRevealSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(LaunchFlags.Has("-recipeRevealSmoke"))
                new GameObject("Recipe reveal checks").AddComponent<RecipeRevealSmoke>();
        }
        static void Expect(bool value,string message) { if(!value)throw new Exception("Recipe reveal: "+message); }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null; yield return null;
            FindAnyObjectByType<SystemView>().DismissIntroductionForSmoke(); yield return null;
            var catalog=SphereCatalog.Current; var book=catalog.recipeBook; var original=book.recipes;
            WorldResult award;
            try
            {
                book.recipes=new List<ElementRecipe> {
                    new ElementRecipe { first=ElementId.Fire,second=ElementId.Void,output=catalog.Get(SphereIds.Sunlight) },
                    new ElementRecipe { first=ElementId.Fire,second=ElementId.Fire,output=catalog.Get(SphereIds.Ice) } };
                var amounts=new double[Enum.GetValues(typeof(ElementId)).Length]; amounts[(int)ElementId.Fire]=600; amounts[(int)ElementId.Void]=400;
                WorldLevels.Get(3,0).naturalSpheres.Add(new NaturalSpherePlacement{sphereId=SphereIds.Stone,cell=Vector2Int.up});
                award=GameServices.Progress.Complete(3,0,-.02f,0,new int[]{SphereIds.Stone},amounts);
            }
            finally { book.recipes=original; }
            Expect(award.discoveries.Count==3 && award.discoveryDetails.Count==3,"natural/pair/self award provenance");
            var restored=JsonUtility.FromJson<CompletedWorld>(JsonUtility.ToJson(award.world));
            Expect(restored.discoveryDetails.Count==3 && restored.discoveryDetails[1].first==ElementId.Fire && restored.discoveryDetails[1].second==ElementId.Void && restored.discoveryDetails[2].first==restored.discoveryDetails[2].second,"serialized ingredients changed");
            SceneManager.LoadScene("Results"); yield return null; yield return null;
            var results=FindAnyObjectByType<ResultsView>();
            Expect(results && results.IsRecipeReveal && results.DiscoverySpheres.Count==3 && !results.ContinueButton.interactable,"sequential reveal mode missing");
            bool merge=false,charge=false,flash=false;
            for(int index=0;index<3;index++)
            {
                float deadline=Time.realtimeSinceStartup+10;
                while((results.RevealIndex!=index || results.RevealPhase!=DiscoveryRevealPhase.Ready) && Time.realtimeSinceStartup<deadline)
                {
                    Expect(!results.OkButton.interactable && !results.ContinueButton.interactable,"animation allowed early confirmation");
                    if(results.RevealIndex==1)
                    {
                        Expect(results.CurrentDiscovery.kind==DiscoveryKind.Recipe && results.CurrentDiscovery.first==ElementId.Fire && results.CurrentDiscovery.second==ElementId.Void,"recipe inputs were inferred from the current book instead of saved provenance");
                        if(results.RevealPhase==DiscoveryRevealPhase.Merge && !merge)
                        {
                            var preview=GameObject.Find("Void ingredient").GetComponent<SpherePreview>();
                            Expect(preview.PreviewCamera && preview.PreviewCamera.transform.parent.Find("Sphere glow"),"Void ingredient preview omitted its shared halo");
                            Capture(results,"merge"); merge=true;
                        }
                        if(results.RevealPhase==DiscoveryRevealPhase.Volatile && !charge) { Capture(results,"volatile"); charge=true; }
                        if(results.RevealPhase==DiscoveryRevealPhase.Flash && !flash) { Expect(results.DiscoveryPlayCount==1,"announcement started before the combination ended"); Capture(results,"flash"); flash=true; }
                    }
                    yield return null;
                }
                Expect(results.RevealPhase==DiscoveryRevealPhase.Ready && results.RevealIndex==index,"reveal timed out");
                Expect(results.RevealedDescription==SphereCatalog.Current.Get(results.CurrentDiscovery.sphereId).description,"revealed sphere description missing");
                if(index==2) Expect(results.CurrentDiscovery.first==ElementId.Fire && results.CurrentDiscovery.second==ElementId.Fire,"self recipe lost its identical inputs");
                if(index==1)Capture(results,"revealed");
                if(index>0)
                {
                    Expect(results.CombinePlayCount==index,"combination sound missing or repeated");
                    double duration=GameAudioSettings.Current.sphereCombine.length;
                    Expect(Math.Abs(results.DiscoveryAudioStart-results.CombineAudioStart-duration)<.001,"animation/announcement schedule does not match clip duration");
                    Expect(results.CombinationFinishedAt>=results.DiscoveryAudioStart && results.CombinationFinishedAt-results.DiscoveryAudioStart<.5,"visual reveal failed to finish with the combination clip");
                }
                else Expect(results.CombinePlayCount==0,"natural discovery played a combination sound");
                int expectedAudio=index+1; Expect(results.DiscoveryPlayCount==expectedAudio,"discovery audio repeated or missing");
                results.OkButton.onClick.Invoke(); results.OkButton.onClick.Invoke();
                yield return null;
            }
            float finalDeadline=Time.realtimeSinceStartup+5;
            while(!results.Settled && Time.realtimeSinceStartup<finalDeadline)yield return null;
            Expect(results.Settled && results.ContinueButton.interactable && results.DiscoverySpheres.TrueForAll(s=>s.parent.name.EndsWith("collected")),"sequence failed to collect every discovery");
            Expect(merge && charge && flash,"recipe skipped a required animation phase");
            Capture(results,"collected");
            GameServices.Progress.TryOpenCompletedResults(3,0); SceneManager.LoadScene("Results"); yield return null; yield return null;
            results=FindAnyObjectByType<ResultsView>();
            Expect(!results.IsRecipeReveal && results.Settled && results.DiscoverySpheres.Count==3 && results.DiscoveryPlayCount==0,"revisited world replayed the reveal or lost history");
            Expect(GameServices.Progress.Complete(3,0,0,0,new int[]{SphereIds.Stone}).discoveries.Count==0 && award.world.discoveryDetails.Count==3,"revisit repeated a discovery reward");
            Debug.Log("ECOSPHERE_RECIPE_REVEAL_PASSED: saved pair/self ingredients, mixed discovery sequence, merge/charge/flash/reveal/collection, input gating, responsive renders and no replay rewards");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        static void Capture(ResultsView results,string name)
        {
            foreach(var size in new[] {new Vector2Int(900,1600),new Vector2Int(1600,900)})
            {
                var target=new RenderTexture(size.x,size.y,24); target.Create(); var camera=results.View;
                var previous=camera.targetTexture; float aspect=camera.aspect; var oldActive=RenderTexture.active;
                camera.targetTexture=target; camera.aspect=(float)size.x/size.y; camera.Render(); results.Reflow(); Canvas.ForceUpdateCanvases();
                foreach(var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None))if(preview.PreviewCamera)preview.PreviewCamera.Render();
                camera.Render(); RenderTexture.active=target;
                var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Recipe-"+name+"-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                RenderTexture.active=oldActive; camera.targetTexture=previous; camera.aspect=aspect; results.Reflow();
                target.Release(); Destroy(target); Destroy(pixels);
            }
        }
    }
}
