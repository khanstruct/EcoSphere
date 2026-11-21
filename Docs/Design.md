# EcoSphere — working design

Shared gameplay rules and defaults belong in common components and reusable data assets. New levels should inherit those behaviors automatically. Use world data for deliberate differences; avoid per-level copies of universal rules, clip assignments, or checks against world names. See [Audio.md](Audio.md) for the shared audio pattern.

Current sphere attributes, opening pair, contribution rules and discovery behavior are documented in [Spheres.md](Spheres.md). Those implementation notes supersede the earlier exploratory contribution and progression rules below.

## First milestone
Assemble approved cannon sprites; aim left/right; independently open arms on release; load an Ice or Sunlight sphere behind the foreground. Then implement one playable rotating bubble-shooter board. Use temporary backing/track if needed: those final assets are not yet approved.

## Board and rendering
- Textured 3D spheres, with independent camera-facing transparent icons. Surface rotates without rotating icons.
- Gameplay in a 2D plane, deterministic shots and wall ricochets. Six neighboring attachment cells on a shared rotating hex grid.
- Match only identical sphere types, including corrupted variants of that type.
- Remove matches of three or more, then release clusters disconnected from the core.
- Planet occupies top of portrait screen; released material travels upward into it. Cannon below cluster; grouped contribution bars at bottom.

## Contributions and discovery — prototype rule
- Sphere identity, unlock recipe, contribution list, and requirements are independent definitions.
- A Human sphere may contribute Human, Mammal, and Life despite its Mammal + Stone recipe.
- Each selected sphere type has a visualizer group containing its contributed resource bars. Repeated resources show identical planet-wide totals.
- Matches clear the cluster but contribute zero resources. Only disconnected breakaways contribute, at x3 per sphere. Matched spheres disappear in place; only breakaways streak to the planet.
- The visualizer line is a fixed per-level resource goal, initially 15 for Ice and Sunlight in FirstLight. It does not depend on average totals. Resources at or above their targets qualify for recipes after successful completion. Some recipes require more than two resources. All totals affect final planet appearance and description.
- Clearing the board succeeds even with zero contributed resources; this is a barren-world outcome, not a failure. Actual discovery progression and planet-result imagery remain future work.
- Corrupted Water matches Water for safe disposal; released corrupted Water reduces final Water. Generalize per type as authored.
- Requirements are exploratory, not finalized. Avoid cascading penalties and death spirals.

## Progression and rewards
- Start with a small selection such as Ice, Stone, Sunlight. Ice + Sunlight unlocks Water; Stone + Sunlight may unlock Magma. Recipes remain provisional.
- New sphere reveals precede next-world selection. Fixed number of active sphere types; some required slots, other slots player-selected.
- Several planets form a system; earlier planets affect later availability. Systems contribute to galaxy progression; persistence details remain undecided.
- Completed planet moves center, zooms, fades to a layered ground-view portrait and matching description. Ingredients determine coherent environment, life and civilization. Magic + reptiles can produce dragons without a Dragon sphere.
- Large releases may trigger themed announcer calls, such as Hydrosphere or Biosphere.
- Powerups under consideration: extended aiming for two shots and wild sphere adopting the type it hits. Acquisition rules not final.

## Approved art and cannon assembly
- Approved: latest left/right arms with hinge joints; main foreground shell; Ice and Sunlight surface/icon pairs.
- Do not substitute earlier rejected launcher component batches.
- Back-to-front: stationary track rear, rotating rigid body backing with blue lights, sphere, articulated arms, rotating foreground shell, stationary track front.
- Arms pivot open independently; rigid backing does not open. Main shell covers hinge joints. User initially accepted these three cannon pieces as sufficient to begin.
