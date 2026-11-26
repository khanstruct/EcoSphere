using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Renders the live planet with and without its city lights (same frame state) so the difference is only the lights.
        internal IEnumerator CityLightSmokeRun()
        {
            // -cityLightLooks renders every case for tuning and logs failed checks instead of stopping.
            bool looksOnly=LaunchFlags.Has("-cityLightLooks");
            void Expect(bool value,string message){if(value)return;if(looksOnly)Debug.LogWarning("CITY_LIGHT_CHECK_FAILED "+message);else throw new Exception("City light smoke: "+message);}
            enabled=false;gameplayCanvas.enabled=false;
            var surface=planet.GetComponent<PlanetSurface>();surface.rotationSpeed=0;
            var renderer=planet.GetComponent<Renderer>();
            Light sun=null;foreach(var candidate in FindObjectsByType<Light>(FindObjectsInactive.Exclude))if(candidate.type==LightType.Directional){sun=candidate;break;}
            Expect(sun,"no directional light");var gameLight=sun.transform.rotation;
            var sideLight=Quaternion.Euler(0,-90,0);   // lit from screen right: the left half is night
            var target=new RenderTexture(512,512,24);target.Create();view.targetTexture=target;
            var input=new double[Enum.GetValues(typeof(ElementId)).Length];
            Texture2D Capture(string name)
            {
                view.transform.position=new Vector3(planet.position.x,planet.position.y,-25);view.orthographicSize=5.6f;view.aspect=1;
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var capture=new Texture2D(512,512,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,512,512),0,0);capture.Apply();RenderTexture.active=previous;
                if(name!=null)File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/city-lights-"+name+".png"),capture.EncodeToPNG());return capture;
            }
            void Lights(bool on)
            {
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                if(!on)block.SetFloat("_Civilization",0);else surface.Stats.Apply(block);
                renderer.SetPropertyBlock(block);
            }
            float Luma(Color c)=>c.r*.2126f+c.g*.7152f+c.b*.0722f;
            // Emission = render with lights minus the same render without them, split into the night (left) and day (right) halves.
            (int lit,float night,float day,Color hue,float meanLit) Measure(Texture2D with,Texture2D without)
            {
                int lit=0;float night=0,day=0,litSum=0;Color hue=Color.clear;
                for(int y=56;y<456;y+=2)for(int x=56;x<456;x+=2)
                {
                    float nx=(x-256)/200f,ny=(y-256)/200f;if(nx*nx+ny*ny>1)continue;
                    var d=with.GetPixel(x,y)-without.GetPixel(x,y);float l=Luma(d);
                    if(nx<-.3f)night+=Mathf.Max(0,l);else if(nx>.3f)day+=Mathf.Max(0,l);
                    if(l>.04f){lit++;litSum+=l;hue+=d;}
                }
                return (lit,night,day,lit>0?hue/lit:Color.clear,lit>0?litSum/lit:0);
            }
            IEnumerator Compose(params (ElementId element,double points)[] extra)
            {
                Array.Clear(input,0,input.Length);
                input[(int)ElementId.Water]=400;input[(int)ElementId.Stone]=400;input[(int)ElementId.Plant]=200;
                foreach(var part in extra)input[(int)part.element]=part.points;
                surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
            }
            (int lit,float night,float day,Color hue,float meanLit) result=default;
            IEnumerator Case(string name,Quaternion lighting,params (ElementId element,double points)[] extra)
            {
                yield return Compose(extra);sun.transform.rotation=lighting;
                Lights(true);var with=Capture(name);Lights(false);var without=Capture(null);Lights(true);
                result=Measure(with,without);Destroy(with);Destroy(without);
                Debug.Log($"ECOSPHERE_CITY_LIGHT_CASE {name}: lit={result.lit} night={result.night:0.0} day={result.day:0.0} mean={result.meanLit:0.000} hue={result.hue} civ={surface.Stats[PlanetStat.Civilization]:0.00} intensity={surface.Stats[PlanetStat.CityLightIntensity]:0.00}");
            }

            yield return Case("wild",sideLight);
            Expect(result.lit==0,"a world without civilization must show no lights");
            // Civilization alone is population sprawl: lights spread but stay dim; Technology brightens the same sprawl.
            yield return Case("settled",sideLight,(ElementId.Civilization,200));var settled=result;
            Expect(settled.lit>20,"a small civilization must show a few settlements");
            yield return Case("sprawl",sideLight,(ElementId.Civilization,900));var sprawl=result;
            Expect(sprawl.lit>settled.lit*1.5f,"more civilization must spread the lights further");
            yield return Case("sprawl-technology",sideLight,(ElementId.Civilization,900),(ElementId.Technology,450));
            Expect(result.meanLit>sprawl.meanLit*1.4f,"Technology must brighten a civilization's lights");
            Expect(result.lit>sprawl.lit*1.2f,"only Technology may fill sprawl in with the dense urban network");
            yield return Case("city",sideLight,(ElementId.City,900));var city=result;
            Expect(city.lit>settled.lit*1.5f,"more civilization must reveal more settlements");
            Expect(city.night>city.day*2,"lights must be strongest on the night side");
            Expect(city.day>0,"lights must stay faintly visible on the lit side");
            Expect(city.hue.r>city.hue.b*1.3f,"default lights must be warm");
            yield return Case("technology",sideLight,(ElementId.City,450),(ElementId.Technology,450));
            // Bright pixels clip in the 8-bit capture, compressing the measured gain.
            Expect(result.meanLit>city.meanLit*1.1f,"Technology must brighten the lights");
            yield return Case("magic",sideLight,(ElementId.City,600),(ElementId.Magic,600));
            Expect(result.hue.b>result.hue.g*1.2f && result.hue.r>result.hue.g,"Magic must turn the lights violet");

            // Clouds partly hide the lights: the same lights render dimmer with the cloud shell than without it.
            yield return Compose((ElementId.City,900),(ElementId.Air,600));sun.transform.rotation=sideLight;
            var clouds=planet.GetComponentInChildren<PlanetClouds>(true);Expect(clouds,"no cloud shell");var shell=clouds.GetComponent<Renderer>();
            Lights(true);var a=Capture("clouded");Lights(false);var b=Capture(null);
            shell.enabled=false;Lights(true);var c=Capture(null);Lights(false);var d=Capture(null);shell.enabled=true;Lights(true);
            // Compare light emission only where the cloud shell actually covers the surface (its pixels differ with the shell on),
            // so how much of the planet the drifting clouds happen to cover does not matter.
            float covered=0,uncovered=0;int cloudPixels=0;
            for(int y=56;y<456;y+=2)for(int x=56;x<456;x+=2)
            {
                if(Mathf.Abs(Luma(b.GetPixel(x,y))-Luma(d.GetPixel(x,y)))<.03f)continue;
                covered+=Mathf.Max(0,Luma(a.GetPixel(x,y)-b.GetPixel(x,y)));uncovered+=Mathf.Max(0,Luma(c.GetPixel(x,y)-d.GetPixel(x,y)));cloudPixels++;
            }
            Destroy(a);Destroy(b);Destroy(c);Destroy(d);
            Expect(cloudPixels>200 && uncovered>0 && covered<uncovered*.85f,"clouds must partly obscure the lights beneath them ("+covered+" vs "+uncovered+" over "+cloudPixels+" cloud pixels)");

            // Twinkle is local: the same frame a moment later differs per pixel but keeps the same overall glow.
            yield return Compose((ElementId.City,900));sun.transform.rotation=sideLight;
            shell.enabled=false;   // drifting clouds would also change pixels
            var first=Capture(null);yield return new WaitForSeconds(.6f);var second=Capture(null);shell.enabled=true;
            int changed=0;float firstSum=0,secondSum=0;
            for(int y=56;y<456;y+=2)for(int x=56;x<456;x+=2)
            {
                float l1=Luma(first.GetPixel(x,y)),l2=Luma(second.GetPixel(x,y));firstSum+=l1;secondSum+=l2;
                if(Mathf.Abs(l1-l2)>.01f)changed++;
            }
            Destroy(first);Destroy(second);
            Expect(changed>20 && Mathf.Abs(firstSum-secondSum)<firstSum*.05f,"lights must twinkle locally, not pulse together ("+changed+" changed)");

            // The game's own lighting shows mostly the lit face; lights must still read there.
            yield return Case("game-sprawl",gameLight,(ElementId.Civilization,900));
            Expect(result.lit>20,"plain sprawl must be visible under the game's lighting");
            yield return Case("game-magic",gameLight,(ElementId.Civilization,600),(ElementId.Magic,600));
            Expect(result.lit>20,"magical lights must be visible under the game's lighting");
            yield return Case("game-lighting",gameLight,(ElementId.City,450),(ElementId.Technology,450));
            Expect(result.lit>20,"lights must be visible under the game's lighting");

            // A completed world keeps its lights in the snapshot and preview.
            var completed=GameServices.Progress.Complete(8,0,surface.TargetSeaLevel,planet.eulerAngles.y,null,input).world;
            var copy=JsonUtility.FromJson<CompletedWorld>(JsonUtility.ToJson(completed));
            Expect(copy.planetStats[PlanetStat.Civilization]>0 && copy.planetStats[PlanetStat.CityLightIntensity]>0,"saved world lost its city lights");
            var ui=new GameObject("Saved city preview",typeof(RectTransform),typeof(RawImage));var preview=ui.AddComponent<SpherePreview>();
            preview.surfaceMaterial=receivingPlanetMaterial;preview.planetStats=copy.planetStats;preview.seaLevel=copy.seaLevel;preview.initialRotation=copy.rotation;preview.rotationSpeed=0;
            yield return null;preview.PreviewCamera.Render();
            var block=new MaterialPropertyBlock();preview.BodyRenderer.GetPropertyBlock(block);
            Expect(Mathf.Approximately(block.GetFloat("_Civilization"),copy.planetStats[PlanetStat.Civilization]) &&
                Mathf.Approximately(block.GetFloat("_CityLightIntensity"),copy.planetStats[PlanetStat.CityLightIntensity]),"preview lost city light inputs");
            var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);var previous=RenderTexture.active;RenderTexture.active=preview.PreviewCamera.targetTexture;
            pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/city-lights-saved-preview.png"),pixels.EncodeToPNG());
            Destroy(pixels);Destroy(ui);sun.transform.rotation=gameLight;view.targetTexture=null;target.Release();Destroy(target);
            Debug.Log("ECOSPHERE_CITY_LIGHT_SMOKE_PASSED: none without civilization, coverage grows, night-side strength with a lit-side floor, warm default, Technology brightness, Magic violet, cloud obscuring, local twinkle, game lighting, saved preview");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
    public sealed class CityLightSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-cityLightSmoke"))new GameObject("City light checks").AddComponent<CityLightSmoke>();}
        IEnumerator Start(){yield return null;yield return FindAnyObjectByType<EcoSpherePrototype>().CityLightSmokeRun();}
    }
}
