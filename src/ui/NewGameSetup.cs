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
        BuildControls();
    }

    private void BuildControls()
    {
        var root = GameUi.Panel("504D8F", 0);
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        root.AddChild(column);
        var header = GameUi.Panel("413153", 16);
        header.CustomMinimumSize = new Vector2(0, 93);
        column.AddChild(header);
        var title = new HBoxContainer();
        header.AddChild(title);
        var heading = GameUi.Label("New Game", 36);
        heading.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.AddChild(heading);
        title.AddChild(GameUi.Button("Home", "Home", () => {
            GetWindow().Title = "NewDawn";
            GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
        }, 120));

        var body = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        foreach (string side in new[] { "left", "right", "top", "bottom" }) body.AddThemeConstantOverride("margin_" + side, 16);
        column.AddChild(body);
        var split = new HBoxContainer();
        split.AddThemeConstantOverride("separation", 17);
        body.AddChild(split);
        var left = GameUi.Panel("F7FFF0", 12);
        left.CustomMinimumSize = new Vector2(400, 0);
        split.AddChild(left);
        var options = new VBoxContainer();
        options.AddThemeConstantOverride("separation", 12);
        left.AddChild(options);
        options.AddChild(GameUi.Label("Game Mode", 30, "000000"));
        var commander = new CheckBox { Name = "KillCommander", Text = "Kill Commander", ButtonPressed = true };
        commander.AddThemeFontSizeOverride("font_size", 24);
        commander.AddThemeColorOverride("font_color", Colors.Black);
        commander.AddThemeColorOverride("font_pressed_color", Colors.Black);
        commander.AddThemeColorOverride("font_hover_pressed_color", Colors.Black);
        commander.AddThemeColorOverride("font_hover_color", Colors.Black);
        // A one-item radio group matches the legacy screen: no Contest selector exists.
        commander.ButtonGroup = new ButtonGroup();
        commander.Toggled += selected => { if (selected) _data.GameModeChecked(true); };
        options.AddChild(commander);
        var explanation = GameUi.Label("Win if you destroy the opponent commander unit.", 18, "666666");
        explanation.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        options.AddChild(explanation);
        var contest = GameUi.Label("Contest flags to get points; first to 100 wins.", 18, "666666");
        contest.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        options.AddChild(contest);
        options.AddChild(new HSeparator());
        options.AddChild(GameUi.Label("Units", 30, "000000"));
        var unitGroup = new ButtonGroup();
        var defaults = new CheckBox { Name = "DefaultUnits", Text = "Default Units (DefaultUnits.csv)", ButtonGroup = unitGroup, ButtonPressed = true };
        var custom = new CheckBox { Name = "CustomUnits", Text = "Custom Units (from Saves/Units)", ButtonGroup = unitGroup };
        foreach (var button in new[] { defaults, custom }) {
            button.AddThemeFontSizeOverride("font_size", 20);
            button.AddThemeColorOverride("font_color", Colors.Black);
            button.AddThemeColorOverride("font_pressed_color", Colors.Black);
            button.AddThemeColorOverride("font_hover_pressed_color", Colors.Black);
            button.AddThemeColorOverride("font_hover_color", Colors.Black);
            options.AddChild(button);
        }
        _templates = new OptionButton { Name = "UnitTemplates", Disabled = true, FitToLongestItem = false,
            CustomMinimumSize = new Vector2(0, 34) };
        options.AddChild(_templates);
        _templates.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = Colors.White });
        _templates.AddThemeColorOverride("font_color", Colors.Black);
        _templates.AddThemeColorOverride("font_pressed_color", Colors.Black);
        _templates.AddThemeColorOverride("font_hover_pressed_color", Colors.Black);
        _templates.AddThemeColorOverride("font_hover_color", Colors.Black);
        _templates.AddThemeColorOverride("font_disabled_color", new Color("666666"));
        foreach (var item in _data.Templates) _templates.AddItem(item.DisplayName);
        if (_data.SelectedTemplateIndex >= 0) _templates.Select(_data.SelectedTemplateIndex);
        _templateTime = GameUi.Label(_data.SelectedTemplateTime, 16, "666666", "TemplateTime");
        options.AddChild(_templateTime);
        defaults.Toggled += selected => { if (selected) SetCustom(false); };
        custom.Toggled += selected => { if (selected) SetCustom(true); };
        _templates.ItemSelected += index => {
            _data.SelectedTemplateIndex = (int)index;
            _templateTime.Text = _data.SelectedTemplateTime;
        };

        var right = GameUi.Panel("F7FFF0", 12);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        split.AddChild(right);
        var mapsColumn = new VBoxContainer();
        right.AddChild(mapsColumn);
        mapsColumn.AddChild(GameUi.Label("Select Map", 30, "000000"));
        var mapsFolder = GameUi.Label("Maps folder: " + Map.GetMapsDirectory(), 16, "666666", "MapsFolder");
        mapsFolder.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        mapsColumn.AddChild(mapsFolder);
        var maps = new ItemList { Name = "Maps", SizeFlagsVertical = SizeFlags.ExpandFill };
        maps.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.White });
        maps.AddThemeColorOverride("font_color", Colors.Black);
        maps.AddThemeColorOverride("font_selected_color", Colors.Black);
        maps.AddThemeStyleboxOverride("selected", new StyleBoxFlat { BgColor = new Color("D3CBEA") });
        maps.AddThemeStyleboxOverride("selected_focus", new StyleBoxFlat { BgColor = new Color("D3CBEA") });
        mapsColumn.AddChild(maps);
        foreach (var item in _data.Maps) maps.AddItem($"{item.DisplayName}    {item.LastWriteTime:yyyy-MM-dd HH:mm}");
        if (_data.SelectedMapIndex >= 0) maps.Select(_data.SelectedMapIndex);
        maps.ItemSelected += index => _data.SelectMap((int)index);

        var footer = GameUi.Panel("504D8F", 16);
        footer.CustomMinimumSize = new Vector2(0, 130);
        column.AddChild(footer);
        var footerRow = new HBoxContainer();
        footer.AddChild(footerRow);
        var status = GameUi.Label("Select a game mode and a map.", 24, "AAA7C4", "Status");
        status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footerRow.AddChild(status);
        footerRow.AddChild(GameUi.Button("Play", "Play", Play, 500, "D84F43"));
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
