# Editable UI scenes

Static UI is stored in `.tscn` files so Godot's 2D editor shows the layout before
running the game. Open a screen under `scenes/menus/`, `scenes/editors/`, or
`scenes/game/`, then expand its scene tree to select panels, labels, and buttons.
The saved-game window lives in `scenes/dialogs/game_saves.tscn`.

- Edit positioning, sizes, colors, text, and container spacing in the scenes.
- C# binds saved controls using scene-unique names (`%Shop`, `%Rows`, etc.). Keep
  those names and their **Unique Name** setting when moving controls. If a name
  changes, update the corresponding C# binding.
- `unit_editor_row.tscn` defines a reusable table row. C# instantiates it for each
  unit and binds the original values and edit handlers.
- Map drawing, terrain icons, available files, shop entries, and unit values are
  populated at runtime. They are not saved as sample game data in the scenes.
- Godot's built-in file pickers, confirmation dialogs, and messages are created
  on demand in C#; their content depends on the current action.

The migration moves presentation only. Gameplay rules, persistence, defaults,
legacy quirks, and the reference source are unchanged. Running gameplay directly
with F6 still shows the instruction to start a match from New Game; use F5 and
start a match to test the live board.

Validation: `tests/ui_scene_layout_smoke.gd` checks controls exist in saved scenes
before `_Ready` and that native layouts work without their root C# scripts.
Existing map editor, unit editor, and gameplay smoke checks cover the connected
actions, editing, navigation, deployment, turns, and saves.
