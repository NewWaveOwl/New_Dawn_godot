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
            GetNode<Control>("Background").Hide();
            GetNode<Control>("StartHint").Show();
            GetNode<Button>("StartHint/Home").Pressed += Home;
            return;
        }
        _state = new GameplayController(game);
        BindControls();
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

    private void BindControls()
    {
        _p1Fuel = GetNode<Label>("%P1Fuel"); _p1Alloy = GetNode<Label>("%P1Alloy");
        _p2Fuel = GetNode<Label>("%P2Fuel"); _p2Alloy = GetNode<Label>("%P2Alloy");
        _turnPlayer = GetNode<Label>("%TurnPlayer");
        _info = GetNode<Label>("%Info");
        _shop = GetNode<ItemList>("%Shop");
        _canvas = GetNode<GameMapCanvas>("%GameCanvas");
        _canvas.PaintEnabled = false;
        _shop.ItemSelected += index => _state.SelectShopItem((int)index);
        _canvas.TileClicked += tile => _state.GameHex_LeftClick(tile);
        GetNode<Button>("%Move").Pressed += _state.BtnMove_Click;
        GetNode<Button>("%Attack").Pressed += _state.BtnAttack_Click;
        GetNode<Button>("%Special").Pressed += _state.BtnSpecail_Click;
        GetNode<Button>("%Cancel").Pressed += _state.BtnCancel_Click;
        GetNode<Button>("%NextTurn").Pressed += _state.BtnNextTurn_Click;
        GetNode<Button>("%Save").Pressed += _state.BtnSave_Click;
        GetNode<Button>("%Load").Pressed += _state.BtnLoad_Click;
        GetNode<Button>("%Home").Pressed += _state.BtnBack_Click;
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
        var dialog = GD.Load<PackedScene>("res://scenes/dialogs/game_saves.tscn").Instantiate<GameSavesDialog>();
        AddChild(dialog);
        dialog.GameLoaded += game => GameSession.Open(GetTree(), game);
        dialog.PopupCentered(new Vector2I(800, 450));
    }
}
