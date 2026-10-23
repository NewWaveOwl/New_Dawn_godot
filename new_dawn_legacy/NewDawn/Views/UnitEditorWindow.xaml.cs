using Microsoft.Win32;
using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace NewDawn.Views
{
    /// <summary>
    /// Unit Editor window for customizing unit statistics and saving/loading unit templates.
    /// Provides a DataGrid interface for editing all 14 unit types with real-time validation.
    /// Unit templates can be saved to CSV files and loaded in the main game.
    /// </summary>
    public partial class UnitEditorWindow : Window
    {
        private string _saveLocation = "";
        private ObservableCollection<UnitRow> _units;

        // <summary>
        /// Initializes the Unit Editor window
        /// </summary>
        public UnitEditorWindow()
        {
            InitializeComponent();
            Initialize();
        }

        /// <summary>
        /// Sets up the unit editor by loading default units and binding to the DataGrid
        /// </summary>
        private void Initialize()
        {
            // Initialize unit collection
            _units = new ObservableCollection<UnitRow>();
            // Load default units or create them
            LoadDefaultUnits();
            // Bind to DataGrid
            UnitsDataGrid.ItemsSource = _units;
        }

        //===========================
        // BUTTON HANDLERS
        //===========================

        /// <summary>
        /// Handles the Back button click - prompts to save before returning to main menu
        /// </summary>
        private void ButtonGoBack_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Do you want to save changes before exiting?",
                "Unit Editor",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BtnSave_Click(sender, e);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            MainWindow mainWindow = new MainWindow();
            this.Close();
            mainWindow.Show();
        }

        /// <summary>
        /// Handles the Save button click - opens file dialog and saves unit templates to CSV
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV File|*.csv";
            string dir = SaveAndLoaderHandler.GetUnitTemplatesDirectoryPath();
            Directory.CreateDirectory(dir);
            saveFileDialog.InitialDirectory = dir;
            saveFileDialog.FileName = "MyUnits.csv"; // Default suggestion

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    SaveUnitsToFile(saveFileDialog.FileName);
                    MessageBox.Show("Units saved successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// Converts UnitRow objects to Unit objects and saves them to a CSV file
        /// </summary>
        /// <param name="filepath">Full path to the file to save</param>
        private void SaveUnitsToFile(string filepath)
        {
            // Convert UnitRow to Unit
            List<Unit> units = new List<Unit>();

            foreach (UnitRow row in _units)
            {
                Unit.UnitType type = GuessUnitTypeFromName(row.Name); 
                Unit.UnitSpecial special = GuessUnitSpecialFromUnitType(type); 


                Unit unit = new Unit(
                    row.Armored,
                    row.HP,
                    row.UnitsPerTile,
                    row.Name,
                    row.PriceAlloy,
                    row.Range,
                    row.Speed,
                    row.Damage,
                    row.FuelConsumption,
                    row.CoreType,
                    type,
                    special
                );
                unit.UnitSetUp();
                unit.IsMech = row.IsMech;
                units.Add(unit);
            }

            SaveAndLoaderHandler.SaveUnitTemplates(units, filepath);
        }

        /// <summary>
        /// Maps UnitType to its corresponding special ability
        /// </summary>
        /// <param name="unitType">Unit type</param>
        /// <returns>Corresponding UnitSpecial enum value</returns>
        private static Unit.UnitSpecial GuessUnitSpecialFromUnitType(Unit.UnitType unitType)
        {
            switch (unitType)
            {
                case Unit.UnitType.Railgunner: return Unit.UnitSpecial.RailgunnerSpecial;
                case Unit.UnitType.Protectors: return Unit.UnitSpecial.ProtectorsSpecial;
                case Unit.UnitType.Engeneer: return Unit.UnitSpecial.EngeneerSpecial;
                case Unit.UnitType.RACM: return Unit.UnitSpecial.RACMSpecial;
                case Unit.UnitType.Hawk: return Unit.UnitSpecial.HawkSpecial;
                case Unit.UnitType.HoverTank: return Unit.UnitSpecial.HoverTankSpecial;
                case Unit.UnitType.Wasp: return Unit.UnitSpecial.WaspSpecial;
                case Unit.UnitType.BOHS: return Unit.UnitSpecial.BOHSpecial;
                case Unit.UnitType.Sunlight: return Unit.UnitSpecial.SunlightSpecial;
                case Unit.UnitType.Breacher: return Unit.UnitSpecial.BreacherSpecial;
                case Unit.UnitType.Redoubt: return Unit.UnitSpecial.RedoubtSpecial;
                case Unit.UnitType.CMP: return Unit.UnitSpecial.CMPSpecial;
                case Unit.UnitType.HoverTransporter: return Unit.UnitSpecial.HoverTransporterSpecial;
                default: return Unit.UnitSpecial.None;
            }
        }

        /// <summary>
        /// Converts UnitRow objects to Unit objects and saves them to a CSV file
        /// </summary>
        /// <param name="filepath">Full path to the file to save</param>

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV File|*.csv";
            string dir = SaveAndLoaderHandler.GetUnitTemplatesDirectoryPath();
            Directory.CreateDirectory(dir);
            openFileDialog.InitialDirectory = dir;
            openFileDialog.Title = "Load Unit Templates";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadUnitsFromFile(openFileDialog.FileName);  
                    _saveLocation = openFileDialog.FileName;
                    MessageBox.Show("Units loaded successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load units.\n\n{ex.Message}");
                }
            }
        }

        /// <summary>
        /// Saves units using just a filename (adds directory path automatically)
        /// </summary>
        /// <param name="filename">Filename without path</param>s

        private void SaveUnits(string filename)
        {
            // Check if we have a save location
            if (string.IsNullOrEmpty(_saveLocation))
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV File|*.csv";
                saveFileDialog.InitialDirectory = Path.Combine(Environment.CurrentDirectory, "Saves", "Unit");
                saveFileDialog.FileName = "DefaultUnits.csv";

                if (saveFileDialog.ShowDialog() == true)
                {
                    _saveLocation = saveFileDialog.FileName;
                }
            }

            if (!string.IsNullOrEmpty(_saveLocation))
            {
                try
                {
                    SaveUnits(_saveLocation); // Pass full path instead of just filename
                    MessageBox.Show("Units saved successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Loads units using just a filename (adds directory path automatically)
        /// </summary>
        /// <param name="filename">Filename without path or extension</param>
        private void LoadUnits(string filename)
        {
            // Use SaveAndLoaderHandler to load
            List<Unit> units = SaveAndLoaderHandler.LoadUnitTemplates(filename);

            // Convert Unit objects to UnitRow objects
            _units.Clear();

            foreach (Unit unit in units)
            {
                _units.Add(new UnitRow
                {
                    Name = unit.Name,
                    HP = unit.HP,
                    Damage = unit.Damage,
                    Range = unit.Range,
                    Speed = unit.Speed,
                    Armored = unit.Armored,
                    CoreType = unit.ThisUnitCoreType,
                    FuelConsumption = unit.FuelConsumption,
                    IsMech = unit.IsMech,
                    UnitsPerTile = unit.UnitsPerTile,
                    PriceAlloy = unit.PriceAlloy
                });
            }
        }


        /// <summary>
        /// Loads default unit templates from file or creates hardcoded defaults if file doesn't exist
        /// </summary>
        private void LoadDefaultUnits() 
        {
            // Check if default units file exists
            if (SaveAndLoaderHandler.UnitTemplateExists("DefaultUnits"))
            {
                try
                {
                    LoadUnits("DefaultUnits");
                    return;
                }
                catch
                {
                    Console.WriteLine("Failed to load default units, loading new template of units.");
                }
            }

            // Create the 14 default units
            _units.Clear();

            // 1. Railgunner
            _units.Add(new UnitRow
            {
                Name = "Railgunner",
                HP = 100,
                Damage = 75,
                Range = 3,
                Speed = 2,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 1,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 2. Protectors
            _units.Add(new UnitRow
            {
                Name = "Protectors",
                HP = 150,
                Damage = 50,
                Range = 2,
                Speed = 3,
                Armored = true,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 3. Engeneer
            _units.Add(new UnitRow
            {
                Name = "Engeneer",
                HP = 80,
                Damage = 20,
                Range = 1,
                Speed = 3,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 1,
                IsMech = false,
                UnitsPerTile = 1
            });

            // 4. RACM
            _units.Add(new UnitRow
            {
                Name = "RACM",
                HP = 120,
                Damage = 60,
                Range = 4,
                Speed = 2,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 5. Hawk
            _units.Add(new UnitRow
            {
                Name = "Hawk",
                HP = 200,
                Damage = 150,
                Range = 1,
                Speed = 9,
                Armored = true,
                CoreType = Unit.Core.None,
                FuelConsumption = 3,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 6. HoverTank
            _units.Add(new UnitRow
            {
                Name = "HoverTank",
                HP = 180,
                Damage = 100,
                Range = 2,
                Speed = 5,
                Armored = true,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = false,
                UnitsPerTile = 1
            });

            // 7. Wasp
            _units.Add(new UnitRow
            {
                Name = "Wasp",
                HP = 90,
                Damage = 40,
                Range = 3,
                Speed = 7,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 8. BOHS
            _units.Add(new UnitRow
            {
                Name = "BOHS",
                HP = 130,
                Damage = 70,
                Range = 2,
                Speed = 3,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 9. Sunlight
            _units.Add(new UnitRow
            {
                Name = "Sunlight",
                HP = 110,
                Damage = 80,
                Range = 5,
                Speed = 2,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 10. Breacher
            _units.Add(new UnitRow
            {
                Name = "Breacher",
                HP = 160,
                Damage = 120,
                Range = 1,
                Speed = 4,
                Armored = true,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 11. Redoubt
            _units.Add(new UnitRow
            {
                Name = "Redoubt",
                HP = 200,
                Damage = 50,
                Range = 2,
                Speed = 2,
                Armored = true,
                CoreType = Unit.Core.None,
                FuelConsumption = 1,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 12. CMP
            _units.Add(new UnitRow
            {
                Name = "CMP",
                HP = 250,
                Damage = 100,
                Range = 2,
                Speed = 3,
                Armored = true,
                CoreType = Unit.Core.None,
                FuelConsumption = 3,
                IsMech = true,
                UnitsPerTile = 1
            });

            // 13. HoverTransporter
            _units.Add(new UnitRow
            {
                Name = "HoverTransporter",
                HP = 100,
                Damage = 10,
                Range = 1,
                Speed = 6,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 2,
                IsMech = false,
                UnitsPerTile = 1
            });

            // 14. Rebel
            _units.Add(new UnitRow
            {
                Name = "Rebel",
                HP = 50,
                Damage = 10,
                Range = 1,
                Speed = 4,
                Armored = false,
                CoreType = Unit.Core.None,
                FuelConsumption = 1,
                IsMech = false,
                UnitsPerTile = 1
            });
        }

        /// <summary>
        /// Loads unit templates from a CSV file and populates the DataGrid
        /// </summary>
        /// <param name="filepath">Full path to the file to load</param>
        private void LoadUnitsFromFile(string fullFilePath)
        {
            List<Unit> units = SaveAndLoaderHandler.LoadUnitTemplates(fullFilePath);

            _units.Clear();

            foreach (Unit unit in units)
            {
                _units.Add(new UnitRow
                {
                    Name = unit.Name,
                    HP = unit.HP,
                    Damage = unit.Damage,
                    Range = unit.Range,
                    Speed = unit.Speed,
                    Armored = unit.Armored,
                    CoreType = unit.ThisUnitCoreType,
                    FuelConsumption = unit.FuelConsumption,
                    IsMech = unit.IsMech,
                    UnitsPerTile = unit.UnitsPerTile,
                    PriceAlloy = unit.PriceAlloy
                });
            }
        }


        /// <summary>
        /// Infers the UnitType enum from a unit's name string
        /// </summary>
        /// <param name="name">Unit name</param>
        /// <returns>Corresponding UnitType enum value</returns>
        private static Unit.UnitType GuessUnitTypeFromName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Unit.UnitType.Rebel;

            string n = name.Trim();

            // Exact matches
            if (n.Equals("Railgunner", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Railgunner;
            if (n.Equals("Protectors", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Protectors;
            if (n.Equals("Engeneer", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Engeneer;
            if (n.Equals("RACM", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.RACM;
            if (n.Equals("Hawk", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Hawk;
            if (n.Equals("HoverTank", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.HoverTank;
            if (n.Equals("Wasp", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Wasp;
            if (n.Equals("BOHS", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.BOHS;
            if (n.Equals("Sunlight", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Sunlight;
            if (n.Equals("Breacher", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Breacher;
            if (n.Equals("Redoubt", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Redoubt;
            if (n.Equals("CMP", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.CMP;
            if (n.Equals("HoverTransporter", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.HoverTransporter;
            if (n.Equals("Rebel", StringComparison.OrdinalIgnoreCase)) return Unit.UnitType.Rebel;

            // Fallback: unknown names become Rebel
            return Unit.UnitType.Rebel;
        }

    }


    /// <summary>
    /// Data model for a single unit row in the Unit Editor DataGrid.
    /// Implements INotifyPropertyChanged for two-way binding with validation.
    /// </summary>
    public class UnitRow : INotifyPropertyChanged
    {
        //backign fields
        private const int MaxHpValue = 250;
        private const int MinHpValue = 0;
        private const int MaxSpeedValue = 10;
        private const int MaxRangeValue = 10;
        private const int MaxDamageValue = 250;
        private const int MaxFuelValue = 10; //not sure if we want to change this
        private string _name = "New Unit";
        private int _priceAlloy = 0;
        private int _hp = 100;
        private int _damage = 10;
        private int _range = 1;
        private int _speed = 3;
        private bool _armored = false;
        private Unit.Core _coreType = Unit.Core.None;
        private int _fuelConsumption = 1;
        private bool _isMech = false;
        private int _unitsPerTile = 1;

        /// <summary>
        /// Alloy cost to purchase this unit
        /// </summary>
        public int PriceAlloy
        {
            get { return _priceAlloy; }
            set
            {
                if (value < 0) _priceAlloy = 0;
                else _priceAlloy = value;

                OnPropertyChanged(nameof(PriceAlloy));
            }
        }
        /// <summary>
        /// Unit name (displayed in game)
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>
        /// Hit points (0-500 valid range)
        /// </summary>

        public int HP
        {
            get { return _hp; }
            set 
            {
                if (value < MinHpValue)
                    _hp = MinHpValue;
                else if (value > MaxHpValue)
                    _hp = MaxHpValue;
                else
                    _hp = value;

                OnPropertyChanged(nameof(HP));
            }
        }

        /// <summary>
        /// Base damage (0-250 valid range)
        /// </summary>
        public int Damage
        {
            get { return _damage; }
            set 
            {
                if (value < 0)
                    _damage = 0;
                else if (value > MaxDamageValue)
                    _damage = MaxDamageValue;
                else
                    _damage = value;

                OnPropertyChanged(nameof(Damage));
            }
        }

        /// <summary>
        /// Attack range in tiles (0-10 valid range)
        /// </summary>
        public int Range
        {
            get { return _range; }
            set 
            {
                if (value < 0)
                    _range = 0;
                else if (value > MaxRangeValue)
                    _range = MaxRangeValue;
                else
                    _range = value;

                OnPropertyChanged(nameof(Range));
            }
        }


        /// <summary>
        /// Movement speed in tiles per turn (0-10 valid range)
        /// </summary>
        public int Speed
        {
            get { return _speed; }
            set 
            {
                if (value < 0)
                    _speed = 0;
                else if (value > 10)
                    _speed = 10;
                else
                    _speed = value;

                OnPropertyChanged(nameof(Speed));
            }
        }

        /// <summary>
        /// Whether unit has armor (can cross water/frozen terrain)
        /// </summary>
        public bool Armored
        {
            get { return _armored; }
            set { _armored = value; OnPropertyChanged(nameof(Armored)); }
        }


        /// <summary>
        /// Core type providing passive bonuses
        /// </summary>
        public Unit.Core CoreType
        {
            get { return _coreType; }
            set { _coreType = value; OnPropertyChanged(nameof(CoreType)); }
        }


        /// <summary>
        /// Fuel consumed per tile moved (1-5 valid range)
        /// </summary>
        public int FuelConsumption
        {
            get { return _fuelConsumption; }
            set 
            {
                if (value < 0)
                    _fuelConsumption = 0;
                else if (value > MaxFuelValue)
                    _fuelConsumption = MaxFuelValue;
                else
                    _fuelConsumption = value;

                OnPropertyChanged(nameof(FuelConsumption));
            }
        }


        /// <summary>
        /// Whether this is a mech unit (affects gameplay)
        /// </summary>
        public bool IsMech
        {
            get { return _isMech; }
            set { _isMech = value; OnPropertyChanged(nameof(IsMech)); }
        }

        /// <summary>
        /// How many units can occupy one tile (currently always 1)
        /// </summary>

        public int UnitsPerTile
        {
            get { return _unitsPerTile; }
            set { _unitsPerTile = value; OnPropertyChanged(nameof(UnitsPerTile)); }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Provide list of core types for ComboBox
        public List<Unit.Core> CoreTypes
        {
            get { return Enum.GetValues(typeof(Unit.Core)).Cast<Unit.Core>().ToList(); }
        }
        
    }
}
