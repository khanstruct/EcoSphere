# Tutorial levels

The tutorial worlds are now named **Genesis Alpha** and **Genesis Beta**. Their identity, guaranteed System 1 placement, sphere requirements and numeric rules live in predefined-world assets; their specialized scene/layout code stays in place. See [Worlds.md](Worlds.md). “Tutorial 1/2” below identifies their lesson sequence and existing scene filenames.

Current opening: Tutorial 1 defaults to Fire and Void, using the actual pair chosen in preparation. The 30/30 layout and default 620-unit outcomes remain the same. Fire + Void discovers the existing Sunlight sphere when both reach inclusion on a successful world. Tutorial 2 uses all three selected spheres; Stone supplies its authored points and scripted discovery, but naturally occurring-only elements do not appear on the visualizer. See [Spheres.md](Spheres.md) for shared definitions.

## System — first selection

System is now the first build scene. On the first visit to System 1 in a fresh session, before Genesis Alpha is completed and while only the two starting spheres are available, a paused modal says “Welcome to EcoSphere. This is your first Star System.” Continue advances to “You currently only have two Elemental Spheres, so click on the world with a Sphere Requirement of two.” A gold line points to Genesis Alpha. The second Continue dismisses the modal and restores play; it never automatically opens a world. The introduction does not repeat when returning to System during that session, and its seen state resets with development progress.

Selecting an unfinished world with a higher sphere requirement than the number of distinct unlocked/selectable sphere types opens “You do not have enough spheres for this world. Unlock new elements and try again.” Continue clears the popup and restores the previous time scale. Both tutorial and warning modals shield the map and prevent dismissal clicks from reaching planet buttons. Completed worlds retain their direct-results route, even if the current collection is smaller than their requirement.

`WorldCatalogTools.SystemTutorialSmoke -systemTutorialSmoke` checks exact text, both Continue steps, pointer ownership, click audio during pause, raycast shielding, click-through prevention, one-time display, insufficient/sufficient collections and completed-world results. It renders all three boxes in portrait and wide layouts under `Logs/SystemTutorial-*.png`.

## Genesis Alpha — introduction

`Assets/EcoSphere/Scenes/Tutorial1.unity` is Genesis Alpha, selected from the initial System screen. Its Level Layout is First Tutorial. The four-ring board defaults to 30 connected Fire spheres on the left and 30 connected Void spheres on the right, using the actual pair chosen in preparation. There is no Stone or laser. Center-column cells are divided by vertical position to keep antipodal cells opposite and both halves equal.

Genesis Alpha opens with “Your goal is to match and release elements to create a new planet.” Continue advances to “In the center of the cannon, you can see the sphere that is currently loaded. Aim, then tap to shoot. Connect 3 or more matching spheres to clear them.” The pointer targets the loaded sphere. Continue then dismisses the overlay and resumes gameplay. Both messages pause gameplay and shield input, with the existing dismissal-frame click protection. These opening messages no longer appear in Genesis Beta.

Inclusion is 500 per element; excess is 1,500. A clean matching shot clears a half plus its projectile (31 × 20 = 620 points), allowing both elements to reach inclusion without reaching excess. Ammo uses the ordinary board-aware selector, so after one half is cleared only the remaining element loads. Fire + Void discovers Sunlight on successful completion. Genesis Beta's staged ammo and Stone messages do not run here.

`FirstTutorialTools.Build` creates/updates the scene from the shared gameplay setup; `FirstTutorialTools.Smoke -firstTutorialSmoke` checks both opening messages, the cannon pointer, pause/input shielding, the exact 30/30 split, two real shots and 620 points for each element. `Logs/Tutorial1-preview.png` is a gameplay preview; `Logs/tutorial-welcome.png` and `Logs/tutorial-cannon.png` capture the intro overlays.

## Genesis Beta — Stone discovery

The existing `FirstLight.unity` scene is Genesis Beta. The second planet in System 1 (the three-sphere planet) routes through preparation to this scene. It opens paused with the naturally occurring elements lesson pointing to Stone; welcome and cannon/ammo lessons remain exclusive to Genesis Alpha. Its large temporary thresholds and the two Stone-related messages are retained.

The authored four-ring cluster places Stone on the outer ring at axial (0, -4). The remaining 23 outer cells and Stone’s one inward support at (0, -3) use the first selected non-Stone sphere type. All occupied neighbors of Stone therefore belong to the connected outer match. Remaining interior cells are shuffled among the chosen sphere types; the immediate core neighbors remain balanced so a mixed interior survives the guided clear. Setup order is preserved; direct launch defaults to Fire, Void and Sunlight.

Ammo uses the supporting outer-ring type until Stone is actually released, including after misses or nonmatching attachments. Clearing the connected outer match removes every support and guarantees Stone becomes disconnected. After release, ammo uses the ordinary remaining-board selector and repeat cap. Stone remains excluded from the visualizer unless a selected sphere contributes its element.

The naturally occurring elements message appears as soon as Genesis Beta begins and points to outer Stone. After Stone releases, the finishing message appears after turn resolution. Continue restores gameplay; modal shielding and dismissal-frame guards remain in effect.

Run `EcoSphere.Editor.TestLevelTools.RunSmoke` with `-tutorialSmoke` for actual-shot checks of ring layout, modal pause/input, repeated supporting ammo after two misses, single-match Stone release, and final dismissal. Preview renders are written under the validation project's Logs directory. Regular gameplay and laser smoke modes bypass interactive messages.

Run `EcoSphere.Editor.SphereCatalogTools.TutorialTwoNavigationSmoke` with both `-tutorialTwoNavigationSmoke -tutorialSmoke` to verify System planet 2, the three-slot preparation gate, a deliberately reordered Sunlight/Fire/Void loadout, the opening natural-element lesson, Stone release and subsequent board-aware ammo. The run logs `ECOSPHERE_TUTORIAL_TWO_NAVIGATION_PASSED` and `ECOSPHERE_TUTORIAL_SMOKE_PASSED`.

Genesis Alpha's setup lessons are authored in its PredefinedWorld.setupTutorial list: required slots, owned collection, selection instructions, then a Begin callout that waits for a full loadout. TutorialPopup provides the shared modal, pointer, input shield, and pause restoration. Continue advances the first three lessons; selection resumes after the third. The final callout accepts Begin directly through a targeted input opening, or Continue dismisses it. WorldProgress stores each world's setup step so returning to setup does not restart completed lessons; development exit resets this progress with the rest of the session. Other worlds have no setup lessons unless assigned in their data. Validated exact copy, targets, pause/input guards, full-loadout trigger, direct Begin launch, portrait/wide renders, setup navigation and menu audio.

StoneTutorialTools.Validate checks 192 seeded layouts and alternate loadout orders for outer Stone placement, controlled supports, guaranteed disconnection after the outer match, and surviving mixed interior.

## Genesis Alpha — discovery followups

After the new-sphere prompt is acknowledged and its collection animation finishes, a paused tutorial says “Excellent work! An excess of Void and Fire on your world has unlocked a new element.” Continue dismisses it and restores the results navigation, with dismissal-frame click-through protection. Returning to System then shows “With 3 elements now in your collection, you have enough for the next level.” with a pointer to Genesis Beta. Continue dismisses that box and restores map selection.

These optional messages and the next-world target ID are authored on PredefinedWorld. Seen flags are stored on CompletedWorld, separately from discovery rewards and history. The results lesson applies only to fresh results with an actual new discovery; the System followup is eligible after that lesson is acknowledged, provided its target is in the current system, unfinished, and selectable with the current collection. Both messages are one-time within progress and reset with development session progress. Revisited results do not repeat either lesson or reward.

FirstTutorialTools.FollowupSmoke with -alphaFollowupSmoke verifies exact opening copy, result-message sequencing, pauses and click-through guards, the Beta pointer, history on revisit, suppression of repeated lessons, and navigation into Beta setup. Portrait and wide renders are saved as Logs/Alpha-results-lesson-*.png and Logs/Alpha-next-world-lesson-*.png in the validation project.
