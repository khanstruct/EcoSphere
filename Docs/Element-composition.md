# Element standings and combination potential

The visualizer is one shared frame beneath the cannon in both orientations. Each distinct element contributed by the selected loadout has one energy bar, regardless of contribution weight or source sphere. Bars use the element name and its namesake sphere color; source-sphere labels are omitted. Elements with at least 35% of selected elemental points that also meet the world inclusion minimum receive flares around their filled bars; an element above 50% that also meets the inclusion minimum receives a stronger gold double flare. No per-sphere frames, background highlights, or top-three summary strip remain.

Standings belong to **elements**, not sphere types or visualizer groups. Multiple spheres can deliver the same element; their contributions accumulate into one total. Each element bar shows that accumulated total, height and leading/self status once. Zero-contribution elements do not receive a rank or highlight. Rank alone never grants eligibility: at least 35% is required, so at most two elements can lead.

`ElementStandings` supplies the shared ranking and composition calculation. Ranking uses actual accumulated amounts rather than animated bar heights. Equal amounts preserve the previous order: the incumbent third-place element remains highlighted until a challenger strictly exceeds it. Initial ties without an incumbent follow the visualizer's first-appearance order. Gameplay passes its final order into discovery evaluation so completion does not choose different tie winners.

## Threshold guides and star score strip

Bars stay scaled to the leader (85% of the available height) so they don't sit at the bottom. Because every bar shares that scale, the recipe thresholds are single heights that apply to all bars, drawn behind them (`EcoSphereVisualizerGuides`):

- **Match line:** the higher of 35% of the selected elemental total and the world's inclusion minimum. A bar above it qualifies for a recipe.
- **Self-match line:** the higher of 50% of the total and the inclusion minimum.
- A muted zone fills the space between the lines and a gold zone sits above the self line. Lines and zones follow the bars' eased scale. A line above the frame pins to the top with a `^` (the UI font has no ▲ glyph). Labels sit at the right end, behind the bars, so bar totals stay readable where they meet. The existing flares still mark fully qualified bars.

A slim strip along the top of the frame shows the level's rating score, the same value the world's stars are earned from: the selected elemental total (`ElementStandings.Total`, the sum of the bar totals) that completion saves as `highestScore`. It deliberately does not show the raw per-sphere `score`, which also counts points that never become displayed elemental points and can exceed a star threshold the rating doesn't reach. The strip has an eased fill, the score number, and three stars at the world's `oneStarScore`/`twoStarScore`/`threeStarScore`, on a track ending 15% past three stars. Each star switches from `Art/UI/star-empty.png` (source 1254 px; imported at the 256 px limit) to the catalog's rating star with a short pop and the shared star-score sound as the fill passes it. The "Score · Matched · Released" counters line above the visualizer was removed.

Colors, line thickness, labels, strip height, star size and the track's range are on `VisualizerStyle`. Checked in portrait and wide renders at a fresh start and with injected mid-game totals (`Logs/Visualizer-start-zoom.png`, `Logs/Visualizer-progress-zoom.png`, `Logs/Visualizer-progress-wide-zoom.png`).

## Recipe eligibility

- Both recipe inputs must individually have at least 35% of selected elemental points and meet the world’s existing minimum inclusion amount. Exactly 35% qualifies; values below it are not rounded up.
- A self-pair such as A + A additionally requires `2 × amount(A) > total elemental contributions`. Exactly 50% does not qualify.
- The denominator sums each element total once, across the entire planet, including contributions from all source spheres.
- Eligibility requires an authored two-element recipe. Crossing 50% never invents a new recipe or sphere.
- Recipe outputs are direct SphereDefinition references. A sphere's contributed elements do not define its unlock ingredients.

There is no newly imposed 25% qualification rule or automatic normalization of contribution amounts. The existing amounts, goals, excess markers and scoring bonuses remain configured as before. The visual highlight indicates a leading candidate; actual discovery also needs its minimum and recipe.

At most two elements can reach 35%, supplying at most one distinct-element pair. At most one can exceed 50%, supplying an additional possible self-pair. Authored natural discoveries remain a separate reward path. No qualifying recipe is withheld by a random reward selector or a player-choice menu.

Results now display a fourth discovery instead of truncating it. The discovery modal adapts row size to the viewport, and collection spacing/animation uses the actual displayed sizes. User-facing discovery headings now say spheres rather than elements.

## Validation

**EcoSphere / Validate Element Standings** covers zero totals, repeated channels, incumbent ties, strict overtaking, exactly/just above 50%, large integer totals, the three-pairs-plus-self bound and minimum requirements. `ElementStandingsTools.Smoke -compositionSmoke` checks live highlights, shared percentages, marker changes and real self-recipe awards. Portrait and wide previews are under the validation project's `Logs/Composition-*.png`.

The existing gameplay suite checks contribution amounts, source colors, fixed goal positions, both tutorial flows, audio and results. Results smoke also exercises four discoveries in portrait/wide layouts and their collection animation.

Only elements supplied by the selected loadout participate in composition and recipe evaluation. Naturally occurring-only elements retain collected totals internally but do not enter the visualizer, rankings, percentage denominator, or recipes; scripted natural discoveries remain separate. Contribution weights also support nonnegative unallocated weight: it consumes elemental yield without contributing to composition or lowering total score. Existing definitions default to zero unallocated weight.

## Procedural appearance

All bars share the generated 256×256 grayscale energy texture and UIEnergyBar shader. The shader scrolls the texture, lights the edges and traces flares around each filled bar. Each element has a subtle height pulse and a steady exact-value marker. VisualizerStyle in Resources assigns the material/frame and controls maximum bar width (82 UI units), minimum spacing (12), and pulse strength. Bars remain at maximum width when space allows, with surplus spacing evenly distributed between element bars. Dense layouts narrow the bars to fit all 16 or 20 distinct elements on one baseline; auto-sized element names and totals keep labels within their slots. The camera reserves bottom space so the cannon and playfield do not overlap the panel.
