# Sphere definitions

World identity, saved system generation, Genesis Alpha/Beta placement and per-world requirements/modifiers are documented in [Worlds.md](Worlds.md). Tutorials retain their specialized layouts while using those shared world records.

Sphere data now lives in `Assets/EcoSphere/Data/Spheres/*.asset`, referenced by `Assets/EcoSphere/Resources/SphereCatalog.asset`. Edit these assets in the Unity Inspector. Gameplay, cannon loading, previews, preparation, discovery results, streaks, resource bars and element voice selection all read this catalog. Scene-local copies of artwork and colors are no longer used.

Each definition has a stable integer ID, explicit namesake element, category, display name, surface texture, icon, color, 1–4 total contribution units (repeats allowed via amount), a nonnegative `unallocatedWeight`, a gameplay role, selection/initial-availability flags, an unlit flag, and an optional resource voice. A sphere is not an element: its contribution list and the ingredients required to unlock it are independent. Recipes reference their output sphere directly; artwork and element voices resolve through the explicit namesake, never the first contribution or a single-contribution test. Catalog validation rejects duplicate IDs and namesakes. Large-match voices also use the matched sphere's own definition.

| Sphere | ID | Default contribution | Role |
| --- | --- | --- | --- |
| Core | -1 | Core: 1 | Structural, never collected or selected |
| Ice | 0 | Water: 1, Ice: 2, Null: 1 | Element |
| Sunlight | 1 | Fire: 1, Sunlight: 2, Null: 1 | Element |
| Laser | 2 | Laser: 1 | Powerup; does not match or enter ordinary ammo |
| Stone | 3 | Stone: 3, Null: 1 | Element |
| Fire | 4 | Fire: 3, Null: 1 | Starting element |
| Void | 5 | Void: 2, Null: 2 | Starting element, unlit surface |
| Water | 6 | Water: 3, Null: 1 | Element; DEV available, unlocked by Ice + Fire |
| Plant Life | 7 | Life: 1, Plant: 2, Null: 1 | Element; DEV available, normal locked |
| Life | 8 | Life: 3, Null: 1 | Element; DEV available, Water + Mud recipe |
| Mud | 9 | Water: 1, Stone: 1, Mud: 2 | Element; DEV available, Stone + Water recipe |

Fire and Void contribute only their namesake elements. Ice contributes Water plus Ice; Sunlight contributes Fire plus Sunlight. All six playable elemental spheres now use four total units including Null. Water now has its own registered sphere definition and matching surface/icon. Its assigned blue is #1464E1, darker than Ice, and its voice is hydrosphere. Laser now contributes its own resource when consumed, while retaining zero total score for the consumed powerup. Core has the same attribute schema but its structural role prevents collection. Its existing halo serves as its artwork icon, while the board keeps its special halo presentation.

## Amounts and colors

Each definition's `resourceVoice` is its editable sphere voice: Fire → pyrosphere, Void → shadowsphere, Ice → cryosphere, Sunlight → heliosphere, Stone → lithosphere. Callouts are decided once for the entire shot using raw earned points grouped by source sphere ID, before contribution percentages or Null. GameAudioSettings.minimumMatchedSpheresForVoice (default 6, minimum 3) defines the threshold as that match size's total score: currently 96 points, equivalent to a six-sphere match or two 50-point breakaways of one type. Matched and released spheres of the same type accumulate together. One qualifying type plays its assigned voice; two or more qualifying types play EcoSphere once instead of any individual voices. A mixed-element sphere remains one sphere type and cannot trigger EcoSphere by itself. The audio threshold is independent of recipe requirements; gain-only compatibility callouts use the same audio point threshold.

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

Current test contribution mixes (four total units each): Fire = 3 Fire + 1 Null; Sunlight = 1 Fire + 2 Sunlight + 1 Null; Water = 3 Water + 1 Null; Ice = 1 Water + 2 Ice + 1 Null; Stone = 3 Stone + 1 Null; Void = 2 Void + 2 Null. Null remains unallocatedWeight. This changes elemental yields, visualizer unit counts and resulting composition/planet stats; total score rates, recipes and thresholds are unchanged. Element voice selection prefers a dedicated single-element definition, then the strongest matching mixed contribution, so Sunlight cannot steal Fire's voice and Ice cannot steal Water's. Large-match voices remain assigned per sphere.

Balance consequence: the intended two-shot Genesis Alpha clear still scores 1240, but yields 465 Fire and 310 Void. Both are below its unchanged 500-point minimum, so the normal opening clear no longer guarantees the Sunlight recipe. This is an intentional contribution-test observation pending separate balance decisions.

Genesis Alpha's inclusion minimum is now 300 (excess remains 1500). Its intended two-shot clear yields 465 Fire and 310 Void, so both ingredients meet the minimum and the normal Fire + Void Sunlight unlock is restored.

Genesis Alpha now uses a 300-point inclusion minimum and a 1,000,000-point excess threshold. Self-combination remains enabled under the shared rules, but requires reaching that high excess threshold.

Plant Life is registered as sphere ID 7 with medium green #3EA04A, the 256 x 128 foliage surface, transparent 256 x 256 leaf icon, and Audio/Voice/florasphere.mp3. Contributions: 1 Life, 2 Plant, 1 Null (25% Life / 50% Plant / 25% unallocated). It is selectable in DEV, not a normal starting sphere, and has no unlock recipe yet. Existing Life/Plant base-stat profiles receive its yields; no new vegetation rendering is added at this step. PlantSphereTools.Configure validates authored data and PlantSphereTools.Smoke with -plantSphereSmoke verifies normal/DEV availability, setup, a balanced generated board, actual match/breakaway yields, real-unit bars, source color, voice and portrait/wide previews.

## Life sphere

Life uses the pink-white cellular surface and cell icon in Art/Spheres/Life, pale pink #F6B7CD, and the biosphere voice. It is the canonical namesake for ElementId.Life, so Life recipe ingredients and element callouts use this artwork and voice rather than borrowing Plant's. Its 3 Life / 1 Null split yields 9 Life from a 12-point match and 37.5 Life from a 50-point breakaway. The existing Life planet profile supplies biological coverage and soil tint. DEV mode exposes it through the shared catalog rules; ordinary starting availability is false. Its active creation recipe is Water + Mud.

## Mud sphere

Mud (sphere ID 9, appended element ID 52) uses its wet-mud artwork, light brown #CA9C6B (distinct from pale-gray Stone #D0D3D8), and pelitosphere voice. Its four units are 1 Water / 1 Stone / 2 Mud, with no Null: a 12-point sphere yields 3 Water, 3 Stone and 6 Mud; a 50-point breakaway yields 12.5 Water, 12.5 Stone and 25 Mud. Stone + Water unlocks Mud; Water + Mud unlocks Life through the shared recipe/discovery rules. Both are available for DEV testing while remaining locked normally until earned. Mud's planet profile contributes Soil at weight 1, compared with Life 0.2 and Plant 0.25, strongly warming exposed land toward brown through the existing percentage/buildup and soil shader response. The Water and Stone units additionally use their existing independent planet profiles.

## Match clear visuals

Ordinary shot matches pass their attachment cell to MatchWave, which walks matched hex neighbors in breadth-first layers. Equal-distance branches flash/collapse together. Each layer starts up to 0.035 seconds after the preceding one; long chains compress the interval so the last layer starts within 0.22 seconds. Each sphere flashes white briefly and collapses over 0.20 seconds, keeping the scheduled visual clear within 0.42 seconds plus frame presentation. The shared additive MatchFlare material works on lit and unlit surfaces, including Void, without changing source materials or brightening its persistent halo. No new sprite assets are needed. Scoring, connectivity, release detection, announcements and the existing turn-resolution wait remain immediate/unchanged; the wave is visual only and uses the same rules in every level. Run MatchWaveTools.Configure for shader/graph/timing checks and MatchWaveTools.Smoke with -matchWaveSmoke for the branched visual test.

## Gravity-captured planet streaks

Every breakaway uses a cubic path with randomized downward/lateral controls, then accelerates into a sampled point on the visible bottom hemisphere of the actual receiving sphere. Visual-only System.Random is independent of board/ammo random sequences. Flights take 0.68-0.80 seconds, preserve the source sphere color and shrink its visible head. Trails stop at the surface and fade for 0.23 seconds instead of being cut off immediately. Each arrival creates a bright additive white-core flash lasting 0.24 seconds, attached to the planet. Impacts start at a visible size, hold peak brightness for 0.045 seconds, then expand and fade. Their dedicated shared material draws over the surface so the planet cannot clip the glow; travelling energy retains normal depth testing. The procedural EnergyFlash shader needs no sprite texture.

A shot match still clears within the MatchWave time budget. Exactly one sphere from the latest wave layer (stable coordinate tie-break) emits a smaller single energy streak when it finishes dissolving; other matched spheres emit none. This representative is a visual cue, with no change to awarded points or elemental totals. Completed-world transitions allow active flights/tails to finish before changing scenes. Per-sphere identities, colors, contribution weights and audio remain catalog-driven.

PlanetStreakTools.Configure checks 500 randomized paths for initial downward motion, accelerating approach, endpoint surface geometry and impact variation. PlanetStreakTools.Smoke with -planetStreakSmoke checks breakaway counts, initial falls, impact flashes, representative selection on tied branches, scores and effect cleanup.

Stone now uses stone-surface-v2.png and stone-icon-v2.png, a pale-gray rock/mountain motif, through its shared sphere definition. Its #D0D3D8 color drives both streaks and visualizer bars. The original brown v1 artwork remains archived; the lithosphere voice and 3 Stone / 1 Null contributions are unchanged.

## Setup categories

SphereDefinition.category is an editable SphereCategory enum: Basic, Forces, Weather, Life, Technology, Beyond. All is a setup filter, not an assignable category. Existing elemental spheres, including Air, are Basic except Void (Forces) and Plant Life/Life (Life). Core is Forces and Laser is Technology for data completeness, but these nonselectable roles never create empty collection tabs.

Each setup opens on All. The tab row contains All followed by categories represented by owned, selectable spheres, in the enum order. DEV availability uses the same collection rules and therefore shows all current elemental spheres/categories. New unlocks appear on reopening setup. Filtering repacks visible tiles, starts the collection at the top, and preserves the chosen loadout and Begin gating. Hidden collection previews disable their separate render stages/cameras. When all categories do not fit, the row supports horizontal scrolling and arrow navigation. No per-world category lists or new art assets are needed.

Category tabs use dim, nearly neutral gray for inactive labels/borders. The active tab keeps its warm highlight and shows a small white-gold light immediately before its label. TabSelectionLight renders the soft glow directly as UI geometry, with no sprite or texture asset. Space is reserved in every tab so changing the active category does not shift the labels.
