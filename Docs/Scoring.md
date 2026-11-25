# Element scoring

## Current rules (supersede the older values below)

All values live in `Resources/ScoringSettings.asset` (`ScoringSettings`); `ElementScoring` reads them.

- **Matches:** 20 points per sphere for a group of three. Each sphere beyond three adds 5 points per sphere (a streak), up to 50 per sphere: 3 = 60, 4 = 100, 5 = 150, 6 = 210, 9 or more = 50 each. Groups smaller than three score nothing.
- **Breakaways:** 75 points per released sphere.
- **Ricochet bonus:** 15 points per ricochet on a shot that makes a match.

**Level score (`LevelScore`).** The rating score is the elemental total (the visualizer bar totals) plus bonus points. Bonuses such as the ricochet bonus never enter element totals, so they raise stars without changing element shares or recipes. The star strip, the end-of-level stars and the saved `highestScore` all use this total. Losing clears bonuses with the rest of the score.

**Stars** are shared fractions of par, where par = starting cluster cells × breakaway points (36 × 75 = 2,700 for three rings): 1 star at 30%, 2 at 46%, 3 at 60% (810 / 1,242 / 1,620). They follow board size and point values automatically; there are no per-world star numbers. Tuned from playtests: a first round of three 4-bar levels scored 1,179–1,325; a second round of six procedural levels (4 and 5 bars) scored 1,470–1,658, all earning 2 stars, with the best two (1,643 and 1,658) reaching 3 stars at 60%.

**Recipe thresholds** are relative to the number of element bars: an element qualifies at 1.1× its fair share (100% ÷ bars), capped at 35%; a self-pair needs strictly more than 1.6× its fair share, capped at 50%. Two or three bars (the Genesis tutorials) keep 35% / 50%; four bars use 27.5% / 40%; five use 22% / 32%; six use 18.3% / 26.7%. Gameplay and completion use the same bar count.

**No point minimum.** Shares alone decide recipe eligibility. Recipes are only evaluated on completion, when the board has been cleared, so element totals are always full-board amounts and a fixed minimum only blocked otherwise qualifying elements. Worlds can switch recipe discoveries off entirely with `WorldRules.recipeDiscoveries` (Genesis Beta, which teaches the natural Stone discovery); such worlds also hide the visualizer's flares and recipe guides. The old `inclusionGoal` (500, Beta's 1,000,000) is removed.

**DEV breakdown.** In DEV mode each level's composition readout (log and Results) also lists the score breakdown: elemental points, ricochet bonus and bank matches, the star thresholds and par, shots, match group sizes, breakaway spheres, and the recipe/self thresholds at the level's bar count. Use it to calibrate the star fractions from real play.

Validation: **EcoSphere / Validate Scoring** (level score, par-based stars, streak rates, relative thresholds) and **Validate Element Standings** (exact relative boundaries). Gameplay, composition and world-rating smoke checks derive their expectations from `ScoringSettings`.

## Earlier rules

Visualizer leadership required at least 35% of selected elemental points (now relative; see above). A bar flares only when it also meets the world inclusion minimum, and self-pairs receive the stronger gold self-combination effect. Recipes use these same composition/minimum requirements. See [Element-composition.md](Element-composition.md).

Current contribution behavior is defined in [Spheres.md](Spheres.md). Each sphere divides its earned points among its authored 1–4 elements according to their relative weights. Fractional totals remain in double precision through final unlock evaluation; displayed element scores truncate toward zero. Stone has its own resource bar, and the consumed Laser supplies its authored Laser resource while retaining zero total score. All bars and streaks use the source sphere's color. Successful completion evaluates shared discovery recipes against the level's inclusion goal. This supersedes the single-resource and Stone-only visualizer exceptions described below; match-size scoring and thresholds remain as documented.

Matches award `N × min(20, 10 + 2 × (N − 3))` points, where N is the entire connected group cleared by the shot. Groups smaller than three do not score. Examples: 3 = 30, 4 = 48, 5 = 70, 6 = 96, 8 = 160, 10 = 200. The rate caps at 20 points per sphere starting at eight spheres.

Every released sphere awards 50 points. Laser-cleared targets retain the breakaway rate of 50; the consumed laser itself awards nothing. Misses award nothing. Losing discards the level's score and contributions.

Matches and breakaways use the same normalized distribution: earned points × element weight / (sum of element weights + unallocated weight). Unallocated weight consumes a share without adding element points; it never lowers total score. Adding more contributed elements never increases the points distributed by a sphere. Stone has its own collected total, but a naturally occurring-only element has no visualizer entry and does not enter composition or recipe evaluation. Only elements supplied by the selected loadout participate in those systems. Matches dissolve; breakaways ascend toward the receiving world.

The default inclusion threshold is 500 and the excess threshold is 1,500. These gameplay values do not set visualizer bar heights: the leading element targets 85% of available height, and other elements scale proportionally. Scale and values animate smoothly; numbers retain the actual truncated point totals. There are no fixed threshold lines on this relative display. Clearing the board remains the victory condition; recipe eligibility still uses full-precision totals and the inclusion/composition rules.

The resource announcer's qualifying gain threshold also follows the new 500-point inclusion value. Score rules live in ElementScoring; scene inclusion and abundance thresholds are inspector fields.

For the current Stone discovery demonstration, First Light overrides these defaults with 1,000,000 / 3,000,000. The 500 / 1,500 defaults remain available for later level tuning. This temporary override keeps the results demonstration focused on releasing Stone.

Validation: gameplay smoke covers match-size scaling/cap, per-element contributions, Stone's independent score, fixed threshold positions, and excess labels. Tutorial and laser smoke checks exercise real shots with the new values.
