# Shared runtime data and evaluation

Sphere identity is explicit: `SphereDefinition.namesakeElement` identifies the sphere's element independently of its contribution list. Ice can contribute Water and Ice without losing its Ice artwork or voice in a recipe reveal. Each registered sphere has a unique stable ID and namesake. New definitions must assign both. Existing definitions have been migrated; IDs and contribution weights are unchanged.

`SphereCatalog` lazily indexes IDs and namesakes. `ElementRecipeBook` indexes recipes by `ElementPair`, an unordered value key implementing `IEquatable<ElementPair>`. Fire + Ice and Ice + Fire are the same key, and validation rejects duplicate pairs. Self recipes remain supported. Inspector changes invalidate indexes automatically, as do list replacements and count changes. Runtime tools editing IDs, namesakes, or recipe ingredients in place must call `InvalidateIndexes()` or `InvalidateIndex()` after editing.

`ElementComposition` provides a nonallocating view of element totals. It computes the denominator once per evaluation and shares percentage, dominance, and influence calculations. Invalid nonfinite or negative values contribute zero. Planet stat evaluation retains the existing percentage and buildup formulas. `PlanetSurface` only rebuilds target stats when totals or the stat catalog revision change; visual interpolation still runs every frame. Inspector changes notify automatically. Runtime profile editors must call `PlanetStatCatalog.NotifyChanged()` after modifying profiles or calibration values.

Shot selection reuses its candidate buffers and still samples distinct remaining types, preserving its seeded ordering and repetition limit. The gameplay aim guide reuses a path buffer through `BoardRules.TraceAimPath`; the allocating `AimPath` API remains available to existing callers.

World progress indexes generated systems and completed-world locations, rebuilding those indexes on session/load reset. Save serialization remains unchanged. Results and System previews use `SpherePreview.ConfigurePlanet`, with legacy appearance conversion supplied by `CompletedWorld.AppearanceStats`. Climate snapshot copying is centralized and snapshots retain independent value arrays.

General prototype smoke helpers live in `EcoSphereSmokeChecks.cs`, alongside the other partial-class smoke modules. They exercise current weighted sphere definitions rather than obsolete pure-element yields.

Run `EcoSphere.Editor.RefactorChecks.Validate` in Unity batch mode for data, generation, board rules, namesake lookup, recipe indexing, composition caching/invalidation, snapshot independence, and aim-path regression checks. Gameplay smoke suites additionally cover actual tutorial shots, mixed scoring, procedural play, discovery animation, historical results, Plant, and foliage.

These systems remain data-driven. Adding an element or sphere does not require a new behavior subclass; special gameplay roles and shared evaluators provide the behavior while definitions supply the data.
