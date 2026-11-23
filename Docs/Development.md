# Development mode

DEV mode is currently enabled in Assets/EcoSphere/Resources/DevelopmentSettings.asset. Clear devModeEnabled in the Inspector to restore normal collection gating and tutorials on the next game start. The -noDevMode command-line flag forces normal behavior for automated checks without editing the asset. Configure Development Mode creates a missing settings asset but preserves an existing preference.

While enabled, every catalog sphere eligible for ordinary player selection is available in level setup. Core and Laser retain their structural/powerup roles. This is an availability override: it does not populate the earned-unlock list or acknowledge tutorial steps. The shared ownership query treats these spheres as owned for selection and both recipe/natural discovery checks, so DEV does not announce duplicate discoveries. Recipe reveals are tested with -noDevMode. No new unlock recipes are invented by DEV mode.

System introduction and next-world callouts, authored setup lessons, gameplay tutorial boxes, and post-discovery tutorial messages are suppressed. Selection counts, world rules, recipe animations, audio, completed-world results and insufficient-collection feedback retain their normal behavior. Existing development session progress still resets on game exit.

Water is registered as sphere ID 6 with 3 Water units and 1 Null unit, blue #1464E1, and Audio/Voice/hydrosphere.mp3. Its texture/icon are 256×128 and 256×256. It is not a normal starting sphere and unlocks through Ice + Fire in normal play; DEV exposes it for immediate testing.

DevelopmentTools.Configure validates Water artwork, voice, contribution, and color. DevelopmentTools.Smoke with -devModeSmoke checks collection access, toggling back to the actual earned collection, Water selection/board/scoring/large-match voice/visualizer color, all tutorial suppression sites, unchanged tutorial acknowledgments and suppression of duplicate discoveries. Normal tutorial checks use -noDevMode. work/validate-spheres.ps1 includes that flag for the regular regression suite.
