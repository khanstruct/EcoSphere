# Tutorial levels

## Tutorial 1

`Assets/EcoSphere/Scenes/Tutorial1.unity` is the introductory level and the first scene in build settings. Its Level Layout is First Tutorial. The four-ring board contains exactly 30 connected Ice spheres on the left and 30 connected Sunlight spheres on the right, with no Stone or laser. Center-column cells are divided by vertical position to keep antipodal cells opposite and both halves equal.

Inclusion is 500 per element; excess is 1,500. A clean matching shot clears a half plus its projectile (31 × 20 = 620 points), allowing both elements to reach inclusion without reaching excess. Ammo uses the ordinary board-aware selector, so after one half is cleared only the remaining element loads. Tutorial 2's staged ammo rules and message windows are disabled here. Tutorial 1's own message sequence and combination-discovery reward will be implemented separately.

`FirstTutorialTools.Build` creates/updates the scene from the shared gameplay setup; `FirstTutorialTools.Smoke -firstTutorialSmoke` checks the exact 30/30 split, connected halves, no messages, two real shots, and 620 points for each element. `Logs/Tutorial1-preview.png` is a gameplay preview.

## Tutorial 2 — Stone discovery

The existing `FirstLight.unity` scene remains the Stone-discovery level and now displays Tutorial 2 in its HUD. Its large temporary thresholds and existing tutorial messages are retained.

The initial four-ring cluster has a complete Sunlight outer ring. Rings two and three contain Ice, except for one Stone at axial coordinate (0, -3). The innermost ring shuffles three Ice and three Sunlight spheres, keeping a small final cluster after the Ice lesson. Stone is outside the core's immediate neighbors and can be released.

Ammo is Sunlight until the first successful match, Ice until Stone is released, then the normal available Ice/Sunlight selection. Missed and nonmatching shots retain the current lesson's ammo. Stone remains board-only and contributes nothing to the visualizer.

Four rounded messages introduce the goal, point to the loaded sphere, point to Stone after the first match, and explain finishing the level after Stone is released. The first message advances to the cannon message; subsequent Continue buttons dismiss the overlay. Later messages open after turn resolution and rotation, before aiming resumes. Gameplay time stops while a message is open; input is shielded and the dismissal frame cannot fire or restart the level. Existing frame artwork, Outfit text, and the CelestialButton prefab are reused.

Run `EcoSphere.Editor.TestLevelTools.RunSmoke` with `-tutorialSmoke` for actual-shot checks of ring layout, modal pause/input, both ammo phases after misses, first match, Stone release, and final dismissal. Preview renders are written under the validation project's Logs directory. Regular gameplay and laser smoke modes bypass interactive messages.
