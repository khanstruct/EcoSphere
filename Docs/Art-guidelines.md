# Game art size budget

Non-background game image sources must be no more than 256 pixels on their longest side. Preserve aspect ratio and alpha. Sphere icons are 256x256; 2:1 sphere surface maps are 256x128. Button and heading layers share 256x85 canvases. The already-trimmed sphere requirement icon remains 128x123.

Full-screen starfield backgrounds retain their original resolution. TMP font atlases are generated text resources and are excluded from this image-source budget.

Full-resolution originals and their pre-resize import metadata are archived outside Unity's Assets folder under SourceArt/Originals-2026-11-18. The CSV manifest lists old/new dimensions and PNG byte sizes. Forty-two oversized game images were reduced from 43.51 MiB to 2.05 MiB of source PNG data. That is a source-file comparison, not a measured build-size or GPU-memory figure.

Sprite GUIDs and normalized pivots are preserved. Pixels-per-unit and nine-slice borders were scaled with the images to retain existing world size and corner dimensions. Editor builders use resolution-relative slice borders and retain pixels-per-unit. GameArtImportRules caps future non-background imports at 256, including explicit platform overrides; authoring sources should still be exported at the correct size.
