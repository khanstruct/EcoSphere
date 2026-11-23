# EcoSphere coding instructions

These are standing user requirements for all work in this repository.

## Architecture

- Always make code as modular, reusable, and data-driven as reasonably possible. Avoid hard coding whenever possible.
- Keep each fact in one authoritative place. System, Setup, Gameplay, Results, and previews must consume the same world, sphere, element, modifier, scoring, and progress definitions rather than maintaining independent copies.
- Treat worlds as data: names, sphere requirements, modifiers, thresholds, natural discoveries, and other traits belong in shared definitions/records. Scene code reads those traits; it must not infer them from a world number, scene name, or specific sphere ID when a general rule can represent them.
- Implement discoveries, ownership checks, rewards, scoring, and modifier behavior through shared mechanisms. Do not add a dedicated flag or reward branch for an individual sphere or level when the same concept applies to others.
- Use classes, composition, inheritance, and interfaces where they clarify responsibilities or support actual reuse. Avoid unnecessary abstractions and speculative frameworks.
- Intentionally authored tutorial layouts and messages may be unique content. Their reusable mechanics, eligibility checks, and rewards must still use shared systems.
- Development mode must use consistent ownership/availability semantics across collection, selection, discoveries, and rewards. Do not let independent checks disagree about whether a sphere is already owned.
- When adding a feature, inspect its existing equivalents and extend the shared architecture. If a touched path contains a relevant special-case shortcut, address it within the task or clearly identify it as remaining work; do not describe it as fully generalized.
- Verify behavior through the actual integration path. For UI input, testing onClick directly is insufficient to establish that pointer input works.

## Assets

- No runtime sprite or texture may exceed 256 pixels on its longest dimension, unless the user explicitly authorizes an exception.

