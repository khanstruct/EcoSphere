# Receiving planet surface

The First Light receiving world keeps its existing position, scale, and partial-screen framing. It now uses `Art/Planets/barren-planet-v1.mat` and rotates at two degrees per second through `PlanetSurface`. Rotation uses gameplay time, so tutorial messages pause it.

Runtime textures are both 256 × 128:

- `Assets/EcoSphere/Art/Planets/barren-surface-v1.png`: gray rocky albedo.
- `Assets/EcoSphere/Art/Planets/barren-heightmap-v1.png`: matching elevation, imported as linear data.

Both textures use horizontal repeat, vertical clamp, bilinear filtering, and mipmaps. The material references the custom URP shader directly, so the shader is included in builds. Source images and generation prompts are preserved in `SourceArt/Planets`, outside runtime Assets.

The world starts barren with Sea Level at -0.02. During Play Mode, select the runtime **Receiving planet** object and adjust **Planet Surface → Sea Level** to preview flooding: dark terrain fills first, lighter regions remain exposed. This is a developer preview; element contributions are not connected yet. Clouds, ice coverage, atmospheric glow, and visual response to received elements remain later work.

`EcoSphere.Editor.PlanetTools.Setup` imports the pair, creates/updates the material, and patches First Light without rebuilding its scene. The test-level builder also preserves these assignments.

Run `EcoSphere.Editor.TestLevelTools.RunSmoke` with `-planetSmoke` to check texture assignment, rotation, pause behavior, and increasing water coverage. Full-sphere previews are written to `Logs/planet-barren.png`, `planet-lakes.png`, and `planet-oceans.png` within the validation project.
