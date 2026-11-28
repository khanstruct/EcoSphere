using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    public sealed class CloudSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-cloudSmoke"))new GameObject("Cloud checks").AddComponent<CloudSmoke>();}
        static void Expect(bool value,string message){if(!value)throw new Exception("Clouds: "+message);}
        static float Brightness(Camera camera,RenderTexture target,string save=null)
        {
            camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=old;
            float sum=0;foreach(var c in pixels.GetPixels32())sum+=c.r+c.g+c.b;
            if(save!=null)File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/"+save),pixels.EncodeToPNG());
            Destroy(pixels);return sum;
        }
        // Flashes appear only inside storm clouds, land on cloudy spots, brighten the render, stop while paused,
        // and come from a real Lightning + Air composition through the shared stats.
        IEnumerator CheckLightning(PlanetSurface surface,PlanetClouds clouds,Camera camera,RenderTexture target)
        {
            void Storm(float cover,float weather,float electric)
            {
                surface.Stats.Clear();surface.Stats[PlanetStat.CloudCover]=cover;surface.Stats[PlanetStat.WeatherActivity]=weather;surface.Stats[PlanetStat.ElectricalActivity]=electric;
            }
            IEnumerator Quiet(string label)
            {
                yield return new WaitForSeconds(2.5f);int before=clouds.FlashCount;   // shown stats ease toward the new ones first
                yield return new WaitForSeconds(3f);
                Expect(clouds.FlashCount==before && clouds.FlashStrength==0,label+" must not flash");
            }
            Storm(.65f,1,0);yield return Quiet("cloudy skies without electricity");
            Storm(0,1,1);yield return Quiet("clear skies");
            Storm(.65f,1,1);yield return new WaitForSeconds(2.5f);   // let the clouds settle so lit and dark frames match
            int start=clouds.FlashCount;float deadline=Time.time+30,dark=-1,lit=-1;bool saved=false;int facing=0;
            while(Time.time<deadline && (clouds.FlashCount<start+4 || lit<0 || dark<0))
            {
                yield return null;
                if(clouds.FlashCount>start && clouds.FlashStrength>0)
                    Expect(clouds.CurrentCloud>.3f,"a flash landed in clear sky (cloud "+clouds.CurrentCloud+" at "+clouds.FlashUV+", strength "+clouds.FlashStrength+")");
                if(clouds.FlashStrength==0 && lit>0 && dark<0)dark=Brightness(camera,target,"Cloud-lightning-dark.png");
                if(clouds.FlashStrength>PlanetVisualStyle.Current.lightningBrightness*.5f && lit<0 && Vector3.Dot(clouds.transform.TransformDirection(clouds.FlashDirection),(camera.transform.position-clouds.transform.position).normalized)>.3f)
                {lit=Brightness(camera,target,saved ? null : "Cloud-lightning.png");saved=true;Debug.Log("ECOSPHERE_CLOUD_FLASH_AT "+clouds.FlashDirection+" strength "+clouds.FlashStrength);}
            }
            Expect(clouds.FlashCount>=start+4,"a strong storm must flash repeatedly ("+(clouds.FlashCount-start)+" flashes)");
            Expect(lit>0 && dark>0 && lit>dark*1.003f,"a facing flash must brighten the clouds (lit "+lit+", dark "+dark+", "+(clouds.FlashCount-start)+" flashes)");
            Time.timeScale=0;yield return new WaitForSecondsRealtime(.4f);int paused=clouds.FlashCount;float pausedStrength=clouds.FlashStrength;
            yield return new WaitForSecondsRealtime(2f);
            Expect(clouds.FlashCount==paused && clouds.FlashStrength==pausedStrength,"lightning must hold while paused");Time.timeScale=1;
            var amounts=new double[ElementComposition.Capacity];amounts[(int)ElementId.Lightning]=500;amounts[(int)ElementId.Air]=250;
            var real=PlanetStatCalculator.Evaluate(amounts);Expect(real.CloudLightning>PlanetVisualStyle.Current.lightningMinimumStorm,"a Lightning sphere's Lightning and Air must make a storm ("+real.CloudLightning+")");
            surface.Stats.CopyFrom(real);start=clouds.FlashCount;deadline=Time.time+20;
            while(Time.time<deadline && clouds.FlashCount<start+1)yield return null;
            Expect(clouds.FlashCount>start,"a real Lightning + Air storm must flash");
            Debug.Log("ECOSPHERE_CLOUD_LIGHTNING: storm="+real.CloudLightning.ToString("0.00")+", lit/dark brightness "+(lit/dark).ToString("0.000"));
        }
        IEnumerator Start()
        {
            yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();game.enabled=false;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            var surface=FindAnyObjectByType<PlanetSurface>();surface.rotationSpeed=0;
            var clouds=surface.GetComponentInChildren<PlanetClouds>();if(!clouds)throw new Exception("Gameplay globe has no cloud shell");
            var camera=game.GameplayView;camera.transform.position=surface.transform.position+new Vector3(0,0,-15);camera.transform.rotation=Quaternion.identity;
            camera.orthographic=true;camera.aspect=1;camera.orthographicSize=surface.transform.lossyScale.x*.66f;
            var target=new RenderTexture(512,512,24);target.Create();camera.targetTexture=target;
            byte[] previous=null;
            foreach(string sample in new[]{"clear","wispy","dense","dark","mixed"})
            {
                surface.Stats.Clear();surface.Stats[PlanetStat.CloudCover]=sample=="clear"?0:.65f;
                surface.Stats[PlanetStat.CloudThickness]=sample=="dense" || sample=="dark" ?.75f:0;
                surface.Stats[PlanetStat.CloudDarkness]=sample=="dark"?.85f:0;
                if(sample=="mixed")
                {
                    var amounts=new double[ElementComposition.Capacity];amounts[(int)ElementId.Air]=500;amounts[(int)ElementId.Water]=500;
                    surface.Stats.CopyFrom(PlanetStatCalculator.Evaluate(amounts));
                    if(surface.Stats[PlanetStat.CloudCover]!=1)throw new Exception("Mixed planet did not reach full clouds");
                }
                yield return new WaitForSeconds(1.6f);
                camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
                var pixels=new Texture2D(512,512,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();
                var bytes=pixels.EncodeToPNG();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Cloud-"+sample+".png"),bytes);
                if(previous!=null && Convert.ToBase64String(previous)==Convert.ToBase64String(bytes))throw new Exception("Cloud appearance did not change");
                previous=bytes;RenderTexture.active=old;Destroy(pixels);
            }
            yield return CheckLightning(surface,clouds,camera,target);
            var renderer=clouds.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);float phase=block.GetFloat("_Drift");
            yield return new WaitForSeconds(.2f);renderer.GetPropertyBlock(block);if(phase==block.GetFloat("_Drift"))throw new Exception("Cloud texture did not drift independently of stationary globe");
            Time.timeScale=0;phase=block.GetFloat("_Drift");yield return new WaitForSecondsRealtime(.2f);renderer.GetPropertyBlock(block);
            if(phase!=block.GetFloat("_Drift"))throw new Exception("Cloud drift ignored pause");Time.timeScale=1;
            camera.targetTexture=null;target.Release();Destroy(target);
            int cloudCount=FindObjectsByType<PlanetClouds>(FindObjectsSortMode.None).Length;
            var previewObject=new GameObject("Completed cloud preview",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));var preview=previewObject.AddComponent<SpherePreview>();preview.surfaceMaterial=surface.GetComponent<Renderer>().sharedMaterial;
            preview.planetStats=surface.Stats.Copy();yield return null;yield return null;
            if(FindObjectsByType<PlanetClouds>(FindObjectsSortMode.None).Length!=cloudCount+1)throw new Exception("Completed preview did not create its cloud shell");
            Debug.Log("ECOSPHERE_CLOUD_RENDER_PASSED: live clear/wispy/dense/dark renders, lightning flashes in storm clouds only (none when calm, clear or paused; real Lightning + Air storm), independent drift, pause, completed-preview creation");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
