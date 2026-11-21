# Shared gameplay audio

All common gameplay clips are assigned once in `Assets/EcoSphere/Resources/GameAudioSettings.asset`. This shared asset contains music, Shoot, Ricochet, Match, Breakaway, Miss, Laser and the World Complete, Creation Failed, Impressive and EcoSphere callouts, plus the shared Sphere Combine effect. It loads automatically; gameplay scenes do not store their own clip assignments.

`EcoSpherePrototype` requires and initializes `LevelAudio` through shared gameplay startup, including a fallback for legacy objects missing that component. New authored or procedural levels using this gameplay component receive the same sound defaults without per-level setup. LevelAudio owns audio sources and runtime mixing controls; GameAudioSettings owns clip selection. Initialization is idempotent and duplicate LevelAudio components are disallowed.

The universal bottom-boundary loss handler calls `LevelAudio.Miss()` once, removes the projectile and advances the turn. `miss.mp3` plays through the effects source in every level using this shared gameplay path. Wall rebounds use Ricochet. Nonmatching attachments are not bottom losses, and misses do not request routine announcer lines.

Sphere-specific large-match voices remain on sphere definitions; see [Spheres.md](Spheres.md). The level builder and laser setup helper configure shared assets instead of assigning clips to levels.

## Playback and mixing

- Music loops independently at the runtime music volume (default .18).
- Effects use volume .85 by default and do not stop music.
- Cannon shots retain independent volume/pitch controls and briefly duck other audio for .22 seconds before restoring it over approximately .12 seconds.
- Match plays once for a successful match. Breakaway follows when that match also releases spheres.
- A qualifying large match plays the matched sphere's assigned voice. Other qualifying resource gains use element voice selection, with EcoSphere for mixed gains. The threshold remains 500 points.
- Impressive retains its ricochet chance and voice-suppression rules.
- World Complete queues behind resource callouts; Creation Failed cancels pending announcements. Voice playback ducks the background.
- Audio is non-spatial, with separate music, effects, resolution, cannon and voice sources.

## Authoring and validation

**EcoSphere / Configure Shared Audio** seeds missing defaults without replacing existing edits. Runtime validation catches incomplete settings. Assign common clips on the shared asset; do not add per-world sound fields or name checks for universal rules. Deliberate future exceptions should be explicit configuration.

`GameAudioTools.PlaybackSmoke -sharedAudioSmoke` creates a bare gameplay object with no scene audio configuration, verifies its automatic LevelAudio dependency, and checks actual miss playback without invoking the announcer. Existing gameplay smoke exercises real bottom losses, ricochets, matches, result announcements and turn/reload behavior.

Recipe reveals play sphere-combine.mp3 once from the shared sphereCombine assignment. Ingredient hold, merge, charge, flash and colored reveal are scaled to the actual clip length and driven by AudioSettings.dspTime. The existing element-discovered announcement is scheduled at the combine clip’s exact end; separate announcement sources preserve prior voice tails across consecutive discoveries. Natural discoveries and completed-world revisits never play the combine effect.
