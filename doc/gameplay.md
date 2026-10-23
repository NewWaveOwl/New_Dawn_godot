# New Game setup and gameplay

Run with Godot 4.7.2 .NET and the .NET 9 SDK. Choose **New Game** from the main
menu, select a saved map, select default or custom unit templates, and press
**Play**. If no maps exist, create and save one in Map Editor first.

The setup scene is `scenes/menus/new_game_setup.tscn`. Gameplay is
`scenes/game/main_game.tscn`; start it through setup so it receives an initialized
C# Game object. Running the gameplay scene alone shows a main-menu shortcut.

## Controls

- Select a unit in the shop, then click a highlighted tile to deploy it near
  a Commander or Hub. Buildings require a nearby Engineer and valid terrain.
- Click a map unit, choose **Move**, **Attack**, or **Special**, then click a
  highlighted target. **Cancel** cancels a mode or deselects the unit.
- **Next turn** switches players and applies the original income, refresh,
  cleanup, and cooldown rules.
- **Save** writes `Saves/GameSaves/autosave_game<yyyy_MM_dd_HH_mm_ss>.sagm`.
  **Load** opens the original alphabetically sorted save list; select a save
  and press Load or double-click it. Close returns to the current match.
- **Home** attempts `autosave_current.sagm`, then returns to the menu. Its
  autosave errors are silently ignored, as in the legacy handler.

Saves and inputs retain the legacy working-directory locations: `Saves/Maps`,
`Saves/Units`, and `Saves/GameSaves`. Formats remain unchanged. No sample maps
or unit templates are automatically installed or substituted for user files.

## Preservation and adaptations

`src/states/GameplayController.cs` copies the MainGameWindow handlers, template
defaults, target rules, costs, and ability paths. UI references are replaced by
shop/text state, notifications, and navigation events. `NewGameSetupData.cs`
retains setup enumeration, selection, loading, and initialization behavior.
Existing Game, Player, Map, Unit, Building, and persistence classes are unchanged.
The original archive remains unchanged.

Godot controls replace WPF controls and windows. `GameSession` transfers the
same Game instance into the gameplay scene. `GameSavesDialog` replaces the
modal SavesWindow. `GameMapCanvas` reuses the editor's flat-top geometry,
adds the original fill/stroke priorities, and draws buildings below units.
Asset paths map to `assets/textures/`; missing unit textures retain the short
text-label fallback. Shop building instructions remain visible below the list
and in the divider tooltip because Godot ItemList does not support multiline
item text. Existing map-editor painting remains the default canvas behavior;
gameplay uses click selection and cannot paint terrain.

## Known legacy behavior retained

These are observations of the existing implementation, not fixes introduced
by the port:

- Setup has only a Kill Commander selector; the Contest explanation is visible,
  but there is no Contest selector. The original checked handler unconditionally
  assigns Contest, and the selected mode is never applied to Game.
- File lists load once. They do not refresh while setup stays open. Switching
  to Custom after initialization can retain the DefaultUnits basename until
  a different dropdown item is selected. Fallback unit templates remain available
  in gameplay when the selected template cannot be loaded.
- Initial resource labels stay blank until the first original SyncTopBar call.
  The turn header remains `Turn:` without a counter. Loaded Player 2 games can
  initially display Player 1 until the next sync. An empty template CSV exits
  the original loaded handler before map rendering.
- Successful building placement deducts the price twice. The first deduction
  also happens before the placement attempt. No cost fixes are included.
- Special clicks use Unit.UseSpecial directly. The separate, unused window
  helper is preserved without wiring it into the click path. Wasp/Breacher target
  mismatches, incomplete CMP/HoverTransporter actions, mine placeholders,
  shield/aura effects on nearby opponents, and uncharged Redoubt jump fuel remain.
- The save format does not serialize cooldown, has-acted, or fortify flags.
  Those fields reset on load. The game-state enum is not updated with each
  player switch, even though CurrentPlayer is switched and saved correctly.
- Commander destruction marks GameOver before removing that commander from
  its owner's list. GetWinner may then return null; the next-turn victory dialog
  uses the original Player 2 fallback. No new game-over input lock is added.

Changes to any of these behaviors require explicit author approval.

## Checks

```powershell
dotnet build NewDawn.csproj
dotnet run --project tests/gameplay/gameplay.csproj -- .
& '<Godot .NET executable>' --headless --path . --script tests/gameplay_smoke.gd
```

The standalone checks run in a unique `.godot/gameplay-checks/` directory.
They cover setup defaults and failures, all purchasable templates, deployment,
movement, ownership, combat, buildings, turns, all 12 targeted special paths,
save/load behavior, and game-over quirks.

The Godot scene check creates uniquely named map/template fixtures, opens
setup from the main menu, starts a custom-template match, clicks to deploy,
switches turns, saves, and loads through the save-list dialog. It removes its
fixtures afterward. It avoids replacing an existing timestamped save or Home
autosave; Home is also tested separately in the isolated standalone checks.
Running without `--headless` additionally captures setup/gameplay previews
under `.godot/`. Existing map and unit editor checks remain under `tests/`.
