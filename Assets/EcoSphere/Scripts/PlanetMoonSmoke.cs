using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    // Run with PlanetMoonTools.Smoke and -planetMoonSmoke (DEV mode).
    public sealed class PlanetMoonSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-planetMoonSmoke"))new GameObject("Planet moon checks").AddComponent<PlanetMoonSmoke>();}
        static void Expect(bool value,string message){if(!value)throw new Exception("Planet moons: "+message);}
        static void Capture(Camera camera,RenderTexture target,string file)
        {
            camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/"+file),pixels.EncodeToPNG());Destroy(pixels);
        }
        IEnumerator Start()
        {
            yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();game.enabled=false;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            var surface=FindAnyObjectByType<PlanetSurface>();var moons=surface.Moons;Expect(moons,"the gameplay planet has no moon system");
            var style=PlanetVisualStyle.Current;
            var camera=game.GameplayView;camera.transform.position=surface.transform.position+new Vector3(0,0,-15);camera.transform.rotation=Quaternion.identity;
            camera.orthographic=true;camera.aspect=1;camera.orthographicSize=surface.transform.lossyScale.x*1.3f;
            var target=new RenderTexture(512,512,24);target.Create();camera.targetTexture=target;

            // Thresholds: none, one, two, three moons as the Moons stat rises.
            float[] levels={0,(style.moonOrbits[0].threshold+style.moonOrbits[1].threshold)/2,(style.moonOrbits[1].threshold+style.moonOrbits[2].threshold)/2,1};
            for(int expected=0;expected<levels.Length;expected++)
            {
                surface.Stats.Clear();surface.Stats[PlanetStat.Moons]=levels[expected];
                yield return new WaitForSeconds(3f);
                Expect(moons.VisibleCount==expected,"Moons "+levels[expected]+" must show "+expected+" moon(s), showed "+moons.VisibleCount);
                Capture(camera,target,"Planet-moons-"+expected+".png");
            }
            // They orbit, stay clear of the planet, keep their own frame while the globe spins, and hold while paused.
            var first=moons.Bodies[0];var start=first.position;surface.rotationSpeed=40;
            yield return new WaitForSeconds(1f);
            Expect((first.position-start).magnitude>.001f,"moons must orbit");
            Expect(moons.transform.rotation==Quaternion.identity,"the orbit frame must not spin with the globe");
            foreach(var body in moons.Bodies)
                Expect((body.position-surface.transform.position).magnitude>surface.transform.lossyScale.x*.5f+body.lossyScale.x*.5f,"a moon intersects the planet");
            Time.timeScale=0;yield return null;var held=first.position;yield return new WaitForSecondsRealtime(.5f);
            Expect(first.position==held,"moons must hold while paused");Time.timeScale=1;surface.rotationSpeed=0;
            camera.targetTexture=null;target.Release();Destroy(target);

            // Real compositions through the shared stats: a little Moon gives one moon, a Moon world gives all three.
            int Count(params (ElementId element,double amount)[] parts)
            {
                var totals=new double[ElementComposition.Capacity];foreach(var p in parts)totals[(int)p.element]=p.amount;
                float stat=PlanetStatCalculator.Evaluate(totals).Moons;int count=0;foreach(var orbit in style.moonOrbits)if(stat>=orbit.threshold)count++;return count;
            }
            // One Moon sphere among four in a 2000-point world gives about 19% Moon (its 3 of 4 units on a quarter of the points).
            int few=Count((ElementId.Moon,375),(ElementId.Void,1625)),some=Count((ElementId.Moon,600),(ElementId.Void,1400)),many=Count((ElementId.Moon,2000));
            Expect(Count((ElementId.Void,2000))==0 && few==1 && some==2 && many==style.moonOrbits.Count,"compositions: none 0, one Moon sphere of four "+few+" (want 1), 30% Moon "+some+" (want 2), Moon world "+many);

            // Completed-world previews (Results, System) show their moons at once and zoom out to keep them in frame.
            SpherePreview Preview(float moonStat)
            {
                var go=new GameObject("Moon preview",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));var preview=go.AddComponent<SpherePreview>();
                preview.surfaceMaterial=surface.GetComponent<Renderer>().sharedMaterial;var stats=new PlanetStats();stats[PlanetStat.Moons]=moonStat;preview.planetStats=stats;return preview;
            }
            var full=Preview(1);var bare=Preview(0);yield return null;yield return null;
            Expect(full.Moons && full.Moons.VisibleCount==style.moonOrbits.Count && full.PreviewCamera.orthographicSize>1,"a moon world's preview must show all its moons and zoom out");
            Expect(bare.Moons && bare.Moons.VisibleCount==0 && Mathf.Approximately(bare.PreviewCamera.orthographicSize,1),"a moonless preview keeps its framing");
            var previewCamera=full.PreviewCamera;
            foreach(var body in full.Moons.Bodies)
            {
                var local=previewCamera.transform.InverseTransformPoint(body.position);float reach=body.lossyScale.x*.5f;
                Expect(Mathf.Abs(local.x)+reach<=previewCamera.orthographicSize+.001f && Mathf.Abs(local.y)+reach<=previewCamera.orthographicSize+.001f,"a preview moon falls outside the frame");
            }
            previewCamera.Render();var old=RenderTexture.active;var texture=(RenderTexture)full.GetComponent<UnityEngine.UI.RawImage>().texture;RenderTexture.active=texture;
            var shot=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);shot.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);shot.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Planet-moons-preview.png"),shot.EncodeToPNG());Destroy(shot);
            Debug.Log("ECOSPHERE_PLANET_MOONS_PASSED: 0/1/2/3 moons by threshold, orbiting clear of the planet in a fixed frame, held while paused, compositions "+few+"/"+some+"/"+many+", previews show and frame them");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
