# Ammo chambers

Reserve chambers sit in a connected row beside the cannon. `WorldRules.ammoChambers` sets the count per world (0–4, validated by the world catalog). It defaults to 0, so Genesis Alpha and Beta have none; `WorldCatalog.proceduralRules.ammoChambers` is 1, so chambers appear from the first procedural world (level 3) on.

## Behavior

- **Swap:** Spacebar, clicking a chamber, or tapping one turns the ring one step: cannon → last chamber, chamber 1 → cannon, each other chamber moves one place toward the cannon. Swaps are free. They are ignored while a shot is in flight, the arms are moving, a tutorial is open, or the level has ended. Each swap plays `Audio/ammo-swap.mp3` (`GameAudioSettings.ammoSwap`).
- **After a shot:** the ring advances: chamber 1 slides into the cannon and the last chamber receives a new draw from the ordinary ammo selector.
- **Absent types:** at the start of each turn, a held type that is no longer on the board is replaced with a fresh draw before the ring advances.
- **Draws:** chambers and cannon use the same draw (`TryDrawAmmo`), including Genesis Beta's guided supporting ammo. Laser smoke tests still force the first cannon load.

## Input

- Desktop: hover to aim, click to fire, Space or click a chamber to swap, R restarts.
- Touch: drag to aim, release to fire. Releasing over the resource meters or below the cannon cancels the shot (the aim line hides while cancelling). Tap a chamber to swap. Mouse events are ignored for 0.5 s after touch input to avoid double handling.

## Code and data

- `AmmoRing` — pure ring logic (fill, advance, rotate, replace).
- `EcoSphereAmmoChamber.cs` — chamber visuals, turn animation and shooter input (partial of `EcoSpherePrototype`).
- `Resources/AmmoChamberStyle.asset` — frame sprite (`Art/Cannon/ammo-chamber.png`, 256×234), frame width, sphere scale, first chamber offset (used without a cradle and for the left-hand row), connector overlap, chambers per side, turn duration, and the cradle settings below. A missing sprite falls back to a generated placeholder ring.

## Cannon cradle

Cannon size, rotation pivot and aim limit are authored on the style (`cannonScale` 0.9, `pivotHeight` 0.49 = the body center, where the loaded sphere rests, `maxAimAngle` 65°, `cannonVerticalOffset` -0.12 so the cannon's lower rim covers the cradle's rim). With the pivot at the body center the cannon turns in place over the cradle and the loaded sphere stays still; a bottom pivot (tried at -0.34) swung the cannon out of the cradle. A pivot off-center makes the sphere swing with the cannon; `ShotOrigin` (read through `GameplayGeometry`) gives the shot start for gameplay, the geometry checks and the smoke checks, and the bank-shot check searches only within the aim limit. A stationary cradle (`Art/Cannon/cannon-cradle.png`, 256×105) is layered by `cradleSortingOrder`/`cradleDepth` (currently behind the cannon: 1 / 0.4; in front: 8 / -0.8) on a non-rotating "Cannon mount" at the cannon base (`GameplayGeometry.CannonBase`), in every level (including those without chambers). `cradlePivot` marks the center of its U and is placed at `cradleOffset` (the cannon body's center); `cradleConnector` marks the outer edge of its right connector, and the first right-hand chamber chains from there, so moving or resizing the cradle moves the chambers with it. The cradle counts as part of the cannon for the "cluster touches the cannon" loss check.

Sizes and positions were fitted to the approved mockup (cannon, cradle and chamber assembled): the mockup's cannon bounds were matched to the in-game cannon artwork, and the cradle and chamber were converted with the same scale (cradle 2.28 world units wide, chamber 1.70). Layering follows the mockup: arms behind the cannon body, the cannon body behind the cradle, and each chamber behind the piece to its left, so its left connector tucks under that connector by `connectorOverlap` (0.06). The chamber position computed from the cradle connector agrees with the mockup's measured chamber center to within 0.001 units.

## Validation

Verified in Play mode on the Procedural scene through real Input System events: a Space key press swaps; a touch tap on the chamber swaps; a touch drag onto the meters fires nothing; a touch drag-and-release fires; after the turn an absent held type is replaced and the ring advances. Portrait capture: `Logs/AmmoChamber-portrait.png`. Not yet covered: an automated smoke routine, landscape layout, 2+ chambers, and physical-device touch.
