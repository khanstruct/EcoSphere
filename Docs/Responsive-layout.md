# Responsive presentation

System, Level Prep and First Light share safe-area sizing through ScreenLayout. Layouts switch at a safe-area aspect ratio of 1.2. Portrait uses a minimum logical 900x1600 canvas; wide uses 1600x900. The remaining dimension grows to fill the available window. Resolution, orientation and safe-area changes trigger reflow. Menu backgrounds cover the screen without stretching their starfield textures.

System reparents its interactive content into a safe-area container at runtime. Orbits expand across its width, the sun uses a smaller landscape presentation, and heading/navigation keep their edge anchoring.

Level Prep expands its collection grid into additional columns as space permits. Portrait keeps stacked loadout slots and world conditions below both panels. Wide layouts use a two-column loadout grid, with world conditions beneath it on the right. Selection, rotating preview textures, Begin gating and Back navigation are unchanged.

Gameplay fits a canonical 9.3 x 16.533333 world rectangle (9:16) uniformly inside the safe area. Its fixed walls, cannon, core, sphere radii, collision rules and projectile speed are unchanged. The single camera exposes only decorative space outside that rectangle; no extra playable area is added. Pointer projection uses this camera and shots can originate only from pointer presses inside the fitted rectangle. Both mouse and primary-touch input are supported. In landscape, status/instructions move left and resource meters move right; portrait retains the compact HUD.

Validation: Unity 6000.6.7f1 compiled successfully. Nine renders were generated across 900x1600, 1200x1600 and 1600x900 for System, LevelPrep and FirstLight. The renders were visually inspected and saved under Logs/Responsive. Automated checks covered safe-area fitting, fixed playfield pixel proportions, world/screen pointer round trips, existing board and bank-shot behavior, scoring, loss, result audio, prep selection and navigation. Physical-device touch/notch behavior has not been tested. The validation editor logged its pre-existing SearchDatabase indexing exception; the checks completed successfully.

Run EcoSphere.Editor.ResponsiveTools.Smoke with -responsiveSmoke in the validation project to regenerate the preview set. Other existing gameplay and prep smoke routines remain available.
