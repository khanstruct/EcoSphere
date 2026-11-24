using UnityEngine;
namespace EcoSphere
{
    public sealed class PlanetAtmosphere : MonoBehaviour
    {
        Renderer shell;
        MaterialPropertyBlock properties;
        public static PlanetAtmosphere Create(Transform globe)
        {
            var style=PlanetVisualStyle.Current;
            if(!style || !style.atmosphereMaterial)return null;
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name="Planet atmosphere";go.layer=globe.gameObject.layer;
            go.transform.SetParent(globe,false);Destroy(go.GetComponent<Collider>());
            var atmosphere=go.AddComponent<PlanetAtmosphere>();
            atmosphere.shell=go.GetComponent<Renderer>();atmosphere.shell.sharedMaterial=style.atmosphereMaterial;
            atmosphere.shell.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            atmosphere.shell.receiveShadows=false;atmosphere.properties=new MaterialPropertyBlock();
            atmosphere.shell.enabled=false;return atmosphere;
        }
        public void SetAppearance(Color color,float strength,float width)
        {
            shell.enabled=strength>.0001f;
            float extent=PlanetStatCatalog.Current.ClampAtmosphereWidth(width);
            transform.localScale=Vector3.one*(1+extent);
            properties.SetColor("_AtmosphereColor",color);
            properties.SetFloat("_Strength",Mathf.Clamp01(strength));
            properties.SetFloat("_Width",extent);
            shell.SetPropertyBlock(properties);
        }
    }
}
