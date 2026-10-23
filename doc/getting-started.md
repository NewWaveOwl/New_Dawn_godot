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
closes the application. **Map Editor** opens the terrain editor, and
**Unit Editor** opens the unit template editor. **New Game** opens setup;
select a saved map and choose Play to start a two-player match.

The original WPF source remains in `new_dawn_legacy/`. The Godot C# project
compiles only `src/`, keeping WPF code out of the game assembly.
See `AGENTS.md` for preservation and pull request approval rules.

## Folder structure

```text
src/
  config/             Configuration code
  enums/              Enumerations
  objects/            Game objects and models
  states/             Game and interaction states
  services/           Shared services
  ui/                 C# scripts for user interface scenes
scenes/
  menus/              Main menu and other menu scenes
  game/               Gameplay scenes
  editors/            Map and unit editor scenes
assets/
  textures/
    ui/               Interface images and icons
    tiles/            Terrain textures
    units/            Unit textures
    buildings/        Building textures
  fonts/              Font files
  audio/
    music/            Music tracks
    sounds/           Sound effects
lib/                  Manually included third-party libraries
addons/               Godot editor and runtime plugins
doc/                  Documentation for the Godot port
new_dawn_legacy/      Reference archive, excluded from Godot imports
```

Open `scenes/menus/main_menu.tscn` to edit the current menu. Runtime scenes
reference `src/` and `assets/`, never the legacy archive. Existing artwork was
copied without changes into the matching texture folders; original files stay
in the archive for reference. The archive's `.gdignore` prevents Godot from
importing its files or including them as runtime resources.

Keep C# code under `src/`, Godot scenes under `scenes/`, and each asset type in
its matching `assets/` subfolder. Empty planned folders contain `.gitkeep` so
they survive checkout. Use NuGet package references in `NewDawn.csproj` for
package-managed libraries; reserve `lib/` for manually included dependencies
and `addons/` for Godot plugins. No libraries or plugins are added yet.

Store port documentation in [`doc/`](README.md). Original documentation
remains in the legacy archive for reference.

## Map editor

Choose **Map Editor** from the main menu or open
`scenes/editors/map_editor.tscn`. Select a terrain in the palette, then click
or drag with the left mouse button to paint. Right-click paints Mountain.
Use the grid inputs and **Create** to replace the map; **Save** writes the
named map, **Load** loads a selected map or a typed name, and **Home** returns
to the menu. The legacy editor does not prompt to save before Create or Home.

Maps use the unchanged version-5 `.map` format and the legacy working-directory
location `Saves/Maps`. See [map editor port notes](map-editor.md) for
preserved quirks, platform adaptations, and verification instructions.
