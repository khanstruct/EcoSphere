# Session-only test progress

Persistent saving is disabled during this testing phase. Completed worlds, appearance, discoveries, and the current system live only in WorldProgress's in-memory session data. They survive scene changes, so completed levels show rotating worlds on the System map during the same run.

A new game run or Unity Play session starts with System 1, empty planet slots, and fresh discoveries. RuntimeInitializeOnLoadMethod(SubsystemRegistration) resets progress and the prep/navigation context even when Unity's domain reload is disabled. Previous PlayerPrefs saves are ignored; progress neither reads nor writes disk. A permanent save system will be added later.

AutoSaveTools.WriteCheck tests completion, appearance, navigation, and one-time discovery within a session. AutoSaveTools.ReadCheck runs in a separate Unity process and verifies that no previous progress returns and Stone can be discovered again. Results smoke checks verify that scene-to-scene progress and completed-world previews still work.