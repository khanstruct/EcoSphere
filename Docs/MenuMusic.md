# Continuous menu music

System and LevelPrep each contain a MenuMusic entry point referencing system-background.mp3. The first entry creates a looping 2D AudioSource at 18% volume and persists it across scene changes. Subsequent entries discard themselves before creating an AudioSource, preserving the original playback position without duplicate tracks. Entering gameplay stops and removes the menu music so level audio can take over. Opening LevelPrep directly also starts the track.

MenuMusicTools.Setup patches the existing scenes. Their builders configure the same entry points. MenuMusicTools.Smoke with -menuMusicSmoke passed in Unity: the same AudioSource and advancing sample position survived System -> LevelPrep -> Back; only one music component remained; gameplay removed it; direct Prep entry restarted menu playback. See Logs/MenuMusic-setup.log and Logs/MenuMusic-smoke.log. The editor's existing SearchDatabase indexing exception did not prevent completion.
