using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace EcoSphere
{
    public sealed class CloudSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-cloudSmoke")>=0)new GameObject("Cloud checks").AddComponent<CloudSmoke>();}
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
            var renderer=clouds.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);float phase=block.GetFloat("_Drift");
            yield return new WaitForSeconds(.2f);renderer.GetPropertyBlock(block);if(phase==block.GetFloat("_Drift"))throw new Exception("Cloud texture did not drift independently of stationary globe");
            Time.timeScale=0;phase=block.GetFloat("_Drift");yield return new WaitForSecondsRealtime(.2f);renderer.GetPropertyBlock(block);
            if(phase!=block.GetFloat("_Drift"))throw new Exception("Cloud drift ignored pause");Time.timeScale=1;
            camera.targetTexture=null;target.Release();Destroy(target);
            int cloudCount=FindObjectsByType<PlanetClouds>(FindObjectsSortMode.None).Length;
            var previewObject=new GameObject("Completed cloud preview",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));var preview=previewObject.AddComponent<SpherePreview>();preview.surfaceMaterial=surface.GetComponent<Renderer>().sharedMaterial;
            preview.planetStats=surface.Stats.Copy();yield return null;yield return null;
            if(FindObjectsByType<PlanetClouds>(FindObjectsSortMode.None).Length!=cloudCount+1)throw new Exception("Completed preview did not create its cloud shell");
            Debug.Log("ECOSPHERE_CLOUD_RENDER_PASSED: live clear/wispy/dense/dark renders, independent drift, pause, completed-preview creation");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
