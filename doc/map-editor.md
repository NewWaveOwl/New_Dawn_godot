# Map editor port

The scene is `scenes/editors/map_editor.tscn`; `src/ui/MapEditor.cs` builds its
Godot controls and `src/ui/MapCanvas.cs` handles drawing and mouse input.
Original implementation: `new_dawn_legacy/NewDawn/Views/MapEditorWindow.xaml`
and its C# code-behind.

## Preserved behavior

- The initial map is 12×8 Field tiles, with Field selected in the palette.
- Invalid/non-positive grid inputs use 12 or 8; positive values clamp to
  1–200. Creating a map retains the current palette selection.
- The nine non-None terrain types keep their original order and textures.
- Left-click and drag paint the selected terrain. Release or leaving the
  canvas stops painting. Right-click paints Mountain.
- Flat-top hexes use radius 48 and the original display-column/row offsets.
- Map list selection fills the name; Load prefers the selection over a typed
  name. Entries sort by last write time, newest first.
- Saves use the original version-5 header, tile, building, and unit records.
  Loaded units/buildings remain in the model and are saved again, but the
  editor renders terrain only, like the legacy plain RenderMap method.
- Save converts None terrain to Field. Existing comments mentioning Mountain
  contradict the original implementation; Field behavior is preserved.
- Terrain effects retain their existing state across paints. They are not
  reset or corrected by this port.
- Create and Home discard unsaved changes without a confirmation prompt.
- Storage remains `Saves/Maps` relative to the process working directory.
  Existing files with the same name are overwritten, as in the legacy editor.
  Older save versions remain unsupported. No migration is applied.

## Platform adaptations

WPF controls, image brushes, message boxes, window navigation, and mouse
capture are represented by Godot controls, textured polygons, AcceptDialog,
scene navigation, and input events. The map canvas draws only hexes intersecting
the scroll viewport and uses the same hex polygon for hit testing. This keeps
the original geometry usable at the 200×200 limit without 40,000 UI nodes.

Model dependencies were copied into `src/objects/` and the existing persistence
class into `src/services/`, retaining their namespace, nested enums, and method
bodies. They include Game/Player/Unit/Building dependencies required by the
original persistence class; this does not make gameplay screens available.

Unused WPF imports were removed from the copied models. Tile.ToScreenPosition
now returns a named `(double X, double Y)` tuple instead of a WPF Point, with
the same calculations and double precision. The editor uses the original
MapEditorWindow display formula, which differs from that Tile method.
Nullable warnings are disabled for the copied models without changing their
runtime behavior. The compiler still reports three pre-existing warnings:
CS0472, CS0169, and CS0414. No legacy source fixes are included.

## Verification

Build with `dotnet build NewDawn.csproj`. Run the editor integration checks:

```powershell
& '<Godot .NET executable>' --headless --path . --script tests/map_editor_smoke.gd
```

This checks menu/Home navigation, all terrain icons, click/drag/right-click
painting, release handling, defaults and dimension limits, a 40,000-tile map,
scrolling, version rejection, blank names, and a painted map save/load round trip.
It uses a temporary map under `.godot/` and a uniquely named list fixture under
`Saves/Maps`, removes both afterward, and does not overwrite user saves.

On Windows with the .NET 9 SDK, run the original/ported model comparison:

```powershell
./tests/model-parity/Compare-Models.ps1
```

The same harness is compiled against the untouched WPF models and the ported
models. It compares serialized round trips and error outcomes for all 19 maps
in the original archive, plus transitions through all 10 terrain values.
Three bundled maps are version 5 and load successfully; 16 are older and are
rejected identically. Fixtures and outputs stay under `.godot/model-parity/`.

For a visual check, run the menu, open Map Editor, paint several terrain types,
scroll a large map, save and reload, and return Home. The UI retains the legacy
toolbar and purple three-column layout. Godot fonts, dialogs, and focus visuals
use the engine's native rendering.
