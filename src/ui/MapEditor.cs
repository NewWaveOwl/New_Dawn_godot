using Godot;
using NewDawn.Models;
using System;
using System.IO;
using System.Linq;

namespace NewDawn;

/// <summary>Godot controls for the original MapEditorWindow commands.</summary>
public partial class MapEditor : Control
{
    public Map? CurrentMap { get; private set; }
    private Tile.TerrainType _selectedTerrain = Tile.TerrainType.Field;
    private LineEdit _mapName = null!, _gridW = null!, _gridH = null!;
    private ItemList _maps = null!, _palette = null!;
    private MapCanvas _canvas = null!;
    private string[] _mapFiles = Array.Empty<string>();

    public override void _Ready()
    {
        GetWindow().Title = "Map Editor";
        if (!DisplayServer.GetName().Equals("headless"))
            GetWindow().Mode = Window.ModeEnum.Maximized;
        BuildControls();
        BuildPalette();
        RefreshMapList();
        CreateNewMapFromUi();
    }

    private static PanelContainer Panel(string color)
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat { BgColor = new Color(color) };
        style.ContentMarginLeft = style.ContentMarginRight = 10;
        style.ContentMarginTop = style.ContentMarginBottom = 10;
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static void Button(HBoxContainer parent, string name, string text, Action action, int width = 80)
    {
        var button = new Godot.Button { Name = name, Text = text, CustomMinimumSize = new Vector2(width, 40) };
        var style = new StyleBoxFlat { BgColor = new Color("2D1B4E"), BorderColor = new Color("707070") };
        style.SetBorderWidthAll(1);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeColorOverride("font_color", new Color("F0E68C"));
        parent.AddChild(button);
        button.Pressed += action;
    }

    private static LineEdit Entry(HBoxContainer parent, string name, string text, int width)
    {
        var entry = new LineEdit { Name = name, Text = text, CustomMinimumSize = new Vector2(width, 0) };
        parent.AddChild(entry);
        return entry;
    }

    private void BuildControls()
    {
        var column = new VBoxContainer { Name = "Layout" };
        column.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 0);
        AddChild(column);
        var toolbarPanel = Panel("5C39AB");
        toolbarPanel.CustomMinimumSize = new Vector2(0, 70);
        column.AddChild(toolbarPanel);
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        toolbarPanel.AddChild(toolbar);
        Button(toolbar, "Save", "Save", SaveMap);
        Button(toolbar, "Load", "Load", LoadMap);
        Button(toolbar, "Home", "Home", () => {
            GetWindow().Title = "NewDawn";
            GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
        });
        toolbar.AddChild(new Control { CustomMinimumSize = new Vector2(17, 0) });
        toolbar.AddChild(new Label { Text = "Map name:" });
        _mapName = Entry(toolbar, "MapName", "", 180);
        toolbar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        toolbar.AddChild(new Label { Text = "Grid size:" });
        _gridW = Entry(toolbar, "GridWidth", "12", 45);
        toolbar.AddChild(new Label { Text = "x" });
        _gridH = Entry(toolbar, "GridHeight", "8", 45);
        Button(toolbar, "Create", "Create", CreateNewMapFromUi, 90);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 0);
        column.AddChild(body);
        var left = Panel("DAA6FF");
        left.CustomMinimumSize = new Vector2(240, 0);
        body.AddChild(left);
        var mapsColumn = new VBoxContainer();
        left.AddChild(mapsColumn);
        mapsColumn.AddChild(new Label { Text = "Map list", Modulate = Colors.Black });
        _maps = new ItemList { Name = "MapList", SizeFlagsVertical = SizeFlags.ExpandFill };
        _maps.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("B0A2E6") });
        _maps.AddThemeColorOverride("font_color", Colors.Black);
        mapsColumn.AddChild(_maps);
        _maps.ItemSelected += index => _mapName.Text = Path.GetFileNameWithoutExtension(_mapFiles[index]);

        var center = Panel("614596");
        center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(center);
        var scroll = new ScrollContainer { Name = "MapScroll" };
        center.AddChild(scroll);
        _canvas = new MapCanvas { Name = "MapCanvas" };
        scroll.AddChild(_canvas);

        var right = Panel("A34ECC");
        right.CustomMinimumSize = new Vector2(200, 0);
        body.AddChild(right);
        var paletteColumn = new VBoxContainer();
        right.AddChild(paletteColumn);
        paletteColumn.AddChild(new Label { Text = "Palette", Modulate = Colors.Black });
        _palette = new ItemList { Name = "Palette", SizeFlagsVertical = SizeFlags.ExpandFill,
            FixedIconSize = new Vector2I(24, 24) };
        _palette.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("BCB3E0") });
        _palette.AddThemeColorOverride("font_color", Colors.Black);
        paletteColumn.AddChild(_palette);
        _palette.ItemSelected += index => {
            _selectedTerrain = (Tile.TerrainType)(int)_palette.GetItemMetadata((int)index);
            _canvas.SelectedTerrain = _selectedTerrain;
        };
    }

    private void BuildPalette()
    {
        foreach (Tile.TerrainType terrain in Enum.GetValues(typeof(Tile.TerrainType)))
        {
            if (terrain == Tile.TerrainType.None) continue;
            int index = _palette.AddItem(terrain.ToString(), MapCanvas.TextureFor(terrain));
            _palette.SetItemMetadata(index, (int)terrain);
            if (terrain == Tile.TerrainType.Field) _palette.Select(index);
        }
    }

    public void CreateNewMapFromUi()
    {
        if (!int.TryParse(_gridW.Text, out int w) || w <= 0) w = 12;
        if (!int.TryParse(_gridH.Text, out int h) || h <= 0) h = 8;
        w = Clamp(w, 1, 200);
        h = Clamp(h, 1, 200);
        CurrentMap = new Map(w, h);
        foreach (Tile tile in CurrentMap.GetAllTiles()) tile.SetTerrain(Tile.TerrainType.Field);
        _canvas.SetMap(CurrentMap);
    }

    public void SaveMap()
    {
        if (CurrentMap == null) { Message("No map is currently loaded/created."); return; }
        string name = (_mapName.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) { Message("Please enter a map name."); return; }
        foreach (Tile tile in CurrentMap.GetAllTiles())
            if (tile.TileTerrainType == Tile.TerrainType.None) tile.SetTerrain(Tile.TerrainType.Field);
        try { SaveAndLoaderHandler.SaveMap(CurrentMap, name); RefreshMapList(); }
        catch (Exception ex) { Message($"Failed to save map.\n\n{ex.Message}"); }
    }

    public void LoadMap()
    {
        int[] selected = _maps.GetSelectedItems();
        string? selectedFileName = selected.Length > 0 ? _mapFiles[selected[0]] : null;
        string typed = (_mapName.Text ?? string.Empty).Trim();
        string filename = selectedFileName ?? typed;
        if (string.IsNullOrWhiteSpace(filename)) {
            Message("Select a map from the list (or type a map name) before loading."); return;
        }
        try {
            CurrentMap = SaveAndLoaderHandler.LoadMap(filename);
            _gridW.Text = CurrentMap.Width.ToString();
            _gridH.Text = CurrentMap.Height.ToString();
            _canvas.SetMap(CurrentMap);
        }
        catch (Exception ex) { Message($"Failed to load map.\n\n{ex.Message}"); }
    }

    public void RefreshMapList()
    {
        _maps.Clear();
        string dir = Map.GetMapsDirectory();
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _mapFiles = Directory.GetFiles(dir, "*.map", SearchOption.TopDirectoryOnly);
        Array.Sort(_mapFiles, CompareFilesByLastWriteTimeUtcDesc);
        foreach (string file in _mapFiles) {
            var info = new FileInfo(file);
            _maps.AddItem($"{Path.GetFileNameWithoutExtension(info.Name)}\nLast save: {info.LastWriteTime:dd.MM.yy HH:mm}");
        }
    }

    public static int CompareFilesByLastWriteTimeUtcDesc(string a, string b)
    {
        DateTime ta = File.GetLastWriteTimeUtc(a);
        DateTime tb = File.GetLastWriteTimeUtc(b);
        return tb.CompareTo(ta);
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private void Message(string text)
    {
        var dialog = new AcceptDialog { Title = "Map Editor", DialogText = text, Exclusive = true, Transient = true };
        AddChild(dialog);
        dialog.Confirmed += dialog.QueueFree;
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered();
    }
}
