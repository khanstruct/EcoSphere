using UnityEngine;
namespace EcoSphere
{
    public sealed class PlanetClouds : MonoBehaviour
    {
        Renderer shell;
        MaterialPropertyBlock properties;
        float phase,speed;
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
            clouds.shell.enabled=false;return clouds;
        }
        public void SetAppearance(PlanetStats stats)
        {
            float coverage=stats[PlanetStat.CloudCover];shell.enabled=coverage>.0001f;
            properties.SetFloat("_Coverage",coverage);properties.SetFloat("_Density",stats[PlanetStat.CloudThickness]);
            properties.SetFloat("_Darkness",stats[PlanetStat.CloudDarkness]);
            shell.SetPropertyBlock(properties);
        }
        void Update()
        {
            phase=Mathf.Repeat(phase+Time.deltaTime*speed,1);
            properties.SetFloat("_Drift",phase);shell.SetPropertyBlock(properties);
        }
    }
}
