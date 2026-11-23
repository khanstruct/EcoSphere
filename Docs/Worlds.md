# Worlds and system generation

World identity and placement now come from `Assets/EcoSphere/Resources/WorldCatalog.asset`. Existing tutorial layouts and guided ammo remain specialized gameplay code, referenced by the predefined assets:

| Asset | World name | Guaranteed location | Required spheres | Gameplay scene |
| --- | --- | --- | --- | --- |
| `Assets/EcoSphere/Data/Worlds/genesis-alpha.asset` | Genesis Alpha | System 1, slot 1 | 2 | Tutorial1 |
| `Assets/EcoSphere/Data/Worlds/genesis-beta.asset` | Genesis Beta | System 1, slot 2 | 3 | FirstLight |

These are guaranteed reservations, not probability rolls. Reserved slots are filled before conditional worlds. Both IDs are claimed when System 1 is generated, regardless of completion, and can never be placed again in the same save. Opening later systems before System 1 cannot consume either tutorial. Public slot numbers above are one-based; asset `guaranteedSlot` values are zero-based.

## Authored worlds

Create an **EcoSphere / Predefined World** asset, give it a permanent unique ID and add it to the world catalog. Definitions contain a display name, gameplay scene, required sphere count, inclusion/excess goals, turn rotation settings and a variable modifier list.

Set a guaranteed system/slot for mandatory placement. A `guaranteedSystem` of zero instead enables conditional appearance: minimum/maximum system number, appearance chance, required unlocked sphere IDs and required completed predefined-world IDs. Maximum system zero means no upper limit. Conditional worlds choose a random free slot after passing eligibility and their appearance roll. They are claimed on placement, not completion. Existing generated systems are never retrofitted when eligibility later changes.

World rules are copied into the placed record and applied when setup launches gameplay. The tutorial scenes keep their existing layout and message implementations. Their HUD, System selection, setup title and results all use the world record's name. Direct editor launches of the tutorial scenes also resolve their guaranteed identity and rules from the catalog.

## Procedural world records

Each newly requested system receives a stable seed derived from the save's universe seed and system number. Remaining slots receive generated names, per-world seeds, independent rule snapshots and modifier-ID lists. System creation has no configured final system limit. The current map supports two to five planet slots per system, defaulting to five. All procedural slots now route to the shared Procedural gameplay scene.

The catalog exposes minimum/maximum procedural sphere requirements, both defaulting to **4**. Validation rejects a minimum below four; the generator enforces the minimum, and `WorldRecord.RequiredSpheres` also clamps loaded procedural data to at least four. Authored tutorial exceptions remain two and three. Preparation no longer imposes a four-sphere upper cap; both its slot layout and the System requirement badges read the world record.

`proceduralRules` holds the shared goals and rotation defaults. Its sphere count is replaced by the generated value from the minimum/maximum range. Procedural modifiers are an editable asset pool; each modifier has its own selection probability. A generated world can have zero, one or several modifiers, and its setup screen displays only its saved list in a scrolling panel. Authored worlds use their explicit list rather than this probability pool. The default tutorials have no modifiers; the old unconditional modifier placeholders have been removed.

The name generator combines curated openings/endings and occasional middle fragments, rejects excessive length and triple-letter runs, and avoids duplicate names within a system. Its fragments are editable on the catalog. Names may repeat in distant systems; stable world IDs distinguish them. A bounded fallback handles very small custom fragment pools without hanging. Names and seeds are saved on creation and remain stable when generator settings change.

Procedural world records now have playable seeded board layouts. proceduralGameplayScene is Procedural, and Begin enables once the four required spheres are selected. Older procedural records with an empty scene string resolve through the current shared-scene setting. Modifier records/UI are configurable now; their individual gameplay effects remain separate implementations. There is no silent fallback to a tutorial scene or tutorial layout.

## Save behavior

During development, progress is **session-only**. `WorldCatalog.persistProgressBetweenSessions` is disabled in the world catalog asset, so normal gameplay neither loads nor writes disk saves. Completed worlds, discoveries, generated systems, placement claims and the current system survive scene changes within a run, then clear when the game exits or a new Play session starts. Existing disk saves are left untouched and ignored. Fresh normal sessions receive a new universe seed; smoke runs retain their deterministic test seed.

The future persistence implementation remains available behind that catalog setting. When enabled, `worlds-v1.json` under Unity's `Application.persistentDataPath` (exposed by `GameServices.Progress.SavePath`) stores the universe seed, generated systems/records, claimed predefined IDs, completed worlds, unlocked spheres and current system. Temporary-file replacement commits a system and its placement claims together. Command-line smoke runs and editor-only authoring remain isolated in-memory sessions. Persistence is tested separately using a file under the validation project's Logs directory.

## Completed-world navigation

System is the startup scene and has a two-step, one-time introduction for the first two-sphere selection. Unfinished worlds now require enough unlocked/selectable sphere types before preparation opens; otherwise System displays a paused warning popup. See [Tutorial.md](Tutorial.md) for the exact text and modal behavior.

Clicking a completed world on System opens its existing Results directly. It bypasses preparation/gameplay, retains the original sea level and rotation, and does not replay discovery overlays, voices or rewards. Continue returns to that world's system. Uncompleted worlds retain the preparation route. Preparation also checks for completed worlds, and `GameServices.Progress.Complete` is idempotent so a stale or duplicate completion cannot replace an outcome or grant additional rewards.

## Validation

**EcoSphere / Configure Worlds** creates missing built-in assets without replacing existing edits. **EcoSphere / Validate World Generation** checks the catalog and exercises 250 generated systems, deterministic names, guaranteed and conditional placement, uniqueness before completion, variable requirements/modifier pools, and save/load including atomic replacement. The tests report `ECOSPHERE_WORLD_GENERATION_CHECKS_PASSED`.

`WorldCatalogTools.PreparationSmoke -worldPrepSmoke` checks Genesis names, dynamic System badges, a six-sphere preparation screen and per-world modifiers, and renders portrait/wide previews. Genesis Beta's navigation smoke covers its three-sphere setup and guided shots. Existing gameplay, results and preparation checks cover the integration with the shared world records.

Validated in Unity 6000.6.7f1 in the isolated validation project: compilation, 2,518 generation assertions over 250 systems, atomic save/load, both Genesis flows, the six-slot/modifier UI, and all seven existing sphere/gameplay regressions passed. Portrait and wide preparation renders were inspected. All 47 serialized references across the 14 game-data assets resolve in the main workspace. Logs are under `Logs/WorldGeneration-*.log`, `Logs/GenesisBeta-final-navigation.log` and `Logs/SphereCatalog-*-smoke.log`.

`WorldCatalogTools.DevelopmentProgressCheck -developmentProgressCheck` exercises the normal runtime persistence policy without a smoke-mode bypass. It verifies session unlocks, completed-world results/no replay, duplicate-completion protection, unfinished-world setup, session reset and an unchanged disk save. Its log is `Logs/Development-progress-check.log`.

## Procedural cluster grouping

WorldRules.clustering is a saved 0–1 grouping value, randomized per procedural world from WorldCatalog.minimumClustering/maximumClustering (currently 0.1–0.9). Higher values favor same-type neighbors and easier large matches; lower values produce a more scattered mix. The value is independent of unlock thresholds, point contributions and sphere count. Rule copies and saved systems retain it.

ProceduralCluster fills the same core-connected four-ring, 60-cell hexagon using the selected loadout. It gives each chosen type an equal quota (15 each for four spheres), then grows a seeded frontier and weights assignments toward already placed matching neighbors according to clustering. Startup groups do not auto-clear: ordinary shot-seeded matching applies. The same world seed and selection order reproduce the board without consuming Unity’s global random state.

All generated worlds currently require exactly four spheres. The common gameplay scene retains normal cannon/ammo selection, match and breakaway scoring, misses, rotation, recipes, visualizer, completion and saved results. Selected Stone is an ordinary sphere here, never a scripted natural unlock; naturally occurring Stone remains confined to its authored tutorial topology. Modifier lists/UI remain saved as before; their separate special effects are not introduced by the clustering generator.

ProceduralLevelTools.Configure registers the shared scene in build settings, seeds Ice + Fire → Water, and validates 192 deterministic connected balanced layouts. Same-type adjacency averaged 23% at clustering 0 and 53% at clustering 1. ProceduralLevelTools.Smoke with -proceduralSmoke verifies System → four-slot setup → generated gameplay, a real shot, completion/results, preserved world settings and an actual Water recipe award. Portrait/wide previews are under the validation project’s Logs/Procedural-*.png.

System world badges use the gold outline circle sprite sphere-requirement-v2.png (256x256). The existing Sphere Count text is centered inside it, colored gold #FFD361, and continues to read each WorldRecord.RequiredSpheres value via SystemView.RefreshSystemHeading. Number auto-sizing supports multi-digit requirements. SystemSceneTools.ConfigureRequirements updates existing scene badges, while the scene builder uses the same styling helper.

## Alien infestation

Alien Infestation is a typed InfestationModifierDefinition registered in WorldCatalog, using the standalone gold spider modifier icon. Its asset currently guarantees System 1, zero-based slot 2 (world 3), with ordinary procedural chance 0. Guaranteed modifier placement is generic world-generation data and applies to any modifier with a configured system/slot; the world remains procedural and retains four required spheres.

The asset sets 8-12 randomly selected non-core hosts, the alien sprite, front-overlay material and alien-hit clip. Host selection uses a separate seeded RNG and never changes sphere types or layout. A different level can opt in by adding the same modifier ID. Infested is independent board-cell state, not a sphere type or contributed element.

BoardRules excludes infested cells from match seeding and matching links, while physical connectivity still traverses them. When a fired shot resolves, every infested host adjacent to the shot's final attached hex cell loses its alien, regardless of which sphere caused the physical collision. Normal matching still resolves from the fired sphere in that same shot. Infested hosts behave as an unmatchable type; hosts cleansed by this attachment are excluded from this shot's match check, then use their normal type for subsequent shots. Cleansing an alien never cancels a valid chain through other normal spheres. Nonadjacent aliens remain protected. Indirect matches cannot remove the alien or host while structurally supported. Detached hosts are collected and scored normally, with their aliens removed separately. Alien removal itself yields no score, elemental contributions or sphere voice qualification.

The camera-facing overlay uses an always-in-front sprite shader and high sorting order; it adds no collider. The pale ivory/lavender v2 creature sprite is displayed at alienScale 1.25 using its natural colors: brightness 1, outline width 0. The optional showHostMarker is disabled, removing the previous mint rim. Visibility comes from the artwork; the center stays open for the sphere icon. An attachment that removes one or more aliens plays alien-hit once. Both direct hits and detached hosts send the alien into a red streak toward the nearer left/right play-area boundary, then clean up beyond that edge. These alien effects do not target the planet or create planet-impact flashes. Completed-level transitions also wait for active alien streaks to finish.

InfestationTools.Configure installs/validates assets and registration. Its data checks cover 100 seeded layouts, all counts 8-12, core exclusion, blocked matching, preserved support and cleansing. InfestationTools.Smoke with -infestationSmoke tests setup display, actual attaching hits, audio, left/right escapes, protected chains, normal breakaway scoring and rendered layouts.

Alien animation alternates the wider v2 claws and inward v3 claws. Both 256px sprites share canvas, custom pivot and pixels-per-unit. The modifier sets animationFramesPerSecond (2) and animationSpeedVariation (0.15), yielding about 1.7-2.3 frame changes per second per alien. Starting frame and fractional frame progress are randomized independently using a separate visual RNG, without affecting board layout or infestation placement. Animation follows gameplay time and freezes its current pose when knocked off; facing/escape effects continue normally.

The alien animation explicitly binds each frame's texture through a renderer property block for the custom URP material. Each host has a unique stable sorting priority, so changing sprite bounds cannot flip neighboring aliens' overlap order. Knocked-off aliens use a separate foreground priority. The animation smoke check compares two rendered 512px close-ups as well as checking frame counters and texture bindings.

System world badges also display each WorldRecord modifier's registered icon beside its sphere requirement, using the same definitions as setup. The complete row stays centered beneath the world; counts remain centered in their own circle. Lists longer than three modifiers wrap to further rows at the same icon size. Badge image objects are reused when systems change, stale images are hidden, duplicate modifier IDs display once, and images do not intercept planet clicks.

WorldRecord is the authoritative per-world source for its name, sphere requirement, rules, seed and modifier IDs. PredefinedWorld and WorldCatalog are generation templates; scenes do not maintain separate copies of world traits. System and Setup resolve modifier presentation through WorldRecord.ModifierDefinitions, and infestation gameplay resolves its behavior from those same definitions. The scene handoff carries system/world identity and the selected loadout; RequiredSpheres is a computed lookup, never a separately assigned count. Tutorial layouts remain deliberately authored, but their requirements still come from the world record.

WorldRules owns oneStarScore/twoStarScore/threeStarScore (temporary defaults 500/1000/2500), copied into generated worlds alongside their other rules. WorldRecord.highestScore stores the full-precision combined elemental points awarded on completion; Null contributes no points. EarnedStars is computed from that score and the world's thresholds. Viewing completed results does not change scores or grant rewards. System replaces completed-world requirements/modifiers with only the earned gold stars (zero below the first threshold). No discovery bonuses are awarded yet; persistence continues to follow the existing session/development setting.

Winning gameplay saves the completed world's score, then shows the shared StarRatingPresentation after the world-complete announcement and active collection streaks finish. Earned stars pop in at half-second intervals; each uses the shared starScore sound via PlayOneShot, allowing overlap. The horizontal row is centered just above screen center and scales with viewport width, with a height limit for wide displays. Continue appears at bottom center after the reveal and is required to open Results. A zero-star completion still offers Continue. This applies to all worlds through the common CompleteWorld flow.

The authoritative ownership hierarchy is now PlayerProfile > SystemRecord > WorldRecord > optional CompletedWorld snapshot. Shared services replace the former WorldProgress static data store. Scene context is in GameSession; completion checks are read-only and do not generate systems. See Architecture.md for service boundaries, natural discovery data, migration, and retained development behavior.
