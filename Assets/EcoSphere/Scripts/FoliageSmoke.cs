using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal IEnumerator FoliageSmokeRun()
        {
            enabled=false;gameplayCanvas.enabled=false;
            var surface=planet.GetComponent<PlanetSurface>();surface.rotationSpeed=0;
            var renderer=planet.GetComponent<Renderer>();
            var target=new RenderTexture(512,512,24);target.Create();view.targetTexture=target;
            var input=new double[Enum.GetValues(typeof(ElementId)).Length];
            Texture2D Capture(string name)
            {
                view.transform.position=new Vector3(planet.position.x,planet.position.y,-25);view.orthographicSize=5.6f;view.aspect=1;
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var capture=new Texture2D(512,512,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,512,512),0,0);capture.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/foliage-"+name+".png"),capture.EncodeToPNG());return capture;
            }
            bool Green(Color c)=>c.g>c.r*1.2f && c.g>c.b*1.15f && c.g>.15f;
            int CountGreen(Texture2D image)
            {
                int result=0;for(int y=40;y<472;y++)for(int x=40;x<472;x++)if((x-256)*(x-256)+(y-256)*(y-256)<210*210 && Green(image.GetPixel(x,y)))result++;return result;
            }
            int bare=0,temperate=0,dense=0;
            foreach(string name in new[]{"bare","temperate","dense","hot","cold"})
            {
                Array.Clear(input,0,input.Length);
                input[(int)ElementId.Water]=300;
                if(name=="bare"){input[(int)ElementId.Stone]=300;input[(int)ElementId.Void]=600;}
                else {input[(int)ElementId.Plant]=name=="dense"?1200:600;input[(int)ElementId.Life]=300;}
                if(name=="hot")input[(int)ElementId.Sunlight]=3000;
                if(name=="cold")input[(int)ElementId.Ice]=3000;
                surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
                var image=Capture(name);int green=CountGreen(image);
                if(name=="bare")bare=green;
                if(name=="temperate")
                {
                    temperate=green;
                    yield return CheckFoliageElevation(image,renderer,surface);
                }
                if(name=="dense")dense=green;
                if((name=="hot" || name=="cold") && green>temperate*.2f+50)throw new Exception("Extreme climate did not reduce foliage: "+name);
                Debug.Log($"ECOSPHERE_FOLIAGE_PREVIEW {name}: greenPixels={green}, growth={surface.Stats.VegetationPotential}, tempF={surface.Stats.TemperatureFahrenheit}");Destroy(image);
            }
            if(temperate<bare+1000 || dense<=temperate)throw new Exception($"Foliage must grow in clusters with contribution: bare={bare}, temperate={temperate}, dense={dense}");
            // Water tint changes only its color; compare the same submerged terrain with tint toggled.
            Array.Clear(input,0,input.Length);input[(int)ElementId.Plant]=600;input[(int)ElementId.Life]=300;input[(int)ElementId.Water]=300;
            surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            float tint=block.GetFloat("_WaterGreenness");
            var tinted=Capture("water-tint");
            block.SetFloat("_WaterGreenness",0);renderer.SetPropertyBlock(block);var untinted=Capture("water-plain");
            double delta=0;int count=0;
            for(int y=80;y<432;y++)for(int x=80;x<432;x++)
            {
                var a=tinted.GetPixel(x,y);var b=untinted.GetPixel(x,y);
                if(b.b>b.r*2 && b.b>b.g*1.3f){delta+=a.g-b.g;count++;}
            }
            Destroy(tinted);Destroy(untinted);
            if(tint<=0 || count<100 || delta/count<=.00001 || delta/count>.02)throw new Exception($"Water tint must be very subtle and positive: delta={delta/count}, pixels={count}");
            var completed=WorldProgress.Complete(8,0,surface.TargetSeaLevel,planet.eulerAngles.y,false,input).world;
            var copy=JsonUtility.FromJson<CompletedWorld>(JsonUtility.ToJson(completed));
            if(copy.planetStats.VegetationPotential!=surface.Stats.VegetationPotential || copy.planetStats[PlanetStat.WaterGreenness]!=tint)throw new Exception("Saved foliage appearance mismatch");
            var ui=new GameObject("Saved foliage preview",typeof(RectTransform),typeof(RawImage));var preview=ui.AddComponent<SpherePreview>();
            preview.surfaceMaterial=receivingPlanetMaterial;preview.planetStats=copy.planetStats;preview.seaLevel=copy.seaLevel;preview.initialRotation=copy.rotation;preview.rotationSpeed=0;
            yield return null;preview.PreviewCamera.Render();
            var previewBlock=new MaterialPropertyBlock();preview.PreviewCamera.transform.parent.Find("Sphere").GetComponent<Renderer>().GetPropertyBlock(previewBlock);
            if(previewBlock.GetFloat("_Vegetation")!=copy.planetStats.VegetationPotential || previewBlock.GetFloat("_WaterGreenness")!=tint || previewBlock.GetVector("_TerrainBounds")!=PlanetSurface.TerrainBounds(receivingPlanetMaterial))throw new Exception("Saved preview lost foliage/tint/terrain parameters");
            Destroy(ui);view.targetTexture=null;target.Release();Destroy(target);
            Debug.Log($"ECOSPHERE_FOLIAGE_SMOKE_PASSED clustered growth, mid-height preference, hot/cold reduction, ocean mask, subtle water tint delta={delta/count}, saved preview stats");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        IEnumerator CheckFoliageElevation(Texture2D image,Renderer renderer,PlanetSurface surface)
        {
            // Raycast actual mesh UVs to compare rendered vegetation against the source elevation.
            var mesh=planet.gameObject.AddComponent<MeshCollider>();mesh.sharedMesh=planet.GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();
            var source=renderer.sharedMaterial.GetTexture("_HeightMap");var rt=RenderTexture.GetTemporary(256,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            Graphics.Blit(source,rt);var previous=RenderTexture.active;RenderTexture.active=rt;
            var heights=new Texture2D(256,128,TextureFormat.RGBA32,false,true);heights.ReadPixels(new Rect(0,0,256,128),0,0);heights.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
            var bounds=PlanetSurface.TerrainBounds(renderer.sharedMaterial);float low=Mathf.Max(surface.seaLevel,bounds.x);
            int[] samples=new int[3],green=new int[3];
            for(int y=50;y<462;y+=5)for(int x=50;x<462;x+=5)
            {
                var ray=view.ViewportPointToRay(new Vector3((x+.5f)/512,(y+.5f)/512,0));
                if(!mesh.Raycast(ray,out var hit,100))continue;
                float elevation=heights.GetPixelBilinear(hit.textureCoord.x,hit.textureCoord.y).r;
                if(elevation<=surface.seaLevel+.015f)continue;
                float altitude=Mathf.Clamp01((elevation-low)/Mathf.Max(.05f,bounds.y-low));
                int band=altitude<.1f?0:altitude>=.25f && altitude<=.6f?1:altitude>.85f?2:-1;
                if(band<0)continue;samples[band]++;
                var c=image.GetPixel(x,y);if(c.g>c.r*1.2f && c.g>c.b*1.15f && c.g>.15f)green[band]++;
            }
            Destroy(mesh);Destroy(heights);
            float valley=(float)green[0]/Mathf.Max(1,samples[0]),middle=(float)green[1]/Mathf.Max(1,samples[1]),peak=(float)green[2]/Mathf.Max(1,samples[2]);
            if(samples[0]<5 || samples[1]<20 || samples[2]<5 || middle<=valley || middle<=peak)throw new Exception($"Foliage elevation preference: valley={valley}({samples[0]}), middle={middle}({samples[1]}), peak={peak}({samples[2]})");
            Debug.Log($"ECOSPHERE_FOLIAGE_ELEVATION_PASSED valley={valley}, middle={middle}, peak={peak}");yield break;
        }
    }
    public sealed class FoliageSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foliageSmoke")>=0)new GameObject("Foliage checks").AddComponent<FoliageSmoke>();}
        IEnumerator Start(){yield return null;yield return FindAnyObjectByType<EcoSpherePrototype>().FoliageSmokeRun();}
    }
}
