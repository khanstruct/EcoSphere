# Scale and reflected aiming guide

Spheres, core, halo, icons, and cannon artwork now use 60% of their original dimensions. Hex spacing and projectile collision radius were reduced proportionally. The loaded sphere lives outside the scaled artwork hierarchy so its size matches fired and attached spheres.

The guide remains the loaded sphere's RGB color. Its maximum path length is 75% of the playfield width (6.525 world units), including reflected segments. Side and top contacts use the same sphere-radius-adjusted bounds as live shots; bottom contacts terminate the guide. Each segment stops at the nearest sphere or core using the live shot's combined collision radius, including after ricochets. Rotated cluster positions are refreshed each frame. The guide predicts impact, not the final snapped cell. Direct nearest-sphere and post-ricochet interception checks passed in Unity.

Unity compiled successfully; the gameplay smoke run passed matching, release, bank shots, rotation/reload and loss. Four additional checks passed for first side reflection, radius clearance, total reflected path length, and top reflection, alongside the existing 17 board checks. The smaller layout was inspected in a rendered screenshot. Audio and starting cluster composition were left unchanged.
