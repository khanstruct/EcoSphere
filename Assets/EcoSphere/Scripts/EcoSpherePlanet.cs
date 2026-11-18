using System;
using System.Collections;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        IEnumerator PlanetSmokeRun()
        {
            yield return new WaitForSeconds(.2f);
            var surface = planet.GetComponent<PlanetSurface>();
            var renderer = planet.GetComponent<Renderer>();
            if (!receivingPlanetMaterial || !renderer.sharedMaterial.GetTexture("_HeightMap") || surface.seaLevel >= 0)
                throw new Exception("Receiving planet is missing its barren material or elevation map");
            float angle = planet.eulerAngles.y;
            yield return new WaitForSeconds(.3f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, planet.eulerAngles.y)) < .1f)
                throw new Exception("Receiving planet did not rotate");
            Time.timeScale = 0;
            angle = planet.eulerAngles.y;
            yield return new WaitForSecondsRealtime(.1f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, planet.eulerAngles.y)) > .01f)
                throw new Exception("Planet rotated while gameplay was paused");
            Time.timeScale = 1;
            surface.rotationSpeed = 0;
            gameplayCanvas.enabled = false;
            var originalPosition = view.transform.position;
            float originalSize = view.orthographicSize;
            view.transform.position = new Vector3(planet.position.x, planet.position.y, -25);
            view.orthographicSize = 5.6f;
            var target = new RenderTexture(512, 512, 24);
            view.targetTexture = target; view.aspect = 1;
            int previousWater = -1;
            foreach (float level in new[] { -.02f, .3f, .6f })
            {
                surface.seaLevel = level;
                // Run the component update before capturing the material's new threshold.
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties); properties.SetFloat("_SeaLevel", level); renderer.SetPropertyBlock(properties);
                view.Render();
                var oldActive = RenderTexture.active;
                RenderTexture.active = target;
                var pixels = new Texture2D(512, 512, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); pixels.Apply();
                RenderTexture.active = oldActive;
                int waterPixels = 0;
                // Only sample the sphere interior, excluding the blue space backdrop.
                for (int y = 100; y < 412; y++) for (int x = 100; x < 412; x++)
                {
                    Color color = pixels.GetPixel(x, y);
                    if (color.b > color.r * 1.6f && color.b > color.g * 1.3f) waterPixels++;
                }
                if (level < 0 && waterPixels > 10 || previousWater >= 0 && waterPixels <= previousWater)
                    throw new Exception("Heightmap preview did not fill progressively with water: " + waterPixels);
                previousWater = waterPixels;
                string name = level < 0 ? "barren" : level < .5f ? "lakes" : "oceans";
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/planet-" + name + ".png"), pixels.EncodeToPNG());
                Destroy(pixels);
                Debug.Log($"ECOSPHERE_PLANET_PREVIEW {name} waterPixels={waterPixels}");
            }
            surface.seaLevel = -.02f;
            view.targetTexture = null;
            view.transform.position = originalPosition; view.orthographicSize = originalSize;
            gameplayCanvas.enabled = true;
            target.Release(); Destroy(target);
            Debug.Log("ECOSPHERE_PLANET_SMOKE_PASSED: textures, rotation, pause, progressive heightmap flooding");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
