# Planet texture generation

Generated with the built-in image generation tool. Original images are retained here; the game consumes resized 256 × 128 copies under Assets/EcoSphere/Art/Planets.

## Heightmap prompt

Use case: stylized-concept. Asset type: production game planet HEIGHTMAP texture, flat 2:1 equirectangular full-frame rectangular texture, not a rendered globe. Generate a seamless horizontally wrapping grayscale elevation map for a barren rocky moonlike world with broad irregular lowland basins and connected highland continent shapes, plus restrained small shallow circular craters. Black is low elevation and white is high elevation. Most values medium gray with meaningful wide dark basins and lighter highlands. No lighting, shadows, highlights, perspective, globe, margins, labels, stars, transparency or water. Poles near top/bottom should have quiet low-frequency detail to avoid pinching. Left and right edge MUST match seamlessly. Broad shapes must remain legible after reducing to 256x128. This is physical elevation data, not a shaded rock photograph. Output landscape aspect 2:1.

## Albedo prompt

Input: generated heightmap as the edit reference.

Use case: precise-object-edit. Input image: exact terrain layout reference and edit target, a flat 2:1 equirectangular grayscale heightmap. Create its MATCHING BARREN ROCK ALBEDO texture for a small mobile game planet. Preserve EXACT positioning, shape and outline of EVERY basin, highland and crater; do not move, invent, enlarge, erase or rearrange terrain. Retexture it with natural neutral gray moonlike stone, fine rocky grain, dusty gray lowland floors, subtly brighter gray highlands. This is unlit albedo: no directional cast shadows, no highlights, no global light gradient. Keep contrast moderate and all terrain gray, barren and lifeless. The result must remain a flat edge-to-edge 2:1 texture, seamless across left/right, quiet polar detail. No globe, water, vegetation, atmosphere, borders, labels, text or transparency. Broad terrain must read at 256x128. Output landscape 2:1.
