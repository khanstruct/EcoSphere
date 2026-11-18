# Element scoring

Matches award `N × min(20, 10 + 2 × (N − 3))` points, where N is the entire connected group cleared by the shot. Groups smaller than three do not score. Examples: 3 = 30, 4 = 48, 5 = 70, 6 = 96, 8 = 160, 10 = 200. The rate caps at 20 points per sphere starting at eight spheres.

Every released sphere awards 50 points. Laser-cleared targets retain the breakaway rate of 50; the consumed laser itself awards nothing. Misses award nothing. Losing discards the level's score and contributions.

Matching contributes the earned points to the matched element. Breakaways contribute 50 to each released sphere's element. Total score is tracked independently, so board-only Stone awards 50 total points and keeps its brown ascent streak without adding a visualizer bar. Matches keep their dissolve animation; breakaways ascend toward the receiving world.

The default lower threshold is 500 (included/READY) and the upper threshold is 1,500 (excess/EXCESS). The visualizer uses a fixed 0–1,800 display range, with white and gold threshold lines respectively. Values above the range remain visible numerically while bars saturate. Both portrait and wide layouts show the two thresholds. These are visual classifications; clearing the board remains the victory condition. Discovery recipes are not implemented by these threshold displays.

The resource announcer's qualifying gain threshold also follows the new 500-point inclusion value. Score rules live in ElementScoring; scene inclusion and abundance thresholds are inspector fields.

For the current Stone discovery demonstration, First Light overrides these defaults with 1,000,000 / 3,000,000. The 500 / 1,500 defaults remain available for later level tuning. This temporary override keeps the results demonstration focused on releasing Stone.

Validation: gameplay smoke covers match-size scaling/cap, per-element contributions, Stone's independent score, fixed threshold positions, and excess labels. Tutorial and laser smoke checks exercise real shots with the new values.
