using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    public sealed class SpherePreview : MonoBehaviour
    {
        public SphereDefinition definition;
        public Texture2D surface;
        public Sprite icon;
        public Material surfaceMaterial;
        public float rotationSpeed = 12, initialRotation, seaLevel = -.02f;
        public float stoneRuggedness;
        public float iceCoverage;
        public float fireHeat, atmosphereStrength, atmosphereWidth;
        public Color atmosphereColor;
        public PlanetStats planetStats;
        static int serial;
        GameObject stage;
        Transform globe;
        SphereShape shape;
        float elapsed;
        RenderTexture target;
        Material material;
        public Camera PreviewCamera { get; private set; }
        public Renderer BodyRenderer { get; private set; }
        public Quaternion SurfaceRotation => globe ? globe.localRotation : Quaternion.identity;
        public void ConfigurePlanet(CompletedWorld world,Material planetMaterial,float spin=2)
        {
            var style=PlanetVisualStyle.Current;
            surfaceMaterial=style ? style.Terrain(world.terrainStyleId,planetMaterial) : planetMaterial;planetStats=world.AppearanceStats;
            seaLevel=world.seaLevel;initialRotation=world.rotation;rotationSpeed=spin;
            stoneRuggedness=world.stoneRuggedness;iceCoverage=world.iceCoverage;fireHeat=world.fireHeat;
            atmosphereColor=world.atmosphereColor;atmosphereStrength=world.atmosphereStrength;atmosphereWidth=world.atmosphereWidth;
        }
        void Start()
        {
            if (definition) { surface = definition.surface; icon = definition.icon; }
            int layer = 30;
            stage = new GameObject("Sphere preview stage");
            stage.transform.position = new Vector3((serial++ % 100) * 10, -1000, 0);
            shape = definition ? definition.shape : SphereShape.Sphere;
            var sphere = SphereShapes.CreateBody(shape, "Preview body");
            BodyRenderer = sphere.GetComponent<Renderer>();
            sphere.transform.SetParent(stage.transform, false); sphere.layer = layer;
            globe = sphere.transform; globe.localScale = Vector3.one * 1.65f;
            globe.localRotation = SphereShapes.Orientation(shape, 0, rotationSpeed, initialRotation);
            material = surfaceMaterial ? new Material(surfaceMaterial) : new Material(Shader.Find(definition && definition.unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            if (surfaceMaterial) { material.SetFloat("_SeaLevel", seaLevel); material.SetFloat("_StoneRuggedness",stoneRuggedness); material.SetFloat("_IceCoverage",iceCoverage); }
            else
            {
                material.SetTexture("_BaseMap", surface); material.SetColor("_BaseColor", definition ? definition.surfaceTint : Color.white);
                material.SetFloat("_Smoothness", .35f);
                SphereShapes.ApplyFinish(material, shape);
            }
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            if(surfaceMaterial)
            {
                material.SetFloat("_FireHeat",fireHeat);
                var stats=planetStats ?? PlanetStats.Legacy(stoneRuggedness,iceCoverage,fireHeat,atmosphereColor,atmosphereStrength,atmosphereWidth);
                var block=new MaterialPropertyBlock();stats.Apply(block);block.SetVector("_TerrainBounds",PlanetSurface.TerrainBounds(material));sphere.GetComponent<Renderer>().SetPropertyBlock(block);
                var atmosphere=PlanetAtmosphere.Create(globe);
                if(atmosphere)atmosphere.SetAppearance(stats.atmosphereColor,stats.AtmosphereDensity,stats.atmosphereWidth);
                var clouds=PlanetClouds.Create(globe);if(clouds)clouds.SetAppearance(stats);
            }
            SphereGlow.Create(stage.transform,definition,1.65f,layer);
            var symbol = new GameObject("Sphere icon"); symbol.layer = layer;
            symbol.transform.SetParent(stage.transform, false); symbol.transform.localPosition = new Vector3(0, 0, -.85f);
            var renderer = symbol.AddComponent<SpriteRenderer>(); renderer.sprite = icon;
            if (icon) symbol.transform.localScale = Vector3.one * 1.05f / icon.bounds.size.x;
            var cameraObject = new GameObject("Sphere preview camera");
            cameraObject.transform.SetParent(stage.transform, false); cameraObject.transform.localPosition = new Vector3(0,0,-4);
            PreviewCamera = cameraObject.AddComponent<Camera>(); PreviewCamera.orthographic = true; PreviewCamera.orthographicSize = 1;
            PreviewCamera.nearClipPlane = .1f; PreviewCamera.farClipPlane = 7;
            PreviewCamera.clearFlags = CameraClearFlags.SolidColor; PreviewCamera.backgroundColor = Color.clear;
            PreviewCamera.cullingMask = 1 << layer;
            target = new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); target.Create();
            PreviewCamera.targetTexture = target;
            PreviewCamera.aspect = 1;
            var image=GetComponent<RawImage>();image.texture = target;
            if(surfaceMaterial)
            {
                var style=PlanetVisualStyle.Current;
                if(style && style.previewMaterial)image.material=style.previewMaterial;
            }
        }
        void Update()
        {
            if (!globe) return;
            elapsed += Time.deltaTime;
            globe.localRotation = SphereShapes.Orientation(shape, elapsed, rotationSpeed, initialRotation);
        }
        void OnEnable(){if(stage)stage.SetActive(true);}
        void OnDisable(){if(stage)stage.SetActive(false);}
        void OnDestroy()
        {
            if (stage) Destroy(stage);
            if (target) { target.Release(); Destroy(target); }
            if (material) Destroy(material);
        }
    }
}
