# Unit editor port

Run the project in Godot 4.7.2 .NET, then choose **Unit Editor**. Its scene is
`scenes/editors/unit_editor.tscn`. Edit the numeric columns, Armored checkbox,
and Special dropdown, then choose Save to write a CSV. Load replaces the table
with templates from a CSV. Both file dialogs start in `Saves/Units`, relative
to the process working directory. The save suggestion remains `MyUnits.csv`.

## Preserved legacy behavior

- Startup loads `DefaultUnits.csv` if present; if missing or invalid, it creates
  the original 14 fallback rows, in their original order and with original stats.
- Names are read-only. Rows cannot be added or removed. The **Special** column
  selects `Unit.Core`, just as the original XAML does.
- Price updates immediately. Other numeric cells commit on submission, loss of
  focus, or Save. Invalid non-integer edits retain the previous model value.
- The original UnitRow setters clamp HP/Damage to 0–250, Range/Speed to 0–10,
  and Price to zero or greater. The subsequent conversion to Unit retains its
  own original constraints: for example, Damage 0 saves as 1 and Range 10 as 5.
- Hidden FuelConsumption, IsMech, and UnitsPerTile values are retained through
  the original loading and saving operations, including their existing validation.
- Save infers unit type and ability from the name; unknown names become Rebel.
  The original spelling `Engeneer` is unchanged.
- CSV parsing and its 13-column format use the existing SaveAndLoaderHandler.
  Errors keep the original message text. No CSV escaping or parser fixes are added.
- Home always asks Yes/No/Cancel. Cancel stays; No leaves; Yes opens Save and
  then leaves after its result is acknowledged. Canceling that save dialog still
  leaves, matching the legacy handler. The ordinary Save button's cancellation stays.

## Implementation

`src/ui/UnitEditor.cs` replaces WPF controls, bindings, dialogs, and window
navigation with Godot controls, file pickers, and scenes. The table keeps the
original eight column labels, widths, purple panels, and alternating row colors.

The UnitRow class is copied into `src/objects/UnitRow.cs` with its original
namespace, setter bodies, events, and nested core references. Six data methods
are copied without changing their bodies into `src/services/UnitEditorData.cs`:
SaveUnitsToFile, GuessUnitSpecialFromUnitType, LoadUnits, LoadDefaultUnits,
LoadUnitsFromFile, and GuessUnitTypeFromName. The legacy archive is untouched.
The uncalled legacy SaveUnits helper is not wired into this UI; the actual Save
button uses SaveUnitsToFile, as before. No existing helper logic was corrected.

## Verification

Build with `dotnet build NewDawn.csproj`, then run:

```powershell
& '<Godot .NET executable>' --headless --path . --script tests/unit_editor_smoke.gd
```

Checks cover menu navigation, fallback defaults when no default file exists,
read-only names, core choices, CSV loading/saving, original clamping and hidden
fields, unknown names, invalid edits, malformed CSV, save cancellation, and the
Home Yes/No/Cancel paths. The test CSV is created under `.godot/` and removed.
Original model comparison and map-editor checks remain available under `tests/`.
