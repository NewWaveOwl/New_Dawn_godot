using Godot;
using NewDawn.Models;

namespace NewDawn;

public partial class NewGameSetup : Control
{
    private readonly NewGameSetupData _data = new();
    private OptionButton _templates = null!;
    private Label _templateTime = null!;

    public override void _Ready()
    {
        GetWindow().Title = "New Game";
        if (DisplayServer.GetName() != "headless") GetWindow().Mode = Window.ModeEnum.Maximized;
        _data.Notice += (text, title) => GameUi.Message(this, text, title);
        _data.Initialize();
        BindControls();
    }

    private void BindControls()
    {
        _templates = GetNode<OptionButton>("%UnitTemplates");
        _templateTime = GetNode<Label>("%TemplateTime");
        GetNode<Button>("%Home").Pressed += () => {
            GetWindow().Title = "NewDawn";
            GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
        };
        GetNode<CheckBox>("%KillCommander").Toggled += selected => { if (selected) _data.GameModeChecked(true); };
        foreach (var item in _data.Templates) _templates.AddItem(item.DisplayName);
        if (_data.SelectedTemplateIndex >= 0) _templates.Select(_data.SelectedTemplateIndex);
        _templateTime.Text = _data.SelectedTemplateTime;
        GetNode<CheckBox>("%DefaultUnits").Toggled += selected => { if (selected) SetCustom(false); };
        GetNode<CheckBox>("%CustomUnits").Toggled += selected => { if (selected) SetCustom(true); };
        _templates.ItemSelected += index => {
            _data.SelectedTemplateIndex = (int)index;
            _templateTime.Text = _data.SelectedTemplateTime;
        };
        GetNode<Label>("%MapsFolder").Text = "Maps folder: " + Map.GetMapsDirectory();
        var maps = GetNode<ItemList>("%Maps");
        foreach (var item in _data.Maps) maps.AddItem($"{item.DisplayName}    {item.LastWriteTime:yyyy-MM-dd HH:mm}");
        if (_data.SelectedMapIndex >= 0) maps.Select(_data.SelectedMapIndex);
        maps.ItemSelected += index => _data.SelectMap((int)index);
        GetNode<Button>("%Play").Pressed += Play;
    }

    private void SetCustom(bool custom)
    {
        _data.UnitsModeChecked(custom);
        _templates.Disabled = !_data.UseCustomUnits;
        if (_data.SelectedTemplateIndex >= 0) _templates.Select(_data.SelectedTemplateIndex);
        _templateTime.Text = _data.SelectedTemplateTime;
    }

    private void Play()
    {
        Game? game = _data.BuildGame();
        if (game != null) GameSession.Open(GetTree(), game);
    }
}
