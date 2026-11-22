using System.Collections;
using UnityEngine;
namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        readonly System.Random streakRandom=new System.Random();
        Material energyFlashMaterial;
        Material impactFlashMaterial;
        int activeStreaks;
        Material EnergyFlashMaterial => energyFlashMaterial ? energyFlashMaterial : (energyFlashMaterial=Resources.Load<Material>("EnergyFlash"));

        GameObject EnergyDisc(string name,Vector3 position,Color color,float strength,bool impact=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform,true);go.transform.position=position;go.transform.rotation=view.transform.rotation;
            var renderer=go.GetComponent<Renderer>();
            if(impact && !impactFlashMaterial)
            {
                // Only surface impacts bypass depth: travelling energy still respects planet occlusion.
                impactFlashMaterial=new Material(EnergyFlashMaterial){name="Planet impact glow"};
                impactFlashMaterial.SetInt("_ZTest",(int)UnityEngine.Rendering.CompareFunction.Always);
                materials.Add(impactFlashMaterial);
            }
            renderer.sharedMaterial=impact ? impactFlashMaterial : EnergyFlashMaterial;
            renderer.sortingOrder=impact ? 12 : 9;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var block=new MaterialPropertyBlock();block.SetColor("_TintColor",color);block.SetFloat("_Strength",strength);renderer.SetPropertyBlock(block);
            return go;
        }
        void StartMatchedStreak(Vector3 position,int type)
        {
            var head=EnergyDisc("Matched energy streak",position,ColorFor(type),1.7f);head.transform.localScale=Vector3.one*.18f;
            StartCoroutine(Ascend(head,type,true));
        }
        IEnumerator Ascend(GameObject visual,int type,bool matched=false)
        {
            activeStreaks++;
            try
            {
                visual.transform.SetParent(transform,true);
                Vector3 initialScale=visual.transform.localScale;
                var path=new PlanetStreakPath(visual.transform.position,planet.position,planet.lossyScale.x*.5f,streakRandom);
                var streak=visual.AddComponent<TrailRenderer>();streak.sharedMaterial=lineMaterial;
                streak.time=.23f;streak.startWidth=matched ? .095f : .16f;streak.endWidth=0;streak.minVertexDistance=.025f;
                Color color=ColorFor(type);streak.startColor=color;streak.endColor=new Color(color.r,color.g,color.b,0);streak.sortingOrder=8;
                for(float t=0;t<path.duration;t+=Time.deltaTime)
                {
                    if(!visual)yield break;
                    float p=Mathf.Clamp01(t/path.duration);
                    visual.transform.position=path.Position(p);
                    visual.transform.localScale=initialScale*(matched?1:Mathf.Lerp(1,.12f,Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.35f))));
                    yield return null;
                }
                if(!visual)yield break;
                visual.transform.position=path.destination;streak.emitting=false;
                StartCoroutine(PlanetImpact(path.destination,path.normal,color,matched));
                // Preserve the tail briefly after the head reaches the actual surface.
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())if(renderer!=streak)renderer.enabled=false;
                yield return new WaitForSeconds(.26f);
            }
            finally{if(visual)Destroy(visual);activeStreaks--;}
        }
        IEnumerator PlanetImpact(Vector3 position,Vector3 normal,Color color,bool matched)
        {
            var flash=EnergyDisc("Planet streak impact",position+normal*.035f,Color.Lerp(color,Color.white,.55f),3.5f,true);
            flash.transform.SetParent(planet,true);
            var renderer=flash.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            const float duration=.24f;
            for(float t=0;t<duration;t+=Time.deltaTime)
            {
                float p=Mathf.Clamp01(t/duration),size=Mathf.Lerp(matched ? .4f : .55f,matched ? .8f : 1.1f,p);
                flash.transform.localScale=Vector3.one*size/planet.lossyScale.x;
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.045f,duration,t));
                block.SetFloat("_Strength",3.5f*fade);renderer.SetPropertyBlock(block);yield return null;
            }
            Destroy(flash);
        }
    }
}
