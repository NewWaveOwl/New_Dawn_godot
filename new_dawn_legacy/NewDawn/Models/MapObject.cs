using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewDawn.Models.Game;
using static NewDawn.Models.Tile;

namespace NewDawn.Models
{

    /// <summary>
    /// Abstract base class for all objects that can be placed on the hex map.
    /// Provides common properties like HP, armor, and pricing.
    /// Inherited by Unit and Building classes to share core functionality.
    /// </summary>
    public abstract class MapObject
    {
        private const int MaxHpValue = 250;
        private const int MinHpValue =0;

        private readonly int MAXOBJECTSPERTILE = 10; //max amount of units on a tile
        private int _Id;
        private string _name;
        private int _hp;
        private bool _armored;
        private int _placementPerTile;
        private double _damageReduction; // 0.NUMBER value which represents damage reduction 1 (no modifier) | 0.5 50% damage reduction, also can be higher than 1 like 1.2 , so the incoming damage will be increased
        private int _priceAlloy;
        //List of Actions Must contains all possible actions for MapObject (like move, attack, build e.t.c)
        //constructors
        /// <summary>
        /// Creates a new map object with basic properties
        /// </summary>
        /// <param name="newArmored">Whether the object has armor</param>
        /// <param name="newHP">Initial hit points</param>
        /// <param name="placementPerTile">How many can occupy one tile</param>
        /// <param name="mapObjectName">Display name</param>
        /// <param name="newPriceAlloy">Cost in alloy</param>

        public MapObject(bool newArmored, int newHP, int placementPerTile, string mapObjectName, int newPriceAlloy)
        {
            Armored = newArmored;
            HP = newHP;
            Name = mapObjectName;
            UnitsPerTile = placementPerTile;
            PriceAlloy = newPriceAlloy;
        }
        //properties
        /// <summary>
        /// Gets or sets the unique identifier for this object.
        /// Must be non-negative; defaults to 0 if invalid.
        /// </summary>
        public int Id // should get a new empty id for new object
        {
            get { return _Id; }
            set
            {
                if (value >= 0)
                    _Id = value;
                else
                {
                    _Id = 0;
                }
            }
        }
        /// <summary>
        /// Gets or sets the alloy cost to purchase/build this object.
        /// Must be non-negative; defaults to 0 if invalid.
        /// </summary>

        public int PriceAlloy
        {
            get { return _priceAlloy; }
            set
            {
                if (value >= 0)
                    _priceAlloy = value;
                else
                {
                    _priceAlloy = 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the display name of this object.
        /// Defaults to "UnknownObject" if null or empty.
        /// </summary>
        public string Name 
        {
            get { return _name; } 
            set 
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _name = value;
                }
                else  _name = "UnknownObject"; //default name for any map object
            }
        }
        /// <summary>
        /// Gets or sets how many of this object can occupy a single tile.
        /// Must be between 1 and MAXOBJECTSPERTILE; defaults to 1 if invalid.
        /// Currently, the game always uses 1 (one unit per tile).
        /// </summary>

        public int UnitsPerTile
        {
            get { return _placementPerTile; }
            set
            {
                if(value > 0 && value < MAXOBJECTSPERTILE)
                    _placementPerTile = value;
                else
                {
                    _placementPerTile = 1;
                }
            }
        }
        /// <summary>
        /// Gets or sets whether this object has armor.
        /// Armored units can traverse water and frozen terrain; non-armored cannot.
        /// Armor also affects damage calculations.
        /// </summary>

        public bool Armored
        {
            get { return _armored; } 
            set 
            { 
                if(value != null)
                    _armored = value;
                else
                    _armored = false;
            }
        }

        /// <summary>
        /// Gets or sets the current hit points.
        /// Clamped between MinHpValue (0) and MaxHpValue (250).
        /// Object dies when HP reaches 0.
        /// </summary>
        public int HP
        {
            get { return _hp; }
            set
            {
                if (value >= 0)
                    _hp = value;
                else if (value >= MaxHpValue)
                    _hp = MaxHpValue;              
                else
                    _hp = MinHpValue;
            }
        }

        /// <summary>
        /// Gets or sets the damage reduction multiplier.
        /// Applied to incoming damage in ReceiveDamage method.
        /// Values:
        /// - 1.0 = normal damage (no reduction)
        /// - 0.5 = 50% damage reduction
        /// - 1.2 = 20% damage increase
        /// Must be non-negative; defaults to 1.0 if invalid.
        /// </summary>
        public double DamageReduction
        {
            get { return _damageReduction; }
            set
            {
                if (value >= 0)
                    _damageReduction = value;
                else
                {
                    _damageReduction = 1; // default 
                }
            }
        }

        /// <summary>
        /// Applies damage to this object, reducing its HP.
        /// Damage is multiplied by the DamageReduction factor before being applied.
        /// Can be overridden by derived classes for custom damage handling.
        /// </summary>
        /// <param name="incomingDamage">Base damage amount before reduction</param>
        public virtual void ReceiveDamge(int incomingDamage) 
        { 
            HP = HP - (int)((incomingDamage)* DamageReduction);
        }


        /// <summary>
        /// Generates a resource amount based on the resource type.
        /// Used by buildings to produce alloy or fuel.
        /// Default values: 15 fuel or 5 alloy per generation.
        /// </summary>
        /// <param name="resourseToGenerate">Type of resource to generate (Fuel or Alloy)</param>
        /// <returns>Amount of resource generated</returns>
        public int GenerateResourse(Resourese resourseToGenerate)
        {
            int outputResource;
            switch (resourseToGenerate)
            {
                case Resourese.Fuel:
                    outputResource = 15; //default fuel generation
                    break;
                case Resourese.Alloy:
                    outputResource = 5; //default alloy generation
                    break;
                default:
                    outputResource = 0;
                    break;
            }
            return outputResource;
        }
        

    }
}
