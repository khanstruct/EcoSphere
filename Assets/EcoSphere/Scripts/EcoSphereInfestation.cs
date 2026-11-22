using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        InfestationModifierDefinition infestation;
        readonly Dictionary<Vector2Int,GameObject> aliens=new Dictionary<Vector2Int,GameObject>();
        int activeAlienStreaks;
        readonly System.Random alienAnimationRandom=new System.Random();
        void ApplyInfestation()
        {
            if(preparedWorld==null)return;
            foreach(var modifier in preparedWorld.ModifierDefinitions)
                if(modifier is InfestationModifierDefinition definition){infestation=definition;break;}
            if(!infestation)return;
            infestation.Validate();infestation.hitAudio.LoadAudioData();
            InfestationRules.Populate(board,infestation,preparedWorld.seed);
            foreach(var cell in board.Infested)CreateAlien(cell);
        }
        void CreateAlien(Vector2Int cell)
        {
            var go=new GameObject("Alien infestation");go.transform.SetParent(visuals[cell].transform,false);
            go.transform.localPosition=new Vector3(0,0,-.32f);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=infestation.alienSprite;renderer.sharedMaterial=infestation.overlayMaterial;
            // Stable per-host priority prevents bounds changes during frame swaps from reordering neighbors.
            renderer.sortingOrder=100+(InitialRings-cell.y)*(InitialRings*2+1)+cell.x+InitialRings;
            // Canvas has a host-centered pivot; it extends beyond the upper half of the sphere.
            float canvasWidth=renderer.sprite.rect.width/renderer.sprite.pixelsPerUnit;
            go.transform.localScale=Vector3.one*(Radius*2/.70f/canvasWidth*infestation.alienScale);
            var appearance=go.AddComponent<InfestedSphereVisual>();appearance.view=view;
            appearance.InitializeAnimation(infestation,alienAnimationRandom);
            if(infestation.showHostMarker)
            {
                var marker=new GameObject("Infestation host marker");marker.transform.SetParent(visuals[cell].transform,false);
                marker.transform.localPosition=new Vector3(0,0,-.42f);
                var ring=marker.AddComponent<LineRenderer>();ring.sharedMaterial=lineMaterial;ring.useWorldSpace=false;ring.loop=true;
                ring.positionCount=48;ring.startWidth=ring.endWidth=.025f;ring.sortingOrder=99;
                for(int i=0;i<48;i++){float angle=i*Mathf.PI*2/48;ring.SetPosition(i,new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*Radius*1.1f);}
                appearance.marker=ring;appearance.markerColor=infestation.overlayMaterial.GetColor("_OutlineColor");
                ring.startColor=ring.endColor=appearance.markerColor;
            }
            aliens.Add(cell,go);
        }
        HashSet<Vector2Int> ClearAdjacentInfestations(Vector2Int attachedCell)
        {
            HashSet<Vector2Int> cleared=null;
            foreach(var direction in BoardRules.Directions)
            {
                var neighbor=attachedCell+direction;
                if(!board.Infested.Contains(neighbor))continue;
                if(cleared==null)cleared=new HashSet<Vector2Int>();
                KnockOffAlien(neighbor,cleared.Count==0); // One hit sound for the attachment, even with multiple adjacent aliens.
                cleared.Add(neighbor);
            }
            return cleared;
        }
        void KnockOffAlien(Vector2Int cell,bool struck)
        {
            if(!board.Infested.Remove(cell))return;
            if(struck && levelAudio)levelAudio.AlienHit(infestation.hitAudio);
            if(!aliens.TryGetValue(cell,out var alien))return;
            var appearance=alien.GetComponent<InfestedSphereVisual>();appearance.ClearMarker();appearance.StopAnimation();
            aliens.Remove(cell);StartCoroutine(AlienEscape(alien));
        }
        IEnumerator AlienEscape(GameObject alien)
        {
            activeAlienStreaks++;
            try
            {
                alien.name="Alien escape streak";alien.transform.SetParent(transform,true);
                Vector3 start=alien.transform.position,scale=alien.transform.localScale;
                float side=Mathf.Abs(start.x-Left)<=Mathf.Abs(start.x-Right) ? -1 : 1;
                var destination=new Vector3(side<0 ? Left-.8f : Right+.8f,start.y+.35f,-.8f);
                var renderer=alien.GetComponent<SpriteRenderer>();renderer.color=new Color(1,.15f,.1f);
                renderer.sortingOrder=300;
                var trail=alien.AddComponent<TrailRenderer>();trail.sharedMaterial=lineMaterial;trail.sortingOrder=301;
                trail.time=.14f;trail.startWidth=.14f;trail.endWidth=0;trail.minVertexDistance=.02f;
                trail.startColor=new Color(1,.08f,.025f);trail.endColor=new Color(1,.015f,0,0);
                const float duration=.42f;
                for(float t=0;t<duration;t+=Time.deltaTime)
                {
                    float p=Mathf.Clamp01(t/duration),travel=p*p;
                    alien.transform.position=Vector3.Lerp(start,destination,travel)+Vector3.up*Mathf.Sin(travel*Mathf.PI)*.4f;
                    alien.transform.localScale=scale*Mathf.Lerp(1,.06f,Mathf.Clamp01(p/.35f));yield return null;
                }
                alien.transform.position=destination;renderer.enabled=false;trail.emitting=false;
                yield return new WaitForSeconds(.14f);
            }
            finally{if(alien)Destroy(alien);activeAlienStreaks--;}
        }
    }
}
