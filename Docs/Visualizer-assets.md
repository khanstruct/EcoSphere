# Visualizer artwork

- Assets/EcoSphere/Art/UI/Visualizer/energy-grayscale-v1.png: 256×256 opaque grayscale texture for tinting and animated sampling in the future energy-bar shader. Repeat wrapping, linear sampling, no mipmaps, uncompressed, maximum import size 256.
- Assets/EcoSphere/Art/UI/Visualizer/visualizer-frame-v1.png: 256×96 RGBA sprite with transparent center/exterior, clamp wrapping, no mipmaps, uncompressed, maximum import size 256. Nine-slice borders: left/right 24, top/bottom 20 pixels. Use a Sliced UI Image with fill center disabled; add a separate dark translucent panel if wanted.

These are artwork assets only. The material and shared frame are connected to gameplay through Resources/VisualizerStyle.asset. The visualizer sits below the cannon and lays out one bar per contribution unit, grouped by sphere. All selected contribution units fit on one baseline, with an 82-unit width cap and automatic narrowing for dense loadouts.

## Energy material

Shader: Assets/EcoSphere/Shaders/UIEnergyBar.shader (EcoSphere/UI/Energy Bar). Shared material: Assets/EcoSphere/Art/UI/Visualizer/energy-bar-v1.mat.

Use EnergyBarGraphic on a RectTransform and assign that material. Set Graphic.color to the source sphere tint. The rectangle is the filled bar itself: adjust its height to the current displayed value. The component expands the mesh by 12 UI units for the surrounding glow; leave room between adjacent bars. It configures the Canvas extra vertex channels automatically. All instances share a material; vertex data carries tint, size, gameplay time, phase, leader state and self-combination state.

Leading enables the traveling contour flare. SelfCombination strengthens it with a second gold flare opposite the first; the caller must supply the gameplay eligibility state (majority plus minimum). Phase offsets animations between bars. Gameplay time freezes animation during pauses. PreviewTime allows deterministic editor rendering.

Material controls: grayscale texture, tint multiplier, upward flow speed, texture tiling, surface brightness/contrast, edge lighting, soft halo, flare speed and flare brightness. The shader implements soft RectMask2D clipping, UI stencil masking, CanvasGroup/vertex alpha, UI depth testing and optional alpha clipping. It uses alpha-blended glow without requiring a bloom postprocess. The material is designed for EnergyBarGraphic, not a plain UI Image: per-bar data comes from the custom graphic.

Validation: EcoSphere / Build and Validate Energy Bar Material compiles the shader, renders tinted ordinary/leading/self bars, verifies texture movement across two fixed times and checks both rectangular and stencil masks. Preview PNGs are written to the validation project's Logs/Energy-shader-preview.png and Logs/Energy-shader-motion.png.

The panel width follows the projected mobile playfield width rather than the full desktop screen. Dense layouts reduce gutters as well as bar widths; very narrow element labels use initials.

Bar heights now use a smooth leader-relative scale, independent of inclusion/excess thresholds. The largest selected element targets 85% of available bar height (VisualizerStyle.leaderHeightFraction). Other elements use the same scale and preserve proportional heights. Empty bars retain a minimal baseline. Numeric totals and recipe eligibility continue to use the original scores and thresholds. Old fixed threshold reference lines were removed because they no longer describe this relative display.

Leadership and flare eligibility: at least 35% of selected elemental points AND the per-world inclusion minimum. Only then does gameplay set EnergyBarGraphic.Leading. SelfCombination additionally requires strictly more than 50%. A high-ranking element below either requirement remains unaccented.
