using Godot;
using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace NewDawn;

/// <summary>Godot equivalent of MapEditorWindow.RenderMapWithUnits.</summary>
public partial class GameMapCanvas : MapCanvas
{
    private readonly Dictionary<string, Texture2D?> _images = new();

    public void Render(GameplayController state)
    {
        PaintEnabled = false;
        CanvasBackground = new Color("6E4D82");
        var deploy = new HashSet<Tile>(state.DeployTiles);
        var move = new HashSet<Tile>(state.MoveTiles);
        var attack = new HashSet<Tile>(state.AttackTiles);
        var special = new HashSet<Tile>(state.SpecialTiles);
        Tile? selected = state.SelectedUnit?.Position;
        FillOverride = tile => special.Contains(tile) ? new Color(200 / 255f, 0, 1, 100 / 255f)
            : attack.Contains(tile) ? new Color(1, 0, 0, 100 / 255f)
            : move.Contains(tile) ? new Color(0, 150 / 255f, 1, 100 / 255f)
            : deploy.Contains(tile) ? new Color(0, 1, 0, 100 / 255f) : null;
        StrokeColor = tile => tile == selected ? Colors.Gold
            : attack.Contains(tile) ? Colors.IndianRed
            : move.Contains(tile) ? Colors.LightGreen
            : deploy.Contains(tile) ? Colors.LightSkyBlue
            : special.Contains(tile) ? Colors.Purple : new Color("8C8C8C");
        StrokeWidth = tile => tile == selected ? 3.5f
            : attack.Contains(tile) || move.Contains(tile) || deploy.Contains(tile) || special.Contains(tile) ? 2.5f : 1;
        SetMap(state.CurrentGame.CurentGameMap);
    }

    private Texture2D? ImageFor(string address)
    {
        if (_images.TryGetValue(address, out var texture)) return texture;
        string normalized = address.Replace('\\', '/');
        string group = normalized.Contains("Buildings/", StringComparison.OrdinalIgnoreCase) ? "buildings" : "units";
        string path = $"res://assets/textures/{group}/{Path.GetFileName(normalized)}";
        texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        _images[address] = texture;
        return texture;
    }

    private void DrawUniform(Texture2D image, Rect2 box)
    {
        float scale = Math.Min(box.Size.X / image.GetWidth(), box.Size.Y / image.GetHeight());
        Vector2 size = image.GetSize() * scale;
        DrawTextureRect(image, new Rect2(box.Position + (box.Size - size) / 2, size), false);
    }

    protected override void DrawMapOverlays(Rect2 visible)
    {
        float hexW = (float)(2 * HexSize), hexH = (float)(Math.Sqrt(3) * HexSize);
        foreach (var (tile, position) in TilePositions) {
            Building? building = tile.Building;
            if (building == null || string.IsNullOrWhiteSpace(building.BuildingAssetAddress)) continue;
            var box = new Rect2(position + new Vector2(hexW * .01f, hexH * .01f), new Vector2(hexW, hexH));
            if (!visible.Intersects(box)) continue;
            Texture2D? image = ImageFor(building.BuildingAssetAddress);
            if (image != null) DrawUniform(image, box);
        }
        foreach (var (tile, position) in TilePositions) {
            Unit? unit = tile.OccupyingUnit;
            if (unit == null) continue;
            var box = new Rect2(position - new Vector2(hexW * .05f, hexH * .25f), new Vector2(hexW * 1.2f, hexH * 1.2f));
            if (!visible.Intersects(box)) continue;
            Texture2D? image = string.IsNullOrWhiteSpace(unit.UnitAssetAddress) ? null : ImageFor(unit.UnitAssetAddress);
            if (image != null) DrawUniform(image, box);
            else {
                string name = unit.ThisUnitType.ToString();
                string text = name.Length <= 4 ? name : name.Substring(0, 4);
                Vector2 textSize = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 11);
                Vector2 at = position + new Vector2(hexW * .2f, hexH * .35f);
                DrawRect(new Rect2(at, textSize + new Vector2(4, 4)), new Color(1, 1, 1, 160 / 255f));
                DrawString(ThemeDB.FallbackFont, at + new Vector2(2, textSize.Y), text, fontSize: 11, modulate: Colors.Black);
            }
        }
    }
}
