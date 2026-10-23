using Godot;
using System;

namespace NewDawn;

internal static class GameUi
{
    public static void Message(Node parent, string text, string title = "")
    {
        var dialog = new AcceptDialog { Name = "Message", DialogText = text, Title = title,
            Exclusive = true, Transient = true, DialogAutowrap = true };
        parent.AddChild(dialog);
        dialog.Confirmed += dialog.QueueFree;
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered(new Vector2I(640, 180));
    }
}
