using Godot;
using NewDawn.Models;
using NewDawn.Views;
using System;
using System.Collections.Generic;
using System.IO;

namespace NewDawn;

/// <summary>Godot presentation adapter for the legacy UnitEditorWindow.</summary>
public partial class UnitEditor : Control
{
    private UnitEditorData _data = null!;
    private VBoxContainer _rows = null!;
    private readonly List<Action> _commitCells = new();

    public override void _Ready()
    {
        GetWindow().Title = "Unit Editor";
        if (DisplayServer.GetName() != "headless") GetWindow().Mode = Window.ModeEnum.Maximized;
        _data = new UnitEditorData();
        BindControls();
        PopulateRows();
    }

    private void BindControls()
    {
        _rows = GetNode<VBoxContainer>("%Rows");
        GetNode<Button>("%Home").Pressed += AskGoHome;
        GetNode<Button>("%Load").Pressed += OpenLoadDialog;
        GetNode<Button>("%Save").Pressed += () => OpenSaveDialog(false);
    }

    private void PopulateRows()
    {
        foreach (Node child in _rows.GetChildren()) { _rows.RemoveChild(child); child.QueueFree(); }
        _commitCells.Clear();
        for (int index = 0; index < _data.Rows.Count; index++) {
            UnitRow row = _data.Rows[index];
            var panel = GD.Load<PackedScene>("res://scenes/editors/unit_editor_row.tscn").Instantiate<PanelContainer>();
            panel.Name = $"Row{index}";
            var stripe = (StyleBoxFlat)panel.GetThemeStylebox("panel").Duplicate();
            stripe.BgColor = new Color(index % 2 == 0 ? "FFFFFF" : "F9F9F9");
            panel.AddThemeStyleboxOverride("panel", stripe);
            _rows.AddChild(panel);
            var cells = panel.GetNode<HBoxContainer>("Cells");
            var unitName = cells.GetNode<Label>("UnitName");
            unitName.Text = row.Name;
            unitName.TooltipText = row.Name;
            NumericCell(cells, "Price", () => row.PriceAlloy, value => row.PriceAlloy = value, true);
            NumericCell(cells, "HP", () => row.HP, value => row.HP = value);
            NumericCell(cells, "DMG", () => row.Damage, value => row.Damage = value);
            NumericCell(cells, "Range", () => row.Range, value => row.Range = value);
            NumericCell(cells, "Speed", () => row.Speed, value => row.Speed = value);
            var armored = cells.GetNode<CheckBox>("Armored");
            armored.ButtonPressed = row.Armored;
            armored.Toggled += value => row.Armored = value;
            var core = cells.GetNode<OptionButton>("Special");
            foreach (Unit.Core value in row.CoreTypes) core.AddItem(value.ToString(), (int)value);
            core.Select(row.CoreTypes.IndexOf(row.CoreType));
            core.ItemSelected += selected => row.CoreType = (Unit.Core)core.GetItemId((int)selected);
        }
    }

    private void NumericCell(HBoxContainer parent, string name, Func<int> get, Action<int> set, bool immediate = false)
    {
        var entry = parent.GetNode<LineEdit>(name);
        entry.Text = get().ToString();
        void Commit() {
            if (int.TryParse(entry.Text, out int value)) {
                set(value);
                entry.Text = get().ToString();
                entry.RemoveThemeColorOverride("font_color");
                entry.AddThemeColorOverride("font_color", Colors.Black);
            }
            else entry.AddThemeColorOverride("font_color", Colors.Red);
        }
        entry.FocusExited += Commit;
        entry.TextSubmitted += _ => Commit();
        if (immediate) entry.TextChanged += text => { if (int.TryParse(text, out int value)) set(value); };
        _commitCells.Add(Commit);
    }

    private FileDialog FilePicker(string name, string title, FileDialog.FileModeEnum mode)
    {
        string dir = SaveAndLoaderHandler.GetUnitTemplatesDirectoryPath();
        Directory.CreateDirectory(dir);
        var picker = new FileDialog { Name = name,
            Access = FileDialog.AccessEnum.Filesystem, FileMode = mode,
            Filters = new[] { "*.csv ; CSV File" }, CurrentDir = dir,
            Exclusive = true, Transient = true, Title = title };
        AddChild(picker);
        return picker;
    }

    private void OpenSaveDialog(bool goHomeAfter)
    {
        foreach (Action commit in _commitCells) commit();
        var picker = FilePicker("SaveFile", "Save", FileDialog.FileModeEnum.SaveFile);
        picker.CurrentFile = "MyUnits.csv";
        picker.FileSelected += path => {
            picker.Hide();
            picker.QueueFree();
            try { _data.Save(path); Message("Units saved successfully!", goHomeAfter ? GoHome : null); }
            catch (Exception ex) { Message($"Failed to save: {ex.Message}", goHomeAfter ? GoHome : null); }
        };
        picker.Canceled += () => { picker.QueueFree(); if (goHomeAfter) GoHome(); };
        picker.PopupCentered(new Vector2I(900, 600));
    }

    private void OpenLoadDialog()
    {
        var picker = FilePicker("LoadFile", "Load Unit Templates", FileDialog.FileModeEnum.OpenFile);
        picker.FileSelected += path => {
            picker.Hide();
            picker.QueueFree();
            try { _data.Load(path); PopulateRows(); Message("Units loaded successfully!"); }
            catch (Exception ex) { Message($"Failed to load units.\n\n{ex.Message}"); }
        };
        picker.Canceled += picker.QueueFree;
        picker.PopupCentered(new Vector2I(900, 600));
    }

    private void AskGoHome()
    {
        var dialog = new ConfirmationDialog { Name = "HomePrompt", Title = "Unit Editor",
            DialogText = "Do you want to save changes before exiting?", OkButtonText = "Yes",
            CancelButtonText = "Cancel", Exclusive = true, Transient = true };
        AddChild(dialog);
        dialog.AddButton("No", false, "no");
        dialog.Confirmed += () => { dialog.Hide(); dialog.QueueFree(); OpenSaveDialog(true); };
        dialog.CustomAction += _ => { dialog.QueueFree(); GoHome(); };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered();
    }

    private void Message(string text, Action? after = null)
    {
        var dialog = new AcceptDialog { Name = "Message", DialogText = text, Exclusive = true, Transient = true };
        AddChild(dialog);
        void Close() { dialog.QueueFree(); after?.Invoke(); }
        dialog.Confirmed += Close;
        dialog.Canceled += Close;
        dialog.PopupCentered();
    }

    private void GoHome()
    {
        GetWindow().Title = "NewDawn";
        GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
    }
}
