using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal GameObject FirstVisualOf(int type)
        {
            foreach(var cell in board.Cells)if(cell.Value==type)return visuals[cell.Key];
            return null;
        }
        internal void CheckCrystalGameplay()
        {
            var cells=new List<Vector2Int>();foreach(var cell in board.Cells)if(cell.Value==SphereIds.Crystal)cells.Add(cell.Key);
            if(cells.Count!=GameplayGeometry.CellCount(InitialRings)/activeSphereTypes.Length)throw new Exception("Crystal missing from selected procedural board");
            // Every board body comes from the definition's shape through the shared builder.
            foreach(var cell in board.Cells)
            {
                var body=visuals[cell.Key].transform.Find("Rotating surface");var shape=Definition(cell.Value).shape;
                if(!body || visuals[cell.Key].GetComponent<SphereAppearance>().shape!=shape || body.GetComponent<Collider>() ||
                    (shape==SphereShape.Crystal)!=(body.GetComponent<MeshFilter>().sharedMesh==SphereShapes.Mesh(SphereShape.Crystal)))
                    throw new Exception("Board body does not follow its sphere shape at "+cell.Key);
            }
            RemoveCells(new HashSet<Vector2Int>{cells[0]},12);
            var visual=visuals[cells[1]];RemoveCells(new HashSet<Vector2Int>{cells[1]},50,true);UpdateVisualizer();
            var crystal=Definition(SphereIds.Crystal);
            if(totals[(int)ElementId.Crystal]!=31 || totals[(int)ElementId.Stone]<15.5 || totals[(int)ElementId.Magic]!=15.5)
                throw new Exception("Crystal must yield 1 Stone / 1 Magic / 2 Crystal");
            int crystalBar=displayedElements.IndexOf(ElementId.Crystal),magicBar=displayedElements.IndexOf(ElementId.Magic);
            if(crystalBar<0 || magicBar<0 || energyBars[crystalBar].color!=crystal.color || energyBars[magicBar].color!=Catalog.ElementColor(ElementId.Magic) ||
                resourceLabels[magicBar].text!="Magic" || visual.GetComponent<TrailRenderer>().startColor!=crystal.color || LevelAudio.MatchVoice(SphereIds.Crystal,30)!=crystal.resourceVoice)
                throw new Exception("Crystal/Magic bars, labels, streak color or latisphere voice incorrect");
        }
    }
    public sealed partial class EcoSpherePrototype
    {
        // Real pointer shots through aim, Fire and the projectile flight loop: two deflections, then a break, then a same-type attach.
        internal IEnumerator CheckCrystalDeflection()
        {
            void Expect(bool value,string message){if(!value)throw new Exception("Crystal deflection: "+message);}
            var mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var played=new List<AudioClip>();levelAudio.EffectPlayed+=played.Add;
            while(!ready || projectile || armBusy)yield return null;
            rotationDegrees=0; // Keep the target in place between shots; rotation is unrelated to deflection.
            foreach(var model in visuals.Values)Destroy(model);visuals.Clear();board.Cells.Clear();
            foreach(var alien in aliens.Values)Destroy(alien);aliens.Clear();board.Infested.Clear();
            void Add(Vector2Int cell,int type){board.Cells.Add(cell,type);visuals.Add(cell,MakeSphere("Deflection test "+cell,type,cluster,BoardRules.Position(cell)));}
            var target=new Vector2Int(1,-2);var crystal=Definition(SphereIds.Crystal);
            Add(new Vector2Int(0,-1),SphereIds.Void);Add(new Vector2Int(1,-1),SphereIds.Void);Add(target,SphereIds.Crystal);
            IEnumerator Shoot(int type,Vector2Int aimCell)
            {
                while(!ready || projectile || armBusy)yield return null;
                if(loaded)Destroy(loaded);loadedType=type;loaded=MakeSphere("Loaded "+Definition(type).displayName,type,gun,LoadedLocal);
                yield return null;
                Vector2 point=view.WorldToScreenPoint(visuals[aimCell].transform.position);
                played.Clear();
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                Expect(!ready,"real click did not fire");
                while(projectile)yield return null;
            }
            for(int hit=1;hit<=crystal.deflectedHits;hit++)
            {
                int ricochetsBefore=ricochets,lostBefore=lost,scoreBefore=score;var visual=visuals[target];
                yield return Shoot(SphereIds.Fire,target);
                Expect(board.Cells[target]==SphereIds.Crystal && visuals[target]==visual && SphereDamage.HitsOn(visual)==hit,"hit "+hit+" must deflect and leave the crystal with "+hit+" hits");
                Expect(ricochets==ricochetsBefore+1 && shotRicochets==1,"a deflection must count as one ricochet");
                Expect(played.Contains(crystal.deflectSound) && !played.Contains(levelAudio.ricochet),"deflection must play crystal-hit instead of the wall ricochet");
                Expect(lost==lostBefore+1 && score==scoreBefore && !board.Cells.ContainsValue(SphereIds.Fire),"a head-on deflection must bounce the shot back out the bottom");
            }
            var totalsBefore=(double[])totals.Clone();int scoreBeforeBreak=score,lostBeforeBreak=lost,cellsBefore=board.Cells.Count;var broken=visuals[target];
            yield return Shoot(SphereIds.Fire,target);
            Expect(board.Cells.TryGetValue(target,out int occupant) && occupant==SphereIds.Fire && visuals[target]!=broken && board.Cells.Count==cellsBefore,"third hit must break the crystal and seat the shot in its cell");
            Expect(score==scoreBeforeBreak && lost==lostBeforeBreak && shotRicochets==0,"a break is not a miss, ricochet or score");
            for(int i=0;i<totals.Length;i++)Expect(totals[i]==totalsBefore[i],"broken crystal must not contribute "+(ElementId)i);
            Expect(played.Contains(crystal.breakSound) && !played.Contains(crystal.deflectSound),"break must play crystal-breaking");
            // Crystal shots attach to crystal like any same-type sphere.
            var fresh=new Vector2Int(1,-3);Add(fresh,SphereIds.Crystal);int beforeSame=board.Cells.Count,ricochetsBeforeSame=ricochets;
            yield return Shoot(SphereIds.Crystal,fresh);
            Expect(board.Cells.Count==beforeSame+1 && board.Cells[fresh]==SphereIds.Crystal && SphereDamage.HitsOn(visuals[fresh])==0 && ricochets==ricochetsBeforeSame,"crystal shot must attach to crystal");
            levelAudio.EffectPlayed-=played.Add;
        }
    }
    public sealed class CrystalSphereSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(LaunchFlags.Has("-crystalSphereSmoke"))new GameObject("Crystal checks").AddComponent<CrystalSphereSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            if(!GameServices.Progress.IsSphereAvailable(SphereIds.Crystal))throw new Exception("Crystal unavailable in DEV collection");
            var settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");bool previous=settings.devModeEnabled;
            try{settings.devModeEnabled=false;if(GameServices.Progress.IsSphereAvailable(SphereIds.Crystal))throw new Exception("Crystal must start locked normally");}
            finally{settings.devModeEnabled=previous;}
            FindAnyObjectByType<SystemView>().OpenPlanet(3);yield return null;yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();foreach(int id in new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Stone,SphereIds.Crystal})prep.AddSphere(id);
            if(!prep.Begin.interactable)throw new Exception("Crystal not selectable in setup");
            prep.Begin.onClick.Invoke();yield return null;yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            for(float t=0;t<1.5f;t+=Time.deltaTime)yield return null;
            var sample=game.FirstVisualOf(SphereIds.Crystal);
            Render(Camera.main,null,900,1600,"Crystal-board");
            Render(Camera.main,sample.transform,600,600,"Crystal-closeup");
            yield return CapturePreview();
            game.CheckCrystalGameplay();
            yield return game.CheckCrystalDeflection();
            var totals=new double[ElementComposition.Capacity];totals[(int)ElementId.Stone]=500;totals[(int)ElementId.Magic]=500;
            settings.devModeEnabled=false;
            try
            {
                var outcome=GameServices.Progress.Complete(9,0,-.02f,0,null,totals);
                if(!outcome.discoveries.Contains(SphereIds.Crystal) || outcome.world.discoveryDetails[0].kind!=DiscoveryKind.Recipe)throw new Exception("Stone + Magic failed to award Crystal");
            }
            finally{settings.devModeEnabled=previous;}
            Debug.Log("ECOSPHERE_CRYSTAL_GAMEPLAY_PASSED: DEV/normal collection, setup, faceted board bodies from shape data, 1 Stone / 1 Magic / 2 Crystal, Crystal and Magic bar colors, streak, latisphere voice, Stone + Magic discovery, real-shot deflection (two ricochets with crystal-hit, third-hit break with crystal-breaking and no yield), same-type attach, renders");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        IEnumerator CapturePreview()
        {
            var canvas=new GameObject("Crystal preview canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var image=new GameObject("Crystal preview",typeof(RectTransform),typeof(RawImage));image.transform.SetParent(canvas.transform,false);
            var preview=image.AddComponent<SpherePreview>();preview.definition=SphereCatalog.Current.Get(SphereIds.Crystal);
            for(float t=0;t<1f;t+=Time.deltaTime)yield return null;
            preview.PreviewCamera.Render();
            var texture=(RenderTexture)image.GetComponent<RawImage>().texture;
            Save(texture,"Crystal-preview");
            Destroy(canvas.gameObject);
        }

        static void Render(Camera source,Transform focus,int width,int height,string name)
        {
            var camera=new GameObject("Capture camera").AddComponent<Camera>();camera.CopyFrom(source);
            if(focus)
            {
                if(camera.orthographic){camera.orthographicSize=.9f;camera.transform.position=focus.position-camera.transform.forward*10;}
                else{float distance=Vector3.Distance(source.transform.position,focus.position);camera.fieldOfView=8;camera.transform.position=focus.position-camera.transform.forward*distance;}
            }
            var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;camera.Render();
            Save(target,name);camera.targetTexture=null;target.Release();Destroy(target);Destroy(camera.gameObject);
        }

        static void Save(RenderTexture texture,string name)
        {
            var previous=RenderTexture.active;RenderTexture.active=texture;
            var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/"+name+".png"),pixels.EncodeToPNG());
            RenderTexture.active=previous;Destroy(pixels);
        }
    }
}
