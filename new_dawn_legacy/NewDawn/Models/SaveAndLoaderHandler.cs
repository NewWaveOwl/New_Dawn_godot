//All of them technical txt files with custom extensions
// map save extension .map
//game save extension .sagm

//TO DO Make method for loading tile to reuse it in Game and Map
//path combine took from here https://learn.microsoft.com/en-us/dotnet/api/system.io.path.combine?view=net-10.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static NewDawn.Models.Tile;
using static NewDawn.Models.Unit;

namespace NewDawn.Models
{
    /// <summary>
    /// Centralized save/load handler for all persistent game data
    /// </summary>
    static class SaveAndLoaderHandler
    {
        //fields
        private static readonly string _mapExtention = ".map";
        private static readonly string _gameSaveExtention = ".sagm";
        private static readonly string _mapDirectory = Path.Combine("Saves", "Maps");//directory for saved maps
        private static int _currentSaveVersion = 5;//to track save file versions current 3, after adding new stuff to unit and building and mapObject
        private static readonly string _gameSavesDirectory = Path.Combine("Saves", "GameSaves"); //directory for saved games
        private static int _doubleScale = 100; // used for double to int conversion
        private static readonly string _unitTemplateDirectory = Path.Combine("Saves", "Units");

        //properties
        /// <summary>
        /// Gets the current save file format version
        /// </summary>
        public static int CurrentSaveVersion
        {
            get { return _currentSaveVersion; }
        }
        /// <summary>
        /// Gets the double-to-integer scaling factor
        /// </summary>
        public static int DoubleScale
        {
            get { return _doubleScale; }
        }

        /// <summary>
        /// Gets the full path to the unit templates directory
        /// </summary>
        /// <returns>Absolute path to unit templates folder</returns>
        public static string GetUnitTemplatesDirectoryPath()
        {
            return Path.GetFullPath(UnitTemplateDirectory);
        }
        /// <summary>
        /// Gets the map file extension (.map)
        /// </summary>

        public static string mapExtention
        {
            get { return _mapExtention; }
        }
        /// <summary>
        /// Gets the game save file extension (.sagm)
        /// </summary>
        public static string gameSaveExtention
        {
            get { return _gameSaveExtention; }
        }

        /// <summary>
        /// Gets the map directory path (Saves/Maps)
        /// </summary>

        public static string MapDirectory
        {
            get { return _mapDirectory; }
        }

        /// <summary>
        /// Gets the map directory path (Saves/Maps)
        /// </summary>

        public static string GameSavesDirectory
        {
            get { return _gameSavesDirectory; }
        }

        /// <summary>
        /// Gets the unit template directory path (Saves/Units)
        /// </summary>
        public static string UnitTemplateDirectory
        {
            get { return _unitTemplateDirectory; }
        }

        //methods
        /// <summary>
        /// Saves a list of unit templates to a CSV file
        /// CSV Format: Name,HP,DMG,Range,Speed,Armored,CoreType,FuelConsumption,IsMech,UnitsPerTile,PriceAlloy,UnitType,Special
        /// </summary>
        /// <param name="units">List of units to save</param>
        /// <param name="filename">Filename (with or without .csv extension)</param>
        public static void SaveUnitTemplates(List<Unit> units, string filename)
        {
            if (units == null)
                throw new ArgumentNullException(nameof(units));

            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("filename is required.", nameof(filename));

            string path = GetUnitTemplatePath(filename);
            EnsureDirectory(path);

            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                // Write header
                writer.WriteLine("Name,HP,DMG,Range,Speed,Armored,CoreType,FuelConsumption,IsMech,UnitsPerTile,PriceAlloy,UnitType,Special");

                // Write each unit
                foreach (Unit unit in units)
                {
                    if (unit == null)
                        continue;

                    writer.WriteLine(
                        $"{unit.Name}," +
                        $"{unit.HP}," +
                        $"{unit.Damage}," +
                        $"{unit.Range}," +
                        $"{unit.Speed}," +
                        $"{unit.Armored}," +
                        $"{unit.ThisUnitCoreType}," +
                        $"{unit.FuelConsumption}," +
                        $"{unit.IsMech}," +
                        $"{unit.UnitsPerTile}," +
                        $"{unit.PriceAlloy}," +
                        $"{unit.ThisUnitType}," +
                        $"{unit.ThisUnitSpecial}"
                    );
                }
            }
        }

        /// <summary>
        /// Loads unit templates from a CSV file
        /// </summary>
        /// <param name="filename">Filename (with or without .csv extension)</param>
        /// <returns>List of loaded Unit objects</returns>

        public static List<Unit> LoadUnitTemplates(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("filename is required.", nameof(filename));

            string path = GetUnitTemplatePath(filename);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Unit template file '{filename}' not found.", path);

            List<Unit> units = new List<Unit>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            // Skip header line (line 0)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length < 13)
                    throw new InvalidDataException($"Bad unit template record at line {i + 1}: not enough fields");

                try
                {
                    string name = parts[0];
                    int hp = ParseInt(parts[1], "HP", line);
                    int damage = ParseInt(parts[2], "Damage", line);
                    int range = ParseInt(parts[3], "Range", line);
                    int speed = ParseInt(parts[4], "Speed", line);
                    bool armored = ParseBool(parts[5], "Armored", line);
                    Core core = Enum.TryParse<Core>(parts[6], out Core coreResult) ? coreResult : Core.None;
                    int fuelConsumption = ParseInt(parts[7], "FuelConsumption", line);
                    bool isMech = ParseBool(parts[8], "IsMech", line);
                    int unitsPerTile = ParseInt(parts[9], "UnitsPerTile", line);
                    int priceAlloy = ParseInt(parts[10], "PriceAlloy", line);
                    UnitType unitType = Enum.TryParse<UnitType>(parts[11], out UnitType unitTypeResult) ? unitTypeResult : UnitType.None;
                    UnitSpecial special = Enum.TryParse<UnitSpecial>(parts[12], out UnitSpecial unitSpecialResult) ? unitSpecialResult : UnitSpecial.None;

                    Unit unit = new Unit(
                        armored,
                        hp,
                        unitsPerTile,
                        name,
                        priceAlloy,
                        range,
                        speed,
                        damage,
                        fuelConsumption,
                        core,
                        unitType,
                        special
                    );

                    unit.UnitSetUp();

                    unit.IsMech = isMech;

                    units.Add(unit);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error parsing unit template at line {i + 1}: {ex.Message}", ex);
                }
            }

            return units;
        }

        /// <summary>
        /// Checks if a unit template file exists
        /// </summary>
        /// <param name="filename">Filename without extension</param>
        /// <returns>True if file exists, false otherwise</returns>
        public static bool UnitTemplateExists(string filename)
        {
            string path = GetUnitTemplatePath(filename);
            return File.Exists(path);
        }

        //====================
        // Map Save And Load
        //====================

        /// <summary>
        /// Saves a map to a .map file
        /// File Format:
        /// - VERSION|{version}
        /// - SIZE|{width}|{height}
        /// - T|{col}|{row}|{terrain} for each tile
        /// - B|{col}|{row}|{type}|{hp}|{prod}|{price} for each building
        /// - U|{col}|{row}|{...unit data...} for each unit
        /// </summary>
        /// <param name="map">Map to save</param>
        /// <param name="filename">Filename (with or without .map extension)</param>
        public static void SaveMap(Map map, string filename)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("filename is required.", nameof(filename));

            string path = GetPath(filename, mapExtention, MapDirectory);
            EnsureDirectory(path); // make sure directory exists and creates it if not

            using StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8);
            //header
            WriteHeader(writer, map);

            foreach (Tile tile in map.GetAllTiles())
            {
                // Tile terrain
                WriteTileRecord(writer, tile);

                // Building
                if (tile.Building != null)
                {
                    Building b = tile.Building;
                    // B|col|row|type|hp|prod|price
                    WriteBuildingRecord(writer, tile, tile.Building);
                }
                // Unit
                if (tile.OccupyingUnit != null)
                {
                    WriteUnitRecord(writer, tile, tile.OccupyingUnit);
                }
            }
        }

        /// <summary>
        /// Loads a map from a .map file
        /// </summary>
        /// <param name="filename">Filename (with or without .map extension)</param>
        /// <returns>Loaded Map object</returns>
        
        public static Map LoadMap(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("filename is required.", nameof(filename));

            string path = GetPath(filename, mapExtention, MapDirectory);
            if (!File.Exists(path))
                throw new FileNotFoundException("Map save file not found.", path);

            string[] lines = File.ReadAllLines(path, Encoding.UTF8); // https://learn.microsoft.com/en-us/dotnet/api/system.io.file.readalllines?view=net-10.0 

            ReadHeader(lines, out int version, out int width, out int height);

            if (version != CurrentSaveVersion)
                throw new InvalidDataException($"Unsupported save version {version}. Expected {CurrentSaveVersion}.");

            if (width <= 0 || height <= 0)
                throw new InvalidDataException("Save file missing valid SIZE line.");

            Map map = new Map(width, height);

            // Apply data lines
            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                string[] parts = raw.Split('|');
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "T": // Tile
                        // T|col|row|terrain
                            LoadTile(raw, parts, map);
                            break;

                    case "B": // Building
                        {
                            LoadBuilding(raw, parts, map);
                            break;
                        }

                    case "U": // Unit
                        {
                            LoadUnit(raw, parts, map); 
                            break;
                        }
                    }   
            }
            return map;
        }
        /// <summary>
        /// Saves complete game state to a .sagm file
        /// Includes: map data, player resources, unit positions, ownership, turn state
        /// File Format extends .map format with additional records:
        /// - G|{gameState}|{turnCounter}|{currentPlayerId}|{skipTurn}
        /// - UT|{base64encodedTemplateName}
        /// - P|{playerId}|{alloy}|{fuel}
        /// - OWN_U|{col-row}|{ownerId} for unit ownership
        /// - OWN_B|{col-row}|{ownerId} for building ownership
        /// </summary>
        /// <param name="game">Game instance to save</param>
        /// <param name="filename">Filename (with or without .sagm extension)</param>

        private static string GetUnitTemplatePath(string filename)
        {
            string name = filename;

            if (!Path.HasExtension(name))
                name += ".csv";

            return Path.Combine(UnitTemplateDirectory, name);
        }

        //====================
        // Game saving method
        //====================

        /// <summary>
        /// Loads complete game state from a .sagm file
        /// Reconstructs the full game including map, players, units, and turn state
        /// </summary>
        /// <param name="filename">Filename (with or without .sagm extension)</param>
        /// <returns>Loaded Game object</returns>
        public static void SaveGame(Game game, string filename)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("filename is required.", nameof(filename));
            if (game.CurentGameMap == null) throw new InvalidOperationException("Game has no map to save.");

            string path = GetPath(filename, gameSaveExtention, GameSavesDirectory);
            EnsureDirectory(path);

            Map map = game.CurentGameMap;

            using StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8);

            WriteHeader(writer, map);

            // Game meta data 
            int currentPlayerId = (game.CurrentPlayer == game.Player1) ? 1 : 2;
            writer.WriteLine($"G|{(int)game.CurentGameState}|{game.PlayerTurnCounter}|{currentPlayerId}|{game.SkipTurn}");
            string templateName = "DefaultUnits";
            if (!string.IsNullOrWhiteSpace(game.UnitTemplateFileName))
                templateName = game.UnitTemplateFileName;
            writer.WriteLine("UT|" + ToBase64(templateName));
            // Player resources
            writer.WriteLine($"P|1|{game.Player1.AlloyResources}|{game.Player1.FuelResources}");
            writer.WriteLine($"P|2|{game.Player2.AlloyResources}|{game.Player2.FuelResources}");

            foreach (Tile tile in map.GetAllTiles()) // mb we should add field for MapObject, so it will recorde to whom it belongs 
            {
                WriteTileRecord(writer, tile);

                if (tile.Building != null)
                    WriteBuildingRecord(writer, tile, tile.Building);

                if (tile.OccupyingUnit != null)
                    WriteUnitRecord(writer, tile, tile.OccupyingUnit);
            }

            // Saves which player owns which unit/building
            foreach (Tile tile in map.GetAllTiles())
            {
                if (tile.OccupyingUnit != null)
                {
                    Unit u = tile.OccupyingUnit;

                    int owner =
                        (game.Player1.DeployedUnits != null && game.Player1.DeployedUnits.Contains(u)) ? 1 :
                        (game.Player2.DeployedUnits != null && game.Player2.DeployedUnits.Contains(u)) ? 2 : 0;

                    string key = MakeKey(tile.Column, tile.Row);
                    writer.WriteLine($"OWN_U|{key}|{owner}");
                }

                if (tile.Building != null)
                {
                    Building b = tile.Building;

                    int owner =
                        (game.Player1.OwnedBuildings != null && game.Player1.OwnedBuildings.Contains(b)) ? 1 :
                        (game.Player2.OwnedBuildings != null && game.Player2.OwnedBuildings.Contains(b)) ? 2 : 0;

                    string key = MakeKey(tile.Column, tile.Row);
                    writer.WriteLine($"OWN_B|{key}|{owner}");
                }
            }
        }


        /// <summary>
        /// Loads complete game state from a .sagm file
        /// Reconstructs the full game including map, players, units, and turn state
        /// </summary>
        /// <param name="filename">Filename (with or without .sagm extension)</param>
        /// <returns>Loaded Game object</returns>
        public static Game GameLoad(string filename)
        {
            string loadedUnitTemplateName = "DefaultUnits";  

            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("filename is required.", nameof(filename));

            string path = GetPath(filename, gameSaveExtention, GameSavesDirectory);

            if (!File.Exists(path)) throw new FileNotFoundException("Game save file not found.", path);

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            ReadHeader(lines, out int version, out int width, out int height);

            if (version != CurrentSaveVersion) throw new InvalidDataException($"Unsupported save version {version}. Expected {CurrentSaveVersion}.");
            if (width <= 0 || height <= 0) throw new InvalidDataException("Missing or invalid SIZE in .sagm.");

            //map loading
            Map map = new Map(width, height);
            Player p1 = new Player(startingAlloy: 0, startingFuel: 0);
            Player p2 = new Player(startingAlloy: 0, startingFuel: 0);

            Game game = new Game(p1, p2, map);

            int? loadedState = null;
            int loadedTurnCounter = 0;
            int? loadedCurrentPlayerId = null;
            bool loadedSkipTurn = false;

            Dictionary<string, int> ownUnits = new Dictionary<string, int>();
            Dictionary<string, int> ownBuildings = new Dictionary<string, int>();

            //read all files lines
            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                string[] parts = raw.Split('|');
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "UT":
                        // UT|base64Name
                        if (parts.Length != 2)
                            throw new InvalidDataException($"Bad unit template record: {raw}");

                        string? decoded = FromBase64(parts[1]);
                        if (!string.IsNullOrWhiteSpace(decoded))
                            loadedUnitTemplateName = decoded;

                        break;
                    case "T"://Tile
                        LoadTile(raw, parts, map);
                        break;

                    case "B": //Buildings
                        LoadBuilding(raw, parts, map);
                        break;

                    case "U"://Units
                        LoadUnit(raw, parts, map);
                        break;

                    case "G": //Game
                        // G|state|turnCounter|currentPlayerId|skipTurn
                        if (parts.Length != 5) throw new InvalidDataException($"Bad game meta record: {raw}");
                        loadedState = ParseInt(parts[1], "state", raw);
                        loadedTurnCounter = ParseInt(parts[2], "turnCounter", raw);
                        loadedCurrentPlayerId = ParseInt(parts[3], "currentPlayerId", raw);
                        loadedSkipTurn = ParseBool(parts[4], "skipTurn", raw);
                        break;

                    case "P":
                        // P|playerId|alloy|fuel
                        if (parts.Length != 4) throw new InvalidDataException($"Bad player record: {raw}");
                        int pid = ParseInt(parts[1], "playerId", raw);
                        int alloy = ParseInt(parts[2], "alloy", raw);
                        int fuel = ParseInt(parts[3], "fuel", raw);
                        if (pid == 1)
                        {
                            game.Player1.AlloyResources = alloy;
                            game.Player1.FuelResources = fuel;
                        }
                        else if (pid == 2)
                        {
                            game.Player2.AlloyResources = alloy;
                            game.Player2.FuelResources = fuel;
                        }
                        break;

                    case "OWN_U": //own units
                        // OWN_U|col,row|owner
                        if (parts.Length != 3) throw new InvalidDataException($"Bad unit ownership record: {raw}");
                        ownUnits[parts[1]] = ParseInt(parts[2], "owner", raw);
                        break;

                    case "OWN_B": //own buildings
                        // OWN_B|col,row|owner
                        if (parts.Length != 3) throw new InvalidDataException($"Bad building ownership record: {raw}");
                        ownBuildings[parts[1]] = ParseInt(parts[2], "owner", raw);
                        break;
                }
            }
            game.CurentGameState = (Game.GameState)loadedState;
            game.PlayerTurnCounter = loadedTurnCounter;
            game.SkipTurn = loadedSkipTurn;
            // set current player
            if (loadedCurrentPlayerId == 2)
            {
                game.CurrentPlayer = game.Player2;
                game.CurrentOpponent = game.Player1;
            }
            else
            {
                game.CurrentPlayer = game.Player1;
                game.CurrentOpponent = game.Player2;
            }

            // Clear existing Players
            game.Player1.DeployedUnits?.Clear();
            game.Player2.DeployedUnits?.Clear();
            game.Player1.OwnedBuildings?.Clear();
            game.Player2.OwnedBuildings?.Clear();

            //restore ownership of units
            foreach (var kvp in ownUnits)
            {
                string key = kvp.Key;
                int owner = kvp.Value;

                ParseKey(key, out int col, out int row);

                Tile t = map.GetTile(col, row);
                if (t?.OccupyingUnit == null) continue;

                if (owner == 1) game.Player1.DeployedUnits?.Add(t.OccupyingUnit);
                else if (owner == 2) game.Player2.DeployedUnits?.Add(t.OccupyingUnit);
            }
            //restore ownership of buildings
            foreach (var kvp in ownBuildings)
            {
                string key = kvp.Key;
                int owner = kvp.Value;

                ParseKey(key, out int col, out int row);

                Tile t = map.GetTile(col, row);
                if (t?.Building == null) continue;

                if (owner == 1) game.Player1.OwnedBuildings?.Add(t.Building);
                else if (owner == 2) game.Player2.OwnedBuildings?.Add(t.Building);
            }
            game.UnitTemplateFileName = loadedUnitTemplateName;
            return game;
        }


        //====================
        //Helpers
        //====================

        /// <summary>
        /// Writes a tile record: T|col|row|terrain
        /// </summary>
        public static void WriteTileRecord(StreamWriter writer, Tile tile)
        {
            writer.WriteLine($"T|{tile.Column}|{tile.Row}|{tile.TileTerrainType}");
        }

        /// <summary>
        /// Writes a building record: B|col|row|type|hp|prod|price
        /// </summary>
        private static void WriteBuildingRecord(StreamWriter writer, Tile tile, Building b)
        {
            // B|col|row|type|hp|prod|price
            writer.WriteLine($"B|{tile.Column}|{tile.Row}|{b.ThisBuildingType}|{b.HP}|{b.BuildingyProduction}|{b.PriceAlloy}");
        }

        /// <summary>
        /// Loads a tile record and sets its terrain
        /// Format: T|col|row|terrain
        /// </summary>
        public static void LoadTile(string raw, string[] parts, Map map)
        {
            // T|col|row|terrain
            if (parts.Length != 4)
                throw new InvalidDataException($"Bad tile record: {raw}");

            int col = ParseInt(parts[1], "col", raw);
            int row = ParseInt(parts[2], "row", raw);

            Tile tile = map.GetTile(col, row)
                ?? throw new InvalidDataException($"Tile not found ({col},{row}): {raw}");

            if (!Enum.TryParse(parts[3], true, out Tile.TerrainType terrain) ||
                !Enum.IsDefined(typeof(Tile.TerrainType), terrain))
                throw new InvalidDataException($"Bad terrain '{parts[3]}': {raw}");

            tile.SetTerrain(terrain);
        }

        /// <summary>
        /// Writes a unit record with full unit state
        /// Format: U|col|row|type|special|hp|maxhp|dmg|range|speed|armored|fuel|maxfuel|
        ///         fuelConsumption|cooldown|hasActed|damageAmp|damageRed|isFortified|isDamageBlocked
        /// </summary>
        public static void WriteUnitRecord(StreamWriter writer, Tile tile, Unit u)
        {
            string name64 = ToBase64(u.Name ?? "Unit");
            int dmgAmpScaled = ToScaledInt(u.DamageAmplification);
            int dmgRedScaled = ToScaledInt(u.DamageReduction);
            int price = u.PriceAlloy;

            writer.WriteLine(
                "U|" +
                $"{tile.Column}|{tile.Row}|" +
                $"{name64}|" +
                $"{u.Armored}|" +
                $"{u.HP}|" +
                $"{u.UnitsPerTile}|" +
                $"{price}|" +
                $"{u.Range}|" +
                $"{u.Speed}|" +
                $"{u.Damage}|" +
                $"{u.FuelConsumption}|" +
                $"{dmgAmpScaled}|" +
                $"{dmgRedScaled}|" +
                $"{u.IsMech}|" +
                $"{u.ThisUnitType}|" +
                $"{u.ThisUnitCoreType}|" +
                $"{u.ThisUnitSpecial}"
            );
        }


        /// <summary>
        /// Loads a unit record and places it on the map
        /// Format: U|col|row|type|special|hp|maxhp|dmg|range|speed|armored|fuel|maxfuel|
        ///         fuelConsumption|cooldown|hasActed|damageAmp|damageRed|isFortified|isDamageBlocked
        /// </summary>


        public static void LoadUnit(string raw, string[] parts, Map map)
        {
            // U|col|row|name64|armored|hp|unitsPerTile|price|range|speed|damage|fuel|dmgAmpScaled|dmgRedScaled|isMech|unitType|core|special
            if (parts.Length != 18)
                throw new InvalidDataException($"Bad unit record (expected 18 fields): {raw}");

            int uCol = ParseInt(parts[1], "uCol", raw);
            int uRow = ParseInt(parts[2], "uRow", raw);

            Tile tileU = map.GetTile(uCol, uRow)
                ?? throw new InvalidDataException($"Unit tile not found ({uCol},{uRow}): {raw}");

            string name = FromBase64(parts[3])
                ?? throw new InvalidDataException($"Bad unit name64: {raw}");

            bool armored = ParseBool(parts[4], "armored", raw);
            int hp = ParseInt(parts[5], "hp", raw);
            int unitsPerTile = ParseInt(parts[6], "unitsPerTile", raw);
            int price = ParseInt(parts[7], "price", raw);
            int range = ParseInt(parts[8], "range", raw);
            int speed = ParseInt(parts[9], "speed", raw);
            int damage = ParseInt(parts[10], "damage", raw);
            int fuel = ParseInt(parts[11], "fuel", raw);

            int dmgAmpScaled = ParseInt(parts[12], "dmgAmpScaled", raw);
            int dmgRedScaled = ParseInt(parts[13], "dmgRedScaled", raw);
            bool isMech = ParseBool(parts[14], "isMech", raw);

            Unit.UnitType unitType = ParseUnitType(parts[15], raw);
            Core core = ParseCore(parts[16], raw);
            Unit.UnitSpecial special = ParseUnitSpecial(parts[17], raw);

            Unit unit = new Unit(
                armored,
                hp,
                unitsPerTile,
                name,
                price,
                range,
                speed,
                damage,
                fuel,
                core,
                unitType,
                special
            )
            {
                DamageAmplification = dmgAmpScaled / (double)DoubleScale,
                DamageReduction = dmgRedScaled / (double)DoubleScale,
                IsMech = isMech
            };
            unit.UnitSetUp();
            map.PlaceUnit(unit, tileU);
        }

        /// <summary>
        /// Normalizes and validates an asset file path
        /// Ensures consistent forward slashes and proper directory structure
        /// </summary>
        /// <param name="address">Asset address/path</param>
        /// <param name="folder">Folder name (e.g., "Tiles", "Units")</param>
        /// <param name="prefix">File prefix (e.g., "tile_", "unit_")</param>
        /// <returns>Normalized asset path</returns>


        public static string NormalizeAndValidateAssetAddress(string address, string folder, string prefix)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address of the asset cannot be empty.");

            // Allow either:
            //  - "typeOfAsset_field.png"
            //  - "Assets/folder/typeOfAsset_field.png"
            string fileName = Path.GetFileName(address);
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Asset must be named like {prefix}*.png");

            // Store as relative path
            return Path.Combine("Assets", folder, fileName);
        }


        /// <summary>
        /// Loads a building record and places it on the map
        /// Format: B|col|row|type|hp|prod|price
        /// </summary>
        public static void WriteHeader(StreamWriter writer, Map map)
        {
            writer.WriteLine($"VER|{CurrentSaveVersion}");
            writer.WriteLine($"SIZE|{map.Width}|{map.Height}");
        }

        public static void LoadBuilding(string raw, string[] parts, Map map)
        {
            // B|col|row|type|hp|prod
            if (parts.Length != 6 && parts.Length != 7)
                throw new InvalidDataException($"Bad building record (expected 6 or 7 fields): {raw}");

            int bCol = ParseInt(parts[1], "bCol", raw);
            int bRow = ParseInt(parts[2], "bRow", raw);

            Tile tileB = map.GetTile(bCol, bRow)
                ?? throw new InvalidDataException($"Building tile not found ({bCol},{bRow}): {raw}");

            Building.BuildingType bType = ParseBuildingType(parts[3], raw);
            if (bType == Building.BuildingType.None)
                throw new InvalidDataException($"BuildingType cannot be None: {raw}");

            int hp = ParseInt(parts[4], "hp", raw);
            int prod = ParseInt(parts[5], "prod", raw);

            int price = 0;
            if (parts.Length == 7)
                price = ParseInt(parts[6], "price", raw);

            Building building = new Building(
                newBuildingType: bType,
                newArmored: true,
                newHP: 0,
                placementPerTile: 1,
                mapObjectName: bType.ToString(),
                newPriceAlloy: price
            );

            building.HP = hp;
            building.BuildingyProduction = prod;
            building.Position = tileB;
            building.BuildingSetUpAssets();
            // direct assignment (safe even if Map/collections are not initialized)
            tileB.Building = building;
        }
        //helpers private to make less noise in main methods

        //key for econdig positions
        private static void ParseKey(string key, out int col, out int row)
        {
            string[] xy = key.Split(',');
            if (xy.Length != 2)
                throw new InvalidDataException("Bad coord key '" + key + "' (expected 'col,row').");

            col = int.Parse(xy[0]);
            row = int.Parse(xy[1]);
        }


        /// <summary>
        /// Parses the file header to extract version and map size
        /// </summary>

        private static void ReadHeader(string[] lines, out int version, out int width, out int height) // used to Read Headres of save files
        {
            version = 0;
            width = 0;
            height = 0;

            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                string[] parts = raw.Split('|');
                if (parts.Length == 0) continue;

                if (parts[0] == "VER" && parts.Length >= 2)
                    int.TryParse(parts[1], out version);
                else if (parts[0] == "SIZE" && parts.Length >= 3)
                {
                    int.TryParse(parts[1], out width);
                    int.TryParse(parts[2], out height);
                }
            }
        }

        /// <summary>
        /// Creates a unique key from column and row coordinates
        /// Format: "col-row"
        /// </summary>
        private static string MakeKey(int col, int row)
        {
            return col.ToString() + "," + row.ToString();
        }

        /// <summary>
        /// Parses a boolean from a string
        /// </summary>
        private static bool ParseBool(string s, string field, string raw)
        {
            if (!bool.TryParse(s, out bool outBool))
                throw new InvalidDataException($"Bad {field}: {raw}");
            return outBool;
        }

        private static Unit.UnitType ParseUnitType(string s, string raw)
        {
            if (!Enum.TryParse(s, true, out Unit.UnitType outPars) || !Enum.IsDefined(typeof(Unit.UnitType), outPars))
                throw new InvalidDataException($"Bad unitType '{s}': {raw}");
            return outPars;
        }

        private static Core ParseCore(string s, string raw)
        {
            if (!Enum.TryParse(s, true, out Core outValue) || !Enum.IsDefined(typeof(Core), outValue))
                throw new InvalidDataException($"Bad core '{s}': {raw}");
            return outValue;
        }

        private static Unit.UnitSpecial ParseUnitSpecial(string s, string raw)
        {
            if (!Enum.TryParse(s, true, out Unit.UnitSpecial outValue) || !Enum.IsDefined(typeof(Unit.UnitSpecial), outValue))
                throw new InvalidDataException($"Bad special '{s}': {raw}");
            return outValue;
        }

        private static Building.BuildingType ParseBuildingType(string s, string raw)
        {
            if (!Enum.TryParse(s, true, out Building.BuildingType outValue) || !Enum.IsDefined(typeof(Building.BuildingType), outValue))
                throw new InvalidDataException($"Bad buildingType '{s}': {raw}");
            return outValue;
        }
        private static int ParseInt(string s, string field, string raw)
        {
            if (!int.TryParse(s, out int outValue))
                throw new InvalidDataException($"Bad {field}: {raw}");
            return outValue;
        }

        private static string GetPath(string filename, string extention, string directory)
        {
            string name = filename;

            if (!Path.HasExtension(name)) //https://learn.microsoft.com/en-us/dotnet/api/system.io.path.hasextension?view=net-10.0
                name += extention;
            return Path.Combine(directory, name);
        }
        private static void EnsureDirectory(string filePath)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static int ToScaledInt(double value)// coverts double to int by scaling
        {
            // Example: 1.25 -> 125
            // Avoid locale issues entirely
            return (int)Math.Round(value * DoubleScale);
        }

        private static string ToBase64(string s)// written with help from ChatGPT
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        private static string? FromBase64(string s) // written with help from ChatGPT
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(s));
            }
            catch
            {
                return null;
            }
        }
        public static string GetGameSavesDirectoryPath() // used for SaveWindow to get paath to saves
        {
            return Path.GetFullPath(GameSavesDirectory);
        }
 


    }
}
