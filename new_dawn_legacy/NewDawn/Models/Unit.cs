using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewDawn.Models.Tile;

namespace NewDawn.Models
{
    /// <summary>
    /// Represents a combat unit placed on the map.
    /// A Unit is also a MapObject (has HP, Armor, Price, etc.) and adds:
    /// - movement stats (Speed)
    /// - combat stats (Range, Damage)
    /// - fuel tracking
    /// - passive flags (Commander, HoverTransporter)
    /// - special abilities + cooldown management
    /// </summary>
    public class Unit:MapObject
    {
        // ============================================================
        // ENUMS
        // ============================================================


        /// <summary>
        /// "Core" is a special upgrade/trait slot for units.
        /// Currently these values are just identifiers; the effects are implemented elsewhere (or later).
        /// </summary>
        public enum Core
        {
            None = 0,
            SafeEjection = 1,
            WarpDeployment = 2,
            ReverseTimeCore = 3,
            LastWord = 4

        }

        /// <summary>
        /// Per-unit special category identifier.
        /// This is separate from UnitType: UnitType defines the unit "class",
        /// UnitSpecial can be treated as a secondary tag (ability set).
        /// </summary>
        public enum UnitSpecial
        {
            None = 0,
            RailgunnerSpecial = 1,
            ProtectorsSpecial = 2,
            EngeneerSpecial = 3,
            RACMSpecial = 4,
            HawkSpecial = 5,
            HoverTankSpecial = 6,
            WaspSpecial = 7,
            BOHSpecial = 8,
            SunlightSpecial = 9,
            BreacherSpecial = 10,
            RedoubtSpecial = 11,
            CMPSpecial = 12,
            HoverTransporterSpecial = 13
        }


        /// <summary>
        /// Primary unit type. This determines:
        /// - what image asset is used
        /// - passives (Commander, HoverTransporter)
        /// - default attack modifiers (DoDamageMainGun switch)
        /// - which special ability is executed
        /// 
        /// CMP = Commander Mech Platform.
        /// </summary>
        public enum UnitType //CMP - Commader Mech Platform 
        {
            None = 0,
            Railgunner =1, Protectors =2, Engeneer = 3, RACM = 4 , Hawk = 5, HoverTank = 6 , Wasp = 7 , BOHS = 8 , Sunlight = 9 , Breacher = 10 , Redoubt = 11 , CMP = 12 , HoverTransporter = 13 , Rebel = 14
        }

        // ============================================================
        // FIELDS / CONSTANTS
        // ============================================================
        /// <summary>
        /// Base fuel pool used to compute MaxFuel in the constructor.
        /// Static because all units share the same "base fuel" rule.
        /// </summary>
        private static int _baseFuel = 10;

        /// <summary>
        /// Global caps for unit stats.
        /// These limits enforce game design constraints:
        /// - Range is clamped to [MINRANGE..MAXRANGE]
        /// - Speed is clamped (see Speed setter)
        /// </summary>
        private readonly int MAXRANGE = 5; // global maximum attack range
        private readonly int MINRANGE = 1; // global minimum attack range
        private readonly int MAXSPEED = 10; // global maximum movement speed

        /// <summary>
        /// Tracks whether the unit already used its action in the current turn.
        /// Typically used by Game/Turn system.
        /// </summary>
        private bool HasAction;

        /// <summary>
        /// Original stats stored so terrain and temporary buffs can be reset.
        /// Example: TerrainEffectCalculation modifies Range/Speed temporarily.
        /// </summary>
        private int _originalRange;
        private int _range;

        private int _damage;

        private int _originalSpeed;
        private int _speed;

        /// <summary>
        /// How much fuel movement/ability use consumes (depending on design).
        /// </summary>
        private int _fuelConsumption;

        /// <summary>
        /// Unit core and classification.
        /// </summary>
        private Core _coreType;
        private bool _isMech;
        private UnitSpecial _specialType;
        private UnitType _thisUnitType;

        /// <summary>
        /// Damage multipliers.
        /// DamageAmplification increases outgoing damage.
        /// DamageReduction (defined in MapObject) reduces incoming damage.
        /// </summary>
        private double _damageAmplification;

        /// <summary>
        /// The tile this unit occupies. Must remain consistent with Tile.OccupyingUnit.
        /// </summary>
        private Tile _position;

        /// <summary>
        /// Relative path to the unit image asset.
        /// Typically points into Assets/Units/.
        /// </summary>
        private string _unitAssetAddress;

        /// <summary>
        /// Flags to mark special categories.
        /// - Commander affects game setup rules (e.g., only 1 per player).
        /// - HoverTransporter overrides damage receiving logic.
        /// </summary>
        private bool _isCommander = false;
        private bool _isHoverTransporter = false;

        /// <summary>
        /// Cached max HP value. You also have HP in MapObject.
        /// MaxHP is used to calculate health thresholds for abilities (Hawk teleport).
        /// </summary>
        private int _maxHP;

        /// <summary>
        /// Special ability cooldown tracking.
        /// Cooldown counts down each turn until 0 (available).
        /// </summary>
        private int _specialCooldown = 0;
        private const int SPECIAL_COOLDOWN_TURNS = 3;

        // ============================================================
        // STATUS EFFECTS / TEMPORARY BUFF STATE
        // ============================================================

        /// <summary>
        /// Fortify is a "mode" (RACM / HoverTank) that changes damage reduction / range.
        /// Fortify persists until removed (RemoveFortify), usually when the unit moves.
        /// </summary>
        private bool _isFortified = false;

        /// <summary>
        /// Fortify effect parameters.
        /// Stored so we can undo the effect later (RemoveFortify).
        /// </summary>
        private int _fortifyDamageReduction = 0; // % reduction value (informational)
        private int _fortifyRangeBonus = 0;      // range bonus

        /// <summary>
        /// A placeholder flag: "damage blocked" intended for HoverTransporter.
        /// Currently the actual behavior is implemented in ReceiveDamage override.
        /// </summary>
        private bool _isDamageBlocked = false;

        // ============================================================
        // FUEL TRACKING
        // ============================================================

        /// <summary>
        /// Current and maximum fuel.
        /// In your later design you said "Player has fuel, Units should not",
        /// but this code still implements unit-level fuel.
        /// </summary>
        private int _currentFuel;
        private int _maxFuel;

        // ============================================================
        // BUFF TRACKING (BOHS)
        // ============================================================

        /// <summary>
        /// Tracks whether this unit is currently providing its BOHS aura.
        /// Useful for UI/logic; actual buff is applied by Special_BOHS_AuraBoost.
        /// </summary>
        private bool _hasBoostAura = false;


        //properties
        /// <summary>
        /// Base fuel shared by all units (used to calculate MaxFuel at creation).
        /// </summary>
        public static int BaseFuel
        {
            get { return _baseFuel; }
        }
        /// <summary>
        /// Turn system flag: whether the unit already acted this turn.
        /// </summary>
        public bool HasPerformedAction
        {
            get { return HasAction; }
            set { HasAction = value; }
        }
        /// <summary>
        /// Asset address for unit image.
        /// Setter normalizes and validates the path:
        /// - folder should be "Units"
        /// - file prefix should be "unit_"
        /// This is meant to prevent bad paths from CSV or invalid user input.
        /// </summary>
        public string UnitAssetAddress
        {
            get { return _unitAssetAddress; }
            set 
            {
                _unitAssetAddress = SaveAndLoaderHandler.NormalizeAndValidateAssetAddress(value, "Units", "unit_");   // "Units" is folder and "unit_" is prefix for unit assets
            }
        }
        /// <summary>
        /// Unit special tag (secondary classification).
        /// Setter guards against invalid enum values.
        /// </summary>
        public UnitSpecial ThisUnitSpecial
        {
            get { return _specialType; }
            set 
            {
                if(!Enum.IsDefined(typeof(UnitSpecial), value)) _specialType = UnitSpecial.None; //default none
                else _specialType = value;
            }
        }
        /// <summary>
        /// Unit core type (special trait).
        /// Setter guards against invalid enum values.
        /// </summary>
        public Core ThisUnitCoreType
        {
            get { return _coreType; }
            set 
            {
                if(!Enum.IsDefined(typeof(Core), value)) _coreType = Core.None; //default none
                else _coreType = value;
            }
        }
        /// <summary>
        /// Primary unit class.
        /// Setter guards against invalid enum values.
        /// </summary>
        public UnitType ThisUnitType
        {
            get { return _thisUnitType; }
            set 
            {
                if(!Enum.IsDefined(typeof(UnitType), value)) _thisUnitType = UnitType.None; //default none
                else _thisUnitType = value;
            }
        }
        /// Fuel consumption per move/action (depending on how you design fuel).
        /// Cannot be negative.
        /// </summary>
        public int FuelConsumption
        {
            get { return _fuelConsumption; }
            set
            {
                if (value >= 0)
                    _fuelConsumption = value;
                else
                {
                    _fuelConsumption = 0; // default 
                }
            }
        }

        /// <summary>
        /// The tile this unit is currently placed on.
        /// IMPORTANT: This must stay consistent with the Tile's OccupyingUnit reference.
        /// Normally, movement should be executed via Map.MoveUnit so both references update together.
        /// </summary>
        public Tile Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Multiplier for outgoing damage.
        /// Must be >= 1.0 (no "negative buffs" using amplification).
        /// </summary>
        public double DamageAmplification
        {
            get { return _damageAmplification; }
            set
            {
                if (value >= 1)
                    _damageAmplification = value;
                else
                {
                    _damageAmplification = 1; // default 
                }
            }
        }
        /// <summary>
        /// Marks whether the unit is a mech.
        /// Used by terrain logic (City slows mechs).
        /// </summary>
        public bool IsMech
        {
            get { return _isMech; } set { _isMech = value; }
        }
        /// <summary>
        /// Movement speed (tiles per move, or movement budget).
        /// NOTE: This setter has suspicious logic:
        ///   - if value is invalid, it sometimes sets MAXSPEED, sometimes 0.
        ///   - the branch "else if (value < MAXSPEED) _speed = MAXSPEED" will never happen
        ///     because it is already handled by the first if.
        /// Kept as-is, but you likely intended:
        ///   clamp to [0..MAXSPEED]
        /// </summary>
        public int Speed
        {
            get { return _speed; }
            set
            {
                if (value >= 0 && value < MAXSPEED)
                    _speed = value;
                else if (value < MAXSPEED) _speed = MAXSPEED;
                else _speed = 0;
            }
        }
        /// <summary>
        /// Attack range clamped to [MINRANGE..MAXRANGE].
        /// </summary>
        public int Range
        {
            get { return _range; }
            set 
            {
                if (value < MINRANGE) _range = MINRANGE;
                else if (value > MAXRANGE) _range = MAXRANGE; // if the range tries to be higher then 5 it max capped at five
                else _range = value;// range cannot be lower then MIN RANGE
            }
        }
        /// <summary>
        /// Base damage value. Must be positive.
        /// </summary>
        public int Damage 
        { 
            get { return _damage; } 
            set 
            { 
                if(value > 0)
                    _damage = value;
                else
                    _damage = 1;
            }
        }
        // ============================================================
        // SPECIAL ABILITY STATE PROPERTIES
        // ============================================================

        /// <summary>
        /// Turns remaining until special is available.
        /// </summary>
        public int SpecialCooldown
        {
            get { return _specialCooldown; }
            set
            {
                if (value < 0)
                    _specialCooldown = 0;
                else
                    _specialCooldown = value;
            }
        }
        /// <summary>
        /// Convenience property: special is usable when cooldown is 0.
        /// </summary>
        public bool IsSpecialAvailable
        {
            get { return _specialCooldown == 0; }
        }
        /// <summary>
        /// Fortify mode active.
        /// </summary>
        public bool IsFortified
        {
            get { return _isFortified; }
            set { _isFortified = value; }  
        }
        /// <summary>
        /// Placeholder flag for "damage blocked" behavior.
        /// Currently not set anywhere in this class (always false).
        /// The real block behavior is implemented in ReceiveDamge override for HoverTransporter.
        /// </summary>
        public bool IsDamageBlocked
        {
            get { return _isDamageBlocked; }
        }
        // ============================================================
        // FUEL PROPERTIES
        // ============================================================

        /// <summary>
        /// Current fuel cannot go below 0 or above MaxFuel.
        /// </summary>
        public int CurrentFuel
        {
            get { return _currentFuel; }
            set
            {
                if (value < 0)
                    _currentFuel = 0;
                else if (value > _maxFuel)
                    _currentFuel = _maxFuel;
                else
                    _currentFuel = value;
            }
        }
        /// <summary>
        /// Maximum fuel capacity.
        /// No validation here; constructor sets it.
        /// </summary>
        public int MaxFuel
        {
            get { return _maxFuel; }

            set { _maxFuel = value;}
        }
        /// <summary>
        /// Maximum HP snapshot for threshold checks and UI.
        /// </summary>
        public int MaxHP
        {
            get { return _maxHP; }
            set
            {
                if (value >= 0)
                    _maxHP = value;
                else
                    _maxHP = 0;
            }
        }
        /// <summary>
        /// True if this unit is a Commander unit.
        /// Used for game rules and UI.
        /// </summary>
        public bool IsCommander
        {
            get { return _isCommander; }
            set { _isCommander = value; }
        }
        /// <summary>
        /// True if this unit is a HoverTransporter (special damage handling).
        /// </summary>
        public bool IsHoverTransporter
        {
            get { return _isHoverTransporter; }
            set { _isHoverTransporter = value; }
        }

        /// <summary>
        /// BOHS aura active flag.
        /// </summary>
        public bool HasBoostAura
        {
            get { return _hasBoostAura; }
            set { _hasBoostAura = value; }
        }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        //constructors
        public Unit(bool newArmored, int newHP, int placementPerTile, string mapObjectName, int newPrice, int newRange, int newSpeed, int newDamge, int newFuelConsumption, Core newCore, UnitType newUnitType, UnitSpecial newUnitSpecial)
            : base (newArmored, newHP, placementPerTile, mapObjectName, newPrice)
        {
            // Initialize combat stats with clamping.
            MaxHP = newHP;
            Range = newRange;
            _originalRange = Range;
            // Initialize speed and store original speed
            Speed = newSpeed;
            _originalSpeed = newSpeed;
            // Initialize damage
            Damage = newDamge;
            // Fuel system setup.
            FuelConsumption = newFuelConsumption;
            // Max fuel formula: base fuel + 2 * speed.
            // This makes fast units also have larger fuel tanks.
            MaxFuel = BaseFuel + (newSpeed * 2); 
            CurrentFuel = MaxFuel;
            // Classification metadata
            ThisUnitCoreType = newCore;
            ThisUnitType = newUnitType;
            ThisUnitSpecial = newUnitSpecial;

            // Default multipliers (no buffs/debuffs).
            DamageAmplification = 1; //default no amplification

            DamageReduction = 1; //default: no reduction (DamageReduction belongs to MapObject)
        }
        // ============================================================
        // METHODS
        // ============================================================
        /// <summary>
        /// Sets up unit visuals and passives based on ThisUnitType.
        /// This is the method you should call after loading a unit from CSV,
        /// because CSV should store "type" and you derive asset/passives from type.
        /// 
        /// Side effects:
        /// - Sets UnitAssetAddress (validated)
        /// - Sets IsCommander or IsHoverTransporter where applicable
        /// </summary>
        public void UnitSetUp() //sets up the unit asset address based on its type and assigns passives to units
        {
            switch (ThisUnitType)
            {

                case UnitType.RACM:
                    UnitAssetAddress = "Assets/Units/unit_racm.png";
                    break;
                case UnitType.Engeneer:

                    UnitAssetAddress = "Assets/Units/unit_engeneer.png";

                    break;
                case UnitType.Railgunner:
                    UnitAssetAddress = "Assets/Units/unit_railgunner.png";
                    break;
                case UnitType.CMP:
                    UnitAssetAddress = "Assets/Units/unit_cmp.png";
                    IsCommander = true;
                    break;
                case UnitType.Protectors:
                    UnitAssetAddress = "Assets/Units/unit_protectors.png";
                    break;
                case UnitType.Hawk:
                    UnitAssetAddress = "Assets/Units/unit_hawk.png";
                    break;
                case UnitType.HoverTank:
                    UnitAssetAddress = "Assets/Units/unit_hovertank.png";
                    break;
                case UnitType.Wasp:
                    UnitAssetAddress = "Assets/Units/unit_wasp.png";
                    break;
                case UnitType.BOHS:
                    UnitAssetAddress = "Assets/Units/unit_bohs.png";
                    break;
                case UnitType.Sunlight:
                        UnitAssetAddress = "Assets/Units/unit_sunlight.png";
                    break;
                case UnitType.Breacher:
                    UnitAssetAddress = "Assets/Units/unit_breacher.png";
                    break;
                case  UnitType.Redoubt:
                    UnitAssetAddress = "Assets/Units/unit_redoubt.png";
                    break;
                case UnitType.HoverTransporter:
                    UnitAssetAddress = "Assets/Units/unit_hovertransporter.png"; 
                    IsHoverTransporter = true;
                    break;
                case UnitType.Rebel:
                    UnitAssetAddress = "Assets/Units/unit_rebel.png";
                    break;
                // Add cases for other unit types as needed
                default:
                    UnitAssetAddress = "Assets/Units/unit_default.png";
                    break;
            }

        }
        // ============================================================
        // FUEL CONSUMPTION METHODS
        // ============================================================
        /// <summary>
        /// Attempts to consume the given amount of fuel.
        /// Returns true if consumption succeeded; false if not enough fuel.
        /// </summary>
        public bool ConsumeFuel(int amount)
        {
            if (_currentFuel >= amount)
            {
                _currentFuel -= amount;
                return true;
            }
            return false; // Not enough fuel
        }
        /// <summary>
        /// Refills fuel back to maximum.
        /// Typically called at start of turn or when unit resupplies.
        /// </summary>
        public void RefillFuel()
        {
            _currentFuel = _maxFuel;

        }

        /// <summary>
        /// Returns true if the unit has at least <amount> fuel.
        /// </summary>
        public bool HasEnoughFuel(int amount)
        {
             return _currentFuel >= amount;
        }


        // ============================================================
        // STATS METHODS
        // ============================================================
        /// <summary>
        /// Resets Range and Speed back to original values stored at creation.
        /// Use this after temporary effects (terrain debuffs, buffs) end.
        /// </summary>
        public void RefreshRangeAndSpeed() //resets range and speed to original values
        {
            Range = _originalRange;
            Speed = _originalSpeed;
        }

        // ============================================================
        // DAMAGE METHODS
        // ============================================================
        /// <summary>
        /// Calculates damage dealt by the unit's main weapon against Target.
        /// 
        /// Rules:
        /// - If target is outside unit Range, return 0 (cannot hit).
        /// - Then apply special per-unit damage logic:
        ///   Railgunner: triples damage vs armored targets
        ///   RACM/Wasp: half damage vs armored targets
        ///   Sunlight: damage decreases with distance
        ///   Breacher: damage decreases with distance (minimum divisor 1)
        ///   Default: DoDamageDefault()
        /// 
        /// IMPORTANT: This method returns a damage number; it does NOT apply it.
        /// Caller is expected to call Target.ReceiveDamage(damage).
        /// </summary>
        public int DoDamgeMainGun(Unit Target, int distanceToTarget)
        {
            // Out of range check: cannot deal damage outside Range.
            if (distanceToTarget > this.Range)
            {
                return 0;//cannot damage targets outside of the distance
            }
            int outcomingDamage = 0;
            switch (ThisUnitType)
            {
                case UnitType.Railgunner:
                if (Target.Armored)
                    outcomingDamage = (int)(Damage * (DamageAmplification) * 3); //triples damage to armored Units
                else
                    outcomingDamage = DoDamgeDefault();
                break;
                case UnitType.RACM:
                    if(Target.Armored) outcomingDamage = DoDamgeDefault() / 2; //deals twice less damage to all armored units
                    else outcomingDamage = DoDamgeDefault();
                break;
                case UnitType.Wasp:
                    if (Target.Armored) outcomingDamage = DoDamgeDefault() / 2; //deals twice less damage to all armored units
                    else outcomingDamage = DoDamgeDefault();
                break;
                case UnitType.Sunlight:
                    outcomingDamage = DoDamgeDefault()/ distanceToTarget;
                    break;
                case UnitType.Breacher:
                    outcomingDamage = DoDamgeDefault() / Math.Max(1, distanceToTarget);//deals 10% damage to none armored units
                    break;
                default:
                outcomingDamage = DoDamgeDefault(); 
                break;
            }
            return outcomingDamage;
        }
        /// <summary>
        /// Default damage formula: base Damage multiplied by current DamageAmplification.
        /// Any rounding truncates because of int cast.
        /// </summary>
        public int DoDamgeDefault()
        {
            return (int)(Damage * DamageAmplification);
        }
        // ============================================================
        // TERRAIN METHODS
        // ============================================================

        /// <summary>
        /// Applies terrain effects from the tile the unit occupies.
        /// This method adjusts temporary stats:
        /// - Range is reduced by tile.RangedDebuff
        /// - Speed is halved if tile.Slows
        /// - Special terrain rules (forest/swamp/city/frozen desert)
        /// 
        /// IMPORTANT: This method mutates stats directly.
        /// You should call RefreshRangeAndSpeed() or EndTurnCleanup() when appropriate.
        /// </summary>
        public void TerrainEffectCalculation(Tile tileUnitOcupies) 
        {
            Range = _originalRange - tileUnitOcupies.RangedDebuff;
            if (tileUnitOcupies.Slows) Speed = _originalSpeed / 2;
            switch (tileUnitOcupies.TileTerrainType) //switch statement to apply effects of the tiles on the unit
            {
                case TerrainType.Forest:
                    if (!this.Armored) DamageReduction = tileUnitOcupies.DamageReductionModifier; // damage reduction for none armored units in the Forest
                    break;
                case TerrainType.Swamp:
                    if (!this.Armored)
                    {
                        DamageReduction = tileUnitOcupies.DamageReductionModifier; //none armored units get 50% damage reduction  in the swamp
                        DamageAmplification = tileUnitOcupies.DamageModifier; // none armored units get 50% damage buff in the swamp
                    }
                    break;
                case TerrainType.City:
                    if (IsMech)
                    {
                        Speed = _originalSpeed / 2;
                    }
                    break;
                case TerrainType.FrozenDesert:
                    DamageReduction = tileUnitOcupies.DamageReductionModifier; // all units in the Frozen desert receive 10% more damage
                    break;
                default: // add error handling
                    break;

            }
        }


        #region Special Ability Stuff
        /// <summary>
        /// Decrements special cooldown by 1 turn (down to 0).
        /// Call this once at the start of the unit's turn.
        /// </summary>
        public void ReduceSpecialCooldown()
        {
            if (_specialCooldown > 0)
            {
                _specialCooldown--;
            }
        }

        /// <summary>
        /// Starts the cooldown after a successful special use.
        /// </summary>
        private void ActivateSpecialCooldown()
        {
            _specialCooldown = SPECIAL_COOLDOWN_TURNS;
        }

        /// <summary>
        /// Removes temporary buffs that should end at end of turn.
        /// IMPORTANT: This resets DamageAmplification and DamageReduction to base values.
        /// That means any long-duration buffs are not supported unless handled separately.
        /// 
        /// Note: Fortify is NOT removed here; fortify persists until unit moves or RemoveFortify is called.
        /// </summary>
        public void EndTurnCleanup()
        {
            // Reset amplification and reduction to base values
            DamageAmplification = 1.0;
            DamageReduction = 1.0;

            // Remove BOHS aura if present
            _hasBoostAura = false;

            // Note: Don't remove fortify here - it persists until unit moves
        }

        /// <summary>
        /// Applies an incoming-damage reduction multiplier.
        /// Example: reductionMultiplier = 0.9 means "take 10% less damage".
        /// Must be (0..1].
        /// </summary>
        public void ApplyDamageReduction(double reductionMultiplier)
        {
            if (reductionMultiplier > 0 && reductionMultiplier <= 1.0)
            {
                DamageReduction *= reductionMultiplier;
            }
        }
        /// <summary>
        /// Applies an outgoing-damage amplification multiplier.
        /// Example: amplificationMultiplier = 1.1 means "deal 10% more damage".
        /// Must be >= 1.0.
        /// </summary>
        public void ApplyDamageAmplification(double amplificationMultiplier)
        {
            if (amplificationMultiplier >= 1.0)
            {
                DamageAmplification *= amplificationMultiplier;
            }
        }
        /// <summary>
        /// Executes this unit's special ability.
        /// 
        /// Parameters:
        /// - map: required, because most specials need map queries or movement.
        /// - targetTile: optional, used by tile-targeted specials (Engineer mine, teleport, jump, etc.)
        /// - targetUnit: optional, used by unit-targeted specials (Sunlight line damage, Breacher displace)
        /// 
        /// Returns true if the special successfully executed and cooldown was started.
        /// Returns false if the special is unavailable or conditions fail.
        /// </summary>
        public bool UseSpecial(Map map, Tile targetTile = null, Unit targetUnit = null)
        {
            // Check if special is available
            if (!IsSpecialAvailable)
            {
                return false; // On cooldown
            }

            bool success = false;

            // Execute special based on unit type
            switch (ThisUnitType)
            {
                case UnitType.Railgunner:
                case UnitType.Rebel:
                    // No special ability
                    return false;

                case UnitType.Protectors:
                    success = Special_Protectors_Shield(map);
                    break;

                case UnitType.Engeneer:
                    success = Special_Engineer_PlaceMine(map, targetTile);
                    break;

                case UnitType.RACM:
                    success = Special_RACM_Fortify();
                    break;

                case UnitType.Hawk:
                    success = Special_Hawk_Teleport(map, targetTile);
                    break;

                case UnitType.HoverTank:
                    success = Special_HoverTank_Fortify();
                    break;

                case UnitType.Wasp:
                    success = Special_Wasp_MoveAfterAttack(map, targetTile);
                    break;

                case UnitType.BOHS:
                    success = Special_BOHS_AuraBoost(map);
                    break;

                case UnitType.Sunlight:
                    success = Special_Sunlight_LineDamage(map, targetUnit);
                    break;

                case UnitType.Breacher:
                    success = Special_Breacher_Displace(map, targetUnit, targetTile);
                    break;

                case UnitType.Redoubt:
                    success = Special_Redoubt_Jump(map, targetTile);
                    break;

                case UnitType.CMP:
                    // does not have a special on could down uses build method and generates passive income but this ability does not have cooldown || this implemented im Map class(method PlaceBuilding) and Player class (UpdateBuildingIncome)
                    break;

                case UnitType.HoverTransporter:
                    //doesn't have special ability, all incoming daamge is 100% by deathful so I put it in UnitSetup and overwritten recive damage method
                    break;

                default:
                    return false;
            }

            // If successful, activate cooldown
            if (success)
            {
                ActivateSpecialCooldown();
            }

            return success;
        }

        /// <summary>
        /// Protectors special:
        /// Applies a 50% damage reduction buff to all adjacent units.
        /// 
        /// IMPORTANT: This modifies DamageReduction directly.
        /// If you call EndTurnCleanup at end turn, it will reset reductions to 1.0.
        /// That means this buff likely lasts only until cleanup.
        /// </summary>
        private bool Special_Protectors_Shield(Map map)
        {
            if (Position == null || map == null)
                return false;

            List<Tile> adjacentTiles = map.GetNeighbors(Position);

            foreach (Tile tile in adjacentTiles)
            {
                Unit unitOnTile = tile.OccupyingUnit;
                if (unitOnTile != null)
                {
                    // Apply 50% damage reduction buff
                    // Again note to Matthew: Need to implement buff system - for now modify DamageReduction
                    unitOnTile.DamageReduction *= 0.5; // 50% reduction
                }
            }

            return true;
        }

        // ============================================================
        // OVERRIDES
        // ============================================================
        /// <summary>
        /// Overrides MapObject.ReceiveDamge.
        /// 
        /// HoverTransporter ignores DamageReduction and always takes full incoming damage.
        /// For other units, defer to base behavior.
        /// </summary>
        public override void ReceiveDamge(int incomingDamage)
        {
            if (IsHoverTransporter)
            {
                // Incoming damage always 100% (ignore DamageReduction)
                HP -= incomingDamage;
                return;

            }
            base.ReceiveDamge(incomingDamage);
        }


        /// <summary>
        /// Engineer special:
        /// Place a mine on an adjacent empty tile.
        /// Currently not implemented (returns true after validation).
        /// 
        /// Validation:
        /// - tile must be adjacent
        /// - tile must be unoccupied and have no building
        /// </summary>
        private bool Special_Engineer_PlaceMine(Map map, Tile targetTile)
        {
            if (targetTile == null || map == null)
                return false;

            // Check if tile is adjacent
            List<Tile> adjacentTiles = map.GetNeighbors(Position);
            if (!adjacentTiles.Contains(targetTile))
                return false;

            // Check if tile is empty
            if (targetTile.IsOccupied() || targetTile.Building != null)
                return false;

            // TODO: Create Mine object and place on tile
            // For now, mark it somehow -  need to implement a Mine class
            // targetTile.Mine = new Mine(this, maxHP * 0.5);

            return true;
        }
        /// <summary>
        /// RACM special: Fortify.
        /// - sets fortified state
        /// - adds range bonus
        /// - applies damage reduction multiplier
        /// 
        /// NOTE: Fortify persists until RemoveFortify is called.
        /// </summary>
        private bool Special_RACM_Fortify()
        {
            IsFortified = true;
            _fortifyDamageReduction = 50; // 50% reduction
            _fortifyRangeBonus = 2;

            // Apply the bonuses
            DamageReduction *= 0.5; // 50% damage reduction
            Range += 2;

            return true;
        }


        /// <summary>
        /// Removes the fortify bonuses and resets state.
        /// Intended to be called when the unit moves.
        /// </summary>
        public void RemoveFortify()
        {
            if (!IsFortified)
                return;

            IsFortified = false;

            // Remove bonuses
            DamageReduction /= 0.5; // Restore original
            Range -= _fortifyRangeBonus;

            _fortifyDamageReduction = 0;
            _fortifyRangeBonus = 0;
        }

        /// <summary>
        /// Hawk special: Teleport to a target tile within 4 tiles
        /// only if HP is below 10% of MaxHP.
        /// 
        /// Validation:
        /// - HP must be <= 10% MaxHP
        /// - distance to target <= 4
        /// - target tile must be walkable
        /// Then: map.MoveUnit to update tile references correctly.
        /// </summary>
        private bool Special_Hawk_Teleport(Map map, Tile targetTile)
        {
            if (Position == null || map == null || targetTile == null)
                return false;

            // Check HP requirement
            if (HP > MaxHP * 0.1) // More than 10% HP
                return false;

            // Check distance (4 tiles max)
            int distance = map.GetDistance(Position, targetTile);
            if (distance > 4)
                return false;

            // Check if target tile is walkable
            if (!targetTile.IsWalkable())
                return false;

            // Teleport
            map.MoveUnit(this, Position, targetTile);

            return true;
        }

        /// <summary>
        /// HoverTank special: Fortify.
        /// Your comment says "reduce damage by 75%" but your code applies "*= 0.75",
        /// which is actually "take 75% damage" (25% reduction).
        /// Kept as-is; fix if intended.
        /// </summary>
        private bool Special_HoverTank_Fortify()    
        {
            IsFortified = true;
            _fortifyDamageReduction = 25; // 25% reduction

            // Apply the bonus
            DamageReduction *= 0.75; // 25% damage reduction

            return true;
        }
        /// <summary>
        /// Wasp special: Move after attack (or reposition).
        /// Here it simply moves to a reachable tile.
        /// 
        /// Validation:
        /// - target tile must be in movement range
        /// - movement is executed via map.MoveUnit
        /// </summary>
        private bool Special_Wasp_MoveAfterAttack(Map map, Tile targetTile)
        {
            if (Position == null || map == null || targetTile == null)
                return false;

            // Check if tile is reachable
            List<Tile> reachableTiles = map.GetMovementRange(this);
            if (!reachableTiles.Contains(targetTile))
                return false;

            // Move with double fuel consumption
            // li would leave this here unitl we implement fuel system
            // ConsumeFuel(FuelConsumption * 2);

            map.MoveUnit(this, Position, targetTile);

            return true;
        }

        /// <summary>
        /// BOHS special: Aura boost within radius 2.
        /// Affects other units (not itself):
        /// - +10% outgoing damage
        /// - -10% incoming damage
        /// 
        /// IMPORTANT: EndTurnCleanup resets these multipliers unless you manage buff durations differently.
        /// </summary>
        private bool Special_BOHS_AuraBoost(Map map)
        {
            if (Position == null || map == null)
                return false;

            List<Tile> tilesInRange = map.GetTilesInRadius(Position, 2);

            foreach (Tile tile in tilesInRange)
            {
                Unit unitOnTile = tile.OccupyingUnit;
                if (unitOnTile != null && unitOnTile != this)
                {
                    // Apply +10% damage and -10% incoming damage
                    unitOnTile.DamageAmplification *= 1.1; // +10% outgoing
                    unitOnTile.DamageReduction *= 0.9;     // -10% incoming
                }
            }

            HasBoostAura = true;
            return true;
        }


        /// <summary>
        /// Breacher special: Displace an enemy unit by moving it to an adjacent destination tile.
        /// 
        /// Validation:
        /// - target unit must be within 1 tile distance
        /// - destination tile must be adjacent to target unit
        /// - destination tile must be walkable and unoccupied
        /// Then: map.MoveUnit(targetUnit, oldPos, destinationTile)
        /// </summary>
        private bool Special_Breacher_Displace(Map map, Unit targetUnit, Tile destinationTile)
        {
            if (Position == null || map == null || targetUnit == null || destinationTile == null)
                return false;

            // Check if target unit is within 1 tile radius
            int distanceToTarget = map.GetDistance(Position, targetUnit.Position);
            if (distanceToTarget > 1)
                return false;

            // Check if destination tile is adjacent to target unit
            List<Tile> adjacentTiles = map.GetNeighbors(targetUnit.Position);
            if (!adjacentTiles.Contains(destinationTile))
                return false;

            // Check if destination tile is walkable and unoccupied
            if (!destinationTile.IsWalkable() || destinationTile.IsOccupied())
                return false;

            // Move target unit
            map.MoveUnit(targetUnit, targetUnit.Position, destinationTile);

            return true;

        }

        /// <summary>
        /// Redoubt special: Jump/Teleport within radius 2.
        /// You commented "consumes 12 fuel" but fuel consumption is not implemented here yet.
        /// 
        /// Validation:
        /// - distance <= 2
        /// - target tile must be walkable
        /// Then: map.MoveUnit to target tile
        /// </summary>
        private bool Special_Redoubt_Jump(Map map, Tile targetTile)
        {
            if (Position == null || map == null || targetTile == null)
                return false;

            // Check distance (2 tiles max)
            int distance = map.GetDistance(Position, targetTile);
            if (distance > 2)
                return false;

            // Check fuel
            // Implement fuel tracking
            // if (CurrentFuel < 12)
            //     return false;

            // Check if target tile is walkable
            if (!targetTile.IsWalkable())
                return false;

            // teleport
            map.MoveUnit(this, Position, targetTile);

            // Consume fuel
            // TODO: ConsumeFuel(12);

            return true;
        }
        /// <summary>
        /// Sunlight special: Deal damage to every unit in a straight line towards targetUnit,
        /// up to this.Range.
        /// 
        /// Implementation detail:
        /// - Get tiles in path via map.GetTilesInDirection
        /// - For each unit encountered:
        ///     damage = baseDamage / distanceFromSunlight
        /// - Apply damage via ReceiveDamge
        /// 
        /// IMPORTANT: if distanceToTarget becomes 0, division by zero would occur.
        /// Usually tile distance from Position to itself is 0, but you skip "this" unit,
        /// so it should be at least 1 for other tiles.
        /// </summary>
        private bool Special_Sunlight_LineDamage(Map map, Unit targetUnit)
        {
            if (Position == null || map == null || targetUnit == null || targetUnit.Position == null)
                return false;

            // Get tiles in the line to target
            List<Tile> tilesInPath = map.GetTilesInDirection(Position, targetUnit.Position, this.Range);

            int baseDamage = DoDamgeDefault();

            foreach (Tile tile in tilesInPath)
            {
                Unit unitOnTile = tile.OccupyingUnit;
                if (unitOnTile != null && unitOnTile != this)
                {
                    int distanceToTarget = map.GetDistance(Position, tile);

                    // Damage formula: BaseDamage / Distance
                    int damage = baseDamage / distanceToTarget;

                    unitOnTile.ReceiveDamge(damage);
                }
            }

            return true;
        }


        #endregion
    }
}
