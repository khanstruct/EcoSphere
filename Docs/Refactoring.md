# Shared runtime data and evaluation

Sphere identity is explicit: `SphereDefinition.namesakeElement` identifies the sphere's element independently of its contribution list. Ice can contribute Water and Ice without losing its Ice artwork or voice in a recipe reveal. Each registered sphere has a unique stable ID and namesake. New definitions must assign both. Existing definitions have been migrated; IDs and contribution weights are unchanged.

`SphereCatalog` lazily indexes IDs and namesakes. `ElementRecipeBook` indexes recipes by `ElementPair`, an unordered value key implementing `IEquatable<ElementPair>`. Fire + Ice and Ice + Fire are the same key, and validation rejects duplicate pairs. Self recipes remain supported. Inspector changes invalidate indexes automatically, as do list replacements and count changes. Runtime tools editing IDs, namesakes, or recipe ingredients in place must call `InvalidateIndexes()` or `InvalidateIndex()` after editing.

`ElementComposition` provides a nonallocating view of element totals. It computes the denominator once per evaluation and shares percentage, dominance, and influence calculations. Invalid nonfinite or negative values contribute zero. Planet stat evaluation retains the existing percentage and buildup formulas. `PlanetSurface` only rebuilds target stats when totals or the stat catalog revision change; visual interpolation still runs every frame. Inspector changes notify automatically. Runtime profile editors must call `PlanetStatCatalog.NotifyChanged()` after modifying profiles or calibration values.

Shot selection reuses its candidate buffers and still samples distinct remaining types, preserving its seeded ordering and repetition limit. The gameplay aim guide reuses a path buffer through `BoardRules.TraceAimPath`; the allocating `AimPath` API remains available to existing callers.

World progress indexes generated systems and completed-world locations, rebuilding those indexes on session/load reset. Save serialization remains unchanged. Results and System previews use `SpherePreview.ConfigurePlanet`, with legacy appearance conversion supplied by `CompletedWorld.AppearanceStats`. Climate snapshot copying is centralized and snapshots retain independent value arrays.

General prototype smoke helpers live in `EcoSphereSmokeChecks.cs`, alongside the other partial-class smoke modules. They exercise current weighted sphere definitions rather than obsolete pure-element yields.

Run `EcoSphere.Editor.RefactorChecks.Validate` in Unity batch mode for data, generation, board rules, namesake lookup, recipe indexing, composition caching/invalidation, snapshot independence, and aim-path regression checks. Gameplay smoke suites additionally cover actual tutorial shots, mixed scoring, procedural play, discovery animation, historical results, Plant, and foliage.

These systems remain data-driven. Adding an element or sphere does not require a new behavior subclass; special gameplay roles and shared evaluators provide the behavior while definitions supply the data.

## Projectile/aim extraction and targeted rendering cleanup

ProjectileSimulation is scene-independent C# logic shared by aim prediction and live shots. It returns a typed ShotContact (wall, sphere, bottom exit, or none), distance, normal and sphere index. The level controller remains responsible for spawning/moving visuals, attachment resolution, audio, and turn transitions. BoardRules.TraceAimPath is a compatibility entry point delegating to this same solver; it no longer implements separate bounce rules.

BoardCollisionSnapshot supplies reusable centers/cell identities to the solver. It invalidates transformed centers when the cluster matrix changes, recognizes membership changes even when the cell count stays equal, and avoids repeated TransformPoint calls across bounces. World-space cached centers intentionally preserve the existing collision-radius behavior under transformed clusters. No frame-rate/device performance claim is implied; actual profiling is still needed before choosing pooling or a spatial acceleration structure.

PlanetVisualStyle.Current now provides the same lazy shared resource access pattern as the other catalogs. LaunchFlags parses command-line arguments once; runtime and validation callers query it instead of repeatedly reading process arguments.

EnergyBarGraphic uses shader gameplay time for live texture/flare animation, retaining explicit PreviewTime for deterministic previews. Mesh state is dirtied only when its payload changes or normal UI geometry/color changes require it, rather than just to advance time. Camera presentation and star-row layout now avoid unchanged layout work. Popups still track their targets; button edge highlights still animate CPU geometry pending profiling rather than adding unmeasured sub-canvases.

ProjectileSimulationChecks validates nearest contacts, walls/bottom loss, rotated cached geometry, unchanged and same-count board edits, and 500 aim/live first-contact comparisons. Existing actual-shot, laser, infestation, visualizer, tutorial, responsive and star input checks exercise integration.

This is an incremental extraction, not completion of the whole gameplay-controller decomposition. Level state, input/tutorial orchestration and editor-only validation separation still need dedicated follow-up work. Partial smoke tests currently share private controller state; moving them into an Editor assembly requires replacing that coupling first. Folder/assembly reorganization and player-build shader inclusion remain separate work, not claims made by this change.

GameplayGeometry now centralizes sphere scale (.96), collision radius (.4224), neighbor spacing (.864), and play bounds. WorldRules owns clusterRings (default 3); its copy and catalog validation preserve the field and reject unsafe initial geometry. Starting populations and procedural quotas derive from ring count instead of a fixed 60-cell assumption. Tutorial regression expectations derive their matched counts and weighted yields from current board size and sphere definitions.
