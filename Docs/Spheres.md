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
| Air | 10 | Air: 3, Null: 1 | Element; DEV available |
| Crystal | 11 | Stone: 1, Magic: 1, Crystal: 2 | Element; faceted crystal shape, DEV available, Stone + Magic recipe |
| Civilization | 12 | Human: 1, Civilization: 2 (capped by Human), Null: 1 | Element; sandstone #E8B772, Technology category, politosphere voice, DEV available, no recipe yet |
| Magic | 13 | Magic: 3, Void: 1 | Element; turquoise #35CBB6, Beyond category, arcanosphere voice, DEV available, Human + Void or Forest + Spirit recipes |
| Technology | 14 | Civilization: 1, Technology: 2, Null: 1 | Element; electric blue #0D98ED, Technology category, technosphere voice, DEV available, no recipe yet |
| Spirit | 15 | Magic: 1, Spirit: 2, Void: 1 | Element; mint #A8E6C2, Beyond category, ectosphere voice, DEV available, no recipe; occurs naturally (at most one natural sphere per system) |
| Forest | 16 | Forest: 2, Plant: 1, Water: 1 | Element; forest green #1F6B2A, Life category, florasphere voice, DEV available, Plant + Plant recipe |
| Fish | 17 | Life: 2 (capped by Water), Water: 1, Fish: 1 | Element; orange #F76B0D, Life category, biosphere voice, Water's surface with the v2 fish icon, DEV available, Life + Water recipe |
| Sand | 18 | Sunlight: 1, Sand: 2, Null: 1 | Element; pale yellow #FADE9B, Basic category, lithosphere voice, erodes on a miss (sandfall), DEV available, Wind + Stone recipe |
| Bird | 19 | Bird: 2, Life: 1 (capped by Air), Air: 1 | Element; sky blue #78BEF5, Life category, biosphere voice, feather surface with the v2 bird icon, DEV available, Life + Air or Life + Wind recipes |

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

### Limited contributions

A contribution line can be limited by another element. In the Inspector, tick `limited` on the line, choose `limitedBy` and set `limitRatio`; the tooling constructor is `new ElementContribution(ElementId.Life, 2, ElementId.Water)`. The rule is: the total of that element this sphere type has added may not exceed the planet's current `limitedBy` total × `limitRatio`. For example, a Fish whose Life is limited by Water can never have added more Life than the planet has Water.

- **Per sphere type.** Only that sphere type's own limited yield counts against its cap. Life from Plant or Life spheres never uses up Fish's budget.
- **Excess is lost.** Yield above the cap is discarded. It is not converted or stored for later.
- **Score is unaffected.** The cap only clips element yield; match and breakaway points are unchanged, as with unallocated units.
- **Caps rise first.** One shot (its match plus its breakaway) or one laser firing resolves as a batch. Unlimited yield lands first and limited lines are checked when the batch ends, so Water gained anywhere in the same shot raises the cap before Fish's Life is checked, regardless of removal order. Totals only increase during a level, so a cap never takes back yield.
- **Notice.** When a line is clipped, `Not enough <element>` appears in that limiting element's visualizer color (`SphereCatalog.ElementColor`), just above the visualizer. It spans the visualizer's full width (auto-sized up to a large font), rises and fades. A heavy dark outline and a soft drop shadow (TextMeshPro underlay) keep it readable over any background, and its own sorting layer keeps it above the HUD text there. A repeat restarts the notice; different limiting elements at once stack upward. Text, font size, line height, timing, gap, outline and shadow live in `VisualizerStyle`, and `VisualizerStyle.ApplyShortageLook` is the one place that turns them into material properties. `ContributionLimitTools.Configure` bakes them into the saved `Fonts/Shortage notice.mat` (HUD font atlas, referenced as `shortageMaterial`) so builds include the outline + underlay shader variant; rerun it after changing those settings. Each level draws a practically invisible notice for three frames at start, so the first real notice never shows the editor's cyan placeholder while that shader variant compiles.

`ElementLedger` (`Scripts/ElementLedger.cs`) is the only gameplay path from earned points to element totals; `RemoveCells` adds through it, and losing calls `ElementLedger.Clear`, which also resets limited yields. `SphereDefinition.ContributionYield` is the single per-line formula. `AddContributions` stays the raw, uncapped yield used for previews and expectations. Validation rejects a line limited by its own element, an undefined element, or a non-positive ratio. `ContributionLimitTools.Smoke` with `-contributionLimitSmoke` temporarily gives Fire a Fish-style line and checks the rules through a real mouse shot. It confirms that a Water breakaway raises the cap for the same shot's Fire match, and checks clipping and exact loss, unchanged score and Fire yield, the per-type budget, validation, reset, and the notice's text, color and placement. It renders `Logs/ContributionLimit-notice.png`. No current sphere uses a limit yet.

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

## Civilization sphere

Civilization (ID 12) represents population sprawl. It adds the appended `ElementId.Civilization`, whose planet profile drives the `Civilization` stat. That stat sets how far city lights spread; it does not brighten them. Brightness comes from `CityLightIntensity`: Technology, City and Energy. A pure Civilization world shows dim, sprawling lights, and Technology on the same sprawl roughly doubles their brightness (see Planet-stats.md, City lights). The art is `Art/Spheres/Civilization/civilization-{surface,icon}-v1.png` (256×128 surface, 256 sprite icon), and the voice is `Audio/Voice/politosphere.mp3`. The recipe is intentionally unset.

`SphereIntroTools.Configure` imports the art for its listed spheres (Civilization, Magic, Technology, Spirit, Forest) through the shared `SphereCatalogTools.ImportSphereArt` (Air's tool uses it too). It then seeds the spheres, adds their planet profiles through `CityLightTools.Configure`, and validates. `SphereIntroSmoke` (`-sphereIntroSmoke <sphere id>`, DEV mode) checks any sphere entirely from its definition: DEV and normal availability, setup selection, board shape, breakaway yield through `RemoveCells`, one bar per contributed element in the shared colors, streak color, voice and a preview render. Run it with `SphereIntroTools.Smoke` and `-sphereIntroSmoke <id>` (12 Civilization, 13 Magic, 14 Technology, 15 Spirit, 16 Forest). New spheres can reuse it instead of adding a sphere-specific smoke.

## Magic and Technology spheres

**Magic** (ID 13) is turquoise and contributes 3 Magic and 1 Void. Human + Void unlocks it, and so does Plant + Spirit; the recipe book allows several recipes for one sphere. The second recipe stands in for Forest + Spirit: recipes combine elements, and the planned Forest sphere (Plant + Plant) contributes only Plant, so any strong Plant source qualifies. Revisit it once the Forest and Spirit spheres exist. On the planet, Magic tints vegetation and city lights violet through the shared color traits and leaves water and land alone. Its earlier stand-in purple visualizer color was removed: Configure drops any stand-in `elementColors` entry whose element has gained a sphere.

**Technology** (ID 14) is electric blue and contributes 1 Civilization, 2 Technology and 1 Null. The Technology element brightens city lights (`CityLightIntensity` 1), adds some coverage (Civilization 0.7) and shifts the lights toward cool white. Its recipe is unset.

## Spirit and Forest spheres

**Spirit** (ID 15) is mint and contributes 1 Magic, 2 Spirit and 1 Void. Its Air + Life recipe was retired (Air + Life is reserved for Bird). Spirit instead occurs naturally (see Natural occurrence below). It now supplies the Spirit element's color, so the old gray-blue stand-in was dropped automatically.

**Forest** (ID 16) is forest green and contributes 2 Forest, 1 Plant and 1 Water. Plant + Plant (a self-recipe) unlocks it, and Forest + Spirit is Magic's second recipe (the earlier Plant + Spirit stand-in was retired with `RetireRecipe`). Forest represents the appended `ElementId.Forest`. Its planet profile (`PlanetStatCatalog.SeedForest`) shapes the planet more than Plant does:

- vegetation coverage 1 (Plant has 0.8), so it builds coverage faster;
- a deep green vegetation tint (strength 0.9);
- some soil, moisture and cloud cover;
- a stronger blue-green atmosphere.

At a majority share both saturate vegetation coverage, so the visible difference is the darker, lusher green. At the same small share Forest measured about 4% more coverage and 14% darker foliage than Plant. `SphereIntroTools.Configure` migrated the existing Forest asset with `SphereCatalogTools.ReseedSphere`. The no-namesake option (`SphereDefinition.NoNamesake`) remains for future spheres that should not represent an element.

## Spirit element

`ElementId.Spirit` (appended) exists ahead of its sphere, which will come from Air + Life. Until then its visualizer color is a gray-blue stand-in (#7A8CAA) in `SphereCatalog.elementColors`; `SeedElementColor` adds a stand-in only for elements without a sphere, and Configure drops stand-ins once a namesake sphere exists. Its planet profile is empty (no globe effect yet). `PlanetStatCatalog.EnsureProfiles` gives every appended element an empty profile, so new elements never break the one-profile-per-element rule; `SphereIntroTools.Configure` calls it and runs `PlanetStatChecks`.

## Crystal sphere and shapes

Crystal (sphere ID 11, element ID 26) uses the faceted violet surface and crystal-cluster icon in Art/Spheres/Crystal, magenta pink #E36CF2 (the pinkest streaks in its texture), and the latisphere voice, which plays under the same shared large-match/breakaway rules as the other sphere voices. Its four units are 1 Stone / 1 Magic / 2 Crystal: a 12-point sphere yields 3 Stone, 3 Magic and 6 Crystal. Stone + Magic unlocks it through the shared recipe rules; it is DEV available and locked normally.

`SphereDefinition.shape` (Sphere or Crystal) is visual only: collision, matching, scoring and the hex footprint always use the round sphere. `SphereShapes` is the single builder every surface goes through (board, ammo, chambers, setup/results previews, discovery reveal, match flash). Crystal bodies are a code-generated pointy-top hexagonal gem (flat front table, triangular crown facets, thin girdle, pointed back) whose corners touch the sphere footprint and whose flat sides face the six board neighbors. They keep facing the camera with a gentle tilt instead of the round spheres' Y spin. Mesh proportions, texture jitter, smoothness and wobble live in `Resources/SphereShapeStyle`.

### Shot deflection

`SphereDefinition.deflectedHits` makes any element sphere a deflector (Crystal: 1). A shot of a different sphere type that touches it bounces off like a boundary, reflecting about the contact normal. That bounce counts as a ricochet everywhere a wall bounce does (the per-shot ricochet bonus on a match and the impressive callout) and plays the definition's `deflectSound` (crystal-hit) instead of the wall ricochet sound. The hit after `deflectedHits` breaks it: the sphere is simply lost (no score, no element contributions, no streak), `breakSound` (crystal-breaking) plays, and the shot takes its cell and resolves matches and breakaways from there. Shots of the same type (Crystal into Crystal) attach normally. `ShotDeflection.Resolve` is the single rule (attach, deflect or break); hit counts live on the board sphere in `SphereDamage`. Crystal goes solid, then cracked, then shatters: a struck deflector shows its definition's `damagedOverlay` (Crystal: `crystal-cracks-v1.png`) over the whole sphere until it breaks. The Crystal smoke checks the overlay and saves a close-up, `Logs/burst-cracked-crystal.png`. The aim guide still stops at the first sphere it touches; it does not predict deflections.

Magic has no sphere yet. Elements without a namesake sphere take their visualizer/reveal color from `SphereCatalog.elementColors` (Magic: purple #7A3CE0), falling back to the sphere that contributes most of them. `SphereCatalog.ElementColor` is the one lookup for element colors.

## Match clear visuals

Ordinary shot matches pass their attachment cell to MatchWave, which walks matched hex neighbors in breadth-first layers. Equal-distance branches flash/collapse together. Each layer starts up to 0.035 seconds after the preceding one; long chains compress the interval so the last layer starts within 0.22 seconds. Each sphere flashes white briefly and collapses over 0.20 seconds, keeping the scheduled visual clear within 0.42 seconds plus frame presentation. The shared additive MatchFlare material works on lit and unlit surfaces, including Void, without changing source materials or brightening its persistent halo. No new sprite assets are needed. Scoring, connectivity, release detection, announcements and the existing turn-resolution wait remain immediate/unchanged; the wave is visual only and uses the same rules in every level. Run MatchWaveTools.Configure for shader/graph/timing checks and MatchWaveTools.Smoke with -matchWaveSmoke for the branched visual test.

## Gravity-captured planet streaks

Every breakaway uses a cubic path with randomized downward/lateral controls, then accelerates into a sampled point on the visible bottom hemisphere of the actual receiving sphere. Visual-only System.Random is independent of board/ammo random sequences. Flights take 0.68-0.80 seconds, preserve the source sphere color and shrink its visible head. Trails stop at the surface and fade for 0.23 seconds instead of being cut off immediately. Each arrival creates a bright additive white-core flash lasting 0.24 seconds, attached to the planet. Impacts start at a visible size, hold peak brightness for 0.045 seconds, then expand and fade. Their dedicated shared material draws over the surface so the planet cannot clip the glow; travelling energy retains normal depth testing. The procedural EnergyFlash shader needs no sprite texture.

A shot match still clears within the MatchWave time budget. Exactly one sphere from the latest wave layer (stable coordinate tie-break) emits a smaller single energy streak when it finishes dissolving; other matched spheres emit none. This representative is a visual cue, with no change to awarded points or elemental totals. Completed-world transitions allow active flights/tails to finish before changing scenes. Per-sphere identities, colors, contribution weights and audio remain catalog-driven.

PlanetStreakTools.Configure checks 500 randomized paths for initial downward motion, accelerating approach, endpoint surface geometry and impact variation. PlanetStreakTools.Smoke with -planetStreakSmoke checks breakaway counts, initial falls, impact flashes, representative selection on tied branches, scores and effect cleanup.

Stone now uses stone-surface-v2.png and stone-icon-v3.png (icon updated from v2), a pale-gray rock/mountain motif, through its shared sphere definition. Its #D0D3D8 color drives both streaks and visualizer bars. The original brown v1 artwork remains archived; the lithosphere voice and 3 Stone / 1 Null contributions are unchanged.

## Setup categories

SphereDefinition.category is an editable SphereCategory enum: Basic, Forces, Weather, Life, Technology, Beyond. All is a setup filter, not an assignable category. Existing elemental spheres, including Air, are Basic except Void (Forces) and Plant Life/Life (Life). Core is Forces and Laser is Technology for data completeness, but these nonselectable roles never create empty collection tabs.

Each setup opens on All. The tab row contains All followed by categories represented by owned, selectable spheres, in the enum order. DEV availability uses the same collection rules and therefore shows all current elemental spheres/categories. New unlocks appear on reopening setup. Filtering repacks visible tiles, starts the collection at the top, and preserves the chosen loadout and Begin gating. Hidden collection previews disable their separate render stages/cameras. When all categories do not fit, the row supports horizontal scrolling and arrow navigation. No per-world category lists or new art assets are needed.

Category tabs use dim, nearly neutral gray for inactive labels/borders. The active tab keeps its warm highlight and shows a small white-gold light immediately before its label. TabSelectionLight renders the soft glow directly as UI geometry, with no sprite or texture asset. Space is reserved in every tab so changing the active category does not shift the labels.

## Descriptions

Each definition has a short player-facing `description` (required, at most `SphereDefinition.MaximumDescriptionLength` = 160 characters). It says, in plain words, what the sphere brings to a planet (e.g. Water: "A flowing element that fills oceans and aids in humidity."); clearing spheres is a given and isn't repeated. For now it appears only on the results screen when a sphere is discovered: beneath the name in the recipe reveal, and beside the sphere under its name in the natural-discovery modal. A dedicated description screen and setup viewing are planned later. Descriptions will need revisiting when broad planet stats (such as Humidity) arrive.

`ContributionSummary()` also builds an exact yield line from `contributions` (largest share first, unallocated units omitted, Core empty), e.g. Ice → "Ice 50% · Water 25%", for later detail screens. **Validate Sphere Catalog** checks descriptions and summaries; results and recipe-reveal smoke checks confirm the description is shown for each discovery.

## Retired elements

Element IDs are saved as numbers, so an unused element is retired rather than deleted: it keeps its slot with `[Obsolete]`, which also hides it from Unity dropdowns. `ElementIds.IsRetired` and `ElementIds.IsActive` read that attribute. Retired elements get no planet profile (`EnsureProfiles` removes leftovers), and sphere contributions, namesakes, contribution limits and recipes reject them. Tree (ID 37) is retired as `RetiredTree`; Forest covers it.

## Fish sphere

Fish (ID 17) is orange (#F76B0D, from the icon; close to Fire's orange-red) and uses the biosphere voice. It reuses Water's surface texture with its own `fish-icon-v2.png`. It contributes 2 Life, 1 Water and 1 Fish, in the Life category. Life + Water unlocks it.

Its Life line is a **limited contribution** (`new ElementContribution(ElementId.Life, 2, ElementId.Water)`, ratio 1): the Life that Fish spheres have added can never exceed the planet's Water. The excess is lost, score is unchanged, and the visualizer shows "Not enough Water" (see Limited contributions). Fish's own Water counts toward its cap because unlimited yield lands first.

Spheres whose art doesn't follow the `<label>-{surface,icon}-v1.png` pattern are listed in `SphereCatalogTools.ArtOverrides` (Stone's v2 art, Fish borrowing Water's surface). `ImportSphereArt` imports whichever files a folder has. `SphereIntroSmoke` now computes the expected yield with the same cap rule as the ledger and checks that a shortage is reported exactly when a line is clipped.

## Sand sphere and erosion

Sand (ID 18) is pale yellow (#FADE9B, between the icon's highlights and mid-tones), uses the lithosphere voice, and contributes 1 Sunlight, 2 Sand and 1 Null. Wind + Stone unlocks it (the earlier Stone + Sunlight recipe was retired). Wind has no sphere yet, so this recipe becomes reachable once one supplies Wind.

Sand **erodes**: `SphereDefinition.erodesOnMiss` with `erodeSound` (`Audio/sandfall.mp3`). This is sphere data, not a Sand-only branch; validation requires an erode sound on a matchable sphere. After any shot of a matchable sphere that makes no match (it attaches without matching, breaks a Crystal without matching, or is lost out the bottom):

1. The eroding sphere farthest from the core disappears (`ErosionTarget`; ties go to the lower, then leftmost cell). It gives no points or elements, plays its sound, dissolves and bursts into particles (see Lost-sphere bursts).
2. Anything it was holding then falls away as a normal breakaway, with scoring, elements and audio. On an attached shot this is the same breakaway as the shot's own.
3. The ammo check (`TryLoadFromRing`, at the next load) runs after all of this, so eroding the last sand replaces Sand in the cannon and chambers.

A hazarded cell (`IsHazarded`, currently: infested by an alien) doesn't count as its sphere type, so infested sand never erodes; future hazards join that one check. Laser shots aren't matchable, so they never trigger erosion. Crystal breaks and erosion share `LoseSphere` (remove without points, dissolve, play the sphere's sound).

`ErosionSmoke` (`SphereIntroTools.Smoke` with `-erosionSmoke`, DEV mode) uses real pointer shots on test boards with rotation held at 0. It checks:
- a miss erodes the farthest non-infested sand with sandfall and grains;
- a farther infested sand is skipped;
- what the sand held falls away and scores, with no Sand from the eroded sphere;
- after the last sand erodes, the cannon and chambers drop Sand;
- a matching shot erodes nothing;
- infested sand alone erodes nothing;
- a lost shot erodes and drops what the sand held (called directly, since the lost-shot path calls the same method).

## Lost-sphere bursts

A sphere lost without points (eroded, or broken by a shot) bursts into particles where it was. This happens in the shared `LoseSphere` step, using the definition's `lostBurst` (`None`, `Crumble`, `Shatter`) and `lostBurstColor` (fully transparent means the sphere's own color). `SphereBursts.Spawn` builds the burst from the matching profile in `SphereShapeStyle`:
- **Crumble:** grains that sag and fall.
- **Shatter:** fewer, slightly larger shards that fly outward fast, with almost no gravity and about half the lifetime, so it reads as breaking glass.

Both share the saved particle material `Art/Effects/sphere-crumble.mat` (`burstMaterial`), so builds include its shader. Sand crumbles in its own color, and Crystal shatters in purple (#9B5AEB). `SeedLostBurst` only sets a burst that is still unset, so later Inspector edits stay. The erosion smoke checks the crumble and the Crystal smoke checks the shatter. Close-ups: `Logs/burst-crumble.png` and `burst-shatter.png`.

Civilization uses its house icon (`civilization-icon-v2.png`) so it no longer resembles Sand. The art table (`ArtOverrides`) is the single source for spheres whose art doesn't follow the default file names, and `ReapplyArt` updates an existing sphere asset from it.

## Civilization cap and xenosphere

Civilization's 2 Civilization units are capped by Human (an `ElementContribution` limited by Human, ratio 1), the same limited-contribution rule Fish uses: without Human in the world it adds no Civilization, and the shortage notice reports the clipped line. `SphereIntroSmoke 12` checks the capped yield.

When one breakaway frees at least `InfestationModifierDefinition.breakawayVoiceMinimum` (default 2) infested spheres, the infestation's `breakawayVoice` (`Audio/Voice/xenosphere.mp3`) plays through `LevelAudio.Announce`. Both are modifier data seeded by `InfestationTools.Configure`. The erosion smoke drops two, then one, infested sphere with real shots and expects xenosphere only for two.

## Sphere seed table

`Editor/SphereSeeds.cs` holds each sphere's authored starting data in one table: label, display name, namesake element, color, role, starter flag, category, voice, description, contributions (with any caps), Null units, natural weight, and an optional `extra` step for traits beyond the common fields (Crystal's shape and deflection, Sand's erosion, Void's glow). `SphereCatalogTools.Configure` seeds a new asset from its entry, and `ReseedSphere` and `SeedNaturalWeight` re-apply parts of an entry to existing assets. Existing assets otherwise keep their Inspector edits. This replaced the per-ID branches for categories, descriptions, voices and contributions; a new sphere is now one entry here, plus its ID, its art (default file names or an `ArtOverrides` entry) and its recipes.

## Bird sphere

Bird (ID 19, the existing `ElementId.Bird`) is sky blue #78BEF5, sampled from its feather surface. It uses `bird-surface-v1.png` and the red-bird `bird-icon-v2.png` through `ArtOverrides`, plus the biosphere voice and the Life category. It contributes 2 Bird, 1 Life and 1 Air, with no Null. Its Life is capped by the planet's Air, the same limited-contribution rule as Fish (Life by Water) and Civilization (Civilization by Human). Life + Air and Life + Wind both unlock it. Wind has no sphere yet, so the second recipe becomes reachable once one supplies Wind. Bird's planet profile is empty for now (`EnsureProfiles`), so it doesn't change the globe yet. `SphereIntroSmoke 19` covers it.

## Natural occurrence

A system holds at most one naturally occurring sphere. When a system is created, `NaturalOccurrence.Roll` goes through its procedural worlds in slot order and gives each one `WorldCatalog.naturalOccurrenceChance` (10%) to hold one. The first world that rolls it gets one, and the rest of that system is not checked. That world then gets one sphere the player doesn't own yet, picked from the spheres with a `SphereDefinition.naturalWeight` above 0. The weight is relative, so a sphere with weight 2 is picked twice as often as one with weight 1. Spirit has weight 1, and Metal will use the same field. If the player owns every candidate, nothing is placed. Predefined worlds keep only their authored placements.

The roll uses its own random stream from the system seed, so tuning the chance or the weights never changes world names, rules or boards.

A natural sphere never touches the core. `NaturalOccurrence.IsAllowedCell` allows ring 2 out to the cluster edge, because the player never has that sphere as ammo, can't match it, and has to be able to break it away. Any other cell, the outer ring included, is allowed: the sphere can never be matched, so it always has to be broken away. The same rule applies to authored placements: gameplay refuses a placement that breaks it, and `GameplayGeometryChecks` validates the authored worlds against it.

A rolled sphere becomes an ordinary `NaturalSpherePlacement` on the world record. From there it takes the same path as Genesis Beta's authored Stone: the board places it, `IsNaturalPlacement` marks it, releasing it in a breakaway counts it as released, and completion records a Natural discovery that Results shows.

- **Ammo:** naturally placed spheres are never offered as ammo, so the only way to get one is to release it. Ordinary spheres of the same type still count.
- **Ownership:** generation uses the shared `ProfileService.Owns(profile, id)`, the same rule as collection, selection and rewards. In DEV mode every collectable sphere is owned, so nothing occurs naturally there. `WorldGeneration.Eligible` (predefined-world sphere requirements) uses the same rule.
- **Existing saves:** only systems generated after this change roll. Systems already in a save don't gain natural spheres.

The natural-occurrence smoke (`SphereIntroTools.Smoke -naturalSmoke`) checks these cases on throwaway profiles:
- at chance 1, only the first procedural world gets one;
- over 200 systems, never more than one per system, never touching the core, some on the outer ring, and roughly the expected number of systems with one;
- the pick draws from every unowned candidate and skips owned ones;
- nothing is placed when every candidate is owned, at chance 0, or in DEV;
- rolls are deterministic and don't reshuffle the rest of generation.

It then plays a real world record: the rolled Spirit stays out of ammo through setup and a real shot, then releases and is discovered as Natural on Results.
