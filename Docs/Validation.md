# First Light validation

Validated in a separate temporary copy using Unity 6000.6.7f1, with the same Assets, Packages and ProjectSettings. The main project was locked by its editor session, so no running editor was closed or interrupted.

- C# compiled successfully.
- 17 board-rule assertions passed: unrelated pre-existing groups stay untouched, joining groups clear, mixed-type core connectivity, bridge detachment, all six core neighbors, equal grid spacing, swept collisions, misses, and exact side/top reflections.
- Play-mode smoke test preserved all 36 starting spheres before the first shot.
- Known matching shot cleared 16 matched spheres and released 3 disconnected spheres, leaving 18 (36 + 1 - 16 - 3).
- The next shot loaded only after cluster rotation completed.
- A bank shot ricocheted three times and resolved normally.
- A bottom-returning projectile was lost, followed by cluster rotation and reloading.
- A 900 x 1600 render was inspected. Per-renderer texture overrides corrected material sharing in the initial render; Ice and Sunlight now have distinct surfaces. Cannon foreground covers hinge joints and the loaded sphere; the core texture and halo render together. HUD text was moved above the cannon.

Unity's temporary editor copy logged an internal SearchDatabase indexing exception and Burst cache/code-integrity warnings. These did not originate in the prototype scripts or prevent the play-mode checks, screenshot, or successful editor exit. No interactive mouse usability test has been performed yet; the user's first playthrough should judge aim feel, sprite alignment, rotation speed, and readability.

Temporary validation files and full logs live under ignored `work/UnityValidation/`. Tests run through `EcoSphere.Editor.TestLevelTools.BuildAndValidate`; the smoke run uses `EcoSphere.Editor.TestLevelTools.RunSmoke` with `-ecosphereSmoke`.
