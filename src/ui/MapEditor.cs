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
        BindControls();
        BuildPalette();
        RefreshMapList();
        CreateNewMapFromUi();
    }

    private void BindControls()
    {
        _mapName = GetNode<LineEdit>("%MapName");
        _gridW = GetNode<LineEdit>("%GridWidth");
        _gridH = GetNode<LineEdit>("%GridHeight");
        _maps = GetNode<ItemList>("%MapList");
        _palette = GetNode<ItemList>("%Palette");
        _canvas = GetNode<MapCanvas>("%MapCanvas");
        GetNode<Button>("%Save").Pressed += SaveMap;
        GetNode<Button>("%Load").Pressed += LoadMap;
        GetNode<Button>("%Create").Pressed += CreateNewMapFromUi;
        GetNode<Button>("%Home").Pressed += () => {
            GetWindow().Title = "NewDawn";
            GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
        };
        _maps.ItemSelected += index => _mapName.Text = Path.GetFileNameWithoutExtension(_mapFiles[index]);
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
