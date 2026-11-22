using System;
using System.Collections;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        IEnumerator PlanetStatsSmokeRun()
        {
            yield return new WaitForSeconds(.2f);
            var surface=planet.GetComponent<PlanetSurface>();
            if(!receivingPlanetMaterial || surface.Stats.Temperature!=0)throw new Exception("Planet must start neutral");
            float rotation=planet.eulerAngles.y;yield return new WaitForSeconds(.2f);
            if(Mathf.Abs(Mathf.DeltaAngle(rotation,planet.eulerAngles.y))<.1f)throw new Exception("Planet does not rotate");
            Time.timeScale=0;rotation=planet.eulerAngles.y;yield return new WaitForSecondsRealtime(.1f);
            if(Mathf.Abs(Mathf.DeltaAngle(rotation,planet.eulerAngles.y))>.01f)throw new Exception("Planet rotated while paused");
            Time.timeScale=1;surface.rotationSpeed=0;gameplayCanvas.enabled=false;
            displayedElements.Clear();channelSphereTypes.Clear();
            foreach(int type in new[]{SphereIds.Void,SphereIds.Fire,SphereIds.Ice,SphereIds.Water,SphereIds.Stone,SphereIds.Sunlight})IncludeElements(Definition(type));
            displayedElements.Add(ElementId.Sand);channelSphereTypes.Add(SphereIds.Stone);
            displayedElements.Add(ElementId.Lava);channelSphereTypes.Add(SphereIds.Fire);
            var target=new RenderTexture(512,512,24);view.targetTexture=target;
            var cases=new[]{"neutral","water","ice","light-frost","mixed-cold","ice-void","low-score-ice","low-score-second-ice","balanced","hot-desert","rocky-volcano","frozen-volcano","minor-fire"};
            foreach(string sample in cases)
            {
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-frozenVolcanoOnly")>=0 && sample!="frozen-volcano")continue;
                Array.Clear(totals,0,totals.Length);
                switch(sample)
                {
                    case "neutral":totals[(int)ElementId.Void]=900;break;
                    case "water":totals[(int)ElementId.Water]=900;break;
                    case "ice":totals[(int)ElementId.Ice]=900;break;
                    case "light-frost":totals[(int)ElementId.Ice]=100;totals[(int)ElementId.Void]=900;break;
                    case "mixed-cold":totals[(int)ElementId.Ice]=300;totals[(int)ElementId.Void]=700;break;
                    case "ice-void":totals[(int)ElementId.Ice]=490;totals[(int)ElementId.Void]=510;break;
                    case "low-score-ice":totals[(int)ElementId.Ice]=110;totals[(int)ElementId.Void]=100;break;
                    case "low-score-second-ice":totals[(int)ElementId.Ice]=98;totals[(int)ElementId.Void]=102;break;
                    case "balanced":totals[(int)ElementId.Ice]=450;totals[(int)ElementId.Fire]=450;break;
                    case "hot-desert":totals[(int)ElementId.Sunlight]=450;totals[(int)ElementId.Sand]=450;break;
                    case "rocky-volcano":totals[(int)ElementId.Fire]=450;totals[(int)ElementId.Stone]=450;break;
                    case "frozen-volcano":totals[(int)ElementId.Ice]=700;totals[(int)ElementId.Lava]=300;break;
                    case "minor-fire":totals[(int)ElementId.Fire]=600;totals[(int)ElementId.Stone]=1800;totals[(int)ElementId.Void]=300;break;
                }
                UpdateComposition();yield return new WaitForSeconds(3.5f);
                view.transform.position=new Vector3(planet.position.x,planet.position.y,-25);view.orthographicSize=5.6f;view.aspect=1;
                var block=new MaterialPropertyBlock();planet.GetComponent<Renderer>().GetPropertyBlock(block);
                if(Mathf.Abs(block.GetFloat("_Temperature")-surface.Stats.Temperature)>.001f || Mathf.Abs(block.GetFloat("_GeologicalActivity")-surface.Stats.Geology)>.001f)
                    throw new Exception("Shared planet stats did not reach shader: "+sample);
                if(TemperatureLabel.text!=PlanetTemperatureDisplay.Average(surface.Stats.TemperatureFahrenheit))throw new Exception("Gameplay temperature label disagrees with climate: "+sample);
                view.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var capture=new Texture2D(512,512,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,512,512),0,0);capture.Apply();RenderTexture.active=previous;
                int blue=0,lava=0,white=0;
                for(int y=80;y<432;y++)for(int x=80;x<432;x++)
                {
                    var c=capture.GetPixel(x,y);
                    if(c.b>c.r*1.6f && c.b>c.g*1.3f)blue++;
                    if(c.r>.45f && c.r>c.g*1.7f && c.g>c.b*1.5f)lava++;
                    if(c.r>.55f && c.g>.55f && c.b>.55f)white++;
                }
                if(sample=="water" && blue<10000 || sample=="ice" && white<10000 || sample=="hot-desert" && lava>20 || sample=="rocky-volcano" && lava<100 || sample=="minor-fire" && lava<50)
                    throw new Exception($"Planet stat rendering {sample}: blue={blue}, lava={lava}, ice={white}");
                if(sample=="ice-void" && white<50000 || sample=="mixed-cold" && white<20000)
                    throw new Exception($"Minority Ice must create substantial frozen terrain: {sample}, white={white}");
                if(sample.StartsWith("low-score-") && (white<50000 || surface.Stats.IceCoverage<.999f))
                    throw new Exception($"Low-score Ice world must freeze across the globe: {sample}, coverage={surface.Stats.IceCoverage}, white={white}");
                if(sample=="frozen-volcano")System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/planet-stats-frozen-volcano.png"),capture.EncodeToPNG());
                if(sample=="frozen-volcano" && (lava<100 || white<20000 || surface.Stats.Heat>0))throw new Exception($"Cold world lost lava or snow: lava={lava}, white={white}, heat={surface.Stats.Heat}");
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/planet-stats-"+sample+".png"),capture.EncodeToPNG());Destroy(capture);
                Debug.Log($"ECOSPHERE_PLANET_STATS_PREVIEW {sample}: temp={surface.Stats.Temperature}, geology={surface.Stats.Geology}, blue={blue}, lava={lava}, white={white}");
            }
            var completed=WorldProgress.Complete(6,0,surface.TargetSeaLevel,42,false,selectedElementTotals).world;
            var restored=JsonUtility.FromJson<CompletedWorld>(JsonUtility.ToJson(completed));
            if(restored.planetStats==null || restored.planetStats.Temperature!=surface.Stats.Temperature || restored.planetStats.Ruggedness!=surface.Stats.Ruggedness || restored.planetStats.Geology!=surface.Stats.Geology)
                throw new Exception("Completed-world stats did not round trip");
            // A configured catalog change must not rewrite the saved snapshot.
            float storedTemp=restored.planetStats.Temperature;
            var fire=PlanetStatCatalog.Current.elements.Find(x=>x.element==ElementId.Fire);var old=fire.effects[0];
            fire.effects[0]=new PlanetStatEffect(PlanetStat.Temperature,0);
            if(restored.planetStats.Temperature!=storedTemp)throw new Exception("Saved snapshot changed with catalog");
            fire.effects[0]=old;
            view.targetTexture=null;target.Release();Destroy(target);
            Debug.Log("ECOSPHERE_PLANET_SMOKE_PASSED: shared stats, whole-planet percentages, heat/geology distinction, cold water, minority lava, saved snapshots, rotation/pause");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
