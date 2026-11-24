using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EcoSphere
{
    public sealed class ResponsiveSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(LaunchFlags.Has("-responsiveSmoke"))
                new GameObject("Responsive validation").AddComponent<ResponsiveSmoke>();
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            foreach(var size in new[]{new Vector2Int(900,1600),new Vector2Int(1200,1600),new Vector2Int(1600,900)})
            {
                Rect safe=new Rect(25,50,size.x-50,size.y-75);
                var field=ScreenLayout.Playfield(safe);
                if(Mathf.Abs(field.width/field.height-9f/16)>.0001f||!safe.Contains(field.min+Vector2.one)||!safe.Contains(field.max-Vector2.one))
                    throw new System.Exception("Unsafe or distorted playfield");
                var m=ScreenLayout.Measure(size.x,size.y,safe);
                if(m.width*m.scale>safe.width+.1f||m.height*m.scale>safe.height+.1f)throw new System.Exception("Safe layout size");
            }
            foreach(string scene in new[]{"System","LevelPrep","FirstLight"})
            {
                if(SceneManager.GetActiveScene().name!=scene)SceneManager.LoadScene(scene);
                yield return null; yield return null;
                if(scene=="System")
                {
                    var system=FindAnyObjectByType<SystemView>();system.DismissIntroductionForSmoke();yield return null;
                    int original=system.systemNumber;system.SetSystemNumber(original+1);CheckWorldBadges(system);
                    system.SetSystemNumber(original);CheckWorldBadges(system);system.RefreshSystemHeading();CheckWorldBadges(system);
                }
                yield return new WaitForSecondsRealtime(.3f);
                if(scene=="LevelPrep") {var prep=FindAnyObjectByType<LevelPrepView>();prep.DismissSetupTutorialForSmoke();prep.AddSphere(0);prep.AddSphere(1);yield return null;yield return null;}
                foreach(var size in new[]{new Vector2Int(900,1600),new Vector2Int(1200,1600),new Vector2Int(1600,900)})
                {
                    var target=new RenderTexture(size.x,size.y,24);target.Create();
                    Camera camera;
                    Canvas canvas=null;
                    if(scene=="System")
                    {
                        camera=Camera.main; camera.targetTexture=target;
                        var system=FindAnyObjectByType<SystemView>(); canvas=system.GetComponent<Canvas>();
                        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                        system.GetComponent<SystemResponsiveLayout>().Apply(camera);
                    }
                    else if(scene=="LevelPrep")
                    {
                        camera=GameObject.Find("Prep Camera").GetComponent<Camera>();camera.targetTexture=target;
                        FindAnyObjectByType<LevelPrepView>().Reflow();
                    }
                    else
                    {
                        var game=FindAnyObjectByType<EcoSpherePrototype>();game.SetPreviewTarget(target);camera=game.GameplayView;
                        var a=camera.WorldToScreenPoint(new Vector3(-4.65f,0,0));
                        var b=camera.WorldToScreenPoint(new Vector3(4.65f,0,0));
                        if(Mathf.Abs((b.x-a.x)-game.PlayableScreenRect.width)>.01f)
                            throw new System.Exception("Gameplay playfield changed proportions");
                        var pixel=camera.WorldToScreenPoint(new Vector3(1,2,0));
                        var world=camera.ScreenToWorldPoint(pixel);
                        if(Vector2.Distance(world,new Vector2(1,2))>.001f)throw new System.Exception("Pointer projection changed");
                    }
                    yield return null;yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();
                    foreach(var preview in FindObjectsByType<SpherePreview>(FindObjectsSortMode.None))if(preview.PreviewCamera)preview.PreviewCamera.Render();
                    if(scene=="FirstLight")FindAnyObjectByType<EcoSpherePrototype>().RenderPresentation();else camera.Render();
                    RenderTexture.active=target;
                    var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                    Directory.CreateDirectory("Logs/Responsive");
                    File.WriteAllBytes("Logs/Responsive/"+scene+"-"+size.x+"x"+size.y+".png",image.EncodeToPNG());
                    RenderTexture.active=null;
                    if(scene=="FirstLight")FindAnyObjectByType<EcoSpherePrototype>().SetPreviewTarget(null);else camera.targetTexture=null;
                    target.Release();Destroy(target);Destroy(image);
                }
                if(scene=="System") {LevelPrepView.PlanetIndex=2;}
            }
            Debug.Log("ECOSPHERE_RESPONSIVE_PASSED: 9 renders, safe areas, fixed gameplay proportions and pointer projection.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
            #endif
        }
        static void CheckWorldBadges(SystemView system)
        {
            for(int i=0;i<system.planets.Length;i++)
            {
                var world=WorldLevels.Get(system.systemNumber,i);
                var requirement=(RectTransform)system.planets[i].transform.Find("Sphere Requirement");
                var number=requirement.Find("Sphere Count").GetComponent<TMP_Text>();
                var circle=(RectTransform)requirement.Find("Sphere Icon");
                if(number.text!=world.RequiredSpheres.ToString() || number.rectTransform.anchoredPosition!=circle.anchoredPosition)throw new System.Exception("Requirement number must stay inside its circle");
                int active=0;var ids=new System.Collections.Generic.HashSet<string>();
                foreach(Transform badge in requirement.Find("World Modifiers"))
                {
                    if(!badge.gameObject.activeSelf)continue;
                    active++;var image=badge.GetComponent<UnityEngine.UI.Image>();
                    if(!ids.Add(badge.name) || !world.modifiers.Contains(badge.name) || image.sprite!=WorldCatalog.Current.Modifier(badge.name).icon || image.raycastTarget)
                        throw new System.Exception("World modifier badge must use current world data and leave planet clicks unblocked");
                }
                if(active!=world.modifiers.Count)throw new System.Exception("Missing or stale world modifier badges");
            }
            Debug.Log("ECOSPHERE_WORLD_BADGES_PASSED system="+system.systemNumber);
        }
    }
}
