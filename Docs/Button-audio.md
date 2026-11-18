# Button click audio

`Audio/button-click.mp3` plays through `Resources/UIAudio.prefab`, a single persistent 2D AudioSource at 65% volume. The clip is preloaded and decompressed on load for responsive UI playback. Its source survives scene changes and ignores listener pause, so navigation does not cut clicks off and tutorial Continue works while gameplay time is stopped.

`ButtonClickSound` registers in Awake and removes its listener on destruction. Hiding and reopening a modal keeps the sound listener ahead of its action listener, so the click plays before the action hides the button. It checks interactability before playback. Celestial buttons attach it automatically; planet buttons and the runtime sphere/slot buttons attach it before their action listeners. New non-Celestial buttons should call `ButtonClickSound.Attach(button)` before adding action listeners.

`EcoSphere.Editor.ButtonAudioTools.Setup` recreates the playback prefab and configures the clip import. Run `EcoSphere.Editor.MenuMusicTools.Smoke` with `-buttonAudioSmoke` to check planet selection, prep button coverage, disabled-button silence, Back navigation, paused tutorial Continue, and source persistence without duplicates.
