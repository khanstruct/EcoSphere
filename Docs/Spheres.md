# Sphere definitions

World identity, saved system generation, Genesis Alpha/Beta placement and per-world requirements/modifiers are documented in [Worlds.md](Worlds.md). Tutorials retain their specialized layouts while using those shared world records.

Sphere data now lives in `Assets/EcoSphere/Data/Spheres/*.asset`, referenced by `Assets/EcoSphere/Resources/SphereCatalog.asset`. Edit these assets in the Unity Inspector. Gameplay, cannon loading, previews, preparation, discovery results, streaks, resource bars and element voice selection all read this catalog. Scene-local copies of artwork and colors are no longer used.

Each definition has a stable integer ID, display name, surface texture, icon, color, 1–4 total contribution units (repeats allowed via amount), a nonnegative `unallocatedWeight`, a gameplay role, selection/initial-availability flags, an unlit flag, and an optional resource voice. A sphere is not an element: its contribution list and the ingredients required to unlock it are independent. Recipes reference their output sphere directly; they do not infer identity from the first contribution. Large-match voices also use the matched sphere's own definition.

| Sphere | ID | Default contribution | Role |
| --- | --- | --- | --- |
| Core | -1 | Core: 1 | Structural, never collected or selected |
| Ice | 0 | Ice: 1 | Element |
| Sunlight | 1 | Sunlight: 1 | Element |
| Laser | 2 | Laser: 1 | Powerup; does not match or enter ordinary ammo |
| Stone | 3 | Stone: 1 | Element |
| Fire | 4 | Fire: 1 | Starting element |
| Void | 5 | Void: 1 | Starting element, unlit surface |
| Water | 6 | Water: 1 | Element; DEV available, unlocked by Ice + Fire |

Fire and Void contribute only their namesake elements. Existing elemental defaults also remain single-element; additional Water for Ice or Fire for Sunlight can be authored without changing gameplay code. Water now has its own registered sphere definition and matching surface/icon. Its assigned blue is #1464E1, darker than Ice, and its voice is hydrosphere. Laser now contributes its own resource when consumed, while retaining zero total score for the consumed powerup. Core has the same attribute schema but its structural role prevents collection. Its existing halo serves as its artwork icon, while the board keeps its special halo presentation.

## Amounts and colors

Each definition's `resourceVoice` is its editable large-match voice: Fire → pyrosphere, Void → shadowsphere, Ice → cryosphere, Sunlight → heliosphere, Stone → lithosphere. A match earning at least 500 points plays the matched sphere's assigned clip, independently of its contributed elements. This takes priority over the existing resource-gain callout for that shot, avoiding duplicate announcements. Single-element gain announcements still use the element's primary sphere voice, and mixed gains retain the EcoSphere callout when no qualifying match voice was selected.

Contribution amounts are integer units, with at most four total units per sphere including unallocated units. Repeated units use a single entry with amount 2–4. The visualizer creates one bar per real unit; unallocated units create no bars. Existing single-element definitions were migrated from 10 to 1 with unchanged proportional yield. The denominator includes `unallocatedWeight` (default 0), which consumes its share without delivering element points or reducing total match/breakaway score. For example, Fire: 1 + unallocated: 1 supplies 6 Fire from 12 points; Fire: 2 + unallocated: 0 supplies 12 Fire. Unallocated weight consumes part of the four-unit budget and has no element ID, bar, percentage, or recipe. After calculating each sphere’s earned points, divide those points proportionally among its elements. A sphere weighted Void: 1 and Fire: 3 distributes a 12-point match as 3 Void + 9 Fire, or a 50-point breakaway as 12.5 Void + 37.5 Fire. Element totals use double precision throughout the level, including final recipe evaluation. Displayed element scores truncate toward zero without changing the stored totals. No completion-time rounding is applied to eligibility.

Every contribution is accumulated by element ID, independently of sphere ID. Each source sphere's visualizer channels use that sphere's color, exactly as its ascent streak does. Repeated elements in different source groups display the same planet-wide total. Channels are created only from the selected loadout definitions at level initialization. Board-only spheres and powerups never add channels when collected. Naturally occurring-only elements remain outside visualizer standings, composition percentages, and recipe evaluation; their separately scripted discoveries still occur. A natural element can appear normally when a selected sphere also supplies that element. Losing clears every element total.

## Opening and discovery

The catalog's opening pair is Fire and Void. Tutorial 1 uses this pair by default, or the actual pair selected on the preparation screen. Its balanced two-half layout requires two distinct sphere types. Tutorial 2 consumes a distinct three-sphere setup loadout and retains the guided Stone sequence with assigned ring types.

The preparation collection is built from initially available definitions and session discoveries. Fire and Void are initially available; Stone retains its existing breakaway discovery. Recipe-discovered spheres become selectable on subsequent preparations. The second planet in System 1 now maps to Tutorial 2 (`FirstLight`) with three selected spheres. The first selected non-Stone type supplies the outer ring and Stone’s inward support. Stone occupies the outer ring; the remaining interior mixes all selected types. Guided ammo repeats that support type until Stone actually releases, then returns to normal board-aware selection. The natural-element lesson appears at level start. Direct launch defaults to Fire, Void and Sunlight. Later worlds use the shared Procedural scene with a four-sphere loadout and saved randomized grouping.

Recipes live separately in `Assets/EcoSphere/Data/ElementRecipes.asset`. Each has exactly two element inputs and an output SphereDefinition reference. Input order does not matter and duplicate unordered pairs are rejected. The initial recipe is Fire + Void → the Sunlight sphere. Both ingredients must each have at least 35% of the selected elemental contribution total and meet the level’s inclusion goal. A self-pair additionally requires strictly more than 50% of total contributions. Successful worlds award every qualifying authored recipe once; failure awards no discoveries. Matching spheres on the board does not perform direct crafting or consume recipe resources. See [Element-composition.md](Element-composition.md) for ranking and tie behavior.

## Adding a sphere

1. Create an **EcoSphere / Sphere Definition** asset and assign its identity, artwork, color and 1–4 contributions. Give it a unique stable ID; never renumber existing IDs.
2. Add it to the catalog. Set its selection and availability flags as appropriate. New element identities require an `ElementId` enum entry appended at the end; a sphere using existing elements needs no code changes.
3. Add any recipes to the recipe book, assigning two element ingredients and the output sphere asset independently of its contribution list.
4. Run **EcoSphere / Validate Sphere Catalog**. Startup also validates missing art, invalid amounts, duplicate IDs/elements, invalid opening pairs and duplicate recipes.

**EcoSphere / Configure Sphere Catalog** creates missing built-in assets without resetting existing contributions, colors, artwork or recipes. Sphere textures and icons retain the 256px import limit.

## Validation

Validated with Unity 6000.6.7f1 in the isolated `work/UnityValidation` project, leaving existing editor sessions undisturbed. Compilation, catalog/recipe checks, board-rule checks and seven play-mode smoke runs passed: four-element data flow, the Fire/Void opening, general gameplay/scoring/announcements, Laser, the guided Stone tutorial, preparation/navigation, and results/discovery animations. Portrait and wide multi-contribution renders and the opening/collection art were inspected. All 35 serialized GUID references in the nine catalog/data assets resolve in the main workspace.

Logs are under `Logs/SphereCatalog-*.log`; rendered previews are under `work/UnityValidation/Logs`. The Unity editor continues to log its pre-existing `UnityEditor.Search.SearchDatabase` indexing exception, which does not prevent these gameplay checks. The local regression runner is `work/validate-spheres.ps1`.

Recipe awards now retain their two ingredient element IDs with the output sphere ID in CompletedWorld.discoveryDetails. Results use this recorded provenance for fusion reveals, including self-recipes, and retain the existing ID list for historical collection display. See Results.md for the animation and shared shader/style assets.

Sphere definitions also expose optional glowColor and glowStrength. Void enables a subtle white halo at strength 0.42; other built-in spheres default to no added halo. SphereGlow supplies one shared procedural 256px alpha sprite in both gameplay and SpherePreview stages, preserving the black surface and galaxy icon. Gameplay halos face the camera independently of sphere/cluster rotation. Match dissolves multiply each sprite's original alpha so the halo never jumps to full opacity.

Temporary DEV mode exposes the entire selectable collection and skips tutorial messages without marking them completed. See Development.md for the shared toggle and normal-mode testing override.

Ice + Fire → Water is now authored in ElementRecipes.asset and is order-independent. Both ingredients use the shared 35% composition and per-world minimum requirements; successful matches do not bypass recipe qualification.
