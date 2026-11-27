using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // Renders the live planet with element mixes that tint land, vegetation and water through the shared color traits,
        // measuring each change against an untinted baseline, then checks a completed-world preview keeps the tints.
        internal IEnumerator PlanetColorSmokeRun()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Planet color smoke: "+message);}
            enabled=false;gameplayCanvas.enabled=false;
            var surface=planet.GetComponent<PlanetSurface>();surface.rotationSpeed=0;
            var target=new RenderTexture(512,512,24);target.Create();view.targetTexture=target;
            var input=new double[Enum.GetValues(typeof(ElementId)).Length];
            Texture2D Capture(string name)
            {
                view.transform.position=new Vector3(planet.position.x,planet.position.y,-25);view.orthographicSize=5.6f;view.aspect=1;
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var capture=new Texture2D(512,512,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,512,512),0,0);capture.Apply();RenderTexture.active=previous;
                if(name!=null)File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/planet-color-"+name+".png"),capture.EncodeToPNG());return capture;
            }
            // Each case renders twice in the same frame state: with its color traits, then with them cleared on the
            // renderer. Only the tints differ, so element dilution (less Plant, different foliage) cannot confuse the check.
            var renderer=planet.GetComponent<Renderer>();
            (Color land,Color water,Color foliage) tinted=default,plain=default;
            void Split(Texture2D withTint,Texture2D without)
            {
                Color[] a=new Color[3],b=new Color[3];int[] n=new int[3];
                for(int y=60;y<452;y+=2)for(int x=60;x<452;x+=2)
                {
                    if((x-256)*(x-256)+(y-256)*(y-256)>200*200)continue;
                    var reference=without.GetPixel(x,y);
                    if(reference.r+reference.g+reference.b<.06f)continue;
                    // Classified on the untinted render: water is blue-dominant, foliage green-dominant, the rest land.
                    int kind=reference.b>reference.r*1.6f && reference.b>reference.g*1.15f ? 1 : reference.g>reference.r*1.2f && reference.g>reference.b*1.15f ? 2 : 0;
                    a[kind]+=withTint.GetPixel(x,y);b[kind]+=reference;n[kind]++;
                }
                Expect(n[0]>200 && n[1]>200 && n[2]>200,$"view needs land, water and foliage (land {n[0]}, water {n[1]}, foliage {n[2]})");
                tinted=(a[0]/n[0],a[1]/n[1],a[2]/n[2]);plain=(b[0]/n[0],b[1]/n[1],b[2]/n[2]);
            }
            IEnumerator Show(string name,params (ElementId element,double points)[] extra)
            {
                Array.Clear(input,0,input.Length);
                input[(int)ElementId.Water]=300;input[(int)ElementId.Plant]=600;input[(int)ElementId.Life]=300;input[(int)ElementId.Stone]=300;
                foreach(var part in extra)input[(int)part.element]=part.points;
                surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
                var withTint=Capture(name);
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))block.SetColor("_"+trait+"Tint",Color.clear);
                renderer.SetPropertyBlock(block);var without=Capture(name+"-untinted");
                Split(withTint,without);Destroy(withTint);Destroy(without);
            }
            float Luma(Color c)=>c.r*.2126f+c.g*.7152f+c.b*.0722f;

            yield return Show("plant");var plantWater=surface.Stats[PlanetColorTrait.Water];
            Expect(Mathf.Abs(Luma(tinted.land)-Luma(plain.land))<.01f && Mathf.Abs(Luma(tinted.foliage)-Luma(plain.foliage))<.01f,"Plant must leave land and vegetation untinted");

            yield return Show("crystal",(ElementId.Crystal,450));
            Expect(tinted.land.r-tinted.land.g>plain.land.r-plain.land.g+.03f && tinted.land.b-tinted.land.g>plain.land.b-plain.land.g+.02f,"Crystal must turn exposed land pink-magenta");

            yield return Show("magic",(ElementId.Magic,450));
            Expect(tinted.foliage.b-tinted.foliage.g>plain.foliage.b-plain.foliage.g+.03f,"Magic must turn vegetation violet");
            // The base mix has Plant's faint water tint; Magic must not add its own hue to it.
            var magicWater=surface.Stats[PlanetColorTrait.Water];
            Expect(Mathf.Abs(magicWater.r-plantWater.r)+Mathf.Abs(magicWater.g-plantWater.g)+Mathf.Abs(magicWater.b-plantWater.b)<.01f,"Magic must leave the water color alone");

            yield return Show("mud",(ElementId.Mud,450));
            Expect(tinted.water.b<plain.water.b-.01f && tinted.water.r-tinted.water.b>plain.water.r-plain.water.b+.01f,"Mud must muddy the water brown");

            yield return Show("coal",(ElementId.Coal,450));
            // The atmosphere haze over the disc softens the measured drop; a 23% Coal share darkens bare land by roughly 13%.
            Expect(Luma(tinted.land)<Luma(plain.land)*.92f,"Coal must darken the land (tinted "+tinted.land+" plain "+plain.land+", tint "+surface.Stats[PlanetColorTrait.Land]+")");

            yield return Show("forest",(ElementId.Forest,450));
            Expect(Luma(tinted.foliage)<Luma(plain.foliage)*.95f && tinted.foliage.g>tinted.foliage.r && tinted.foliage.g>tinted.foliage.b,"Forest must deepen the vegetation green");
            // Same planet with equal Plant or Forest: Forest covers more land in vegetation.
            int Foliage(Texture2D image)
            {
                int count=0;
                for(int y=60;y<452;y+=2)for(int x=60;x<452;x+=2)
                {
                    if((x-256)*(x-256)+(y-256)*(y-256)>200*200)continue;
                    var c=image.GetPixel(x,y);if(c.g>c.r*1.2f && c.g>c.b*1.15f && c.g>.08f)count++;
                }
                return count;
            }
            int[] grown=new int[2];
            for(int i=0;i<2;i++)
            {
                Array.Clear(input,0,input.Length);
                input[(int)ElementId.Water]=300;input[(int)ElementId.Stone]=300;input[(int)ElementId.Life]=300;input[i==0?(int)ElementId.Plant:(int)ElementId.Forest]=200;   // minority share: a majority saturates either
                surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
                var world=Capture(i==0?"compare-plant":"compare-forest");grown[i]=Foliage(world);Destroy(world);
            }
            Expect(grown[1]>grown[0],"Forest must cover more land in vegetation than Plant (plant "+grown[0]+", forest "+grown[1]+")");

            // Sand starts at the shoreline and spreads inland with more sediment. Each case renders with and without
            // sediment (same frame state), so the difference is only the sand.
            var shore=new (int pixels,float nearWater,float sandLuma,float landLuma)[2];
            for(int i=0;i<2;i++)
            {
                Array.Clear(input,0,input.Length);
                input[(int)ElementId.Water]=350;input[(int)ElementId.Stone]=600;input[(int)ElementId.Sand]=i==0?150:1400;
                surface.ApplyComposition(input);yield return new WaitForSeconds(3.6f);
                var withSand=Capture(i==0?"sand-beaches":"sand-inland");
                var noSand=new MaterialPropertyBlock();renderer.GetPropertyBlock(noSand);noSand.SetFloat("_Sediment",0);renderer.SetPropertyBlock(noSand);
                var without=Capture(null);
                bool Water(Color c)=>c.b>c.r*1.6f && c.b>c.g*1.15f;
                int sandPixels=0,nearWater=0;float sandLuma=0,landLuma=0;
                for(int y=60;y<452;y+=2)for(int x=60;x<452;x+=2)
                {
                    if((x-256)*(x-256)+(y-256)*(y-256)>195*195)continue;
                    var before=without.GetPixel(x,y);var after=withSand.GetPixel(x,y);
                    if(Water(before) || Luma(after-before)<.04f)continue;
                    sandPixels++;sandLuma+=Luma(after);landLuma+=Luma(before);
                    bool close=false;
                    for(int dy=-12;dy<=12 && !close;dy+=3)for(int dx=-12;dx<=12 && !close;dx+=3)close=Water(without.GetPixel(x+dx,y+dy));
                    if(close)nearWater++;
                }
                Destroy(withSand);Destroy(without);
                shore[i]=(sandPixels,sandPixels>0?nearWater/(float)sandPixels:0,sandPixels>0?sandLuma/sandPixels:0,sandPixels>0?landLuma/sandPixels:0);
                Debug.Log($"ECOSPHERE_SAND_SHORE {(i==0?"beaches":"inland")}: sand pixels={shore[i].pixels}, near water={shore[i].nearWater:0.00}, sand luma={shore[i].sandLuma:0.000} vs land {shore[i].landLuma:0.000}");
            }
            Expect(shore[0].pixels>60 && shore[0].nearWater>.75f,"a little sand must form beaches along the water (near water "+shore[0].nearWater+")");
            Expect(shore[1].pixels>shore[0].pixels*2 && shore[1].nearWater<shore[0].nearWater-.1f,"more sand must spread inland (pixels "+shore[1].pixels+", near water "+shore[1].nearWater+")");
            Expect(shore[0].sandLuma>shore[0].landLuma && shore[1].sandLuma>shore[1].landLuma,"sand must be lighter than the land it covers");

            // A completed world keeps its tints in the saved snapshot and its preview shader inputs.
            yield return Show("crystal-magic",(ElementId.Crystal,450),(ElementId.Magic,450),(ElementId.Mud,300));
            var completed=GameServices.Progress.Complete(8,0,surface.TargetSeaLevel,planet.eulerAngles.y,null,input).world;
            var copy=JsonUtility.FromJson<CompletedWorld>(JsonUtility.ToJson(completed));
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))
                Expect(copy.planetStats[trait].a>0 && copy.planetStats[trait]==completed.planetStats[trait],"saved world lost its "+trait+" tint");
            var ui=new GameObject("Saved color preview",typeof(RectTransform),typeof(RawImage));var preview=ui.AddComponent<SpherePreview>();
            preview.surfaceMaterial=receivingPlanetMaterial;preview.planetStats=copy.planetStats;preview.seaLevel=copy.seaLevel;preview.initialRotation=copy.rotation;preview.rotationSpeed=0;
            yield return null;preview.PreviewCamera.Render();
            var block=new MaterialPropertyBlock();preview.BodyRenderer.GetPropertyBlock(block);
            foreach(PlanetColorTrait trait in Enum.GetValues(typeof(PlanetColorTrait)))
                Expect(Mathf.Abs(block.GetColor("_"+trait+"Tint").a-copy.planetStats[trait].a)<1e-4f,"preview lost the "+trait+" tint");
            var previewPixels=new Texture2D(256,256,TextureFormat.RGBA32,false);var previous=RenderTexture.active;RenderTexture.active=preview.PreviewCamera.targetTexture;
            previewPixels.ReadPixels(new Rect(0,0,256,256),0,0);previewPixels.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/planet-color-saved-preview.png"),previewPixels.EncodeToPNG());
            Destroy(previewPixels);Destroy(ui);view.targetTexture=null;target.Release();Destroy(target);
            Debug.Log($"ECOSPHERE_PLANET_COLOR_SMOKE_PASSED: Crystal pink land, Magic violet vegetation with water untouched, Mud brown water, Coal dark land, saved snapshot and preview tints");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
    public sealed class PlanetColorSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-planetColorSmoke"))new GameObject("Planet color checks").AddComponent<PlanetColorSmoke>();}
        IEnumerator Start(){yield return null;yield return FindAnyObjectByType<EcoSpherePrototype>().PlanetColorSmokeRun();}
    }
}
