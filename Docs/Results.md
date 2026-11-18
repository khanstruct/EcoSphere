# Results and completed worlds

Successful First Light clears now lead to Results after all queued announcements, including World Complete, finish actual playback. Losses do not award discoveries or completed worlds. The current Stone tutorial temporarily uses inclusion/excess thresholds of 1,000,000 / 3,000,000 to keep threshold-based rewards out of this demonstration; match and breakaway scoring rates are unchanged. This remains the existing level, intended to become tutorial two when the introductory half-Ice/half-Sunlight level is built later.

Results uses the existing Outfit font, gold frames, heading ornaments, and Celestial buttons. The world-image and description frames intentionally remain empty. The collection frame beneath the description receives the newly discovered spheres.

The modal supports zero to three discoveries. With discoveries, it shows rotating element spheres with names beside them and plays `Audio/Voice/element-discovered.mp3` once on entry. Ok disables itself and animates the same sphere previews down to 100-unit collection slots over 0.8 seconds. Continue stays disabled until the animation finishes. Without discoveries, the modal and discovery sound are skipped and Continue is immediately available. UI button-click audio is reused.

Stone is awarded only after it was released during a successfully completed level, and is awarded once per save. `WorldProgress` keeps completed system/planet slots, sea level, rotation, and the Stone unlock flag in memory for the current testing session. It does not read or write a persistent save. A new game or Play session resets progress; see Autosave.md. Discovery recipe evaluation and additions to the prep sphere catalogue remain separate work.

Continue returns to the completed world's system. Completed planet buttons display a rotating sphere using the same barren-planet material, heightmap, and saved sea level as gameplay. Uncompleted slots remain empty rings. Completed worlds, unlocks, appearance, and the current system persist between scenes only; they reset on a new game session. See Autosave.md. The existing planet click behavior is retained.

`ResultsTools.Build` creates Results, registers it in build settings, assigns the completed-world material to System, and raises First Light's temporary thresholds without rebuilding either existing scene. TestLevelTools/SystemSceneTools preserve these assignments on future rebuilds.

Validation: `ResultsTools.Smoke -resultsSmoke` tests the normal win transition, Stone-only discovery, one discovery voice, Ok click and animation, Continue gating, correct system/planet completion, matching planet material/sea level, rotating map preview, repeat unlock suppression, and zero/one/three discovery cases. It renders portrait, tablet, and wide results previews. All progression is currently memory-only, including normal gameplay. Tutorial and gameplay regression checks also run separately.
