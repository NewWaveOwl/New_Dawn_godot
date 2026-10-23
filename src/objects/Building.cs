#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewDawn.Models.Tile;

namespace NewDawn.Models
{
    //Backing fields
    
    public class Building: MapObject
    {

        public enum BuildingType  
        {
            None,
            AlloyQuarry,
            FuelExtractor,
            AlloyMultiplicator,
            FuelMultiplicator,
            Hub,
            ContestFlag 
        }
        private Tile _position;
        private int _buildingProduction=0;//default 0
        private string _buildingAssetAddress; //  Assets/Buildings/building_name.png
        private BuildingType _thisBuildingType;
        private bool _isUntargetable = false; //for contest flag
        private List<TerrainType> _tilesCanBePlacedOn = new List<TerrainType> { TerrainType.Field, TerrainType.Forest, TerrainType.Swamp, TerrainType.FrozenDesert }; //default possible terrain types

        //properties
        public List<TerrainType> TilesThisCanBePlacedOn
        {
            get { return _tilesCanBePlacedOn; }
            set { _tilesCanBePlacedOn = value; }
        }
        public bool IsUntargetable
        {
            get { return _isUntargetable; }
            set { _isUntargetable = value; }
        }
        public BuildingType ThisBuildingType
        {
            get { return _thisBuildingType; }
            set { _thisBuildingType = value; }
        }
        public string BuildingAssetAddress
        {
            get { return _buildingAssetAddress; }
            set { _buildingAssetAddress = SaveAndLoaderHandler.NormalizeAndValidateAssetAddress(value, "Buildings", "building_"); } // "Buildings" is folder and "building_" is prefix for building assets
        }

        public Tile Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public int BuildingyProduction
        {
            get { return _buildingProduction; }
            set 
            { 
                if(value < 0)
                    _buildingProduction = 0;
                else
                    _buildingProduction = value; 
            }
        }
        //constructors
        public Building(BuildingType newBuildingType, bool newArmored = true, int newHP = 0, int placementPerTile = 1, string mapObjectName="NoName", int newPriceAlloy=0)
            :base(newArmored, newHP, placementPerTile, mapObjectName, newPriceAlloy)
        {
            ThisBuildingType = newBuildingType;
            base.Name = ThisBuildingType.ToString() ; //sets the name of the building to its type
            BuildingsStatBuilder();
            BuildingSetUpAssets();
        }

        //methods
        public int DoubleProductionRate(int inputProduction)
        {
            return inputProduction * 2;
        }
        public void BuildingsStatBuilder() //settings for buildings
        {
            switch (ThisBuildingType)
            {
                case BuildingType.AlloyQuarry:
                    base.HP = 30;
                    BuildingyProduction = 15;
                    base.Armored = true;
                    base.PriceAlloy = 15;
                    TilesThisCanBePlacedOn = new List<TerrainType> { TerrainType.AlloyField}; //can be placed adjacent to maintains 
                    break;
                case BuildingType.FuelExtractor:
                    base.HP = 30;
                    BuildingyProduction = 5;
                    base.Armored = true;
                    base.PriceAlloy = 15;
                    TilesThisCanBePlacedOn = new List<TerrainType> { TerrainType.FuelSource };
                    break;
                case BuildingType.AlloyMultiplicator:
                    base.HP = 300;
                    base.Armored = true;
                    base.PriceAlloy = 90;
                    break;
                case BuildingType.FuelMultiplicator:
                    base.HP = 300;
                    base.Armored = true;
                    base.PriceAlloy = 90;
                    break;
                case BuildingType.Hub:
                    base.HP = 400;
                    base.Armored = true;
                    base.PriceAlloy = 180;
                    break;
                case BuildingType.ContestFlag:
                    IsUntargetable = true;
                    break;
                default: //add handling

                    break;
            }
        }
        public void BuildingSetUpAssets()
        {
            switch (ThisBuildingType)
            {
                case BuildingType.AlloyQuarry:
                    BuildingAssetAddress = "Assets/Buildings/building_alloyquarry.png";
                    break;
                case BuildingType.FuelExtractor:
                    BuildingAssetAddress = "Assets/Buildings/building_fuelextractor.png";
                    break;
                case BuildingType.AlloyMultiplicator:
                    BuildingAssetAddress = "Assets/Buildings/building_alloymultiplicator.png";
                    break;
                case BuildingType.FuelMultiplicator:
                    BuildingAssetAddress = "Assets/Buildings/building_fuelmultiplicator.png";
                    break;
                case BuildingType.Hub:
                    BuildingAssetAddress = "Assets/Buildings/building_hub.png";
                    break;
                case BuildingType.ContestFlag:
                    BuildingAssetAddress = "Assets/Buildings/building_contestflag.png";
                    break;
                default:
                    BuildingAssetAddress = "Assets/Buildings/building_default.png";
                    break;
            }
        }

    }
}
