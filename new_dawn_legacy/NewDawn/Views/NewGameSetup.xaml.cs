using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IOPath = System.IO.Path;
namespace NewDawn.Views
{
    /// <summary>
    /// Interaction logic for NewGameSetup.xaml
    /// </summary>
    public partial class NewGameSetup : Window
    {

        public enum SetupGameMode
        {
            KillCommander,
            Contest
        }
        //fields
        private SetupGameMode? _selectedMode;
        private string? _selectedMapFileName;
        private bool _useCustomUnits = false;
        private string _selectedUnitTemplateBaseName = "DefaultUnits";
        private readonly ObservableCollection<UnitTemplateItem> _unitTemplateFiles = new ObservableCollection<UnitTemplateItem>();
        private readonly ObservableCollection<MapFileItem> _mapFiles = new ObservableCollection<MapFileItem>();
        private bool _isInitializing;

        //properties
        public bool IsInitializing
        {
            get { return _isInitializing; } set { _isInitializing = value; }
        }
        //struct
        public struct MapFileItem
        {
            private string _fileName;
            private string _displayName;
            private DateTime _lastWriteTime;

            public string FileName
            {
               get { return _fileName; }
                set { _fileName = value; }

            }
            public string DisplayName
            {
                get { return _displayName; }
                set { _displayName = value; }

            }
            public DateTime LastWriteTime
            {
                get { return _lastWriteTime; }
                set { _lastWriteTime = value; }
            }
            public MapFileItem(string fileName, string displayName, DateTime lastWriteTime)
            {
                FileName = fileName;
                DisplayName = displayName;
                LastWriteTime = lastWriteTime;
            }
        }
        public struct UnitTemplateItem
        {
            private string _fileName;
            private string _displayName;
            private DateTime _lastWriteTime;

            public string FileName
            {
                get { return _fileName; }
                set { _fileName = value; }
            }
            public string DisplayName
            {
                get { return _displayName; }
                set { _displayName = value; }

            }
            public DateTime LastWriteTime
            {
                get { return _lastWriteTime; }
                set { _lastWriteTime = value; }
            }

            public UnitTemplateItem(string fileName, string displayName, DateTime lastWriteTime)
            {
                FileName = fileName;
                DisplayName = displayName;
                LastWriteTime = lastWriteTime;
            }
        }

        //initailization 
        public NewGameSetup()
        {
            IsInitializing = true;
            InitializeComponent();

            // Bind list once
            LstMaps.ItemsSource = _mapFiles;

            // Bind unit templates dropdown + load files once
            CmbUnitTemplates.ItemsSource = _unitTemplateFiles;   
            LoadUnitTemplateFilesOnce();

            TxtMapsDir.Text = "Maps folder: " + Map.GetMapsDirectory();
            // TxtUnitsDir.Text = "Units folder: Saves/Units";
            TxtUnitsDir.Text = "Units folder: " + SaveAndLoaderHandler.GetUnitTemplatesDirectoryPath();


            // Load maps once (no refresh)
            LoadMapFilesOnce();

            // defaults
            _selectedMode = SetupGameMode.KillCommander;
            RbKillCommander.IsChecked = true;

            if (_mapFiles.Count > 0) LstMaps.SelectedIndex = 0;

            IsInitializing = false;

            UnitsMode_Checked(RbDefaultUnits, new RoutedEventArgs());
        }


        //buttons
        private void BtnExit_Click(object sender, RoutedEventArgs e) //back / exit
        {
            MainWindow mainWindow = new MainWindow();
            this.Close();
            mainWindow.Show();
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e) //Play Button
        {

            if (_selectedMode == null)   _selectedMode = SetupGameMode.KillCommander;

            if (string.IsNullOrWhiteSpace(_selectedMapFileName))
            {
                if (_mapFiles.Count > 0)
                    _selectedMapFileName = _mapFiles[0].FileName;
                else
                {
                    MessageBox.Show("No maps found. Save a map first.");
                    return;
                }
            }


            Map loadedMap;

            try
            {
                // If LoadMap expects NO extension ensure 
                string mapBaseName = IOPath.GetFileNameWithoutExtension(_selectedMapFileName);
                loadedMap = SaveAndLoaderHandler.LoadMap(mapBaseName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load map.\n\n{ex.Message}", "New Game",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //this part for settuping new game can be moved in another place
            // Create new game
            Player p1 = new Player(startingAlloy: 30, startingFuel: 6);
            Player p2 = new Player(startingAlloy: 45, startingFuel: 6);

            Game game = new Game(p1, p2, loadedMap);

            if (_useCustomUnits)
            {
                if (string.IsNullOrWhiteSpace(_selectedUnitTemplateBaseName))
                {
                    MessageBox.Show("Select a custom unit template file (CSV) first.");
                    return;
                }

                game.UnitTemplateFileName = _selectedUnitTemplateBaseName;
            }
            else
            {
                game.UnitTemplateFileName = "DefaultUnits";
            }

            //commander placement 
            game.InitializeGame(loadedMap, p1, p2, player1StartsFirst: true);

            // TODO: apply _selectedMode to Game

            MainGameWindow w = new MainGameWindow(game);
            Close();
            w.Show();
        }
        private void UnitsMode_Checked(object sender, RoutedEventArgs e)
        {
            if (IsInitializing)
                return;

            _useCustomUnits = ReferenceEquals(sender, RbCustomUnits);

            CmbUnitTemplates.IsEnabled = _useCustomUnits;

            if (!_useCustomUnits)
            {
                _selectedUnitTemplateBaseName = "DefaultUnits";
            }
            else
            {
                if (_unitTemplateFiles.Count > 0 && CmbUnitTemplates.SelectedIndex < 0)
                {
                    CmbUnitTemplates.SelectedIndex = 0; // newest by default
                }
            }
        }

        private void CmbUnitTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUnitTemplates.SelectedItem is UnitTemplateItem item)
            {
                _selectedUnitTemplateBaseName = IOPath.GetFileNameWithoutExtension(item.FileName);
                if (TxtSelectedUnitTemplateTime != null)
                {
                    TxtSelectedUnitTemplateTime.Text =
                        "Last modified: " + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                }
            }
        }

        private void GameMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == RbKillCommander)
                _selectedMode = SetupGameMode.KillCommander;
          //else if (sender == RbContest)
                _selectedMode = SetupGameMode.Contest;

        }
        //events
        private void LstMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstMaps.SelectedItem is MapFileItem item)
                _selectedMapFileName = item.FileName;
            else
                _selectedMapFileName = null;

        }
        //methods
        private void LoadMapFilesOnce()
        {
            _mapFiles.Clear();

            string dir = Map.GetMapsDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] filePaths = Directory.GetFiles(dir, "*.map", SearchOption.TopDirectoryOnly);

            string[] ordered = filePaths
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToArray();

            foreach (string filePath in ordered)
            {
                FileInfo fi = new FileInfo(filePath);

                string fileName = fi.Name; // with extension
                string displayName = IOPath.GetFileNameWithoutExtension(fi.Name);

                MapFileItem item = new MapFileItem(fileName, displayName, fi.LastWriteTime);
                _mapFiles.Add(item);
            }
        }
        public void LoadUnitTemplateFilesOnce()
        {
            _unitTemplateFiles.Clear();

            string dir = SaveAndLoaderHandler.GetUnitTemplatesDirectoryPath();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] filePaths = Directory.GetFiles(dir, "*.csv", SearchOption.TopDirectoryOnly);

            string[] ordered = filePaths
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToArray();

            for (int i = 0; i < ordered.Length; i++)
            {
                FileInfo fi = new FileInfo(ordered[i]);

                string fileName = fi.Name;
                string displayName = IOPath.GetFileNameWithoutExtension(fi.Name);

                _unitTemplateFiles.Add(new UnitTemplateItem(fileName, displayName, fi.LastWriteTime));
            }

            // Default select newest 
            if (_unitTemplateFiles.Count > 0 && CmbUnitTemplates.SelectedIndex < 0)
            {
                CmbUnitTemplates.SelectedIndex = 0;
            }
        }


    }
}
