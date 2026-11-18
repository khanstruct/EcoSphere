# Typography

Game text uses TextMesh Pro with Outfit SDF assets in Assets/EcoSphere/Fonts. The System heading and gameplay HUD use Medium; planet requirement counts use SemiBold. The laser uses world-space TextMeshPro with an upright countdown. Existing positions, font sizes and colors were preserved, with a TMP material outline for requirement counts.

Outfit's supplied variable TTF remains unchanged. Static Regular, Medium and SemiBold instances were derived for consistent Unity font weights. SDF atlases use 90-point sampling, 9-pixel padding and 1024-square dynamic atlases with commonly used characters prepopulated. TextMesh Pro essential resources are included under Assets/TextMesh Pro.

FontTools.Apply migrates existing scene labels and assigns serialized runtime font references without rebuilding scene layouts. Scene builders now create TextMeshProUGUI labels. Both FirstLight and System contain no legacy UI Text or TextMesh components after migration.

Unity compilation, five System selection callbacks and the laser play-mode smoke test passed in the validation copy. Rendered System and gameplay previews were visually inspected, including the countdown. Logs and previews are under Logs/TMP-migration.log, Logs/TMP-gameplay.log, Logs/System-preview.png and Logs/Laser-preview.png. The validation editor's existing SearchDatabase indexing exception is unrelated to gameplay text.
