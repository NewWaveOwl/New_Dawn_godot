using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using static NewDawn.Models.Tile;
using System.IO;

namespace NewDawn.Models
{
    /// <summary>
    /// Represents a single hex tile on the game map.
    /// A Tile stores:
    /// - its coordinates (Column/Row)
    /// - its terrain type and terrain effects (slow, ranged debuff, damage modifiers, blocked)
    /// - references to what occupies it (Unit and/or Building)
    /// - the texture asset path used for rendering
    /// </summary>
    public class Tile
    {
        // ============================================================
        // ENUMS
        // ============================================================
        /// <summary>
        /// Supported terrain types for a tile.
        /// The terrain type determines both visual asset (tile_{terrain}.png)
        /// and gameplay effects (slow, range debuff, modifiers, blocked, etc.).
        /// </summary>
        public enum TerrainType
        {
            None,
            Field,
            Forest,
            Swamp,
            Mountain,
            City,
            FuelSource,
            AlloyField,
            FrozenDesert,
            Water
        }
        // ============================================================
        // FIELDS
        // ============================================================
        /// <summary>
        /// Unique tile identifier used for faster access / dictionary keys.
        /// In your coordinate constructor you auto-generate this from Column and Row.
        /// </summary>
        private int _Id;

        /// <summary>
        /// The terrain category of the tile (Field/Forest/etc.).
        /// This is the "source of truth" for terrain effects and asset selection.
        /// </summary>
        private TerrainType _terrainType;

        /// <summary>
        /// Hex coordinate system using Column/Row.
        /// (In your code, Column/Row act as axial coords, and ToScreenPosition uses that mapping.)
        /// </summary>
        private int _column;
        private int _row;

        /// <summary>
        /// Terrain-derived effects:
        /// - Slows: whether the tile halves speed or otherwise slows movement
        /// - RangedDebuff: how many range points are reduced when on this tile
        /// </summary>
        private bool _slows;
        private int _rangedDebuff;

        /// <summary>
        /// Small helper array returning [Row, Column].
        /// Note: This is mutable shared state; returning an array like this can be error-prone.
        /// Keeping as-is, but it is not ideal design.
        /// </summary>
        private int[] _tileCordinates = new int[2];

        /// <summary>
        /// Damage modifiers used by Unit.TerrainEffectCalculation:
        /// - DamageModifier: multiplier for outgoing damage (e.g., Swamp buffs)
        /// - DamageReductionModifier: multiplier for incoming damage (e.g., FrozenDesert)
        /// Defaults are neutral 1.0.
        /// </summary>
        private double _damageModifier = 1;
        private double _damageReductionModifier = 1;

        /// <summary>
        /// Whether the tile is blocked for movement.
        /// Typically true for:
        /// - Mountains
        /// - Water (for non-armored, but currently it is just blocked)
        /// - Any other tiles you want to force as non-walkable
        /// NOTE: IsWalkable() also checks occupancy/building in addition to Blocked.
        /// </summary>
        private bool _blocked = false;

        /// <summary>
        /// Unused field: looks like you intended a tile-specific unit asset address,
        /// but tiles use TileAssetAddress instead.
        /// Kept as-is (no logic relies on it here).
        /// </summary>
        private string _unitsAssetAddress;

        /// <summary>
        /// Occupancy references:
        /// - OccupyingUnit: unit currently on the tile (null if none)
        /// - Building: building on the tile (null if none)
        /// 
        /// IMPORTANT: these references must stay consistent with Unit.Position
        /// and any Map-level collections. Prefer using Map.MoveUnit / Map.PlaceBuilding.
        /// </summary>
        private Unit _occupyingUnit;
        private Building? _building;

        /// <summary>
        /// Tile asset path used for rendering the tile texture.
        /// Example: Assets/Tiles/tile_field.png
        /// This is normalized/validated by SaveAndLoaderHandler.
        /// </summary>
        private string _tileAssetAddress;

        // ============================================================
        // PROPERTIES
        // ============================================================
        /// <summary>
        /// Relative path to the tile texture asset.
        /// Setter normalizes + validates the address:
        /// - folder must be "Tiles"
        /// - file prefix must be "tile_"
        /// </summary>
        public string TileAssetAddress
        {
            get { return _tileAssetAddress; }
            set { _tileAssetAddress = SaveAndLoaderHandler.NormalizeAndValidateAssetAddress(value, "Tiles", "tile_"); }
        }

        /// <summary>
        /// True if the tile is blocked for movement (terrain-level blocking).
        /// Note: IsWalkable() also checks occupancy and building presence.
        /// </summary>
        public bool Blocked
        {
            get { return _blocked; }
            set { _blocked = value; }
        }

        /// <summary>
        /// Incoming damage multiplier applied to units on this tile.
        /// Typical values:
        /// - 1.0 = normal
        /// - 0.9 = take 10% more damage if your damage formula uses division later,
        ///   but in your Unit class you do "DamageReduction = modifier" and base.ReceiveDamage likely multiplies.
        /// 
        /// Guard:
        /// - must be > 0, else defaults to 1.0
        /// </summary>
        public double DamageReductionModifier
        {
            get { return _damageReductionModifier; }
            set
            {
                if (value > 0)
                {
                    _damageReductionModifier = value;
                }
                else
                {
                    _damageReductionModifier = 1;
                }
            }
        }

        /// <summary>
        /// Outgoing damage multiplier for units on this tile.
        /// Guard:
        /// - must be > 0, else defaults to 1.0
        /// </summary>
        public double DamageModifier
        {
            get { return _damageModifier; }
            set
            {
                if (value > 0)
                {
                    _damageModifier = value;
                }
                else
                {
                    _damageModifier = 1;
                }
            }
        }

        /// <summary>
        /// Tile unique ID.
        /// Guard:
        /// - must be > 0, else set to 0 (sentinel "bad" value)
        /// </summary>
        public int Id
        {
            get { return _Id; }
            set
            {
                if (value > 0)
                {
                    _Id = value;
                }
                else
                {
                    _Id = 0;
                }
            }
        }

        /// <summary>
        /// Terrain type of this tile.
        /// Guard:
        /// - if invalid enum value provided, defaults to Field.
        /// </summary>
        public TerrainType TileTerrainType
        {
            get { return _terrainType; }
            set
            {
                if (!Enum.IsDefined(typeof(TerrainType), value))
                {
                    _terrainType = TerrainType.Field;
                }
                else
                {
                    _terrainType = value;
                }
            }
        }

        /// <summary>
        /// Whether the tile slows movement (used by Unit.TerrainEffectCalculation).
        /// </summary>
        public bool Slows
        {
            get { return _slows; }
            set { _slows = value; }
        }

        /// <summary>
        /// Axial/hex coordinate column.
        /// </summary>
        public int Column
        {
            get { return _column; }
            set { _column = value; }
        }

        /// <summary>
        /// Axial/hex coordinate row.
        /// </summary>
        public int Row
        {
            get { return _row; }
            set { _row = value; }
        }

        /// <summary>
        /// Unit currently occupying this tile (null if none).
        /// NOTE: for correctness, Map.MoveUnit should update this and unit.Position together.
        /// </summary>
        public Unit OccupyingUnit
        {
            get { return _occupyingUnit; }
            set { _occupyingUnit = value; }
        }

        /// <summary>
        /// Building placed on this tile (null if none).
        /// NOTE: Your backing field is nullable Building?, but property type is Building (non-nullable).
        /// This will compile with nullable warnings; consider making this `Building?`.
        /// Kept as-is per your request (comments only).
        /// </summary>
        public Building Building
        {
            get { return _building; }
            set { _building = value; }
        }

        /// <summary>
        /// Returns the tile coordinates as an array: [Row, Column].
        /// WARNING: Returns a shared mutable array that belongs to the Tile.
        /// If any caller modifies it, it changes the Tile's internal array.
        /// Prefer returning a new array or a struct/tuple in the future.
        /// </summary>
        public int[] TileCordinates()
        {
            _tileCordinates[0] = Row;
            _tileCordinates[1] = Column;
            return _tileCordinates;
        }

        /// <summary>
        /// Range debuff applied to units on this tile.
        /// Must be >= 0.
        /// </summary>
        public int RangedDebuff
        {
            get { return _rangedDebuff; }
            set
            {
                if (value >= 0) _rangedDebuff = value;
                else _rangedDebuff = 0;
            }
        }
        // ============================================================
        // CONSTRUCTORS
        // ============================================================

        /// <summary>
        /// Coordinate constructor used by Map creation.
        /// - Sets Column/Row
        /// - Auto-generates a stable ID from coordinates
        /// - Applies terrain rules and asset via SetTerrain
        /// 
        /// NOTE: ID generation formula:
        ///   Id = (column + 1000) * 1000 + (row + 1000)
        /// This gives a unique ID for coordinates within roughly [-1000..+999] range,
        /// but can collide outside that range.
        /// </summary>>
        /// 
        public Tile(int column, int row, TerrainType terrainType = TerrainType.Field)
        {
            // Set hex coordinates (Q and R are the same as Column and Row)
            Column = column;
            Row = row;

            // Auto-generate ID based on coordinates
            Id = (column + 1000) * 1000 + (row + 1000);

            // Initialize other properties
            SetTerrain(terrainType);
        }
        // ============================================================
        // TERRAIN EFFECTS
        // ============================================================

        /// <summary>
        /// Applies gameplay effects for a given terrain type.
        /// This method sets:
        /// - Slows
        /// - RangedDebuff
        /// - DamageReductionModifier / DamageModifier
        /// - Blocked (for non-walkable terrain like Mountain, Water)
        /// 
        /// IMPORTANT:
        /// This method does not set TileTerrainType itself; SetTerrain does both:
        /// - sets terrain type
        /// - applies effects
        /// - assigns asset path
        /// </summary>
        public void ApplyTerrainEffects(TerrainType newTerrainType)
        {
            switch (newTerrainType)
            {
                case TerrainType.Field:
                    Slows = false;
                    RangedDebuff = 0;
                    DamageReductionModifier = 0.75;//25% damage reduction for all none armored units
                    break;
                case TerrainType.Forest:
                    Slows = true;
                    RangedDebuff = 1;                  
                    break;
                case TerrainType.Swamp:
                    Slows = true;
                    RangedDebuff = 1;
                    DamageReductionModifier = 0.5; //50% damage modifier
                    DamageModifier = 1.5; //50% damage buff for units in the Swamp
                    break;
                case TerrainType.Mountain:
                    Slows = true; //cannot cross
                    RangedDebuff = 0;
                    Blocked = true;
                    //Adjacent tiles get alloys (handled in map class)
                    break;
                case TerrainType.City:
                    Slows = true;
                    RangedDebuff = 2;
                    break;
                case TerrainType.FuelSource:
                    Slows = true;
                    RangedDebuff = 0;
                    break;
                case TerrainType.AlloyField:
                    Slows = false;
                    RangedDebuff = 0;
                    
                    break;
                case TerrainType.FrozenDesert:
                    Slows = false; //only armored units can cross
                    RangedDebuff = 0;//fully block attack
                    DamageReductionModifier = 0.9; //10% damage to all units on frozen desert tile
                    break;
                case TerrainType.Water:
                    Slows = true; //only armored units can cross
                    RangedDebuff = 0;
                    Blocked = true;
                    break;
                default: // add error handling or defoult values    
                    break;
            }
        }
        // ============================================================
        // TEXTURE / TERRAIN SETUP
        // ============================================================
        /// <summary>
        /// Sets the terrain type for this tile and applies all derived state:
        /// - TileTerrainType (enum)
        /// - ApplyTerrainEffects (slow, debuffs, blocked, modifiers)
        /// - TileAssetAddress (render texture)
        /// 
        /// This should be the only method you call when changing terrain.
        /// </summary>
        public void SetTerrain(TerrainType terrain) //sets terrain and applies its effects
        {
            TileTerrainType = terrain;
            ApplyTerrainEffects(terrain);
            TileAssetAddress = AssetFor(terrain);
        }
        /// <summary>
        /// Returns the relative asset path for a given terrain type.
        /// Convention:
        ///   Assets/Tiles/tile_{terrainLower}.png
        /// Example:
        ///   TerrainType.Field -> Assets/Tiles/tile_field.png
        /// </summary>
        public static string AssetFor(TerrainType terrain) //gets the asset address for the terrain type
        {
 
            string key = terrain.ToString().ToLowerInvariant();
            return Path.Combine("Assets", "Tiles", $"tile_{key}.png"); // Assets/Tiles/tile_{terrain}.png
        }
        // ============================================================
        // MAP-INTERACTION HELPERS
        // ===========================================================
        /// <summary>
        /// Returns true if a unit is currently occupying this tile.
        /// </summary>
        public bool IsOccupied()
        {
            return OccupyingUnit != null;
        }

        /// <summary>
        /// Returns true if the tile is walkable for movement.
        /// Current rule:
        /// - not Blocked
        /// - no Building placed on it
        /// - no unit occupying it
        /// </summary>
        public bool IsWalkable()
        {
            return !Blocked && Building == null && !IsOccupied();
        }
        /// <summary>
        /// Converts hex (Column, Row) coordinates into a screen coordinate (x,y) for rendering.
        /// This uses an axial-coordinate layout mapping.
        /// 
        /// hexSize is the hex "radius" (distance from center to corner) used consistently in rendering.
        /// 
        /// NOTE:
        /// Your MapEditor rendering previously used a different formula (flat-top with offset columns).
        /// This method is a separate axial math approach. If your visuals or clicking mismatch,
        /// you must ensure your entire rendering and hit-testing use the same coordinate model.
        /// </summary>
        // Convert hex coordinates to screen position for rendering (I chatgpt-ed this method because i couldn't do it myself)
        public System.Windows.Point ToScreenPosition(double hexSize)
        {
            double x = hexSize * (3.0 / 2.0 * Column);
            double y = hexSize * (Math.Sqrt(3) / 2 * Column + Math.Sqrt(3) * Row);
            return new System.Windows.Point(x, y);
        }

    }
}
