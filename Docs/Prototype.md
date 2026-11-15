# First Light test level

Open `Assets/EcoSphere/Scenes/FirstLight.unity` in Unity, then press Play. The scene is also available through **EcoSphere > Open Test Level**.

The tutorial starts with four rings (60 elemental spheres) around the core, using Ice and Sunlight. Each resource has a fixed target, initially 15, editable through Resource Goal on the scene's root component. Both targets plus successful completion are the intended Water discovery conditions; actual unlocking and progression are not yet implemented.

- Move the mouse above the cannon to aim, click to fire. The short line uses the loaded element's RGB color.
- After resolution and the 24-degree turn, shots are randomly selected with equal odds among the types still attached to the core. A fourth identical consecutive load is forbidden if another type remains. If only one type remains, it continues to load. Only one shot is allowed until the turn finishes.
- Side and top walls reflect the shot without friction or spin. Returning through the bottom loses it.
- Attachment snaps to one of six neighboring hex positions, including around the core. Only the fired sphere's same-type connected group can match.
- Matches of at least three clear first; all spheres disconnected from the black-hole core then release. Empty core completes the level.
- Matches contribute zero and shrink/fade in place. Only disconnected breakaway spheres streak upward and contribute x3. Clearing the board with no breakaways still succeeds, with zero resources for a barren-world outcome. There is no average-based threshold.
- Ten Ice bars and ten Sunlight bars show planet-wide resource totals. The shared white line is the level's fixed target and remains stationary as totals grow. Labels show total/goal and READY when reached. Bar heights cap at 160% of the target, while numerical totals continue increasing.
- R restarts the level.
- An attached sphere touching any playfield edge or the cannon ends the level with no rewards. Checks run immediately after snapping (before matching/scoring) and after rotation (before loading). A match cannot rescue a losing attachment. All accumulated contribution totals are discarded on failure; queued match/breakaway audio is canceled. Only attached cluster spheres are checked, so fired shots can still ricochet normally. Cannon checks use transformed sprite triangles and the rigid backing, preserving the hollow cradle.

This is a gameplay prototype. The cannon's backing is a temporary code-drawn shape; final track artwork, unlock menus, and planet-result artwork are not implemented. Audio is implemented as described in Audio.md. Sphere identities and RGB colors are explicit; broader multi-resource definitions are deferred until the two-element loop is tested.

The root component exposes the two display colors, projectile speed, rotation angle, and rotation duration. Artwork references are serialized in the scene. **EcoSphere > Validate Board Rules** checks seeded matching, core connectivity, bridge release, six-neighbor spacing, swept impact and wall reflection. **Rebuild Test Level** regenerates only this test scene and imports the prototype textures as appropriate sprites/surface maps.
