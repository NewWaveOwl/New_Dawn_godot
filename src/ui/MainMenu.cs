using Godot;

namespace NewDawn;

/// <summary>Godot presentation adapter for the preserved WPF main menu.</summary>
public partial class MainMenu : Control
{
    // Exact help text expression from MainWindow.BtnHowToPlay_Clicked.
    // Original attribution: Full Credit to Chatpgt for generating this to use.
    private const string HowToPlayText =
"═══════════════════════════════════\n" +
        "        NEW DAWN - HOW TO PLAY\n" +
        "═══════════════════════════════════\n\n" +

        "🎯 OBJECTIVE:\n" +
        "Destroy the enemy Commander (CMP) to win!\n" +
        "Protect your own CMP at all costs.\n\n" +

        "⚔️ GAMEPLAY:\n" +
        "• Deploy units from the shop (costs Alloy)\n" +
        "• Move units across the hex grid (costs Fuel)\n" +
        "• Attack enemies within range\n" +
        "• Use special abilities (cooldown)\n" +
        "• End turn when ready\n\n" +

        "💰 RESOURCES:\n" +
        "• Alloy: Used to purchase units\n" +
        "• Fuel: Refills automatically each turn\n" +
        "• Build extractors on resource tiles for income\n\n" +

        "🛡️ TERRAIN EFFECTS:\n" +
        "• Field/Forest: Bonus defense for infantry\n" +
        "• Mountain/Water: Blocks movement (armored only)\n" +
        "• Swamp: Movement penalty, damage boost\n" +
        "• City: Provides resources to adjacent tiles\n\n" +

        "⚡ TIPS:\n" +
        "• Armored units can cross water/mountains\n" +
        "• Protect your CMP - if it dies, you lose!\n" +
        "• Use terrain to your advantage\n" +
        "• Balance offense and economy\n\n" +

        "Good luck, Commander! 🚀";

    public override void _Ready()
    {
        // WPF MainWindow starts maximized rather than in exclusive fullscreen.
        if (!DisplayServer.GetName().Equals("headless"))
            GetWindow().Mode = Window.ModeEnum.Maximized;

        GetNode<Button>("NewGame").Pressed += () => ShowPendingScreen("New Game");
        GetNode<Button>("MapEditor").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/editors/map_editor.tscn");
        GetNode<Button>("UnitEditor").Pressed += () => ShowPendingScreen("Unit Editor");
        GetNode<Button>("HowToPlay").Pressed += ShowHowToPlay;
        GetNode<Button>("Exit").Pressed += () => GetTree().Quit();
    }

    private void ShowHowToPlay() => ShowMessage("How to Play", HowToPlayText);

    // User approved temporary notices while destination screens await their ports.
    private void ShowPendingScreen(string title) =>
        ShowMessage(title, "This screen has not been ported yet.");

    private void ShowMessage(string title, string message)
    {
        var dialog = new AcceptDialog
        {
            Title = title,
            DialogText = message,
            Exclusive = true,
            Transient = true,
            DialogAutowrap = true
        };
        AddChild(dialog);
        dialog.Confirmed += dialog.QueueFree;
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered(new Vector2I(620, title == "How to Play" ? 800 : 160));
    }
}
