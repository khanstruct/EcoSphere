# Level audio

The FirstLight scene has a LevelAudio component on its root, with all five clips assigned from Assets/EcoSphere/Audio. The scene builder preserves these assignments when rebuilding.

- Deep Space Loop plays continuously at music volume 0.18.
- Effects use volume 0.85. Both controls can be adjusted on LevelAudio, including during Play mode.
- Shoot plays on each fired shot; Ricochet plays on every top or side wall bounce.
- Shoot uses a dedicated high-priority source at volume 1 and pitch 0.9, with its clip preloaded. For 0.22 seconds after firing, other sources drop to 40% of their normal volume, then recover over approximately 0.12 seconds. This makes room for the short attack without stopping or delaying other cues. Shoot Volume and Shoot Pitch are adjustable on LevelAudio.
- Match plays once for a successful match. If that match released other spheres, Breakaway plays after the match clip finishes. No-match contacts and losses play neither cue.
- Cryosphere and Heliosphere announce shots where only Ice or only Sunlight reaches 15 added resources. If two or more resource types each reach 15 in the same shot, EcoSphere plays instead, regardless of relative amounts or ties. Gains are measured per shot from x3 breakaways only (matches contribute zero), not accumulated planet totals. Five released Sunlight spheres alone qualify at 15 points.
- Impressive is considered in the impact frame, immediately after matching/release calculations, without waiting for the delivery and rotation animations. Each top/side ricochet gives an independent 20% opportunity: one bounce 20%, two 36%, three 48.8%, with at most one callout. Per-shot bounce counts reset when firing. Element callouts, completion, an already active/queued voice, and a predicted post-rotation collision suppress it. The prediction checks the final cluster position without applying rewards, changing transforms, or ending the level early. Actual game-over checks still run in their original order. Misses and direct shots never qualify.
- World Complete announces a successful level after final resource delivery and any queued element callouts. Creation Failed announces either game-over checkpoint immediately, canceling pending callouts. Result announcements play once per level on a dedicated high-priority voice source; other music/effects soften while the voice plays. Voice Volume is adjustable. Other files in Voice remain reserved for later.
- Audio is non-spatial, with one listener on the gameplay camera. Music and effects have separate sources so effects do not interrupt the loop.

Unity compilation and the scene's automated matching, release, ricochet and loss smoke run completed successfully with the clips connected. Actual loudness balance should be judged during interactive play. Cluster generation was not changed.
