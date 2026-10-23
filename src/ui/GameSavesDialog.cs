using Godot;
using NewDawn.Models;
using System;
using System.IO;

namespace NewDawn;

/// <summary>Godot equivalent of the modal SavesWindow used by MainGameWindow.</summary>
public partial class GameSavesDialog : Window
{
    public event Action<Game>? GameLoaded;
    private ItemList _saves = null!;
    private string[] _files = Array.Empty<string>();

    public GameSavesDialog()
    {
        Name = "GameSaves";
        Title = "Game Saves";
        Exclusive = true;
        Transient = true;
    }

    public override void _Ready()
    {
        var panel = GameUi.Panel("423254", 12);
        AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var layout = new VBoxContainer();
        panel.AddChild(layout);
        layout.AddChild(GameUi.Label("Name                                           Modified", 16));
        _saves = new ItemList { Name = "Saves", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _saves.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("DFD1EA") });
        _saves.AddThemeColorOverride("font_color", Colors.Black);
        layout.AddChild(_saves);
        string dir = Path.Combine(System.Environment.CurrentDirectory, "Saves", "GameSaves");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _files = Directory.GetFiles(dir, "*" + SaveAndLoaderHandler.gameSaveExtention, SearchOption.TopDirectoryOnly);
        Array.Sort(_files, StringComparer.OrdinalIgnoreCase);
        foreach (string file in _files) {
            var info = new FileInfo(file);
            _saves.AddItem($"{Path.GetFileNameWithoutExtension(info.Name)}    {info.LastWriteTime:yyyy-MM-dd HH:mm}");
        }
        _saves.ItemActivated += _ => LoadSelected();
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        layout.AddChild(buttons);
        buttons.AddChild(GameUi.Button("LoadSave", "Load", LoadSelected));
        buttons.AddChild(GameUi.Button("CloseSaves", "Close", QueueFree));
        CloseRequested += QueueFree;
    }

    public void LoadSelected()
    {
        int[] selected = _saves.GetSelectedItems();
        if (selected.Length == 0) { GameUi.Message(this, "Select a save first.", "Load"); return; }
        try {
            string nameWithout = Path.GetFileNameWithoutExtension(_files[selected[0]]);
            Game game = SaveAndLoaderHandler.GameLoad(nameWithout);
            Hide();
            GameLoaded?.Invoke(game);
            QueueFree();
        }
        catch (Exception ex) { GameUi.Message(this, "Failed to load save.\n" + ex.Message, "Error"); }
    }
}
