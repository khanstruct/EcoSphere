using UnityEngine;
namespace EcoSphere
{
    public sealed class PlanetClouds : MonoBehaviour
    {
        static readonly int LightningCenterId=Shader.PropertyToID("_LightningCenter"),LightningStrengthId=Shader.PropertyToID("_LightningStrength");
        Renderer shell;
        MaterialPropertyBlock properties;
        float phase,speed,coverage,storm;
        Texture2D mask;
        Vector3[] vertices;Vector2[] uvs;
        // Current flash: time since it started (negative while waiting), its peak and whether it flickers twice.
        float flashAge=-1,waitLeft,flashPeak;
        bool flicker;
        internal int FlashCount {get;private set;}
        internal float FlashStrength {get;private set;}
        internal Vector3 FlashDirection {get;private set;}
        internal Vector2 FlashUV {get;private set;}   // where on the cloud mask the current flash was placed
        public static PlanetClouds Create(Transform globe)
        {
            var style=PlanetVisualStyle.Current;
            if(!style || !style.cloudMaterial)return null;
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Planet clouds";go.layer=globe.gameObject.layer;
            go.transform.SetParent(globe,false);go.transform.localScale=Vector3.one*style.cloudShellScale;
            Destroy(go.GetComponent<Collider>());
            var clouds=go.AddComponent<PlanetClouds>();clouds.shell=go.GetComponent<Renderer>();
            clouds.shell.sharedMaterial=style.cloudMaterial;clouds.shell.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            clouds.shell.receiveShadows=false;clouds.properties=new MaterialPropertyBlock();clouds.speed=style.cloudDriftSpeed;
            clouds.mask=style.cloudMaterial.GetTexture("_CloudMap") as Texture2D;
            var mesh=go.GetComponent<MeshFilter>().sharedMesh;clouds.vertices=mesh.vertices;clouds.uvs=mesh.uv;
            clouds.shell.enabled=false;return clouds;
        }
        public void SetAppearance(PlanetStats stats)
        {
            coverage=stats[PlanetStat.CloudCover];shell.enabled=coverage>.0001f;
            // Lightning lives inside actual storm clouds: cloud cover x weather x electrical activity.
            storm=shell.enabled ? stats.CloudLightning : 0;
            properties.SetFloat("_Coverage",coverage);properties.SetFloat("_Density",stats[PlanetStat.CloudThickness]);
            properties.SetFloat("_Darkness",stats[PlanetStat.CloudDarkness]);
            shell.SetPropertyBlock(properties);
        }
        void Update()
        {
            phase=Mathf.Repeat(phase+Time.deltaTime*speed,1);
            properties.SetFloat("_Drift",phase);
            UpdateLightning(Time.deltaTime);
            shell.SetPropertyBlock(properties);
        }

        // One flash at a time: a quick rise and a fade, sometimes flickering twice. Stronger storms flash more often and brighter.
        void UpdateLightning(float dt)
        {
            var style=PlanetVisualStyle.Current;
            if(!style || storm<=style.lightningMinimumStorm){FlashStrength=0;flashAge=-1;properties.SetFloat(LightningStrengthId,0);return;}
            float intensity=Mathf.Clamp01(storm);
            if(flashAge<0)
            {
                if(waitLeft<=0)waitLeft=NextWait(style,intensity);
                waitLeft-=dt;
                if(waitLeft<=0 && TryPickCloud(out var direction))
                {
                    flashAge=0;FlashCount++;FlashDirection=direction;flicker=Random.value<style.lightningFlickerChance;
                    flashPeak=style.lightningBrightness*Mathf.Lerp(.6f,1,intensity)*Random.Range(.75f,1f);
                    properties.SetVector(LightningCenterId,new Vector4(direction.x,direction.y,direction.z,style.lightningRadius*Random.Range(.7f,1.3f)));
                }
            }
            if(flashAge>=0)
            {
                flashAge+=dt;
                float envelope=Pulse(flashAge,style.lightningRise,style.lightningFade);
                if(flicker)envelope=Mathf.Max(envelope,.7f*Pulse(flashAge-style.lightningFlickerDelay,style.lightningRise,style.lightningFade));
                FlashStrength=flashPeak*envelope;
                if(flashAge>style.lightningRise+style.lightningFade+(flicker ? style.lightningFlickerDelay : 0)){flashAge=-1;FlashStrength=0;}
            }
            properties.SetFloat(LightningStrengthId,FlashStrength);
        }
        static float Pulse(float age,float rise,float fade)=>age<0 ? 0 : age<rise ? age/rise : Mathf.Clamp01(1-(age-rise)/fade);
        static float NextWait(PlanetVisualStyle style,float intensity)
        {
            float mean=Mathf.Lerp(style.lightningMaximumInterval,style.lightningMinimumInterval,Mathf.Sqrt(intensity));
            return -Mathf.Log(1-Random.value*.999f)*mean;   // irregular, storm-like spacing around the mean
        }

        // Picks a point on the shell where the cloud mask (same math as the shader) actually shows cloud.
        bool TryPickCloud(out Vector3 direction)
        {
            direction=Vector3.up;
            if(vertices==null || vertices.Length==0)return false;
            for(int attempt=0;attempt<16;attempt++)
            {
                int index=Random.Range(0,vertices.Length);
                if(CloudAt(uvs[index])<.5f)continue;
                direction=vertices[index].normalized;FlashUV=uvs[index];return true;
            }
            return false;   // mostly clear skies: try again on the next frame
        }
        internal float CloudAt(Vector2 uv)
        {
            if(!mask || !mask.isReadable)return coverage>.0001f ? 1 : 0;
            float broad=mask.GetPixelBilinear(uv.x+phase,uv.y).r;
            float detail=mask.GetPixelBilinear(uv.x*2-phase*.43f,uv.y*2+phase*.12f).r;
            float value=Mathf.Lerp(broad,broad*.75f+detail*.25f,.45f);
            float threshold=Mathf.Lerp(.94f,.02f,coverage);
            return Mathf.Lerp(Smooth(threshold-.18f,threshold+.18f,value),1,Smooth(.85f,1,coverage))*Mathf.Clamp01(coverage*20);
        }
        internal float CurrentCloud => CloudAt(FlashUV);
        internal float CloudAtDirection(Vector3 direction)
        {
            int best=0;float bestDot=-2;
            for(int i=0;i<vertices.Length;i++){float d=Vector3.Dot(vertices[i].normalized,direction);if(d>bestDot){bestDot=d;best=i;}}
            return CloudAt(uvs[best]);
        }
        static float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);}
    }
}
