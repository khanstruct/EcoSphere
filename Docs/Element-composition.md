# Element standings and combination potential

The visualizer is one shared frame beneath the cannon in both orientations. Each real contribution unit has one energy bar, grouped by source sphere. Elements with at least 35% of selected elemental points that also meet the world inclusion minimum receive flares around their filled bars; an element above 50% that also meets the inclusion minimum receives a stronger gold double flare. No per-sphere frames, background highlights, or top-three summary strip remain.

Standings belong to **elements**, not sphere types or visualizer groups. Multiple spheres can deliver the same element; their contributions accumulate into one total. Repeated element bars show the same accumulated point total, height and leading/self status and consume only one ranking position. Zero-contribution elements do not receive a rank or highlight. Rank alone never grants eligibility: at least 35% is required, so at most two elements can lead.

`ElementStandings` supplies the shared ranking and composition calculation. Ranking uses actual accumulated amounts rather than animated bar heights. Equal amounts preserve the previous order: the incumbent third-place element remains highlighted until a challenger strictly exceeds it. Initial ties without an incumbent follow the visualizer's first-appearance order. Gameplay passes its final order into discovery evaluation so completion does not choose different tie winners.

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

All bars share the generated 256×256 grayscale energy texture and UIEnergyBar shader. The shader scrolls the texture, lights the edges and traces flares around each filled bar. Repeated units share a subtle height pulse and have steady exact-value markers. VisualizerStyle in Resources assigns the material/frame and controls maximum bar width (82 UI units), minimum spacing (12), extra sphere-group spacing, and pulse strength. Bars remain at maximum width when space allows, with surplus spacing between sphere groups. Dense layouts narrow the bars to fit all 16 or 20 units on one baseline; compact element names and auto-sized totals keep labels within their slots. The camera reserves bottom space so the cannon and playfield do not overlap the panel.
