# Level preparation prototype

The layout now adapts to portrait and wide windows; see Responsive-layout.md for sizing and validation details.

Clicking a System planet loads LevelPrep directly. System number, planet index and required sphere count are passed through LevelPrepView's temporary session context. Back returns to the same system number. Begin enables only for a complete loadout. The first world in System 1 is named Tutorial 1; after selecting Ice and Sunlight, Begin loads the Tutorial1 gameplay scene. WorldLevels supplies the shared display name and scene mapping for System, prep, and results.

Ice and Sunlight are the only collection entries. Select either to fill the first available slot; duplicates are prevented. Click a filled slot to remove it, compacting the selection. Planets require 2, 3, 4, 4, 4 sphere types, so only the two-slot planet can currently enable Begin. The collection uses a masked ScrollRect and is top-aligned; with only two entries there is no overflow to scroll yet.

SpherePreview renders the existing lit 3D surface textures and stationary icons to transparent 256-square render textures, retaining 12-degree-per-second surface rotation in the collection and filled slots. Preview stages, materials and targets are disposed with their UI entries.

Both world modifiers are displayed unconditionally as visual placeholders. They do not change gameplay. Outfit TMP fonts, heading decorations and the reusable animated button prefab are reused. A single panel-frame sprite is nine-sliced for panels and collection tiles.

LevelPrepTools.Build creates the scene and registers it in build settings. The runtime controller constructs its interface from the serialized art/font/prefab references. Unity compilation and play-mode checks passed for rotating surfaces, selection, duplicate prevention, removal/compaction, Begin gating, System-to-Prep navigation, system/planet context, Back navigation and Tutorial 1 launch behavior. A 900x1600 preview was visually inspected and saved to Logs/LevelPrep-preview.png. Validation ran in work/UnityValidation; the editor's pre-existing SearchDatabase indexing exception did not affect these checks.
