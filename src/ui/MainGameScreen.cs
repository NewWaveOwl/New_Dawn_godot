using Godot;
using NewDawn.Models;

namespace NewDawn;

/// <summary>Godot view for the preserved MainGameWindow handlers.</summary>
public partial class MainGameScreen : Control
{
    private GameplayController _state = null!;
    private GameMapCanvas _canvas = null!;
    private ItemList _shop = null!;
    private Label _p1Fuel = null!, _p1Alloy = null!, _p2Fuel = null!, _p2Alloy = null!, _turnPlayer = null!, _info = null!;
    private bool _built;

    public override void _Ready()
    {
        GetWindow().Title = "NewDawn";
        if (DisplayServer.GetName() != "headless") GetWindow().Mode = Window.ModeEnum.Maximized;
        Game? game = GameSession.Take();
        if (game == null) {
            var message = GameUi.Label("Start a match from New Game in the main menu.");
            AddChild(message);
            var home = GameUi.Button("Home", "Home", Home);
            home.Position = new Vector2(0, 50);
            AddChild(home);
            return;
        }
        _state = new GameplayController(game);
        BuildControls();
        _state.Changed += Refresh;
        _state.MapRenderRequested += () => { Refresh(); _canvas.Render(_state); };
        _state.Notice += (text, title) => GameUi.Message(this, text, title);
        _state.HomeRequested += Home;
        _state.LoadRequested += OpenSaves;
        _state.Initialize();
        PopulateShop();
        Refresh();
    }

    private void Home() => GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");

    private void BuildControls()
    {
        var background = GameUi.Panel("614596", 12);
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 10);
        background.AddChild(layout);
        var topPanel = GameUi.Panel("5C39AB");
        layout.AddChild(topPanel);
        var top = new HBoxContainer();
        topPanel.AddChild(top);
        var p1 = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        p1.AddThemeConstantOverride("separation", 12);
        top.AddChild(p1);
        p1.AddChild(GameUi.Label("Player 1", 24));
        p1.AddChild(GameUi.Label("Fuel:", 21));
        _p1Fuel = GameUi.Label("", 21, name: "P1Fuel"); p1.AddChild(_p1Fuel);
        p1.AddChild(GameUi.Label("Alloy:", 21));
        _p1Alloy = GameUi.Label("", 21, name: "P1Alloy"); p1.AddChild(_p1Alloy);
        var turn = GameUi.Panel("6770AD", 6);
        top.AddChild(turn);
        var turnColumn = new VBoxContainer();
        turn.AddChild(turnColumn);
        var turnHeader = GameUi.Label("Turn:", 21); turnHeader.HorizontalAlignment = HorizontalAlignment.Center;
        turnColumn.AddChild(turnHeader);
        _turnPlayer = GameUi.Label("Player 1", 18, name: "TurnPlayer");
        turnColumn.AddChild(_turnPlayer);
        var p2 = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.End };
        p2.AddThemeConstantOverride("separation", 12);
        top.AddChild(p2);
        p2.AddChild(GameUi.Label("Player 2", 24));
        p2.AddChild(GameUi.Label("Fuel:", 21));
        _p2Fuel = GameUi.Label("", 21, name: "P2Fuel"); p2.AddChild(_p2Fuel);
        p2.AddChild(GameUi.Label("Alloy:", 21));
        _p2Alloy = GameUi.Label("", 21, name: "P2Alloy"); p2.AddChild(_p2Alloy);

        var main = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        main.AddThemeConstantOverride("separation", 10);
        layout.AddChild(main);
        var shopPanel = GameUi.Panel("300048");
        shopPanel.CustomMinimumSize = new Vector2(260, 0);
        main.AddChild(shopPanel);
        var shopColumn = new VBoxContainer();
        shopPanel.AddChild(shopColumn);
        shopColumn.AddChild(GameUi.Label("Unit's list", 14));
        _shop = new ItemList { Name = "Shop", SizeFlagsVertical = SizeFlags.ExpandFill };
        _shop.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("675D8D") });
        _shop.AddThemeColorOverride("font_color", Colors.White);
        shopColumn.AddChild(_shop);
        var buildingHint = GameUi.Label("Can only be placed near Engineer", 12);
        buildingHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        shopColumn.AddChild(buildingHint);
        _shop.ItemSelected += index => _state.SelectShopItem((int)index);
        var mapPanel = GameUi.Panel("41204E", 8);
        mapPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        main.AddChild(mapPanel);
        var scroll = new ScrollContainer { Name = "GameScroll" };
        mapPanel.AddChild(scroll);
        _canvas = new GameMapCanvas { Name = "GameCanvas", PaintEnabled = false };
        scroll.AddChild(_canvas);
        _canvas.TileClicked += tile => _state.GameHex_LeftClick(tile);

        var bottomPanel = GameUi.Panel("2B2B2B");
        layout.AddChild(bottomPanel);
        var bottom = new HBoxContainer();
        bottomPanel.AddChild(bottom);
        var infoPanel = GameUi.Panel("632D8A", 8);
        infoPanel.CustomMinimumSize = new Vector2(250, 150);
        bottom.AddChild(infoPanel);
        var infoColumn = new VBoxContainer();
        infoPanel.AddChild(infoColumn);
        infoColumn.AddChild(GameUi.Label("Info", 16));
        _info = GameUi.Label("", 16, name: "Info");
        _info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        infoColumn.AddChild(_info);
        var actions = GameUi.Panel("A557D4", 8);
        actions.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bottom.AddChild(actions);
        var actionRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        actions.AddChild(actionRow);
        actionRow.AddChild(GameUi.Button("Move", "Move", _state.BtnMove_Click, color: "1B4E3A"));
        actionRow.AddChild(GameUi.Button("Attack", "Attack", _state.BtnAttack_Click, color: "4E201B"));
        actionRow.AddChild(GameUi.Button("Special", "Special", _state.BtnSpecail_Click, color: "4B4E1B"));
        actionRow.AddChild(GameUi.Button("Cancel", "Cancel", _state.BtnCancel_Click, color: "4E1B4A"));
        var turnActions = new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) };
        bottom.AddChild(turnActions);
        turnActions.AddChild(GameUi.Button("NextTurn", "Next turn", _state.BtnNextTurn_Click, 232));
        var fileActions = new HBoxContainer();
        turnActions.AddChild(fileActions);
        fileActions.AddChild(GameUi.Button("Save", "Save", _state.BtnSave_Click, 70));
        fileActions.AddChild(GameUi.Button("Load", "Load", _state.BtnLoad_Click, 70));
        fileActions.AddChild(GameUi.Button("Home", "Home", _state.BtnBack_Click, 70));
        _built = true;
    }

    private void PopulateShop()
    {
        _shop.Clear();
        foreach (ShopEntry entry in _state.ShopEntries) {
            int index = _shop.AddItem(entry.Enabled ? entry.Content : "Buildings");
            _shop.SetItemTooltip(index, entry.Content);
            _shop.SetItemSelectable(index, entry.Enabled);
            _shop.SetItemDisabled(index, !entry.Enabled);
        }
    }

    private void Refresh()
    {
        if (!_built) return;
        _p1Fuel.Text = _state.P1FuelText; _p1Alloy.Text = _state.P1AlloyText;
        _p2Fuel.Text = _state.P2FuelText; _p2Alloy.Text = _state.P2AlloyText;
        _turnPlayer.Text = _state.TurnPlayerText;
        _info.Text = _state.Info;
        if (_state.SelectedShopIndex < 0) _shop.DeselectAll();
        else if (_state.SelectedShopIndex < _shop.ItemCount) _shop.Select(_state.SelectedShopIndex);
    }

    private void OpenSaves()
    {
        var dialog = new GameSavesDialog();
        AddChild(dialog);
        dialog.GameLoaded += game => GameSession.Open(GetTree(), game);
        dialog.PopupCentered(new Vector2I(800, 450));
    }
}
