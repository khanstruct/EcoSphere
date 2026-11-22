using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal IEnumerator CheckInfestation()
        {
            if(!infestation || board.Infested.Count<8 || board.Infested.Count>12 || aliens.Count!=board.Infested.Count || board.Infested.Contains(Vector2Int.zero))throw new Exception("World three must start with 8-12 non-core aliens");
            var priorities=new HashSet<int>();
            foreach(var pair in aliens)
            {
                var sprite=pair.Value.GetComponent<SpriteRenderer>();
                if(!priorities.Add(sprite.sortingOrder))throw new Exception("Alien overlap priorities must be unique and stable");
                if(sprite.sortingOrder<=4 || sprite.sharedMaterial.shader.name!="EcoSphere/Alien Overlay" || sprite.sprite.texture.width>256 || pair.Value.GetComponent<Collider>())throw new Exception("Alien overlay depth, sprite size or collider invalid");
            }
            var initialFrames=new Dictionary<InfestedSphereVisual,int>();var changedFrames=new HashSet<InfestedSphereVisual>();
            foreach(var alien in aliens.Values){var appearance=alien.GetComponent<InfestedSphereVisual>();initialFrames.Add(appearance,appearance.CurrentFrame);}
            float animationUntil=Time.time+1/(infestation.animationFramesPerSecond*(1-infestation.animationSpeedVariation))+.1f;
            while(Time.time<animationUntil)
            {
                foreach(var pair in initialFrames)if(pair.Key.CurrentFrame!=pair.Value)changedFrames.Add(pair.Key);
                yield return null;
            }
            if(changedFrames.Count!=aliens.Count)throw new Exception("Every clinging alien must animate between both frames");
            foreach(var alien in aliens.Values)
            {
                var sprite=alien.GetComponent<SpriteRenderer>().sprite;
                if(sprite!=infestation.alienSprite && sprite!=infestation.alienAlternateSprite)throw new Exception("Alien animation used an unexpected sprite");
            }
            Debug.Log("ECOSPHERE_ALIEN_ANIMATION_PASSED: all hosts alternate aligned frames; independent randomized start/phase and speed");
            // Check pixels, not just a frame counter: the custom sprite shader must use the changed texture.
            GameObject focus=null;foreach(var alien in aliens.Values){focus=alien;break;}
            var appearanceFocus=focus.GetComponent<InfestedSphereVisual>();var rendererFocus=focus.GetComponent<SpriteRenderer>();int priority=rendererFocus.sortingOrder;
            var targetFrames=new RenderTexture(512,512,24);targetFrames.Create();
            var oldTarget=view.targetTexture;var oldPosition=view.transform.position;float oldSize=view.orthographicSize,oldAspect=view.aspect;
            view.targetTexture=targetFrames;view.transform.position=new Vector3(focus.transform.position.x,focus.transform.position.y+.08f,-25);view.orthographicSize=.7f;view.aspect=1;
            Texture2D FrameCapture()
            {
                view.Render();var previous=RenderTexture.active;RenderTexture.active=targetFrames;
                var image=new Texture2D(512,512,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/alien-rendered-frame-"+appearanceFocus.CurrentFrame+".png"),image.EncodeToPNG());return image;
            }
            var frameA=FrameCapture();appearanceFocus.AdvanceAnimation(1/appearanceFocus.FramesPerSecond+.00001f);var frameB=FrameCapture();
            var pixelsA=frameA.GetPixels32();var pixelsB=frameB.GetPixels32();int changes=0;
            for(int i=0;i<pixelsA.Length;i++)if(Mathf.Abs(pixelsA[i].r-pixelsB[i].r)+Mathf.Abs(pixelsA[i].g-pixelsB[i].g)+Mathf.Abs(pixelsA[i].b-pixelsB[i].b)>40)changes++;
            var block=new MaterialPropertyBlock();rendererFocus.GetPropertyBlock(block);
            if(changes<500 || block.GetTexture("_MainTex")!=rendererFocus.sprite.texture || rendererFocus.sortingOrder!=priority)throw new Exception("Frame changes must visibly change the sprite texture without changing overlap priority: "+changes);
            view.targetTexture=oldTarget;view.transform.position=oldPosition;view.orthographicSize=oldSize;view.aspect=oldAspect;
            Destroy(frameA);Destroy(frameB);targetFrames.Release();Destroy(targetFrames);
            Debug.Log("ECOSPHERE_ALIEN_RENDERED_ANIMATION_PASSED changedPixels="+changes+"; texture binding and stable draw order");
            foreach(var size in new[]{new Vector2Int(900,1600),new Vector2Int(1600,900)})
            {
                var target=new RenderTexture(size.x,size.y,24);target.Create();SetPreviewTarget(target);
                yield return null;yield return null;
                foreach(var graphic in gameplayCanvas.GetComponentsInChildren<Graphic>())
                {graphic.SetVerticesDirty();graphic.SetMaterialDirty();if(graphic is MaskableGraphic maskable)maskable.RecalculateClipping();}
                foreach(var label in gameplayCanvas.GetComponentsInChildren<TMP_Text>())label.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();RenderPresentation();
                var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/infested-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());
                if(size.x<size.y)
                {
                    foreach(var alien in aliens.Values){alien.GetComponent<SpriteRenderer>().enabled=false;var marker=alien.GetComponent<InfestedSphereVisual>().marker;if(marker)marker.enabled=false;}
                    RenderPresentation();RenderTexture.active=target;
                    var plain=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);plain.ReadPixels(new Rect(0,0,size.x,size.y),0,0);plain.Apply();RenderTexture.active=previous;
                    foreach(var alien in aliens.Values){alien.GetComponent<SpriteRenderer>().enabled=true;var marker=alien.GetComponent<InfestedSphereVisual>().marker;if(marker)marker.enabled=true;}
                    var bright=image.GetPixels32();var dark=plain.GetPixels32();int visiblePixels=0;
                    for(int i=0;i<bright.Length;i++)if(bright[i].r+bright[i].g+bright[i].b>dark[i].r+dark[i].g+dark[i].b+80)visiblePixels++;
                    Destroy(plain);
                    if(visiblePixels<aliens.Count*80)throw new Exception("Infestation must be obvious at gameplay scale: "+visiblePixels);
                    Debug.Log("ECOSPHERE_INFESTATION_VISIBILITY_PASSED highlightedPixels="+visiblePixels+", aliens="+aliens.Count);
                }
                SetPreviewTarget(null);target.Release();Destroy(target);Destroy(image);
            }
            enabled=false;
            void ResetBoard()
            {
                StopAllCoroutines();
                foreach(var model in visuals.Values)Destroy(model);visuals.Clear();board.Cells.Clear();board.Infested.Clear();aliens.Clear();
                if(loaded){Destroy(loaded);loaded=null;}
                cluster.rotation=Quaternion.identity;failed=complete=false;ready=false;
            }
            void Add(Vector2Int cell,int type,bool infested=false)
            {
                board.Cells.Add(cell,type);visuals.Add(cell,MakeSphere("Infestation test "+cell,type,cluster,BoardRules.Position(cell)));
                if(infested){board.Infested.Add(cell);CreateAlien(cell);}
            }
            foreach(int side in new[]{-1,1})
            {
                ResetBoard();yield return null;
                var host=new Vector2Int(side*2,0);Add(new Vector2Int(side,0),SphereIds.Fire);Add(host,SphereIds.Fire,true);
                var alien=aliens[host];int before=score;int matchedBefore=matchedCount;
                var effects=levelAudio.transform.Find("Shot and ricochet effects").GetComponent<AudioSource>();effects.Stop();
                loadedType=SphereIds.Fire;
                Vector3 point=cluster.TransformPoint(BoardRules.Position(host))+Vector3.right*side*(Radius*2);
                projectile=MakeSphere("Attaching shot",SphereIds.Fire,transform,point);
                float started=Time.time;Attach(host,Vector2.left*side);
                if(lastMatch!=0 || score!=before || matchedCount!=matchedBefore || board.Cells.Count!=3 || board.Infested.Contains(host) || aliens.ContainsKey(host) || board.MatchFrom(host).Count!=3)
                    throw new Exception("Direct alien hit must attach/cleanse without clearing a normally qualifying match");
                if(!effects.isPlaying || infestation.hitAudio.name!="alien-hit")throw new Exception("Direct alien hit did not play alien-hit");
                if(alien.GetComponent<InfestedSphereVisual>().marker)throw new Exception("Host marker persisted after cleansing");
                var trail=alien.GetComponent<TrailRenderer>();if(!trail || trail.startColor.r<.9f || trail.startColor.g>.2f)throw new Exception("Knocked alien must become a red streak");
                while(Time.time-started<.44f)yield return null;
                if(!alien || (side<0 ? alien.transform.position.x>=Left : alien.transform.position.x<=Right))throw new Exception("Alien did not escape past the nearer edge");
                yield return new WaitForSeconds(.17f);
                if(alien || activeAlienStreaks!=0)throw new Exception("Alien streak cleanup failed");
            }
            ResetBoard();yield return null;
            // Contact an ordinary sphere, but resolve adjacent to two other infested hosts.
            var contact=new Vector2Int(2,0);var adjacentA=new Vector2Int(2,1);var adjacentB=new Vector2Int(3,1);var distant=new Vector2Int(-1,1);
            Add(new Vector2Int(1,0),SphereIds.Fire);Add(contact,SphereIds.Fire);
            Add(adjacentA,SphereIds.Fire,true);Add(adjacentB,SphereIds.Fire,true);Add(distant,SphereIds.Void,true);
            // Separate physical support keeps the cleansed hosts from falling when normal matches clear.
            Add(new Vector2Int(0,1),SphereIds.Void);Add(new Vector2Int(1,1),SphereIds.Void);
            int beforeAdjacent=score;
            loadedType=SphereIds.Fire;
            projectile=MakeSphere("Adjacent infestation shot",SphereIds.Fire,transform,cluster.TransformPoint(BoardRules.Position(contact))+Vector3.right*(Radius*2));
            Attach(contact,Vector2.left);
            if(board.Cells.ContainsKey(new Vector2Int(3,0)) || !board.Cells.ContainsKey(adjacentA) || !board.Cells.ContainsKey(adjacentB) || board.Infested.Contains(adjacentA) || board.Infested.Contains(adjacentB) || !board.Infested.Contains(distant) || aliens.Count!=1 || activeAlienStreaks!=2 || lastMatch!=3 || score!=beforeAdjacent+30 || board.Cells.Count!=5)
                throw new Exception("One attachment must cleanse adjacent aliens AND score its normal match, excluding freshly cleansed hosts and preserving distant aliens");
            yield return new WaitForSeconds(.62f);
            while(activeStreaks>0 || activeAlienStreaks>0)yield return null;
            ResetBoard();yield return null;
            var infected=new Vector2Int(3,0);
            Add(new Vector2Int(1,0),SphereIds.Fire);Add(new Vector2Int(2,0),SphereIds.Fire);Add(new Vector2Int(1,-1),SphereIds.Fire);
            Add(new Vector2Int(0,1),SphereIds.Void);Add(new Vector2Int(1,1),SphereIds.Void);Add(new Vector2Int(2,1),SphereIds.Void);
            Add(infected,SphereIds.Fire,true);Add(new Vector2Int(4,0),SphereIds.Fire);Add(new Vector2Int(5,0),SphereIds.Fire);
            var match=board.MatchFrom(new Vector2Int(1,0));RemoveCells(match,ElementScoring.MatchRate(match.Count),matchOrigin:new Vector2Int(1,0));
            if(match.Count!=3 || !board.Infested.Contains(infected) || !aliens.ContainsKey(infected) || board.Disconnected().Contains(infected))throw new Exception("Indirect match cleared alien/host or counted it as a matching link");
            int scoreBefore=score;
            var support=board.MatchFrom(new Vector2Int(0,1));RemoveCells(support,ElementScoring.MatchRate(support.Count),matchOrigin:new Vector2Int(0,1));
            var released=board.Disconnected();var escaped=aliens[infected];var hostVisual=visuals[infected];
            RemoveCells(released,ElementScoring.Breakaway,true);
            if(released.Count!=3 || board.Cells.Count!=0 || board.Infested.Count!=0 || aliens.Count!=0 || !escaped.GetComponent<TrailRenderer>() || !hostVisual.GetComponent<TrailRenderer>() || score-scoreBefore!=180)
                throw new Exception("Infested breakaways must clear/score normally and detach alien separately");
            float deadline=Time.time+3;
            while((activeStreaks>0 || activeAlienStreaks>0 || Time.time<deadline-1.5f) && Time.time<deadline)yield return null;
            if(activeStreaks!=0 || activeAlienStreaks!=0)throw new Exception("Infestation effects leaked");
            Debug.Log("ECOSPHERE_INFESTATION_GAMEPLAY_PASSED: 8-12 overlays; adjacency cleansing plus simultaneous normal matches; infested/freshly-cleansed hosts excluded from that match; normal type restored for later shots; distant aliens protected; audio, support, breakaways, red streaks and cleanup");
        }
    }
    public sealed class InfestationSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-infestationSmoke")>=0)new GameObject("Infestation checks").AddComponent<InfestationSmoke>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);float previous=Time.captureDeltaTime;Time.captureDeltaTime=1f/120;
            try
            {
                yield return null;yield return null;
                var system=FindAnyObjectByType<SystemView>();system.DismissIntroductionForSmoke();yield return null;system.OpenPlanet(2);yield return null;yield return null;
                var prep=FindAnyObjectByType<LevelPrepView>();
                var label=GameObject.Find("alien-infestation");
                if(!label || label.GetComponent<TMP_Text>().text!="Alien Infestation")throw new Exception("Setup is missing infestation modifier display");
                foreach(int type in new[]{SphereIds.Fire,SphereIds.Void,SphereIds.Water,SphereIds.Mud})prep.AddSphere(type);
                prep.Begin.onClick.Invoke();yield return null;yield return null;
                yield return FindAnyObjectByType<EcoSpherePrototype>().CheckInfestation();
            }
            finally{Time.captureDeltaTime=previous;}
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
