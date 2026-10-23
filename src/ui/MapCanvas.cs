using Godot;
using NewDawn.Models;
using System;
using System.Collections.Generic;

namespace NewDawn;

/// <summary>Godot drawing/input adapter for MapEditorWindow's flat-top hex canvas.</summary>
public partial class MapCanvas : Control
{
    public const double HexSize = 16 * 3.0;
    public Tile.TerrainType SelectedTerrain { get; set; } = Tile.TerrainType.Field;
    public bool PaintEnabled { get; set; } = true;
    public Color CanvasBackground { get; set; } = new Color("D3CBEA");
    public Func<Tile, Color?>? FillOverride { get; set; }
    public Func<Tile, Color>? StrokeColor { get; set; }
    public Func<Tile, float>? StrokeWidth { get; set; }
    public event Action<Tile>? TileClicked;
    private Map? _map;
    private bool _isPainting;
    private readonly Dictionary<Tile, Vector2> _positions = new();
    private static readonly Dictionary<Tile.TerrainType, Texture2D?> Textures = new();
    private readonly Vector2[] _hex = BuildFlatTopHexGeometry(HexSize);
    protected IReadOnlyDictionary<Tile, Vector2> TilePositions => _positions;

    public override void _Ready() => MouseExited += StopPainting;

    public void SetMap(Map map)
    {
        StopPainting();
        _map = map;
        _positions.Clear();
        double maxRight = 0, maxBottom = 0;
        foreach (Tile tile in map.GetAllTiles()) {
            Vector2 pos = GetTileScreenPosition(tile, HexSize);
            _positions[tile] = pos;
            maxRight = Math.Max(maxRight, pos.X + 2 * HexSize);
            maxBottom = Math.Max(maxBottom, pos.Y + Math.Sqrt(3) * HexSize);
        }
        CustomMinimumSize = new Vector2((float)Math.Ceiling(maxRight + 10), (float)Math.Ceiling(maxBottom + 10));
        QueueRedraw();
    }

    public static Texture2D? TextureFor(Tile.TerrainType terrain)
    {
        if (Textures.TryGetValue(terrain, out var texture)) return texture;
        string path = $"res://assets/textures/tiles/tile_{terrain.ToString().ToLowerInvariant()}.png";
        texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        Textures[terrain] = texture;
        return texture;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), CanvasBackground);
        // Cull to the scroll viewport; the map can contain 40,000 hexes.
        Rect2 visible = GetParent() is ScrollContainer scroll
            ? new Rect2(new Vector2(scroll.ScrollHorizontal, scroll.ScrollVertical), scroll.Size)
            : new Rect2(Vector2.Zero, Size);
        foreach (var (tile, pos) in _positions) {
            if (!visible.Intersects(new Rect2(pos, new Vector2(96, (float)(Math.Sqrt(3) * HexSize))))) continue;
            var points = new Vector2[_hex.Length];
            for (int i = 0; i < points.Length; i++) points[i] = _hex[i] + pos;
            Texture2D? texture = TextureFor(tile.TileTerrainType);
            Color? fill = FillOverride?.Invoke(tile);
            if (fill.HasValue) DrawColoredPolygon(points, fill.Value);
            else if (texture == null) DrawColoredPolygon(points, new Color(235 / 255f, 235 / 255f, 235 / 255f));
            else {
                float width = 96, height = (float)(Math.Sqrt(3) * HexSize);
                float scale = Math.Max(width / texture.GetWidth(), height / texture.GetHeight());
                Vector2 drawn = texture.GetSize() * scale;
                Vector2 padding = (drawn - new Vector2(width, height)) / 2;
                var uv = new Vector2[_hex.Length];
                for (int i = 0; i < uv.Length; i++) uv[i] = (_hex[i] + padding) / drawn;
                DrawPolygon(points, new[] { Colors.White }, uv, texture);
            }
            var outline = new Vector2[7];
            Array.Copy(points, outline, 6);
            outline[6] = points[0];
            DrawPolyline(outline, StrokeColor?.Invoke(tile) ?? new Color("8C8C8C"), StrokeWidth?.Invoke(tile) ?? 1, true);
        }
        DrawMapOverlays(visible);
    }

    protected virtual void DrawMapOverlays(Rect2 visible) { }

    public override void _Process(double delta)
    {
        // Scroll changes which hexes need drawing, without allocating tile nodes.
        if (GetParent() is ScrollContainer scroll) {
            Vector2 offset = new(scroll.ScrollHorizontal, scroll.ScrollVertical);
            if (offset != _lastScroll || scroll.Size != _lastViewport) {
                _lastScroll = offset; _lastViewport = scroll.Size; QueueRedraw();
            }
        }
    }
    private Vector2 _lastScroll, _lastViewport;

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) StopPainting();
    }

    public override void _GuiInput(InputEvent input)
    {
        if (!PaintEnabled) {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click) {
                Tile? tile = TileAt(click.Position);
                if (tile != null) { TileClicked?.Invoke(tile); AcceptEvent(); }
            }
            return;
        }
        if (input is InputEventMouseButton button) {
            if (button.ButtonIndex == MouseButton.Left && button.Pressed) {
                Tile? tile = TileAt(button.Position);
                if (tile == null) return;
                _isPainting = true;
                PaintTile(tile, SelectedTerrain);
                AcceptEvent();
            }
            else if (button.ButtonIndex == MouseButton.Right && button.Pressed) {
                Tile? tile = TileAt(button.Position);
                if (tile != null) { PaintTile(tile, Tile.TerrainType.Mountain); AcceptEvent(); }
            }
        }
        else if (input is InputEventMouseMotion motion && _isPainting) {
            if (!motion.ButtonMask.HasFlag(MouseButtonMask.Left)) { StopPainting(); return; }
            Tile? tile = TileAt(motion.Position);
            if (tile != null) PaintTile(tile, SelectedTerrain);
        }
    }

    public void PaintTile(Tile tile, Tile.TerrainType terrain)
    {
        tile.SetTerrain(terrain);
        QueueRedraw();
    }

    public void StopPainting() => _isPainting = false;

    public Tile? TileAt(Vector2 position)
    {
        if (_map == null) return null;
        int displayCol = (int)Math.Floor(position.X / (1.5 * HexSize));
        int row = (int)Math.Floor(position.Y / (Math.Sqrt(3) * HexSize));
        // Nearby candidates cover the staggered hex bounding boxes; iterate in
        // reverse drawing order so hit testing agrees with the legacy canvas.
        for (int r = row; r >= row - 1; r--)
            for (int c = displayCol; c >= displayCol - 1; c--) {
                Tile? tile = _map.GetTile(c - r / 2, r);
                if (tile != null && Geometry2D.IsPointInPolygon(position - _positions[tile], _hex)) return tile;
            }
        return null;
    }

    public override string _GetTooltip(Vector2 atPosition)
    {
        Tile? tile = TileAt(atPosition);
        if (tile == null) return "";
        return tile.OccupyingUnit is Unit unit
            ? $"HP: {unit.HP} | DMG: {unit.Damage} | Speed: {unit.Speed}"
            : $"Tile: {tile.TileTerrainType}";
    }

    public static Vector2 GetTileScreenPosition(Tile tile, double hexSize)
    {
        double rowStep = Math.Sqrt(3.0) * hexSize;
        double colStep = 1.5 * hexSize;
        int row = tile.Row;
        int displayCol = tile.Column + (row / 2);
        double x = displayCol * colStep;
        double y = row * rowStep;
        if ((displayCol & 1) == 1) y += rowStep / 2.0;
        return new Vector2((float)x, (float)y);
    }

    public static Vector2[] BuildFlatTopHexGeometry(double size)
    {
        double w = 2 * size, h = Math.Sqrt(3) * size;
        return new[] { new Vector2((float)(.5 * size), 0), new Vector2((float)(1.5 * size), 0),
            new Vector2((float)w, (float)(.5 * h)), new Vector2((float)(1.5 * size), (float)h),
            new Vector2((float)(.5 * size), (float)h), new Vector2(0, (float)(.5 * h)) };
    }
}
