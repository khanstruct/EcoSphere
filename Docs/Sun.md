# Animated suns

The System screen's sun is an animated shader on its existing UI Image (`EcoSphere/UI/Animated Sun`), not a static sprite. It has three layers:

- **Surface:** two samples of the tiling plasma mask drift in different directions and distort each other through a slow warp field, so bright regions form and fade rather than simply rotating. Features are projected onto a sphere and darken toward the limb, with a thin bright rim.
- **Corona:** two copies of the corona mask turn slowly in opposite directions, and plasma-driven flicker streams outward through the wisps.
- **Glow:** a soft exponential glow around the disc. Corona and glow only add light, so stars stay visible through them; only the disc is opaque.

## Data

- Shared: `Art/Suns/animated-sun.mat` holds the shader and both grayscale masks (`sun-plasma-mask-v1.png`, repeat wrap; `sun-corona-mask-v1.png`, clamp; both 256×256, linear). `WorldCatalog.animatedSunMaterial` points at it.
- Per sun type: `SunTypeDefinition.appearance` (`SunAppearance`) sets core, surface, spot and corona colors, surface and corona speeds, surface contrast, corona strength and glow strength. A new sun color is a new sun type entry with its own appearance; no new textures or code.
- `SunView` (added to the System sun Image at runtime) makes one material instance per sun and applies the appearance. With no animated material assigned it falls back to the sun type's static `sprite`.

The corona mask's dark disc ends at 0.475 of the image radius, which is the shader's `_DiscRadius`.

## Validation

Checked in Play mode on the System scene: the Yellow Star renders through `SunView` with the animated material, its surface and corona visibly change over five seconds, and a blue appearance applied through the same path renders correctly (`Logs/Sun-compare.png`, `Logs/Sun-yellow-blue.png`, `Logs/Sun-final.png`). Not yet checked: mobile performance, and the portrait System layout (only a wide Game view was captured). Flares are not implemented.
