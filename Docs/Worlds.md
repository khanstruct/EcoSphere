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

The future persistence implementation remains available behind that catalog setting. When enabled, `worlds-v1.json` under Unity's `Application.persistentDataPath` (exposed by `WorldProgress.SavePath`) stores the universe seed, generated systems/records, claimed predefined IDs, completed worlds, unlocked spheres and current system. Temporary-file replacement commits a system and its placement claims together. Command-line smoke runs and editor-only authoring remain isolated in-memory sessions. Persistence is tested separately using a file under the validation project's Logs directory.

## Completed-world navigation

System is the startup scene and has a two-step, one-time introduction for the first two-sphere selection. Unfinished worlds now require enough unlocked/selectable sphere types before preparation opens; otherwise System displays a paused warning popup. See [Tutorial.md](Tutorial.md) for the exact text and modal behavior.

Clicking a completed world on System opens its existing Results directly. It bypasses preparation/gameplay, retains the original sea level and rotation, and does not replay discovery overlays, voices or rewards. Continue returns to that world's system. Uncompleted worlds retain the preparation route. Preparation also checks for completed worlds, and `WorldProgress.Complete` is idempotent so a stale or duplicate completion cannot replace an outcome or grant additional rewards.

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
