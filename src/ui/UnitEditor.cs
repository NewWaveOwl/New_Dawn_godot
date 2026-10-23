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
    private static readonly string[] Headers = { "Unit Name", "Price", "HP", "DMG", "Range", "Speed", "Armored", "Special" };
    private static readonly int[] Widths = { 120, 80, 60, 60, 60, 60, 80, 120 };

    public override void _Ready()
    {
        Theme = new Theme { DefaultFontSize = 12 };
        GetWindow().Title = "Unit Editor";
        if (DisplayServer.GetName() != "headless") GetWindow().Mode = Window.ModeEnum.Maximized;
        _data = new UnitEditorData();
        BuildControls();
        PopulateRows();
    }

    private static PanelContainer Panel(string color, int padding = 0)
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat { BgColor = new Color(color),
            ContentMarginLeft = padding, ContentMarginRight = padding,
            ContentMarginTop = padding, ContentMarginBottom = padding };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static Button MakeButton(string name, string label, Action action, int width = 100, int height = 50)
    {
        var button = new Button { Name = name, Text = label, CustomMinimumSize = new Vector2(width, height) };
        var style = new StyleBoxFlat { BgColor = new Color("2D1B4E"), BorderColor = new Color("888888") };
        style.SetBorderWidthAll(1);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeColorOverride("font_color", new Color("F0E68C"));
        button.Pressed += action;
        return button;
    }

    private void BuildControls()
    {
        var layout = new VBoxContainer { Name = "Layout" };
        AddChild(layout);
        layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        layout.AddThemeConstantOverride("separation", 0);
        var top = Panel("5C39AB", 10);
        top.CustomMinimumSize = new Vector2(0, 50);
        layout.AddChild(top);
        var title = new HBoxContainer();
        top.AddChild(title);
        var heading = new Label { Text = "Unit Editor", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        heading.AddThemeFontSizeOverride("font_size", 20);
        title.AddChild(heading);
        title.AddChild(MakeButton("Home", "Home", AskGoHome, 60, 35));

        var body = Panel("DAA6FF", 20);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(body);
        var tablePanel = Panel("D3CBEA");
        body.AddChild(tablePanel);
        var scroll = new ScrollContainer { Name = "TableScroll" };
        tablePanel.AddChild(scroll);
        var table = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        table.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(table);
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 0);
        table.AddChild(header);
        for (int i = 0; i < Headers.Length; i++) {
            var cell = Panel("E8E8E8", 3);
            cell.CustomMinimumSize = new Vector2(Widths[i], 32);
            cell.AddChild(new Label { Text = Headers[i], Modulate = Colors.Black });
            header.AddChild(cell);
        }
        _rows = new VBoxContainer { Name = "Rows" };
        _rows.AddThemeConstantOverride("separation", 0);
        table.AddChild(_rows);
        var bottom = Panel("46288A", 15);
        bottom.CustomMinimumSize = new Vector2(0, 80);
        layout.AddChild(bottom);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        buttons.AddThemeConstantOverride("separation", 15);
        bottom.AddChild(buttons);
        buttons.AddChild(MakeButton("Load", "Load", OpenLoadDialog));
        buttons.AddChild(MakeButton("Save", "Save", () => OpenSaveDialog(false)));
    }

    private void PopulateRows()
    {
        foreach (Node child in _rows.GetChildren()) { _rows.RemoveChild(child); child.QueueFree(); }
        _commitCells.Clear();
        for (int index = 0; index < _data.Rows.Count; index++) {
            UnitRow row = _data.Rows[index];
            var panel = Panel(index % 2 == 0 ? "FFFFFF" : "F9F9F9");
            panel.Name = $"Row{index}";
            _rows.AddChild(panel);
            var cells = new HBoxContainer { Name = "Cells" };
            cells.AddThemeConstantOverride("separation", 0);
            panel.AddChild(cells);
            cells.AddChild(new Label { Name = "UnitName", Text = row.Name,
                CustomMinimumSize = new Vector2(120, 32), Modulate = Colors.Black,
                ClipText = true, TooltipText = row.Name });
            NumericCell(cells, "Price", 80, () => row.PriceAlloy, value => row.PriceAlloy = value, true);
            NumericCell(cells, "HP", 60, () => row.HP, value => row.HP = value);
            NumericCell(cells, "DMG", 60, () => row.Damage, value => row.Damage = value);
            NumericCell(cells, "Range", 60, () => row.Range, value => row.Range = value);
            NumericCell(cells, "Speed", 60, () => row.Speed, value => row.Speed = value);
            var armored = new CheckBox { Name = "Armored", ButtonPressed = row.Armored,
                CustomMinimumSize = new Vector2(80, 32) };
            cells.AddChild(armored);
            armored.Toggled += value => row.Armored = value;
            var core = new OptionButton { Name = "Special", FitToLongestItem = false,
                CustomMinimumSize = new Vector2(120, 32) };
            foreach (Unit.Core value in row.CoreTypes) core.AddItem(value.ToString(), (int)value);
            core.Select(row.CoreTypes.IndexOf(row.CoreType));
            core.ItemSelected += selected => row.CoreType = (Unit.Core)core.GetItemId((int)selected);
            cells.AddChild(core);
        }
    }

    private void NumericCell(HBoxContainer parent, string name, int width, Func<int> get, Action<int> set, bool immediate = false)
    {
        var entry = new LineEdit { Name = name, Text = get().ToString(),
            CustomMinimumSize = new Vector2(width, 32), ExpandToTextLength = false };
        entry.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = Colors.Transparent });
        entry.AddThemeColorOverride("font_color", Colors.Black);
        parent.AddChild(entry);
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
