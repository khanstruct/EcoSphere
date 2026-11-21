# Element scoring

Visualizer leadership requires at least 35% of selected elemental points, so at most two elements qualify. A bar flares only when it also meets the world inclusion minimum. Strict majorities (>50%) that meet the minimum receive the stronger gold self-combination effect. Recipes use these same composition/minimum requirements. See [Element-composition.md](Element-composition.md).

Current contribution behavior is defined in [Spheres.md](Spheres.md). Each sphere divides its earned points among its authored 1–4 elements according to their relative weights. Fractional totals remain in double precision through final unlock evaluation; displayed element scores truncate toward zero. Stone has its own resource bar, and the consumed Laser supplies its authored Laser resource while retaining zero total score. All bars and streaks use the source sphere's color. Successful completion evaluates shared discovery recipes against the level's inclusion goal. This supersedes the single-resource and Stone-only visualizer exceptions described below; match-size scoring and thresholds remain as documented.

Matches award `N × min(20, 10 + 2 × (N − 3))` points, where N is the entire connected group cleared by the shot. Groups smaller than three do not score. Examples: 3 = 30, 4 = 48, 5 = 70, 6 = 96, 8 = 160, 10 = 200. The rate caps at 20 points per sphere starting at eight spheres.

Every released sphere awards 50 points. Laser-cleared targets retain the breakaway rate of 50; the consumed laser itself awards nothing. Misses award nothing. Losing discards the level's score and contributions.

Matches and breakaways use the same normalized distribution: earned points × element weight / (sum of element weights + unallocated weight). Unallocated weight consumes a share without adding element points; it never lowers total score. Adding more contributed elements never increases the points distributed by a sphere. Stone has its own collected total, but a naturally occurring-only element has no visualizer entry and does not enter composition or recipe evaluation. Only elements supplied by the selected loadout participate in those systems. Matches dissolve; breakaways ascend toward the receiving world.

The default inclusion threshold is 500 and the excess threshold is 1,500. These gameplay values do not set visualizer bar heights: the leading element targets 85% of available height, and other elements scale proportionally. Scale and values animate smoothly; numbers retain the actual truncated point totals. There are no fixed threshold lines on this relative display. Clearing the board remains the victory condition; recipe eligibility still uses full-precision totals and the inclusion/composition rules.

The resource announcer's qualifying gain threshold also follows the new 500-point inclusion value. Score rules live in ElementScoring; scene inclusion and abundance thresholds are inspector fields.

For the current Stone discovery demonstration, First Light overrides these defaults with 1,000,000 / 3,000,000. The 500 / 1,500 defaults remain available for later level tuning. This temporary override keeps the results demonstration focused on releasing Stone.

Validation: gameplay smoke covers match-size scaling/cap, per-element contributions, Stone's independent score, fixed threshold positions, and excess labels. Tutorial and laser smoke checks exercise real shots with the new values.
