# Shared planet stats

PlanetStatCatalog.asset in Resources is the editable source for element-to-planet effects. PlanetStatCalculator evaluates only the selected-element totals already supplied by gameplay. No layout, level, sphere artwork, recipe or unlock rule participates in this calculation.

Every effect starts with that element's share of the WHOLE selected-element total. Most visual stats build toward 250 points per element; temperature instead uses a shared whole-planet buildup toward 100 total points. The existing extra influence above 50% remains. These values are editable in the catalog. No element is excluded from another's denominator. Each profile can affect multiple stats, including signed temperature changes. Sum weighted effects, then clamp temperature to -1..1 and other stats to 0..1. Temperature zero is neutral, not Celsius. These visual weights never change actual contributions, visualizer percentages or recipe qualification.

| Stat | Purpose |
|---|---|
| Temperature | Shared cold/hot balance; determines frozen coverage, heat and scorching |
| Terrain Ruggedness | Surface relief and exposed rock |
| Surface Water | Total water available, including frozen water |
| Sediment / Soil | Sandy ground versus earthy material |
| Moisture | Dry interiors versus wetland conditions |
| Biological Coverage | Available vegetation/biological material |
| Geological Activity | Volcanic potential, independent of temperature |
| Atmosphere Density | Glow density; profiles also supply color; size is a separate signed expansion stat |
| Cloud Cover / Cloud Thickness | Future cloud distribution and opacity |
| Weather Activity | Future wind and organized storms |
| Electrical Activity | Future lightning |
| Civilization | Future development and night lights |
| Pollution | Future haze/water contamination |
| Mineralization | Future material coloration |
| Exotic Activity | Future unusual/magical visual effects |

Derived properties include DesertPotential (sediment, dryness, biological coverage and freezing), WetlandPotential (moisture and soil), VegetationPotential (biology, moisture and temperature), FrozenWater/LiquidWater, VolcanicPotential (geological activity, independent of temperature), StormPotential and CloudLightning. These expose combinations without inventing additional element recipes or forcing every element to have a globe effect.

## Current element wiring

- Fire: +0.8 temperature, +0.7 geological activity, orange atmosphere.
- Ice: -1 temperature, +0.8 surface water, +0.3 moisture, nearly-white blue atmosphere.
- Water: +1 surface water, +0.8 moisture, faint pale-blue atmosphere.
- Stone: +1 ruggedness, +0.25 mineralization. It supplies neither heat nor active volcanism.
- Sunlight: +1 temperature, without volcanic activity.

Fire and Ice combine their signed heating/cooling effects; Ice is stronger than Fire at equal proportions. Water remains conserved beneath frozen coverage. Sunlight + Sand produces a hot sandy world without lava. Fire + Stone produces hot rugged terrain with volcanic potential. Adding Stone still dilutes Fire's whole-planet share, and the smooth fissure response retains sparse lava at minority heat.

## Planned elements

The catalog includes all 50 elements from the curated matrix, retaining the existing numeric IDs and appending new identities. This creates no extra spheres, artwork, ammo types, starting unlocks or recipes. Core and Laser remain special spheres, with no elemental planet profiles. Several small-scale discoveries (including fauna, Sound and Gravity) intentionally have no globe effect. Future terminal discoveries can receive new profiles or contribute no effect while still participating in the total.

Air, Steam, Smoke, Dust and Poison have atmospheric defaults ready for their eventual spheres. Life and vegetation, soil/sediment, weather, civilization, minerals and exotic elements have independent editable weights. These are design defaults, not a physics simulation. Cloud rendering, lightning animation and cities remain future work. Procedural foliage and Plant water/atmosphere effects are now rendered.

## Rendering and saves

PlanetSurface evaluates into a reusable target and smoothly approaches its stats. The shared shader uses temperature for ice and heat, and uses geological activity independently for fissures. It also supports sediment/soil tints. Surface water still maps to elevation through the per-heightmap area distribution. Ice freezes water without destroying its abundance. Atmosphere colors blend by weighted elemental atmosphere contribution. AtmosphereExpansion is a separate signed stat: element weights adjust a baseline width, independently of opacity. The catalog sets minimum/base/maximum widths, and rendering respects those bounds.

CompletedWorld stores a complete, independent PlanetStats snapshot plus its sea level/rotation. Results and System use this snapshot through SpherePreview, not today's catalog. Legacy scalar appearance fields remain for old saves; old worlds without a snapshot use a compatibility conversion. New stat channels must be appended to PlanetStat; missing trailing snapshot values read as zero. New ElementId values must also be appended, never reordered.

PlanetStatChecks validates catalog coverage, stable IDs, temperature balance, denominators, buildup, deserts/volcanoes, frozen/liquid water, wetlands, storm-lightning dependencies and serialization. Run PlanetVisualTools.Configure for setup/data checks, and PlanetVisualTools.Smoke with -planetSmoke for actual shader renders and saved snapshots. Result navigation and recipe/tutorial regressions use the normal shared scenes.

Atmosphere extent: minimum 0.02, baseline 0.05, maximum 0.12 of globe radius. Ice contributes -0.5 expansion, Fire +0.2, and Water +0.85, weighted by whole-planet contribution with existing buildup/dominance. This replaces preset-width blending. Other elements may add positive or negative expansion weights. The resulting bounded width is saved in completed-world snapshots.

Freezing response tuning: temperature still comes from whole-planet contributions, but visible frozen coverage now maps from freezingStartsAt (-0.02) to fullyFrozenAt (-0.45), both editable in the catalog. Established 49% Ice / 51% Void can therefore form a fully frozen world, while low cold influence retains polar frost. Heat contributors still oppose cold through the shared temperature; percentages, point buildup and recipes are unchanged. The response thresholds are copied into completed-world snapshots, shared with the shader, and reused by frozen/liquid-water and biome calculations. Older snapshots default to their previous linear 0..-1 response. Render checks cover 10%, 30% and 49% Ice with Void.

Low-score climate correction: temperature no longer uses each element's separate 250-point visual buildup. It uses the whole planet's total for a common climate buildup (temperaturePointsForFullInfluence, default 100), then applies whole-planet percentages and the existing dominance bonus. Other stats retain their original per-element buildup. This avoids a 110-Ice/100-Void world remaining only partly frozen just because Ice alone has fewer than 250 points. Low-score regression renders include 110/100 and 98/102 Ice/Void; both reach full freezing. DEV completion logs record actual Ice/Fire/Sunlight points, temperature, frozen coverage and freezing threshold for diagnosing live-game mismatches.

Temperature and volcanism are independent. Fire adds +0.8 heat and +0.7 geological activity; Sunlight adds +1 heat and no geological activity. Volcanic fissures depend on geological activity rather than average heat, and can break through snowy land. Liquid oceans mask fissures, while frozen crust can fracture and expose lava. Temperature still controls freezing/scorching; it does not suppress all cold-world volcanism.

Average temperature is calibrated from the same normalized climate value: neutral = 59 F, cold endpoint = -60 F, hot endpoint = 250 F. Cold and hot sides interpolate separately around neutral, making full freezing at -0.45 about 5 F. The catalog exposes these values; snapshots copy the calibration. Gameplay displays a smoothly changing, rounded Average Temperature in Fahrenheit; Results shows the exact final saved climate, rounded through the same formatter, including historical/legacy results. A future Celsius option can convert at display time. Planet stats retain fractional precision.

Fire atmosphere intensity is reduced from 0.65 to 0.12 for a subtle translucent red-orange tint. Its hue, expansion contribution, temperature and geological effects are unchanged.

## Procedural foliage

BiologicalCoverage now renders as stable clustered green canopy, using broad and fine object-space noise. More growth lowers the patch threshold, expanding and connecting patches. The existing heightmap's area-weighted 2nd/98th percentile bounds normalize exposed-land altitude above the current sea level, so middle elevations grow fastest and peaks/valleys grow more slowly across terrain variants. Oceans mask foliage; snow and volcanic crust overlay/interrupt it. No new texture or sprite is needed.

VegetationPotential uses a broad Fahrenheit comfort band: strongest from 45 to 85 F, tapering smoothly toward zero at -25 and 180 F. These four points and the dry-growth floor (0.55) are editable catalog settings copied into snapshots. Moisture improves growth without being a hard prerequisite. Existing global freezing also suppresses foliage.

Plant contributes WaterGreenness 0.04, a very faint biological tint of existing water, plus AtmosphereExpansion +0.12, blue-green atmosphere color (0.56, 0.85, 0.85), and atmosphere strength 0.08. Color/density/extent remain independently weighted from whole-planet contributions. Life contributes BiologicalCoverage 0.5 (Plant contributes 0.8), without gaining those Plant-specific tint/atmosphere effects.

FoliageTools.Configure validates the profile, comfort curve, moisture and snapshot data. FoliageTools.Smoke with -foliageSmoke renders bare/temperate/dense/hot/cold cases, measures green growth and actual mesh-UV elevation bands, compares subtle water tint against identical untinted water, and checks saved preview shader inputs. Previews are under the validation project's Logs/foliage-*.png.

Life and Plant also contribute modest Soil weights (0.20 and 0.25 respectively), warming exposed land toward brown through the shared soil shader tint. The tint recolors the rocky texture luminance toward warm earth tones, preserving detail, with a stronger low-contribution response (`0.9 * (1 - exp2(-8 * Soil))`) so ordinary biological mixes visibly affect bare ground. These use the same percentage/buildup rules. Foliage, water, snow and lava retain their separate color layers.

Mud contributes Soil at weight 1, five times Life's Soil weight for equal elemental influence. This uses the same whole-planet percentages and point buildup, strongly browning exposed land while water, foliage, ice and lava retain their own layers. Mud adds no separate sphere-specific rendering code.

Clouds use a separate PlanetClouds shell for both live PlanetSurface and completed SpherePreview globes. PlanetVisualStyle supplies its shared material, shell scale (1.012) and independent UV drift speed (.008 cycles/second). The 256x128 linear grayscale cloud mask repeats with mipmaps. The shader blends broad and slower secondary samples, soft-thresholds coverage, applies independent thickness opacity and darkness color, and follows main-light shading. Air adds .75 CloudCover, Water adds .2 CloudCover and .6 CloudThickness, Fire adds .6 CloudDarkness through their standard percentage-based influence profiles. Coverage, thickness and darkness remain independent channels. Zero coverage disables rendering even with density/darkness present; zero thickness leaves thin translucent wisps. CloudDarkness is appended to PlanetStat to preserve previous saved indices. Localized lightning shader inputs are reserved but no automatic flashes are enabled. Existing WeatherActivity/ElectricalActivity remain available for future storm behavior.

CloudTools.Configure installs the rendering assets and seeds only those three cloud profile effects. CloudTools.Smoke with -cloudSmoke checks independent stats and snapshot serialization, live clear/wispy/dense/dark renders, drift on a stationary globe, pause, and completed-preview shell creation.

## Per-stat visual response
Element profiles still accumulate percentage-based, point-buildup-weighted contributions. PlanetStatCatalog.responses then maps the combined raw contribution into visual strength, independently for every PlanetStat. contributionForFullEffect and negativeContributionForFullEffect are editable per-stat saturation thresholds; zero stays zero and opposing signed effects combine before this mapping. Initial full-effect inputs: CloudCover .45; BiologicalCoverage and AtmosphereDensity .3; CloudThickness .35; AtmosphereExpansion .6; other visual channels .45. Temperature retains full input 1 and its existing Fahrenheit/freezing calibration. At mature scores, 50% Air + 50% Water yields raw coverage .475, now full coverage. The cloud shader also closes remaining mask gaps at full coverage to allow a genuinely overcast globe. Water area, relief, soil, sediment, moisture, vegetation source strength, geology, cloud darkness and future global channels use the same response pipeline. Vegetation suitability and other environmental interactions still apply afterward. These are visual settings only: score, elemental percentages and recipe eligibility are unchanged. Completed PlanetStats snapshots store resolved output, so changing responses does not reevaluate old worlds.

PlanetResponseTools.Configure seeds only the response settings. Validate checks every stat's endpoints, mixed-composition saturation, early point buildup, independent cloud darkness and snapshot stability. Existing PlanetStatChecks cover temperature and environmental distinctions.
