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

## Black holes

A sun type can name its own `material`; empty uses the catalog's animated sun material. The **Black Hole** type (`black-hole`) uses `Art/Suns/black-hole.mat` with `EcoSphere/UI/Black Hole` and two dedicated masks (256×256, linear):

- `black-hole-disk-mask-v1.png` (clamp): a face-on swirling disk whose hole ends at 0.235 of the image radius (`_DiskMaskInner`), aligned with the disk's inner edge.
- `black-hole-turbulence-mask-v1.png` (repeat): horizontal streaks. It tiles vertically but not perfectly horizontally, so the shader blends two samples half a repeat apart to hide the seam.

- **Accretion disk:** a tilted ring (`_Tilt` 0.19) built from the disk mask in the disk's own plane. Two rigid copies turn at inner and outer speeds and blend by radius, so the inner disk spins faster without the spiral winding up over time. Turbulence streaks flow along the orbit, and the approaching (left) side is brighter.
- **Lensing:** the disk's far side is bent into an arc around the horizon, strongest overhead, sampled from the same disk mask so its swirls match. A thin photon ring sits at the edge of the horizon.
- **Event horizon:** an opaque black disc; the near side of the disk passes in front of it, the far side is hidden behind it.

`SunTypeDefinition.displayScale` sizes a type relative to the System sun slot. The black hole uses 2.375 (sun `_DiscRadius` 0.475 ÷ horizon `_HorizonRadius` 0.2), so its black horizon is the same size as a sun's disc and the disk extends beyond the slot. `SunView` applies it as a local scale and turns off raycasts on the sun image, so an enlarged sun never blocks planet buttons.

The same `SunAppearance` fields drive it: core/surface/spot colors are the disk's inner/middle/outer colors, corona color is the photon ring and glow, surface speed is disk rotation, corona speed is turbulence drift and corona strength is the lensed arc and ring.

Per-system sun type: `SystemRecord.sunTypeId` is saved per system, but `WorldGeneration` currently assigns `WorldCatalog.defaultSunTypeId` (`yellow-star`) to every new system; there is no per-system selection yet. The Black Hole type is available in the catalog but is not chosen by generation.

## Sun in gameplay levels

Every gameplay level shows its system's sun (`SystemRecord.sunTypeId`, falling back to the catalog default) as a reminder of the sun type. `EcoSphereLevelSun` creates a world-space sprite with the same material choice as the System screen (`WorldCatalog.SunMaterial`): in front of the starfield (depth 15, sorting order -50) and behind the cannon, with the HUD visualizer drawing over it.

It forms a very low arch across the bottom of the screen, anchored to the cannon rather than the screen edge so every screen shape shows the same arch:

- `levelSunRadius` (7.5 world units; the playfield is 9.3 wide) sets the disc size: larger is flatter.
- `levelSunTopBelowCannon` (3.2) places the top of the disc below the cannon base, just above the visualizer frame in portrait.
- The animated sun shader's `_CoronaExtent` shortens the corona for this large sun so it rises only to the cannon base: extent = top-below-cannon ÷ (radius × `levelSunCoronaReach`), where `levelSunCoronaReach` (0.85) is how far the full corona visibly extends beyond the disc as a fraction of its radius. The System screen keeps the full corona (extent 1).

The sun type's `displayScale` keeps a black hole's horizon the same size as a sun's disc. The sun repositions whenever the camera refits.

A black hole in a level keeps its full reach by design: its lensed arc sweeps up behind the cannon and its glow tints the lower playfield up to the cluster (the black hole shader has no extent setting; the sun's `_CoronaExtent` does not apply to it).

Known follow-up: in portrait the HUD score line and control hints sit over the corona or lensed arc and are hard to read, especially with a black hole. Options are a text outline/shadow or a different color; the text may also be removed, since the visualizer already shows the important scores.

Checked in Play mode on a procedural level in portrait (900×1600) and wide (1600×900) renders (`Logs/LevelSun-portrait.png`, `Logs/LevelSun-wide.png`). The black hole has not been checked in a level yet.

## Planned: sun selection during system generation

Not implemented yet. Intended design:

- When every planet in a system is complete, **Next** on the System screen generates a new system.
- Generation chooses that system's sun type and/or color from other progress variables, such as unlocked spheres and achievements.
- It then generates the system's planets/levels and their modifiers. Modifier selection is weighted by the system's sun type.

Sun types, appearances and materials are already data (see above), so this should become an eligibility/weighting rule in shared generation data rather than per-sun code.

## Validation

Checked in Play mode on the System scene: the Yellow Star renders through `SunView` with the animated material, its surface and corona visibly change over five seconds, and a blue appearance applied through the same path renders correctly (`Logs/Sun-compare.png`, `Logs/Sun-yellow-blue.png`, `Logs/Sun-final.png`). Not yet checked: mobile performance, and the portrait System layout (only a wide Game view was captured). Flares are not implemented.
