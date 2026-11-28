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
| Electrical Activity | Lightning flashes inside storm clouds (with cloud cover and weather activity) |
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

Temperature and volcanism are independent. The glowing orange fissures have their own stat, `Magma` (appended to `PlanetStat`, shader `_Magma`), which only Lava and Volcano supply, at 1 each. Fire is heat only: +0.8 heat and +0.7 geological activity, but no Magma, so even hot Fire worlds draw no fissures. Sunlight adds +1 heat. Lava and Volcano warm the world only slightly (+0.3 and +0.25 heat). GeologicalActivity no longer draws anything itself; it stays an abstract stat for future effects. Fissures follow Magma rather than average heat, and can break through snowy land. Liquid oceans mask fissures, while frozen crust can fracture and expose lava. Temperature still controls freezing/scorching; it does not suppress all cold-world volcanism.

Average temperature is calibrated from the same normalized climate value: neutral = 59 F, cold endpoint = -60 F, hot endpoint = 250 F. Cold and hot sides interpolate separately around neutral, making full freezing at -0.45 about 5 F. The catalog exposes these values; snapshots copy the calibration. Gameplay displays a smoothly changing, rounded Average Temperature in Fahrenheit; Results shows the exact final saved climate, rounded through the same formatter, including historical/legacy results. A future Celsius option can convert at display time. Planet stats retain fractional precision.

Fire atmosphere intensity is reduced from 0.65 to 0.12 for a subtle translucent red-orange tint. Its hue, expansion contribution, temperature and geological effects are unchanged.

## Procedural foliage

BiologicalCoverage now renders as stable clustered green canopy, using broad and fine object-space noise. More growth lowers the patch threshold, expanding and connecting patches. The existing heightmap's area-weighted 2nd/98th percentile bounds normalize exposed-land altitude above the current sea level, so middle elevations grow fastest and peaks/valleys grow more slowly across terrain variants. Oceans mask foliage; snow and volcanic crust overlay/interrupt it. No new texture or sprite is needed.

VegetationPotential uses a broad Fahrenheit comfort band: strongest from 45 to 85 F, tapering smoothly toward zero at -25 and 180 F. These four points and the dry-growth floor (0.55) are editable catalog settings copied into snapshots. Moisture improves growth without being a hard prerequisite. Existing global freezing also suppresses foliage.

Plant contributes WaterGreenness 0.04, a very faint biological tint of existing water, plus AtmosphereExpansion +0.12, blue-green atmosphere color (0.56, 0.85, 0.85), and atmosphere strength 0.08. Color/density/extent remain independently weighted from whole-planet contributions. Life contributes BiologicalCoverage 0.5 (Plant contributes 0.8), without gaining those Plant-specific tint/atmosphere effects.

FoliageTools.Configure validates the profile, comfort curve, moisture and snapshot data. FoliageTools.Smoke with -foliageSmoke renders bare/temperate/dense/hot/cold cases, measures green growth and actual mesh-UV elevation bands, compares subtle water tint against identical untinted water, and checks saved preview shader inputs. Previews are under the validation project's Logs/foliage-*.png.

Life and Plant also contribute modest Soil weights (0.20 and 0.25 respectively), warming exposed land toward brown through the shared soil shader tint. The tint recolors the rocky texture luminance toward warm earth tones, preserving detail, with a stronger low-contribution response (`0.9 * (1 - exp2(-8 * Soil))`) so ordinary biological mixes visibly affect bare ground. These use the same percentage/buildup rules. Foliage, water, snow and lava retain their separate color layers.

Mud contributes Soil at weight 1, five times Life's Soil weight for equal elemental influence. This uses the same whole-planet percentages and point buildup, strongly browning exposed land while water, foliage, ice and lava retain their own layers. Mud adds no separate sphere-specific rendering code.

Clouds use a separate PlanetClouds shell for both live PlanetSurface and completed SpherePreview globes. PlanetVisualStyle supplies its shared material, shell scale (1.012) and independent UV drift speed (.008 cycles/second). The 256x128 linear grayscale cloud mask repeats with mipmaps. The shader blends broad and slower secondary samples, soft-thresholds coverage, applies independent thickness opacity and darkness color, and follows main-light shading. Air adds .75 CloudCover, Water adds .2 CloudCover and .6 CloudThickness, Fire adds .6 CloudDarkness through their standard percentage-based influence profiles. Coverage, thickness and darkness remain independent channels. Zero coverage disables rendering even with density/darkness present; zero thickness leaves thin translucent wisps. CloudDarkness is appended to PlanetStat to preserve previous saved indices. Lightning flashes inside the clouds. `PlanetStats.CloudLightning` (cloud cover × weather activity × electrical activity) is the storm strength, and `PlanetClouds` drives the shader's `_LightningCenter` and `_LightningStrength` from it, one flash at a time.

- **Timing:** below `lightningMinimumStorm` there are no flashes. Above it, flashes come at irregular intervals whose average shrinks from `lightningMaximumInterval` (4 s) to `lightningMinimumInterval` (0.6 s) as the storm grows.
- **Shape:** each flash rises over `lightningRise` and fades over `lightningFade`. With `lightningFlickerChance` it flickers a second time at 70%, after `lightningFlickerDelay`. Weaker storms flash a little dimmer.
- **Placement:** flashes land only where the cloud mask actually shows cloud. The driver samples points on the shell mesh's own UVs with the shader's mask math, so the mask must be readable; `CloudTools.Configure` sets that and validation checks it. Clear sky never flashes.
- **Look:** in the shader, a flash blends the cloud toward bright blue-white, with a soft glow (`lightningRadius`) and a sharper hot core, and raises the cloud's opacity. Blending instead of adding keeps it readable on sunlit clouds that are already near white.
- **Pause and tuning:** flashes advance with scaled time, so they hold while paused. Every setting is on `PlanetVisualStyle`, so it can be tuned without code.

The same driver runs on the live globe and on completed-world previews (Results and System), since they share `PlanetClouds`. A Lightning sphere supplies the electricity and its Air unit supplies the clouds, so its world storms on its own: Lightning 500 + Air 250 gives a storm strength of 0.53.

CloudTools.Configure installs the rendering assets and seeds only those three cloud profile effects. CloudTools.Smoke with -cloudSmoke checks independent stats and snapshot serialization, live clear/wispy/dense/dark renders, drift on a stationary globe, pause, and completed-preview shell creation. It also checks lightning: none in calm clouds or clear sky; repeated flashes in a storm, each landing on cloud; a facing flash brightening the render (`Logs/Cloud-lightning.png` and `Cloud-lightning-dark.png`); holding while paused; and a real Lightning + Air composition flashing.

## Per-stat visual response
Element profiles still accumulate percentage-based, point-buildup-weighted contributions. PlanetStatCatalog.responses then maps the combined raw contribution into visual strength, independently for every PlanetStat. contributionForFullEffect and negativeContributionForFullEffect are editable per-stat saturation thresholds; zero stays zero and opposing signed effects combine before this mapping. Initial full-effect inputs: CloudCover .45; BiologicalCoverage and AtmosphereDensity .3; CloudThickness .35; AtmosphereExpansion .6; other visual channels .45. Temperature retains full input 1 and its existing Fahrenheit/freezing calibration. At mature scores, 50% Air + 50% Water yields raw coverage .475, now full coverage. The cloud shader also closes remaining mask gaps at full coverage to allow a genuinely overcast globe. Water area, relief, soil, sediment, moisture, vegetation source strength, geology, cloud darkness and future global channels use the same response pipeline. Vegetation suitability and other environmental interactions still apply afterward. These are visual settings only: score, elemental percentages and recipe eligibility are unchanged. Completed PlanetStats snapshots store resolved output, so changing responses does not reevaluate old worlds.

PlanetResponseTools.Configure seeds only the response settings. Validate checks every stat's endpoints, mixed-composition saturation, early point buildup, independent cloud darkness and snapshot stability. Existing PlanetStatChecks cover temperature and environmental distinctions.


## Color traits

Element profiles can tint four planet colors: **Water**, **Vegetation**, **Land** and **City Lights** (see City lights below) (`PlanetColorTrait`, append-only). Each entry in a profile's `colors` list names a trait, a color and a strength (0–1). For each trait, every contributing element is weighted by `strength × its influence`, using the same whole-planet influence as the other stats. The blended hue is the weighted average of those colors. The tint amount is the total weight passed through that trait's `colorResponses` entry: `contributionForFullEffect` sets how much weight reaches the maximum, and `maximumAmount` caps how much of the base color can be replaced (both default to .3 / .85). This is the same weighted blending the atmosphere already uses. Several tints mix their hues; a single strong one dominates.

The shader receives each trait as `_<Trait>Tint` (rgb = color, a = amount):
- **Land** recolors rock luminance toward the hue after soil and sand, before heat, snow, foliage and lava. An sRGB mid-gray hue keeps brightness, dark hues darken and pale hues brighten, and the rock detail survives.
- **Vegetation** recolors the canopy and keeps its light/dark variation. Vegetation coverage is unchanged.
- **Water** recolors the ocean and keeps its shallow-to-deep variation.

Colors are authored in sRGB and converted by `SetColor` (the project renders in linear space).

Default tints, all editable in `PlanetStatCatalog.asset`:

| Element | Water | Vegetation | Land |
|---|---|---|---|
| Plant | faint green (the former WaterGreenness) | | |
| Algae | green | teal | |
| Mud | brown | | |
| Swamp | olive | | |
| Poison | sickly green | yellow-green | |
| Oil | black | | |
| Smoke | murky gray | | |
| Magic | | violet | |
| Crystal | | pale violet | pink-magenta |
| Fungus | | mauve | |
| Forest | | deep green | |
| Flower | | warm orange | |
| Salt | | | white |
| Clay | | | red-orange |
| Coal | | | near-black |
| Ash | | | gray |
| Metal | | | blue-gray |
| Glass | | | pale teal |

`PlanetStat.WaterGreenness` is obsolete. Plant's water tint now comes from its Water color trait. The channel keeps its index so completed worlds saved before color traits still render their green water through `PlanetStats.AppliedTint`. Color traits are saved in completed-world snapshots (`PlanetStats.colorTraits`) and copied to previews. A tint that fades in during play starts at its own hue rather than from black.

`SeedColorTraits` is the single authored default list, used by both `PopulateDefaults` and `PlanetColorTools.Configure`. Configure seeds the asset without touching other tuning and removes the old Plant WaterGreenness effect. `PlanetColorTools.Validate` checks:
- untinted defaults stay untinted;
- the Crystal, Magic and Plant directions are right;
- weighted hue blending and the maximum amounts hold;
- snapshot round trips keep the tints;
- legacy green water still draws;
- tints fade in at their own hue;
- the shader compiles.

`PlanetColorTools.Smoke` with `-planetColorSmoke` (run in DEV mode so the test level's tutorial does not pause time) renders the live planet for several mixes. Each mix is rendered with and without its tints, so only the color traits differ. It checks that Plant leaves land and vegetation untouched, Crystal turns land pink, Magic turns vegetation and water violet, Mud browns the water and Coal darkens the land. It also checks that a completed world and its preview keep all three tints. Renders are saved to `Logs/planet-color-*.png`.

Not yet covered: the atmosphere color. The atmosphere still uses its own `atmosphereColor`/`atmosphereStrength` fields, because its strength also drives atmosphere density and older saves store it separately. It could become a color trait later.


## City lights

City lights are drawn by the planet shader from two 256×128 linear grayscale masks in `Art/Planets`: `city-lights-mask-v1.png` (scattered settlements) and `city-lights-dense-mask-v1.png` (dense urban networks). Three independent controls drive them:

- **Coverage: `Civilization`.** Regions light up in a stable, noise-defined order as civilization rises (`revealAt = .72 − .9 × civilization`), so a small civilization shows a few settlements and about 0.6 lights every region. Civilization only ever shows the scattered mask, boosted by `_CityLightsSharpness.z` (2.5) so sprawl reads as clear points.
- **Dense networks: technology only.** As `CityLightIntensity` rises from 0.45 to 1, the scattered mask blends into the dense mask, reaching a nearly city-covered land surface. Abundant Technology is required; Civilization never triggers it.
- **Brightness: `CityLightIntensity`** (new stat, appended). This sets brightness between `_CityLightsBrightness.x` (0.6, so lights without technology stay modest) and `.y` (3) on the material. Defaults: Technology 1, City 0.6, Energy 0.4; Human adds none. Coverage comes from the Civilization element (1) as well as Human 0.25, Technology 0.7 and City 1.
- **Color: the `CityLights` color trait.** It shifts the material's default warm color (`_CityLightsColor`, sodium gold) through the shared color-trait blend. Technology pulls it toward cool white (strength 0.5) and Magic toward violet (0.8).

Lights appear only on exposed land: never on liquid water, ice or volcanic crust. They're full strength past the terminator. The game, Results and System cameras light the planet from the front, so most of the visible face is daylit; `_CityLightsDayVisibility` (default 0.75) keeps the lights readable there, and `_CityLightsUrbanShade` (0.35) darkens built-up land under the network, so the points keep their contrast in daylight. Each small cell (about 1/60 of the globe) twinkles on its own phase, so the network never pulses together. The cloud shell is drawn over the surface, so clouds naturally dim the lights beneath them.

The masks are tiled `_CityLightsTiling` = 3×3 around the globe. One 256×128 mask stretched once around the planet gives about 3 screen pixels per texel and smears into a glaze; tiled, each texel is about one pixel. `_CityLightsSharpness` (floor 0.04, gain 1.6) turns the soft mask into points. Because they tile on both axes, `GameArtImportRules` exempts `/city-lights-` textures from the planet rule that clamps V; surface maps still clamp at the poles. All of these settings live on the planet material.

`SeedCityLights` is the one authored list of brightness effects, used by both `PopulateDefaults` and `CityLightTools.Configure`. Configure:
- imports the masks (linear, repeat on both axes, 256 max) and assigns them to every terrain material listed in `PlanetVisualStyle`;
- adds the CityLightIntensity response;
- seeds the effects and runs `PlanetColorTools.Configure`.

The first texture assignment after a planet shader reimport can be dropped while the material reloads, so Configure assigns again and confirms. Validate checks:
- size, repeat wrap and material assignment;
- no lights without civilization;
- Human < City coverage and Technology brightness;
- the default warm color and Magic violet;
- snapshots and the shader.

`CityLightTools.Smoke` with `-cityLightSmoke` (DEV mode) renders the live planet lit from the side, so the left half is night. Each case renders with and without its lights, so only the lights differ. It checks:
- none without civilization;
- Human settlements appear, and full City covers far more;
- night is stronger than day, with lights still visible by day;
- the default color is warm, Technology is brighter and Magic is violet;
- clouds dim the lights, and twinkle is local while the total glow stays steady;
- lights are visible under the game's own lighting;
- a completed world's preview keeps its inputs.

Add `-cityLightLooks` to render every case and log failed checks instead of stopping, for tuning. Renders: `Logs/city-lights-*.png`.

A higher-resolution mask would sharpen the lights further, but it needs an explicit exception to the 256px texture rule.

## Sand shorelines

Sediment (from Sand, and from Ash, Dust, Clay and Salt) is drawn as light sand that starts at the shoreline and climbs inland as sediment grows. A little gives narrow beaches along the water; a lot covers sandy plains and deserts. The shader measures each point's height above the waterline, as a share of the land's height range. Sand covers land below a reach that grows linearly from `_SandReach.x` (0.04) at the first sediment to `.y` (1.05) at full sediment. Steep slopes and peaks count as higher (`_SandReach.z`, 0.35), so cliffs and mountains stay rock longest. A little noise (`_SandEdge`, 0.05) keeps the edge from tracing the contours exactly, and a fine grain varies the color.

The color is `_SandColor` (light sand, sRGB 0.93/0.85/0.64). It's applied after the heat and scorch tint, so hot sandy worlds (Sand adds some Sunlight) keep pale sand, and before water, snow, foliage and lava, which draw over it. Moisture no longer suppresses sand: beaches sit right next to water. These settings live on the planet material. The data-side `DesertPotential` is unchanged.

The planet color smoke renders a small (150) and a large (1400) amount of Sand on a Water and Stone world, each with and without sediment. It checks:
- with a little, most sand pixels lie near water (measured about 91%);
- with a lot, there's far more sand and a smaller share near water (about 40%);
- the sand is lighter than the land it covers.

Renders: `Logs/planet-color-sand-beaches.png` and `planet-color-sand-inland.png`.

## Moons

The Moon element raises an appended `Moons` stat (profile: Moons 1; default response, full at 0.45). `PlanetVisualStyle.moonOrbits` lists up to three moons. Each one appears once Moons reaches its threshold:

| Moon | Threshold | Distance | Size | Orbit | Inclination |
| --- | --- | --- | --- | --- | --- |
| 1 | 0.1 | 1.55 | 0.22 | 40 s | 14° |
| 2 | 0.55 | 1.85 | 0.16 | 62 s | −22° |
| 3 | 0.9 | 2.15 | 0.12 | 90 s | 30° |

Distance is in planet radii, and size is a share of the planet's diameter. In practice, one Moon sphere in a four-sphere world (about 19% Moon) gives one moon, about 30% Moon gives two, and a Moon-heavy world gives three. The stat also builds up with the amount of Moon, so a world with very few points needs a bigger share. The thresholds, orbits and grow-in time (`moonAppearSeconds`) can all be tuned on the style.

`PlanetMoons` builds the moons as lit sphere primitives. The live gameplay planet (`PlanetSurface`) and completed-world previews (`SpherePreview`: Results and System) share it, the same way they share `PlanetClouds`. The moons wear `Art/Planets/planet-moon.mat`, which `PlanetMoonTools.Configure` builds from the surface of whichever sphere represents the Moon element, without the icon. The orbits keep a fixed frame toward the viewer while the globe spins underneath them; `PlanetMoons` runs after `PlanetSurface` so the frame is reset last each frame. Fairly flat inclinations let moons pass in front of and behind the planet. Moons are tidally locked, grow in or shrink away when the count changes, and hold while the game is paused.

Previews show their moons fully grown at once. A preview with moons zooms its camera out just enough to keep the outermost visible orbit in frame; a moonless planet keeps its usual framing.

`PlanetMoonTools.Smoke` with `-planetMoonSmoke` checks:
- 0, 1, 2 and 3 moons as the stat rises (`Logs/Planet-moons-*.png`);
- the moons orbit clear of the planet, in a frame that doesn't spin with the globe, and hold while paused;
- real compositions: one Moon sphere of four gives 1 moon, 30% Moon gives 2, and a Moon world gives 3;
- previews show all their moons inside a zoomed-out frame, and a moonless preview keeps its framing (`Logs/Planet-moons-preview.png`).
