using Godot;
using NewDawn.Models;

namespace NewDawn;

/// <summary>Transfers the existing C# Game object between Godot scenes.</summary>
internal static class GameSession
{
    private static Game? _pending;
    public static void Open(SceneTree tree, Game game)
    {
        _pending = game;
        Error error = tree.ChangeSceneToFile("res://scenes/game/main_game.tscn");
        if (error != Error.Ok) { _pending = null; GD.PushError($"Could not open gameplay scene: {error}"); }
    }
    public static Game? Take()
    {
        Game? game = _pending;
        _pending = null;
        return game;
    }
}
