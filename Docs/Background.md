# Space background

FirstLight now references starfield-v2 and twinkle-star-v1. The starfield covers the camera view without distorting its aspect ratio, resizing with the Game view. It sits behind all gameplay geometry.

24 small camera-facing overlay stars independently brighten with smooth pulses, varying size slightly. Most sit toward the edges. Each has a distinct initial delay and subsequent 5–16 second pauses, with pulses lasting 0.8–1.7 seconds. Animation uses a separate seeded random generator, leaving shot selection unaffected. No nebula or shooting-star effects are included.

Unity compilation and automated gameplay checks passed in the temporary validation project. A 900 x 1600 render was visually inspected before wiring the references into the main scene. Scene rebuilds retain these artwork references.
