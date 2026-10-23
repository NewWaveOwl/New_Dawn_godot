using NewDawn.Models;
using NewDawn.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NewDawn;

/// <summary>Legacy Unit Editor data operations, copied without logic changes.</summary>
public class UnitEditorData
{
    private ObservableCollection<UnitRow> _units = new();
    public ObservableCollection<UnitRow> Rows => _units;
    public UnitEditorData() => LoadDefaultUnits();
    public void Save(string path) => SaveUnitsToFile(path);
    public void Load(string path) => LoadUnitsFromFile(path);
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
