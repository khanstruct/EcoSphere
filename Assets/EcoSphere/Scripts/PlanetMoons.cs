using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    // Moons orbiting a planet globe, one per PlanetVisualStyle.moonOrbits entry whose threshold the Moons stat reaches.
    // Shared by the live gameplay planet (PlanetSurface) and completed-world previews (SpherePreview), like PlanetClouds.
    // The orbits keep a fixed orientation to the viewer while the globe spins underneath them.
    [DefaultExecutionOrder(100)]   // after PlanetSurface spins the globe, so the orbit frame is reset last each frame
    public sealed class PlanetMoons : MonoBehaviour
    {
        sealed class Moon {public Transform body;public float grown;}
        readonly List<Moon> moons=new List<Moon>();
        float moonStat,time;
        internal int VisibleCount {get;private set;}
        internal IReadOnlyList<Transform> Bodies {get{var result=new List<Transform>();foreach(var moon in moons)result.Add(moon.body);return result;}}

        public static PlanetMoons Create(Transform globe)
        {
            var style=PlanetVisualStyle.Current;
            if(!style || !style.moonMaterial || style.moonOrbits.Count==0)return null;
            var root=new GameObject("Planet moons");root.layer=globe.gameObject.layer;
            root.transform.SetParent(globe,false);
            var planetMoons=root.AddComponent<PlanetMoons>();
            for(int i=0;i<style.moonOrbits.Count;i++)
            {
                var body=GameObject.CreatePrimitive(PrimitiveType.Sphere);body.name="Moon "+(i+1);body.layer=root.layer;
                Destroy(body.GetComponent<Collider>());body.transform.SetParent(root.transform,false);
                var renderer=body.GetComponent<Renderer>();renderer.sharedMaterial=style.moonMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                body.SetActive(false);planetMoons.moons.Add(new Moon{body=body.transform});
            }
            planetMoons.Place(0);
            return planetMoons;
        }
        public void SetAppearance(PlanetStats stats)=>moonStat=stats.Moons;
        // Completed-world previews show their moons fully grown at once.
        public void Settle(){for(int i=0;i<moons.Count;i++)moons[i].grown=Visible(i) ? 1 : 0;Place(0);}

        // Outermost reach of the visible orbits, in globe-local units (the globe's own radius is .5).
        internal float Extent
        {
            get
            {
                var style=PlanetVisualStyle.Current;float extent=.5f;
                for(int i=0;i<moons.Count;i++)if(Visible(i))extent=Mathf.Max(extent,.5f*(style.moonOrbits[i].distance+style.moonOrbits[i].size));
                return extent;
            }
        }
        bool Visible(int index)
        {
            var orbit=PlanetVisualStyle.Current.moonOrbits[index];
            return orbit.threshold>0 && moonStat>=orbit.threshold;
        }

        void LateUpdate()=>Place(Time.deltaTime);

        void Place(float dt)
        {
            var style=PlanetVisualStyle.Current;if(!style)return;
            time+=dt;
            // Hold the orbit frame still in world space; the parent globe keeps spinning.
            transform.rotation=Quaternion.identity;
            int visible=0;
            for(int i=0;i<moons.Count && i<style.moonOrbits.Count;i++)
            {
                var orbit=style.moonOrbits[i];var moon=moons[i];
                moon.grown=Mathf.MoveTowards(moon.grown,Visible(i) ? 1 : 0,dt/Mathf.Max(.01f,style.moonAppearSeconds));
                bool show=moon.grown>0;if(moon.body.gameObject.activeSelf!=show)moon.body.gameObject.SetActive(show);
                if(!show)continue;
                visible++;
                float angle=(orbit.startAngle+360*time/orbit.period)*Mathf.Deg2Rad;
                var flat=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.5f*orbit.distance);
                moon.body.localPosition=Quaternion.Euler(orbit.inclination,0,orbit.roll)*flat;
                moon.body.localScale=Vector3.one*orbit.size*Mathf.SmoothStep(0,1,moon.grown);
                moon.body.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);   // tidally locked: the same face looks at the planet
            }
            VisibleCount=visible;
        }
    }
}
