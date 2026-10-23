using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NewDawn.Views
{
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
