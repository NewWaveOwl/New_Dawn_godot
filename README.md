# New Dawn — Godot port

Original authors: Artiom Sova and Matthew Buzzetti.

## Run the main menu

Use **Godot 4.7.2 .NET** and the **.NET 9 SDK**. The standard Godot editor
(including the Steam build) cannot run the C# menu script.

1. Import `project.godot` in the .NET editor.
2. Build the C# project, then run the project (F6 runs only the selected scene;
   F5 runs the configured main menu).

The menu reuses the legacy title image, button labels, positions, colors, and
20% black overlay. It starts maximized. Its 1920×1080 reference layout scales
with the window, using letterboxing to keep every button visible.

**How to Play** shows the original help text without corrections, and **Exit**
closes the application. With the author's explicit approval, **New Game**,
**Map Editor**, and **Unit Editor** temporarily display “This screen has not
been ported yet.” Their destination screens are future ports.

The original WPF source remains in `new_dawn_legacy/`. The Godot C# project
compiles only `scripts/`, keeping WPF code out of the game assembly.
See `AGENTS.md` for preservation and pull request approval rules.
