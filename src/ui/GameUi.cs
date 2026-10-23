using Godot;
using System;

namespace NewDawn;

internal static class GameUi
{
    public static PanelContainer Panel(string color, int padding = 10)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(color), ContentMarginLeft = padding, ContentMarginRight = padding,
            ContentMarginTop = padding, ContentMarginBottom = padding });
        return panel;
    }

    public static Label Label(string text, int fontSize = 18, string color = "FFFFFF", string? name = null)
    {
        var label = new Label { Text = text };
        if (name != null) label.Name = name;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(color));
        return label;
    }

    public static Button Button(string name, string text, Action action, int width = 90, string color = "2D1B4E")
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(width, 42) };
        button.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(color) });
        button.AddThemeColorOverride("font_color", new Color("F0E68C"));
        button.Pressed += action;
        return button;
    }

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
