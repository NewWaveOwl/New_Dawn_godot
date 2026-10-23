/// <summary>
/// Opens a window for creating and editing maps for the game.
/// Saved files are saved in the "Saves/Maps" subdirectory of the application.
/// To paint tiles, select a terrain type from the palette on the left, to actually see brushes they should be in the "Assets/Tiles" subdirectory. With the exact nameing convention "tile_{terrain}.png", e.g. "tile_field.png" for Field terrain.
/// <summary>
using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using File = System.IO.File;// for File
//aliases to avoid name conflicts
using IOPath = System.IO.Path; // for Directory, File, FileInfo, Path
using WpfPath = System.Windows.Shapes.Path; // for the drawn hexes
namespace NewDawn.Views
{
    public partial class MapEditorWindow : Window
    {
        //===========================
        //FIELDS
        //===========================
        // Rendering
        private static double _HexSize = 16*ZoomFactor; // "radius" in px (corner distance)
        private const double ZoomFactor = 3.0;
        private static StreamGeometry _hexGeometry = BuildFlatTopHexGeometry(HexSize);

        // UI state
        private bool _isPainting;

        // Data
        private Map? _currentMap;
        private Tile.TerrainType _selectedTerrain = Tile.TerrainType.Field;

        // Lookup / binding
        private readonly Dictionary<string, ImageSource?> _imageCache = new Dictionary<string, ImageSource?>(); //caches loaded images to avoid reloading from disk repeatedly
        //tuples for rendering palette and map files
        private readonly ObservableCollection<Tuple<Tile.TerrainType, string, ImageSource?>> _paletteItems = new();//stores terrain type, name, and image for the palette. used in the terrain list
        private readonly ObservableCollection<Tuple<string, string, DateTime>> _mapFiles = new(); //stores file name, display name, and last modified date for the map files. used in the map list
        //properties
        public static double HexSize
        {
            get {  return _HexSize; }
            set { _HexSize = value; }
        }
        public static StreamGeometry HexGeometry
        {
            get { return _hexGeometry; }
            set { _hexGeometry = value; }
        }





        //===========================
        //BUTTONS handling and EVENTS handling
        //===========================

        private void ButtonGoBakc_Click(object sender, RoutedEventArgs e)
        {
            MainWindow newMainWindow = new MainWindow();
            this.Close();
            newMainWindow.Show();
            //should we auto save the opend map here?
        }
        private void BtnCreate_Click(object sender, RoutedEventArgs e) //creates a new map based on the UI input
        {
            CreateNewMapFromUi();
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e) //saves the current map opened in the editor
        {
            if (_currentMap == null)
            {
                MessageBox.Show("No map is currently loaded/created.", "Map Editor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = (TxtMapName.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a map name.", "Map Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Enforce: blank/None => Mountain
            foreach (Tile t in _currentMap.GetAllTiles())
            {
                if (t.TileTerrainType == Tile.TerrainType.None)
                    t.SetTerrain(Tile.TerrainType.Field);
            }
            try
            {
                SaveAndLoaderHandler.SaveMap(_currentMap, name);
                RefreshMapList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save map.\n\n{ex.Message}", "Map Editor", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            string? selectedFileName = null;

            if (LstMaps.SelectedItem is Tuple<string, string, DateTime> item)
                selectedFileName = item.Item1;  // FileName

            // If the user typed a name, allow loading by name as well
            string typed = (TxtMapName.Text ?? string.Empty).Trim();
            string filename = selectedFileName ?? typed;

            if (string.IsNullOrWhiteSpace(filename))
            {
                MessageBox.Show("Select a map from the list (or type a map name) before loading.", "Map Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                _currentMap = SaveAndLoaderHandler.LoadMap(filename);
                TxtGridW.Text = _currentMap.Width.ToString();
                TxtGridH.Text = _currentMap.Height.ToString();
                RenderMap(MapCanvas, _currentMap!, _hexGeometry, HexSize, _imageCache,  Hex_MouseLeftButtonDown, Hex_MouseEnter, Hex_MouseRightButtonDown);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load map.\n\n{ex.Message}", "Map Editor", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LstPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstPalette.SelectedItem is Tuple<Tile.TerrainType, string, ImageSource?> item)
                _selectedTerrain = item.Item1;
        }

        private void LstMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstMaps.SelectedItem is Tuple<string, string, DateTime> item)
                TxtMapName.Text = item.Item2;
        }
        //===========================
        // initialization
        //===========================
        public MapEditorWindow()
        {
            InitializeComponent();
            Loaded += MapEditorWindow_Loaded;
        }

        private void Initialize()
        {
            // Wire data sources
            LstPalette.ItemsSource = _paletteItems;
            LstMaps.ItemsSource = _mapFiles;

            // Provide templates
            ConfigureListTemplates();

            // Canvas interaction (drag-paint)
            MapCanvas.MouseLeftButtonUp += MapCanvas_MouseLeftButtonUp;
            MapCanvas.MouseLeave += MapCanvas_MouseLeave;

            BuildPalette();
            RefreshMapList();
            // Create initial map from the UI values
            CreateNewMapFromUi();
        }
        //initialize event helpers
        private void MapEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }
        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopPainting();
        }

        private void MapCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            StopPainting();
        }
        //===========================
        //METHODS
        //===========================
        //config methods
        public void ConfigureListTemplates()//setting up how the list items are displayed
        {
            // Palette: [Image] [Name]
            {
                var root = new FrameworkElementFactory(typeof(StackPanel));
                root.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                root.SetValue(FrameworkElement.MarginProperty, new Thickness(2));

                var img = new FrameworkElementFactory(typeof(Image));
                img.SetValue(Image.WidthProperty, 24.0);
                img.SetValue(Image.HeightProperty, 24.0);
                img.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
                img.SetBinding(Image.SourceProperty, new Binding("Item3")); // Preview
                root.AppendChild(img);

                var txt = new FrameworkElementFactory(typeof(TextBlock));
                txt.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                txt.SetBinding(TextBlock.TextProperty, new Binding("Item2")); // Name
                root.AppendChild(txt);

                LstPalette.ItemTemplate = new DataTemplate { VisualTree = root };
            }
            // Maps: DisplayName + "Last save: ..."
            {
                var root = new FrameworkElementFactory(typeof(StackPanel));
                root.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                root.SetValue(FrameworkElement.MarginProperty, new Thickness(2));

                var name = new FrameworkElementFactory(typeof(TextBlock));
                name.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
                name.SetBinding(TextBlock.TextProperty, new Binding("Item2")); // DisplayName
                root.AppendChild(name);

                var saved = new FrameworkElementFactory(typeof(TextBlock));
                saved.SetValue(TextBlock.FontSizeProperty, 11.0);
                saved.SetValue(UIElement.OpacityProperty, 0.75);
                saved.SetBinding(TextBlock.TextProperty, new Binding("Item3") { StringFormat = "Last save: {0:dd.MM.yy HH:mm}" });
                root.AppendChild(saved);

                LstMaps.ItemTemplate = new DataTemplate { VisualTree = root };
            }
        }
        //Map Creating methods
        public void CreateNewMapFromUi() 
        {
            if (!int.TryParse(TxtGridW.Text, out int w) || w <= 0) w = 12;
            if (!int.TryParse(TxtGridH.Text, out int h) || h <= 0) h = 8;

            w = Clamp(w, 1, 200);
            h = Clamp(h, 1, 200);

            _currentMap = new Map(w, h);

            // All none painted/blank tiles are considered mountain. As units cannot traverse mountains by default, this prevents accidental movement through unpainted areas.
            foreach (Tile t in _currentMap.GetAllTiles())
            {
                t.SetTerrain(Tile.TerrainType.Field);
            }

            // Default selection to Field
            if (_paletteItems.Count > 0 && LstPalette.SelectedIndex < 0)
            {
                int idx = -1;

                for (int i = 0; i < _paletteItems.Count; i++)
                {
                    if (_paletteItems[i].Item1 == Tile.TerrainType.Field)
                    {
                        idx = i;
                        break;
                    }
                }
                LstPalette.SelectedIndex = idx >= 0 ? idx : 0;
            }

            RenderMap(MapCanvas,_currentMap!,_hexGeometry,  HexSize, _imageCache,  Hex_MouseLeftButtonDown, Hex_MouseEnter, Hex_MouseRightButtonDown);
        }
        public void RefreshMapList() //refreshes the list of saved maps in the UI
        {
            _mapFiles.Clear();

            string dir = Map.GetMapsDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string[] files = Directory.GetFiles(dir, "*.map", SearchOption.TopDirectoryOnly);

            Array.Sort(files, CompareFilesByLastWriteTimeUtcDesc);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                _mapFiles.Add(Tuple.Create(
                    fi.Name,
                    IOPath.GetFileNameWithoutExtension(fi.Name),
                    fi.LastWriteTime
                ));
            }
        }
        public static int CompareFilesByLastWriteTimeUtcDesc(string a, string b) //compares files by last write time in descending order
        {
            DateTime ta = File.GetLastWriteTimeUtc(a);
            DateTime tb = File.GetLastWriteTimeUtc(b);

            // Descending: newer first
            return tb.CompareTo(ta); //mb we should reverse this? (don't use in build function)
        }
        public static int Clamp(int value, int min, int max) //clamps the value between min and max. used for grid size inputs to force reasonable limits for map dimensions
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }


        public static Brush CreateFillBrush(Tile tile, Dictionary<string, ImageSource?> imageCache)//creates the brush for the tile based on terrain type
        {
            string address = Tile.AssetFor(tile.TileTerrainType);

            ImageSource? img = GetImage(address, imageCache);
            if (img != null)
            {
                return new ImageBrush(img)
                {
                    Stretch = Stretch.UniformToFill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };
            }
            return new SolidColorBrush(Color.FromRgb(235, 235, 235));
        }
        //=================
        //RENDERING 
        //=================
        public static void RenderMap(
                Canvas canvas,
                Map map,
                StreamGeometry hexGeometry,
                double hexSize,
                Dictionary<string, ImageSource?> imageCache,
                MouseButtonEventHandler? onLeftDown,
                MouseEventHandler? onEnter,
                MouseButtonEventHandler? onRightDown
            ) //renders the map onto the canvas
        {
            if (map == null)
                return;
            // No highlight outlines in the editor “plain render” mode
            List<Tile> emptyDeploy = new List<Tile>();
            List<Tile> emptyMove = new List<Tile>();
            List<Tile> emptyAttack = new List<Tile>();

            // Clear old visuals before rendering
            canvas.Children.Clear();

            // RenderTiles will compute bounds for us
            double maxRight;
            double maxBottom;

            RenderTiles(
                canvas,
                map,
                hexGeometry,
                hexSize,
                imageCache,
                emptyDeploy,
                emptyMove,
                emptyAttack,
                null,
                selectedUnit: null,          // no selected unit in plain map render
                onLeftDown,
                onEnter,
                onRightDown,
                out maxRight,
                out maxBottom);


            canvas.Width = Math.Ceiling(maxRight + 10);
            canvas.Height = Math.Ceiling(maxBottom + 10);
        }
        public static StreamGeometry BuildFlatTopHexGeometry(double size) //written with AI help
        {
            double w = 2 * size;
            double h = Math.Sqrt(3) * size;

            Point p0 = new Point(0.5 * size, 0); //top left
            Point p1 = new Point(1.5 * size, 0); //top right
            Point p2 = new Point(w, 0.5 * h); //middle right
            Point p3 = new Point(1.5 * size, h);//bottom right
            Point p4 = new Point(0.5 * size, h);//bottom left
            Point p5 = new Point(0, 0.5 * h);// middle left

            StreamGeometry g = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(p0, isFilled: true, isClosed: true);
                ctx.LineTo(p1, true, false);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
                ctx.LineTo(p4, true, false);
                ctx.LineTo(p5, true, false);
            }
            g.Freeze();//make it unmodifiable for performance
            return g;
        }
        public void PaintTile(WpfPath tilePath, Tile tile, Tile.TerrainType terrain) //paints the tile with the selected terrain type
        {
            tile.SetTerrain(terrain);
            tilePath.Fill = CreateFillBrush(tile, _imageCache);
        }
        public void StopPainting() //stops the painting process
        {
            if (!_isPainting) return;

            _isPainting = false;
            if (Mouse.Captured == MapCanvas)
                Mouse.Capture(null);
        }
        //rendering helpers
        public static ImageSource? GetImage(string assetAddress, Dictionary<string, ImageSource?> imageCache) //loads image from file or cache
        {
            ImageSource? img;

            if (imageCache.TryGetValue(assetAddress, out img))
                return img;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = IOPath.Combine(baseDir, assetAddress);

            if (File.Exists(fullPath))
                img = LoadBitmapFromFile(fullPath);
            else
                img = null;

            imageCache[assetAddress] = img;
            return img;
        }
        public void Hex_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) //handles the mouse down event for hex tiles to start painting
        {
            if (sender is not WpfPath p || p.Tag is not Tile tile)
                return;

            _isPainting = true;
            MapCanvas.CaptureMouse();

            PaintTile(p, tile, _selectedTerrain);
            e.Handled = true;
        }

        public void Hex_MouseEnter(object sender, MouseEventArgs e) //handles the mouse enter event for hex tiles to allow painting while dragging
        {
            if (!_isPainting)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopPainting();
                return;
            }

            if (sender is not WpfPath p || p.Tag is not Tile tile)
                return;

            PaintTile(p, tile, _selectedTerrain);
        }

        private void Hex_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not WpfPath p || p.Tag is not Tile tile)
                return;

            PaintTile(p, tile, Tile.TerrainType.Mountain);
            e.Handled = true;
        }
        //pallet logic
        public void BuildPalette() //makes the terrain palette for the map editor
        {
            _paletteItems.Clear();

            foreach (Tile.TerrainType t in Enum.GetValues(typeof(Tile.TerrainType)))
            {
                if (t == Tile.TerrainType.None)
                    continue;

                string address = Tile.AssetFor(t);
                ImageSource? img = GetImage(address, _imageCache);

                _paletteItems.Add(new Tuple<Tile.TerrainType, string, ImageSource?>(
                    t,
                    t.ToString(),
                    img
                ));
            }
            // Select Field by default
            int idx = -1;
            for (int i = 0; i < _paletteItems.Count; i++)
            {
                if (_paletteItems[i].Item1 == Tile.TerrainType.Field)
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0)
                LstPalette.SelectedIndex = idx;
            else if (_paletteItems.Count > 0)
                LstPalette.SelectedIndex = 0;
            else
                LstPalette.SelectedIndex = -1;
        }
        //We need a BitmapImage (a bitmap) because WPF cannot paint a hex tile from a file path string directly. The tile fill needs an image object (ImageSource) that WPF’s rendering system can draw.
        public static ImageSource LoadBitmapFromFile(string path) //loads bitmap image from file
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path, UriKind.Absolute);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        /// <summary>
        /// Renders the entire map onto the canvas:
        /// 1) draws all tile hexes (including terrain images + highlight outlines)
        /// 2) draws all units AFTER tiles so units are always on top
        /// </summary>
        public static void RenderMapWithUnits(
                  Canvas canvas,
                Map map,
                StreamGeometry hexGeometry,
                double hexSize,
                Dictionary<string, ImageSource?> imageCache,
                IEnumerable<Tile>? deployTiles,
                IEnumerable<Tile>? moveTiles,
                IEnumerable<Tile>? attackTiles,
                List<Tile> specialTiles,
                Unit? selectedUnit,
                MouseButtonEventHandler? onLeftDown,
                MouseEventHandler? onEnter,
                MouseButtonEventHandler? onRightDown) //written with AI Help
        {
            if (map == null)
                return;

            // Three lists  to store which tiles should be outlined on the map:
            // - deployList  -> blue outline
            // - moveList    -> green outline
            // - attackList  -> red outline
            // Null -> no tiles for that group, so we create an empty list
            List<Tile> deployList = (deployTiles != null) ? deployTiles.ToList() : new List<Tile>();
            List<Tile> moveList = (moveTiles != null) ? moveTiles.ToList() : new List<Tile>();
            List<Tile> attackList = (attackTiles != null) ? attackTiles.ToList() : new List<Tile>();

            // Clear old visuals before rendering
            canvas.Children.Clear();

            // These track the maximum drawn extents so we can resize the canvas at the end
            double maxRight = 0.0;
            double maxBottom = 0.0;
            // Draws hex tiles on first layer 
            RenderTiles(
                canvas, map, hexGeometry, hexSize, imageCache,
                deployList, moveList, attackList, specialTiles, selectedUnit,
                onLeftDown, onEnter, onRightDown,
                out maxRight, out maxBottom);
            // Draw Buildings
            RenderBuildings(canvas, map, hexSize, imageCache);
            // Draws units on second layer second
            RenderUnits(canvas, map, hexSize, imageCache);

            // Resize canvas to fit all drawn tiles
            canvas.Width = Math.Ceiling(maxRight + 10.0);
            canvas.Height = Math.Ceiling(maxBottom + 10.0);
        }

        // RenderMap with units helpers
        /// <summary>
        /// Draws ONLY the hex tile visuals:
        /// - computes screen position for each tile
        /// - selects stroke color/thickness based on highlight state
        /// - applies terrain fill (image brush)
        /// - attaches mouse handlers to the hex shape
        /// - sets ToolTip (unit stats if a unit is present)
        /// Also outputs maxRight/maxBottom to size the canvas.
        /// </summary>
        private static void RenderTiles(
                Canvas canvas,
                Map map,
                StreamGeometry hexGeometry,
                double hexSize,
                Dictionary<string, ImageSource?> imageCache,
                List<Tile> deployList,
                List<Tile> moveList,
                List<Tile> attackList,
                List<Tile> specialTiles,
                Unit? selectedUnit,
                MouseButtonEventHandler? onLeftDown,
                MouseEventHandler? onEnter,
                MouseButtonEventHandler? onRightDown,
                out double maxRight,
                out double maxBottom)
        {
            // Hex “bounding box” size (for sizing + centering overlay graphics)
            double hexW = 2.0 * hexSize;
            double hexH = Math.Sqrt(3.0) * hexSize;

            maxRight = 0.0;
            maxBottom = 0.0;
            // If a unit is selected, highlight its tile with a gold outline
            Tile? selectedTile = (selectedUnit != null) ? selectedUnit.Position : null;
            // Goes through all tiles in the map
            foreach (Tile tile in map.GetAllTiles())
            {
                // Convert tile grid coordinates into pixel coordinates on the canvas
                Point pos = GetTileScreenPosition(tile, hexSize);
                double x = pos.X;
                double y = pos.Y;
                //  choose outline based on highlight state 
                Brush stroke = new SolidColorBrush(Color.FromRgb(140, 140, 140));
                double strokeThickness = 1.0;

                // Attack > Move > Deploy priority
                if (attackList.Contains(tile))
                {
                    stroke = Brushes.IndianRed;
                    strokeThickness = 2.5;
                }
                else if (moveList.Contains(tile))
                {
                    stroke = Brushes.LightGreen;
                    strokeThickness = 2.5;
                }
                else if (deployList.Contains(tile))
                {
                    stroke = Brushes.LightSkyBlue;
                    strokeThickness = 2.5;
                }
                else if (specialTiles != null && specialTiles.Contains(tile))  // ← ADD THIS
                {
                    stroke = Brushes.Purple;
                    strokeThickness = 2.5;
                }

                // Selected tile outline overrides everything else
                if (selectedTile == tile)
                {
                    stroke = Brushes.Gold;
                    strokeThickness = 3.5;
                }
                // Create the hex shape
                WpfPath hex = new WpfPath
                {
                    Data = hexGeometry,
                    Stroke = stroke,
                    StrokeThickness = strokeThickness,
                    SnapsToDevicePixels = true,
                    Tag = tile,
                    Fill = CreateFillBrush(tile, imageCache),
                    ToolTip = BuildHoverText(tile)
                };

                ApplyFastTooltip(hex);

                // Store the Tile in Tag so mouse handlers can know which tile was clicked/hovered
                hex.Tag = tile;

                // Apply terrain fill brush (usually an ImageBrush based on terrain type)
                hex.Fill = CreateFillBrush(tile, imageCache);

                if (specialTiles != null && specialTiles.Contains(tile))
                {
                    hex.Fill = new SolidColorBrush(Color.FromArgb(100, 200, 0, 255)); // Purple
                }
                else if (attackList.Contains(tile))
                {
                    hex.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)); // Red
                }
                else if (moveList.Contains(tile))
                {
                    hex.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 150, 255)); // Blue
                }
                else if (deployList.Contains(tile))
                {
                    hex.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0)); // Green
                }
                else
                {
                    hex.Fill = CreateFillBrush(tile, imageCache); // Normal terrain
                }


                // Hover tooltip:
                // - If a unit is on this tile, show unit combat stats
                // - Otherwise show terrain type
                if (tile.OccupyingUnit != null)
                {
                    Unit u = tile.OccupyingUnit;
                    hex.ToolTip = "HP: " + u.HP + " | DMG: " + u.Damage + " | Speed: " + u.Speed;
                }
                else
                {
                    hex.ToolTip = $"Tile: {tile.TileTerrainType}";
                }

                // Attach mouse handlers to the HEX
                if (onLeftDown != null) hex.MouseLeftButtonDown += onLeftDown;
                if (onEnter != null) hex.MouseEnter += onEnter;
                if (onRightDown != null) hex.MouseRightButtonDown += onRightDown;

                // Position the hex on the canvas
                Canvas.SetLeft(hex, x);
                Canvas.SetTop(hex, y);

                // ZIndex 0 means “background layer”
                Canvas.SetZIndex(hex, 0);

                canvas.Children.Add(hex);

                // Update map bounds so the canvas can be resized after drawing
                if (x + hexW > maxRight) maxRight = x + hexW;
                if (y + hexH > maxBottom) maxBottom = y + hexH;
            }
        }
        /// <summary>
        /// Draws ONLY unit overlays on top of tiles.
        /// Units are rendered AFTER tiles and with higher ZIndex so they are always visible.
        /// Important: unit visuals are IsHitTestVisible=false so mouse events still go to the tile hex.
        /// </summary>
        private static void RenderUnits(
                        Canvas canvas,
                        Map map,
                        double hexSize,
                        Dictionary<string, ImageSource?> imageCache)
        {
            double hexW = 2.0 * hexSize;
            double hexH = Math.Sqrt(3.0) * hexSize;

            foreach (Tile tile in map.GetAllTiles())
            {
                // Skip empty tiles
                if (tile.OccupyingUnit == null)
                    continue;


                // Position unit using the same screen position as the tile
                Point pos = GetTileScreenPosition(tile, hexSize);
                double x = pos.X;
                double y = pos.Y;

                Unit unit = tile.OccupyingUnit;

                // The unit provides an asset path (ex: "Assets/Units/rebel_infantry.png")
                string unitAddr = unit.UnitAssetAddress;

                // Try to load from cache or disk
                ImageSource? unitImg = null;
                if (!string.IsNullOrWhiteSpace(unitAddr))
                    unitImg = GetImage(unitAddr, imageCache);

                if (unitImg != null)
                {
                    // Image overlay
                    Image img = new Image();
                    img.Source = unitImg;

                    // Scale to fit inside the hex
                    img.Width = hexW * 1.2; 
                    img.Height = hexH * 1.2;


                    img.Stretch = Stretch.Uniform;

                    img.IsHitTestVisible = false;

                    // Center image inside the hex bounding box
                    Canvas.SetLeft(img, x - (hexW * 0.05));
                    Canvas.SetTop(img, y - (hexH * 0.25));

                    // High ZIndex so the unit is always on top
                    Canvas.SetZIndex(img, 100);
                    canvas.Children.Add(img);
                }
                else
                {
                    // Fallback: if no image exists, display a small label so the unit is still visible
                    TextBlock label = new TextBlock();
                    label.Text = ShortUnitLabel(unit);
                    label.FontSize = 11;
                    label.FontWeight = FontWeights.Bold;
                    label.Background = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255));
                    label.Padding = new Thickness(2);

                    // Critical: label must not block mouse clicks/hover on the hex beneath
                    label.IsHitTestVisible = false;

                    // Place roughly centered
                    Canvas.SetLeft(label, x + (hexW * 0.20));
                    Canvas.SetTop(label, y + (hexH * 0.35));

                    // High ZIndex so the unit is always on top
                    Canvas.SetZIndex(label, 100);
                    canvas.Children.Add(label);
                }
            }
        }
        private static Point GetTileScreenPosition(Tile tile, double hexSize)
        {
            double rowStep = Math.Sqrt(3.0) * hexSize;
            double colStep = 1.5 * hexSize;
            int row = tile.Row;
            int displayCol = tile.Column + (row / 2);
            double x = displayCol * colStep;
            double y = row * rowStep;
            if ((displayCol & 1) == 1)
                y += (rowStep / 2.0);
            return new Point(x, y);
        }

        private static string ShortUnitLabel(Unit unit)
        {
            string s = unit.ThisUnitType.ToString();
            return s.Length <= 4 ? s : s.Substring(0, 4);
        }
        // Methods to render hover tips
        private static string BuildHoverText(Tile tile)
        {
            if (tile == null)
                return string.Empty;

            Unit unit = tile.OccupyingUnit;

            if (unit != null)
            {
                return
                    $"Unit: {unit.ThisUnitType}\n" +
                    $"HP: {unit.HP}\n" +
                    $"DMG: {unit.Damage}\n" +
                    $"Speed: {unit.Speed}";
            }

            return $"Tile: {tile.TileTerrainType}";
        }

        private static void ApplyFastTooltip(UIElement element)
        {
            if (element == null)
                return;

            ToolTipService.SetInitialShowDelay(element, 0);
            ToolTipService.SetBetweenShowDelay(element, 0);
            ToolTipService.SetShowDuration(element, 60000);
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow();
            Close();
            main.Show();
        }
        /// <summary>
        /// Draws building images on top of tiles 
        /// This method loops all tiles, finds tiles that have a Building, loads its image,
        /// computes the screen coordinates, and adds an Image to the Canvas.
        /// </summary>
        public static void RenderBuildings(
                    Canvas canvas,
                    Map map,
                    double hexSize,
                    Dictionary<string, ImageSource?> imageCache)
                  {
            double hexW = 2.0 * hexSize;
            double hexH = Math.Sqrt(3.0) * hexSize;

            foreach (Tile tile in map.GetAllTiles())
            {
                Building building = tile.Building;
                if (building == null)
                    continue;

                string assetAddress = building.BuildingAssetAddress;
                if (string.IsNullOrWhiteSpace(assetAddress))
                    continue;

                ImageSource? imgSrc;
                if (!imageCache.TryGetValue(assetAddress, out imgSrc))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string fullPath = System.IO.Path.Combine(baseDir, assetAddress);

                    if (File.Exists(fullPath))
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        imgSrc = bmp;
                    }
                    else
                    {
                        imgSrc = null;
                    }

                    imageCache[assetAddress] = imgSrc;
                }

                if (imgSrc == null)
                    continue;

                Point pos = GetTileScreenPosition(tile, hexSize);
                double x = pos.X;
                double y = pos.Y;

                Image img = new Image();
                img.Source = imgSrc;


                img.Width = hexW * 1.0;
                img.Height = hexH * 1.0;
                img.Stretch = Stretch.Uniform;
                img.IsHitTestVisible = false;


                Canvas.SetLeft(img, x + (hexW * 0.01));
                Canvas.SetTop(img, y + (hexH * 0.01));

                canvas.Children.Add(img);
            }
        }
    }
}
