# System screen corner buttons

The System screen has icon buttons pinned to its corners: **Settings** (top left, `Art/UI/settings-icon.png`) and **Collection** (top right, `Art/UI/collection-icon.png`). Both source icons are 256 x 256.

Everything is data in `Resources/SystemMenuSettings.asset` (`SystemMenuSettings`): button size (110 layout units), safe-area margin, spacing for buttons sharing a corner, tint colors, and the entry list (id, label, icon, corner). `SystemCornerButtons` builds the buttons at `SystemView.Awake`, inside the safe layout, and attaches the shared `ButtonClickSound`. To add another corner button, add an entry.

The destination screens don't exist yet: a click plays the button sound and raises `SystemCornerButtons.Selected` with the entry id (`settings` or `collection`) for those screens to hook into.

Validation: `DevelopmentTools.Smoke` with `-systemMenuSmoke` checks that the buttons come from the settings, sit in their corners inside the safe area, receive pointer raycasts, and that real Input System mouse clicks play the click sound exactly once and report the right id without opening a planet. Renders: the validation project's `Logs/SystemMenu-portrait.png` and `Logs/SystemMenu-wide.png`.
