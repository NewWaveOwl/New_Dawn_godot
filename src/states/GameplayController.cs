#nullable disable warnings
using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using File = System.IO.File;
using IOPath = System.IO.Path;

namespace NewDawn
{
    /// <summary>
    /// Interaction logic for MainGameWindow.xaml
    /// </summary>
    public class GameplayController
    {
        
        
        private Game.InteractionMode _mode = Game.InteractionMode.None;
        private Unit.UnitType? _selectedShopUnitType = null;
        private Building.BuildingType? _selectedShopBuildingType;
        private Unit? _selectedMapUnit = null;
        private Tile? _selectedTile = null;
        private readonly Game _game;
        private List<Tile> _deployTiles = new();
        private List<Tile> _moveTiles = new();
        private List<Tile> _attackTiles = new();
        private List<Unit> _unitTemplates = null; // Cached unit templates
        private List<Building> _buildingTemplates = null;

        //for special abilities 
        private List<Tile> _specialTargetTiles = new();
        private Unit.UnitSpecial _currentSpecialAbility = Unit.UnitSpecial.None;
        //properties
        

        //=================
        //buttons
        //=================

        public void BtnLoad_Click() => LoadRequested?.Invoke();
        public void BtnBack_Click()
        {
            try
            {
                SaveAndLoaderHandler.SaveGame(_game, "autosave_current");
            }
            catch
            {
                //  autosave failure on exit
            }

            HomeRequested?.Invoke();
        }

        //
        public void SelectShopItem(int index) //handles shop unit selection
        {
            SelectedShopIndex = index;
            if (index < 0 || index >= ShopEntries.Count) return;
            ShopEntry item = ShopEntries[index];

            // Clear previous selection
            _selectedShopUnitType = null;
            _selectedShopBuildingType = null;

            // Divider / header items: Tag is null 
            if (item.Tag == null)
                return;

            // Selecting a UNIT
            if (item.Tag is Unit.UnitType unitType)
            {
                _selectedShopUnitType = unitType;
                _mode = Game.InteractionMode.Deploy;

                _deployTiles = ComputeDeployTiles();
                SetInfo($"Selected unit: {unitType}");

                RenderGameMap();
                return;
            }

            // Selecting a BUILDING
            if (item.Tag is Building.BuildingType buildingType)
            {
                _selectedShopBuildingType = buildingType;
                _mode = Game.InteractionMode.Deploy;

                _deployTiles = ComputeBuildingDeployTiles(buildingType);

                if (_deployTiles.Count == 0)
                    SetInfo("No valid tiles. You need an Engineer deployed (and free adjacent tiles).");
                else
                    SetInfo($"Selected building: {buildingType}");

                RenderGameMap();
                return;
            }
        }
        public List<Tile> ComputeDeployTiles()// computes and returns all valid deploy tiles for current player
        {
            List<Tile> result = new List<Tile>();

            if (_game == null || _game.CurentGameMap == null)
                return result;

            if (_game.CurrentPlayer == null)
                return result;

            if (_selectedShopUnitType != null && _selectedShopUnitType.Value == Unit.UnitType.CMP)
            {
                foreach (Tile t in _game.CurentGameMap.GetAllTiles())
                {
                    if (t == null)
                        continue;

                    if (!t.IsWalkable())
                        continue;

                    if (t.IsOccupied())
                        continue;

                    if (t.Building != null)
                        continue;

                    result.Add(t);
                }

                return result;
            }

            // Anchors: CMP + Hubs
            List<Tile> anchors = new List<Tile>();

            foreach (Unit u in _game.CurrentPlayer.DeployedUnits)
            {
                if (u == null || u.Position == null)
                    continue;

                if (u.ThisUnitType == Unit.UnitType.CMP)
                    anchors.Add(u.Position);
            }

            foreach (Building b in _game.CurrentPlayer.OwnedBuildings)
            {
                if (b == null || b.Position == null)
                    continue;

                if (b.ThisBuildingType == Building.BuildingType.Hub)
                    anchors.Add(b.Position);
            }

            if (anchors.Count == 0)
                return result;

            foreach (Tile anchor in anchors)
            {
                List<Tile> area = _game.CurentGameMap.GetTilesInRadius(anchor, 1);

                foreach (Tile t in area)
                {
                    if (t == null)
                        continue;

                    if (!t.IsWalkable())
                        continue;

                    if (t.IsOccupied())
                        continue;

                    if (t.Building != null)
                        continue;

                    if (!result.Contains(t))
                        result.Add(t);
                }
            }

            return result;
        }

        //------- stuff for special abilities handlings-------------

        // Get valid tiles for special ability targeting
        private List<Tile> GetSpecialTargetTiles(Unit unit)
        {
            List<Tile> targets = new List<Tile>();

            if (unit == null || unit.Position == null)
                return targets;

            switch (unit.ThisUnitSpecial)
            {
                case Unit.UnitSpecial.ProtectorsSpecial: // Shield AOE
                                                         // Target: Adjacent tiles
                    targets = _game.CurentGameMap.GetNeighbors(unit.Position);
                    break;

                case Unit.UnitSpecial.EngeneerSpecial: // Place Mine
                                                       // Target: Adjacent empty tiles
                    foreach (Tile tile in _game.CurentGameMap.GetNeighbors(unit.Position))
                    {
                        if (!tile.IsOccupied())
                            targets.Add(tile);
                    }
                    break;

                case Unit.UnitSpecial.RACMSpecial: // Fortify (self)
                case Unit.UnitSpecial.HoverTankSpecial: // Fortify (self)
                case Unit.UnitSpecial.HoverTransporterSpecial: // Block damage (self)
                                                               // No targeting needed - applies to self
                    targets.Add(unit.Position);
                    break;

                case Unit.UnitSpecial.HawkSpecial: // Emergency Teleport
                                                   // Target: Tiles within 4 range (only if HP < 10%)
                    if (unit.HP <= unit.MaxHP * 0.1)
                    {
                        targets = _game.CurentGameMap.GetTilesInRadius(unit.Position, 4)
                            .Where(t => t.IsWalkable() && !t.IsOccupied())
                            .ToList();
                    }
                    break;

                case Unit.UnitSpecial.WaspSpecial: // Move after attack
                                                   // Just highlight current position
                    targets.Add(unit.Position);
                    break;

                case Unit.UnitSpecial.BOHSpecial: // Aura Boost
                                                  // Target: Tiles in 2 radius
                    targets = _game.CurentGameMap.GetTilesInRadius(unit.Position, 2);
                    break;

                case Unit.UnitSpecial.SunlightSpecial: // Line Damage
                                                       // Target: Enemy units in range
                    foreach (Unit enemy in _game.CurrentOpponent.DeployedUnits)
                    {
                        if (enemy != null && enemy.Position != null)
                        {
                            int dist = _game.CurentGameMap.GetDistance(unit.Position, enemy.Position);
                            if (dist <= unit.Range)
                                targets.Add(enemy.Position);
                        }
                    }
                    break;

                case Unit.UnitSpecial.BreacherSpecial: // Displace
                                                       // Target: Enemy units within 1 tile
                    foreach (Unit enemy in _game.CurrentOpponent.DeployedUnits)
                    {
                        if (enemy != null && enemy.Position != null)
                        {
                            int dist = _game.CurentGameMap.GetDistance(unit.Position, enemy.Position);
                            if (dist <= 1 && enemy.ThisUnitType != Unit.UnitType.CMP)
                                targets.Add(enemy.Position);
                        }
                    }
                    break;

                case Unit.UnitSpecial.RedoubtSpecial: // Jump
                                                      // Target: Tiles within 2 radius
                    targets = _game.CurentGameMap.GetTilesInRadius(unit.Position, 2)
                        .Where(t => t.IsWalkable() && !t.IsOccupied())
                        .ToList();
                    break;

                case Unit.UnitSpecial.CMPSpecial: // Spawn Unit
                                                  // Target: Adjacent empty tiles
                    foreach (Tile tile in _game.CurentGameMap.GetNeighbors(unit.Position))
                    {
                        if (tile.IsWalkable() && !tile.IsOccupied())
                            targets.Add(tile);
                    }
                    break;

                default:
                    break;
            }

            return targets;
        }
        private void HandleSpecialClick(Tile tile)
        {
            if (_selectedMapUnit == null)
            {
                SetInfo("No unit selected.");
                return;
            }

            if (!_specialTargetTiles.Contains(tile))
            {
                SetInfo("Invalid target for this ability.");
                return;
            }

            bool success = ExecuteSpecialAbility(_selectedMapUnit, tile);

            if (success)
            {
                string abilityName = GetSpecialAbilityName(_selectedMapUnit.ThisUnitSpecial);
                SetInfo($"{_selectedMapUnit.Name} used {abilityName}!");

                ClearMode();
                SyncTopBar();
                RenderGameMap();
            }
            else
            {
                SetInfo("Failed to use special ability.");
            }
        }

        private bool ExecuteSpecialAbility(Unit unit, Tile targetTile)
        {
            if (unit == null || targetTile == null)
                return false;

            Unit targetUnit = targetTile.OccupyingUnit;

            switch (unit.ThisUnitSpecial)
            {
                case Unit.UnitSpecial.ProtectorsSpecial: // Shield AOE
                                                         // Apply shield to adjacent units
                    foreach (Tile neighbor in _game.CurentGameMap.GetNeighbors(unit.Position))
                    {
                        Unit ally = neighbor.OccupyingUnit;
                        if (ally != null && _game.CurrentPlayer.DeployedUnits.Contains(ally))
                        {
                            ally.ApplyDamageReduction(0.5); // 50% damage reduction
                        }
                    }
                    unit.UseSpecial(_game.CurentGameMap, targetTile, targetUnit);
                    return true;

                case Unit.UnitSpecial.EngeneerSpecial: // Place Mine
                                                       // TODO: Create Mine object when Mine class exists
                                                       // For now just trigger cooldown
                    unit.UseSpecial(_game.CurentGameMap, targetTile, targetUnit);
                    Message("Mine placed! (Mine system not yet implemented)");
                    return true;

                case Unit.UnitSpecial.RACMSpecial: // Fortify
                case Unit.UnitSpecial.HoverTankSpecial: // Fortify
                    unit.UseSpecial(_game.CurentGameMap, unit.Position, null);
                    return true;

                case Unit.UnitSpecial.HawkSpecial: // Emergency Teleport
                                                   // Only if HP < 10%
                    if (unit.HP <= unit.MaxHP * 0.1)
                    {
                        Tile fromTile = unit.Position;
                        _game.CurentGameMap.MoveUnit(unit, fromTile, targetTile);
                        unit.UseSpecial(_game.CurentGameMap, targetTile, null);
                        return true;
                    }
                    else
                    {
                        SetInfo("Hawk can only teleport when HP is below 10%!");
                        return false;
                    }

                case Unit.UnitSpecial.WaspSpecial: // Move after attack
                                                   // Enable double movement
                    unit.UseSpecial(_game.CurentGameMap, unit.Position, null);
                    // TODO: Implement extra movement phase
                    return true;

                case Unit.UnitSpecial.BOHSpecial: // Aura Boost
                                                  // Boost allies in range
                    foreach (Tile tile in _game.CurentGameMap.GetTilesInRadius(unit.Position, 2))
                    {
                        Unit ally = tile.OccupyingUnit;
                        if (ally != null && _game.CurrentPlayer.DeployedUnits.Contains(ally))
                        {
                            ally.ApplyDamageAmplification(1.1); // +10% damage
                            ally.ApplyDamageReduction(0.9);     // -10% incoming
                        }
                    }
                    unit.UseSpecial(_game.CurentGameMap, targetTile, targetUnit);
                    return true;

                case Unit.UnitSpecial.SunlightSpecial: // Line Damage
                    if (targetUnit != null)
                    {
                        int distance = _game.CurentGameMap.GetDistance(unit.Position, targetTile);
                        int damage = unit.Damage / Math.Max(1, distance);
                        targetUnit.ReceiveDamge(damage);

                        if (targetUnit.HP <= 0)
                        {
                            _game.CurrentOpponent.RemoveUnit(targetUnit);
                            _game.CurentGameMap.RemoveUnit(targetUnit);
                        }

                        unit.UseSpecial(_game.CurentGameMap, targetTile, targetUnit);
                        return true;
                    }
                    return false;

                case Unit.UnitSpecial.BreacherSpecial: // Displace
                    if (targetUnit != null && targetUnit.ThisUnitType != Unit.UnitType.CMP)
                    {
                        // Find empty adjacent tile to push enemy to
                        foreach (Tile neighbor in _game.CurentGameMap.GetNeighbors(targetTile))
                        {
                            if (neighbor.IsWalkable() && !neighbor.IsOccupied())
                            {
                                _game.CurentGameMap.MoveUnit(targetUnit, targetTile, neighbor);
                                unit.UseSpecial(_game.CurentGameMap, targetTile, targetUnit);
                                return true;
                            }
                        }
                        SetInfo("No space to displace target!");
                        return false;
                    }
                    return false;

                case Unit.UnitSpecial.RedoubtSpecial: // Jump
                                                      // Check fuel cost
                    if (unit.HasEnoughFuel(12))
                    {
                        unit.ConsumeFuel(12);
                        Tile fromTile = unit.Position;
                        _game.CurentGameMap.MoveUnit(unit, fromTile, targetTile);
                        unit.UseSpecial(_game.CurentGameMap, targetTile, null);
                        return true;
                    }
                    else
                    {
                        SetInfo("Not enough fuel! Redoubt jump costs 12 fuel.");
                        return false;
                    }

                case Unit.UnitSpecial.CMPSpecial: // Spawn Unit
                                                  // TODO: Implement unit spawning when ready
                    unit.UseSpecial(_game.CurentGameMap, targetTile, null);
                    Message("Unit spawning not yet implemented!");
                    return true;

                case Unit.UnitSpecial.HoverTransporterSpecial: // Block Damage
                    unit.UseSpecial(_game.CurentGameMap, unit.Position, null);
                    return true;

                default:
                    return false;
            }
        }



        //=================
        //initialization
        //=================
        public void Initialize() // renders the game map on window load
        {
            ShopEntries.Clear();


            LoadUnitTemplates();


            if (_unitTemplates == null || _unitTemplates.Count == 0)
            {
                SetInfo("No unit templates loaded.");
                return;
            }

            BuildShopList();

            bool mapHasUnits = _game.CurentGameMap.GetAllTiles().Any(t => t.OccupyingUnit != null);
            bool playersHaveUnits =
                (_game.Player1.DeployedUnits != null && _game.Player1.DeployedUnits.Count > 0) ||
                (_game.Player2.DeployedUnits != null && _game.Player2.DeployedUnits.Count > 0);

            if (!mapHasUnits && !playersHaveUnits)
            {
                _game.InitializeGame(_game.CurentGameMap, _game.Player1, _game.Player2, player1StartsFirst: true);
            }

            SetInfo("Select a unit from the list to deploy.");
            RenderGameMap();
        }
        public GameplayController(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            _game = game;
        }
        public Game CurrentGame => _game;
        public List<ShopEntry> ShopEntries { get; } = new();
        public int SelectedShopIndex { get; set; } = -1;
        public string P1FuelText { get; private set; } = "";
        public string P1AlloyText { get; private set; } = "";
        public string P2FuelText { get; private set; } = "";
        public string P2AlloyText { get; private set; } = "";
        public string TurnPlayerText { get; private set; } = "Player 1";
        public string Info { get; private set; } = "";
        public event Action? Changed;
        public event Action? MapRenderRequested;
        public event Action<string, string>? Notice;
        public event Action? HomeRequested;
        public event Action? LoadRequested;
        public IReadOnlyList<Tile> DeployTiles => _deployTiles;
        public IReadOnlyList<Tile> MoveTiles => _moveTiles;
        public IReadOnlyList<Tile> AttackTiles => _attackTiles;
        public IReadOnlyList<Tile> SpecialTiles => _specialTargetTiles;
        public Unit? SelectedUnit => _selectedMapUnit;
        public Game.InteractionMode Mode => _mode;
        private void Message(string text, string title = "") => Notice?.Invoke(text, title);
        //public MainGameWindow() : this(Game.CreateDefaultGame())
        //{

        //}
        //=================
        //To Bar (Stats)
        //=================
        public void SyncTopBar() //syncs the top bar with current game state
        {
            P1FuelText = _game.Player1.FuelResources.ToString();
            P1AlloyText = _game.Player1.AlloyResources.ToString();
            P2FuelText = _game.Player2.FuelResources.ToString();
            P2AlloyText = _game.Player2.AlloyResources.ToString();

            TurnPlayerText = (_game.CurrentPlayer == _game.Player1) ? "Player 1" : "Player 2";
            Changed?.Invoke();
        }
        public void SetInfo(string text) //sets the info text at the top bar
        {
            Info = text ?? string.Empty;
            Changed?.Invoke();
        }
        //=================
        //Left Bar (Shop)
        //=================
        public void BuildShopList() 
        {
            ShopEntries.Clear();

            LoadUnitTemplates();
            LoadBuildingTemplates();

            if (_unitTemplates == null || _unitTemplates.Count == 0)
            {
                SetInfo("No unit templates loaded.");
                return;
            }
            // =================
            // Units (top section)
            // =================

            // Sort by price, then name
            List<Unit> orderedUnits = new List<Unit>();
            for (int i = 0; i < _unitTemplates.Count; i++)
            {
                Unit u = _unitTemplates[i];
                if (u != null)
                    orderedUnits.Add(u);
            }

            // Sort units by price, then name
            orderedUnits.Sort((a, b) =>
            {
                int priceCompare = a.PriceAlloy.CompareTo(b.PriceAlloy);
                if (priceCompare != 0)
                    return priceCompare;

                string an = a.Name ?? string.Empty;
                string bn = b.Name ?? string.Empty;
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < orderedUnits.Count; i++)
            {
                Unit t = orderedUnits[i];

                // Don't sell invalid types
                if (t.ThisUnitType == Unit.UnitType.None)
                    continue;

                // CMP is commander and should not be purchased
                if (t.ThisUnitType == Unit.UnitType.CMP)
                    continue;

                string name = string.IsNullOrWhiteSpace(t.Name) ? t.ThisUnitType.ToString() : t.Name;
                AddShopItem(t.ThisUnitType, $"{name} — {t.PriceAlloy} Alloy");
            }

            // Divider line between Units and Buildings
            AddShopDivider();
            // =================
            // Buildings (bottom section)
            // =================
            if (_buildingTemplates == null || _buildingTemplates.Count == 0)
                return;

            List<Building> orderedBuildings = new List<Building>();

            for (int i = 0; i < _buildingTemplates.Count; i++)
            {
                Building b = _buildingTemplates[i];
                if (b != null)
                    orderedBuildings.Add(b);
            }

            // Sort buildings by price, then name
            orderedBuildings.Sort((a, b) =>
            {
                int priceCompare = a.PriceAlloy.CompareTo(b.PriceAlloy);
                if (priceCompare != 0)
                    return priceCompare;

                string an = a.Name ?? string.Empty;
                string bn = b.Name ?? string.Empty;
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < orderedBuildings.Count; i++)
            {
                Building b = orderedBuildings[i];

                if (b.ThisBuildingType == Building.BuildingType.None)
                    continue;

                // ContestFlag is a scenario object, not a shop item
                if (b.ThisBuildingType == Building.BuildingType.ContestFlag)
                    continue;

                string name = string.IsNullOrWhiteSpace(b.Name) ? b.ThisBuildingType.ToString() : b.Name;
                AddShopBuildingItem(b.ThisBuildingType, $"{name} — {b.PriceAlloy} Alloy");
            }
        }
        private void AddShopBuildingItem(Building.BuildingType type, string text)
        {
            ShopEntries.Add(new ShopEntry
            {
                Content = text,
                Tag = type
            });
        }
        private void AddShopDivider() => ShopEntries.Add(new ShopEntry { Content = "Buildings\nCan only be placed near Engineer", Enabled = false });
        public void AddShopItem(Unit.UnitType type, string text)
        {
            ShopEntries.Add(new ShopEntry
            {
                Content = text,
                Tag = type
            });
        }
        private void LoadBuildingTemplates()
        {
            if (_buildingTemplates != null)
                return;

            _buildingTemplates = new List<Building>();

            foreach (Building.BuildingType t in Enum.GetValues(typeof(Building.BuildingType)))
            {
                if (t == Building.BuildingType.None)
                    continue;

                if (t == Building.BuildingType.ContestFlag)
                    continue;

                Building b = null;

                try
                {
                    b = new Building(t);
                }
                catch
                {
                    b = null;
                }

                if (b != null)
                    _buildingTemplates.Add(b);
            }
        }

        //=================
        //Map Rendering
        //=================
        public void RenderGameMap() => MapRenderRequested?.Invoke();
        //=================
        //Game Logic
        //=================

        public void GameHex_LeftClick(Tile tile)
        {

            switch (_mode)
            {
                    case Game.InteractionMode.Deploy:
                    {
                        // Check if tile is valid for deployment
                        if (!_deployTiles.Contains(tile))
                        {
                            SetInfo("Cannot deploy here. Tile is occupied or blocked.");
                            return;
                        }

                        // ------------------------
                        // BUILDING DEPLOY
                        // ------------------------
                        if (_selectedShopBuildingType != null)
                        {
                            Building? template = FindBuildingTemplate(_selectedShopBuildingType.Value);
                            if (template == null)
                            {
                                SetInfo("No building template selected from shop.");
                                return;
                            }

                            if (!_game.CurrentPlayer.CanAfford(template.PriceAlloy))
                            {
                                SetInfo($"Not enough alloy! Need {template.PriceAlloy}, have {_game.CurrentPlayer.AlloyResources}.");
                                return;
                            }
                            _game.CurrentPlayer.AlloyResources -= template.PriceAlloy;
                            Building newBuilding = CreateBuildingFromType(_selectedShopBuildingType.Value);

                            try
                            {
                                _game.CurentGameMap.PlaceBuilding(newBuilding, tile); // throws if invalid:contentReference
                            }
                            catch (Exception ex)
                            {
                                SetInfo("Failed to place building: " + ex.Message);
                                return;
                            }

                            // Record ownership (so saves restore it via OwnedBuildings):contentReference
                            _game.CurrentPlayer.OwnedBuildings?.Add(newBuilding);

                            // Spend resources after successful placement
                            _game.CurrentPlayer.AlloyResources -= template.PriceAlloy;

                            SetInfo($"Placed {newBuilding.Name} at [{tile.Column},{tile.Row}].");
                            ClearMode();
                            SelectedShopIndex = -1;
                            SyncTopBar();

                            RenderGameMap();
                            return;
                        }

                        // ------------------------
                        // UNIT DEPLOY (existing code)
                        // ------------------------

                        // Create unit based on selected type
                        Unit newUnit = CreateUnitFromType(_selectedShopUnitType.Value);

                        if (newUnit == null)
                        {
                            SetInfo("Failed to create unit.");
                            return;
                        }

                        // Check if player can afford
                        if (!_game.CurrentPlayer.CanAfford(newUnit.PriceAlloy))
                        {
                            SetInfo($"Not enough alloy! Need {newUnit.PriceAlloy}, have {_game.CurrentPlayer.AlloyResources}.");
                            return;
                        }

                        _game.CurrentPlayer.AddUnit(newUnit);
                        _game.CurrentPlayer.AlloyResources -= newUnit.PriceAlloy;
                        bool success = _game.DeployUnit(newUnit, tile);


                        if (success)
                        {
                            SetInfo($"Deployed {newUnit.Name} at [{tile.Column},{tile.Row}].");
                            ClearMode();
                            SelectedShopIndex = -1; // Clear shop selection
                            SyncTopBar();
                        }
                        else
                        {
                            _game.CurrentPlayer.AddAlloyResources(newUnit.PriceAlloy);
                            SetInfo("Failed to deploy unit.");
                            _game.CurrentPlayer.RemoveUnit(newUnit);
                        }

                        RenderGameMap();
                        return;
                    }

                    case Game.InteractionMode.Move:
                    {
                        if (_selectedMapUnit == null)
                        {
                            SetInfo("No unit selected. Click your unit first, then press Move.");
                            return;
                        }

                        bool moved = _game.MoveUnit(_selectedMapUnit, tile);
                        if (moved)
                        {
                            SetInfo($"Moved {_selectedMapUnit.Name}.");
                            ClearMode();
                            SyncTopBar();
                        }
                        else
                        {
                            SetInfo("Invalid move. Click a highlighted tile.");
                        }

                        RenderGameMap();
                        return;
                    }

                    case Game.InteractionMode.Attack:
                    {
                        if (_selectedMapUnit == null)
                        {
                            SetInfo("No unit selected. Click your unit first, then press Attack.");
                            return;
                        }

                        Unit target = tile.OccupyingUnit;
                        bool attacked = (target != null) && _game.ExecuteAttack(_selectedMapUnit, target);

                        if (attacked)
                        {
                            SetInfo($"Attack executed by {_selectedMapUnit.Name}.");
                            ClearMode();
                            SyncTopBar();
                        }
                        else
                        {
                            SetInfo("Invalid attack. Click a highlighted enemy tile.");
                        }

                        RenderGameMap();
                        return;
                    }

                    case Game.InteractionMode.Special:
                    {
                        if (_selectedMapUnit == null)
                        {
                            SetInfo("No unit selected.");
                            return;
                        }

                        if (!_specialTargetTiles.Contains(tile))
                        {
                            SetInfo("Invalid target for this ability.");
                            return;
                        }

                        // Execute special ability (simplified for now)
                        bool success = _selectedMapUnit.UseSpecial(_game.CurentGameMap, tile, tile.OccupyingUnit);

                        if (success)
                        {
                            string abilityName = GetSpecialAbilityName(_selectedMapUnit.ThisUnitSpecial);
                            SetInfo($"{_selectedMapUnit.Name} used {abilityName}!");
                            ClearMode();
                            SyncTopBar();
                        }
                        else
                        {
                            SetInfo("Failed to use special ability.");
                        }

                        RenderGameMap();
                        return;
                    }

                case Game.InteractionMode.None:
                    default:
                    {
                        // Normal selection
                        _selectedTile = tile;
                        _selectedMapUnit = tile.OccupyingUnit;

                        if (_selectedMapUnit != null)
                        {
                            SetInfo($"Selected unit: {_selectedMapUnit.ThisUnitType} | HP {_selectedMapUnit.HP} | DMG {_selectedMapUnit.Damage} | Speed {_selectedMapUnit.Speed}");
                        }
                        else
                        {
                            SetInfo($"Tile: {tile.TileTerrainType} [{tile.Column},{tile.Row}]");
                        }

                        RenderGameMap();
                        return;
                    }
            }
        }
        // =====================
        // Helpers
        // =====================
        public void ClearMode()
        {
            _mode = Game.InteractionMode.None;
            _deployTiles.Clear();
            _moveTiles.Clear();
            _attackTiles.Clear();
            _specialTargetTiles.Clear();  
            _selectedShopUnitType = null;
            _selectedShopBuildingType = null;
            _currentSpecialAbility = Unit.UnitSpecial.None;  
        }

        private string GetSpecialAbilityName(Unit.UnitSpecial special)
        {
            switch (special)
            {
                case Unit.UnitSpecial.ProtectorsSpecial: return "Shield AOE";
                case Unit.UnitSpecial.EngeneerSpecial: return "Place Mine";
                case Unit.UnitSpecial.RACMSpecial: return "Fortify";
                case Unit.UnitSpecial.HawkSpecial: return "Emergency Teleport";
                case Unit.UnitSpecial.HoverTankSpecial: return "Fortify";
                case Unit.UnitSpecial.WaspSpecial: return "Move After Attack";
                case Unit.UnitSpecial.BOHSpecial: return "Aura Boost";
                case Unit.UnitSpecial.SunlightSpecial: return "Line Damage";
                case Unit.UnitSpecial.BreacherSpecial: return "Displace";
                case Unit.UnitSpecial.RedoubtSpecial: return "Jump";
                case Unit.UnitSpecial.CMPSpecial: return "Spawn Unit";
                case Unit.UnitSpecial.HoverTransporterSpecial: return "Block Damage";
                default: return "Special Ability";
            }
        }

        public void BtnSpecail_Click()
        {
            if (_selectedMapUnit == null)
            {
                SetInfo("No unit selected. Click a unit first.");
                return;
            }

            if (!_game.CurrentPlayer.DeployedUnits.Contains(_selectedMapUnit))
            {
                SetInfo("You can only use abilities of your own units!");
                return;
            }

            if (!_selectedMapUnit.IsSpecialAvailable)
            {
                SetInfo($"Special ability on cooldown: {_selectedMapUnit.SpecialCooldown} turns remaining.");
                return;
            }

            _mode = Game.InteractionMode.Special;
            _deployTiles.Clear();
            _moveTiles.Clear();
            _attackTiles.Clear();
            _specialTargetTiles = GetSpecialTargetTiles(_selectedMapUnit);  // ← POPULATE SPECIAL TILES

            string abilityName = GetSpecialAbilityName(_selectedMapUnit.ThisUnitSpecial);
            SetInfo($"Special mode: {abilityName}. Click a highlighted tile to use ability.");
            RenderGameMap();  // ← RENDER WITH HIGHLIGHTS
        }

        public void BtnMove_Click()
        {
            if (_selectedMapUnit == null)
            {
                SetInfo("No unit selected. Click a unit first.");
                return;
            }

            if (!_game.CurrentPlayer.DeployedUnits.Contains(_selectedMapUnit))
            {
                SetInfo("You can only move your own units!");
                return;
            }

            if (_selectedMapUnit.HasPerformedAction)
            {
                SetInfo("This unit has already acted this turn.");
                return;
            }

            _mode = Game.InteractionMode.Move;
            _deployTiles.Clear();
            _attackTiles.Clear();
            _moveTiles = _game.CurentGameMap.GetMovementRange(_selectedMapUnit);

            SetInfo($"Move mode: Click a highlighted tile to move {_selectedMapUnit.Name}.");
            RenderGameMap();
        }

        public void BtnAttack_Click()
        {
            if (_selectedMapUnit == null)
            {
                SetInfo("No unit selected. Click a unit first.");
                return;
            }

            if (!_game.CurrentPlayer.DeployedUnits.Contains(_selectedMapUnit))
            {
                SetInfo("You can only attack with your own units!");
                return;
            }

            if (_selectedMapUnit.HasPerformedAction)
            {
                SetInfo("This unit has already acted this turn.");
                return;
            }

            _mode = Game.InteractionMode.Attack;
            _deployTiles.Clear();
            _moveTiles.Clear();
            _attackTiles = GetAttackableTiles(_selectedMapUnit);
            SetInfo($"Attack mode: Click a highlighted tile to attack with {_selectedMapUnit.Name}.");
            RenderGameMap();


        }

        private List<Tile> GetAttackableTiles(Unit unit)
        {
            List<Tile> result = new List<Tile>();

            if (unit == null || unit.Position == null)
                return result;

            foreach (Unit enemy in _game.CurrentOpponent.DeployedUnits)
            {
                if (enemy == null || enemy.Position == null)
                    continue;

                int distance = _game.CurentGameMap.GetDistance(unit.Position, enemy.Position);

                if (distance <= unit.Range)
                {
                    result.Add(enemy.Position);
                }
            }

            return result;
        }

        private void LoadUnitTemplates()
        {
            if (_unitTemplates != null)
                return; // this check if its already loaded
            string templateName = _game?.UnitTemplateFileName;
            try
            {
                // Try to load from DefaultUnits.csv
                if (SaveAndLoaderHandler.UnitTemplateExists(templateName))
                {
                    _unitTemplates = SaveAndLoaderHandler.LoadUnitTemplates(templateName);
                    return;
                }
                if (SaveAndLoaderHandler.UnitTemplateExists("DefaultUnits"))
                {
                    _unitTemplates = SaveAndLoaderHandler.LoadUnitTemplates("DefaultUnits");
                    return;
                }
                _unitTemplates = CreateDefaultUnitTemplates();
            }
            catch (Exception ex)
            {
                // Failed to load, use defaults
                _unitTemplates = CreateDefaultUnitTemplates();
                SetInfo($"Failed to load templates: {ex.Message}. Using defaults.");
            }
        }

        //Asked AI for assitance in making this method
        private Unit CreateUnitFromType(Unit.UnitType type)
        {
            // Make sure templates are loaded
            LoadUnitTemplates();

            // Find the matching template
            Unit template = _unitTemplates?.FirstOrDefault(u => u.ThisUnitType == type);

            if (template == null)
            {
                SetInfo($"No template found for {type}. Check DefaultUnits.csv");
                return null;
            }

            // Create a NEW unit from the template (don't use the template directly!)
            Unit newUnit = new Unit(
                template.Armored,
                template.HP,
                template.UnitsPerTile,
                template.Name,
                template.PriceAlloy,
                template.Range,
                template.Speed,
                template.Damage,
                template.FuelConsumption,
                template.ThisUnitCoreType,
                template.ThisUnitType,
                template.ThisUnitSpecial
            );

            newUnit.IsMech = template.IsMech;
            newUnit.MaxHP = template.HP; // Set MaxHP
            newUnit.MaxFuel = 10 + (template.Speed * 2); // Calculate fuel based on speed
            newUnit.CurrentFuel = newUnit.MaxFuel; // Start with full fuel

            newUnit.UnitSetUp();

            return newUnit;
        }

        // I asked AI to help me with the parameters for this method
        private List<Unit> CreateDefaultUnitTemplates()
        {
            List<Unit> templates = new List<Unit>();

            // Rebel
            templates.Add(new Unit(false, 50, 1, "Rebel", 10, 1, 4, 10, 1,
                Unit.Core.None, Unit.UnitType.Rebel, Unit.UnitSpecial.None));

            // Railgunner
            templates.Add(new Unit(false, 100, 1, "Railgunner", 25, 3, 2, 75, 1,
                Unit.Core.None, Unit.UnitType.Railgunner, Unit.UnitSpecial.RailgunnerSpecial));

            // Protectors
            templates.Add(new Unit(true, 150, 1, "Protectors", 30, 2, 3, 50, 2,
                Unit.Core.None, Unit.UnitType.Protectors, Unit.UnitSpecial.ProtectorsSpecial));

            // Engineer
            templates.Add(new Unit(false, 80, 1, "Engineer", 20, 1, 3, 20, 1,
                Unit.Core.None, Unit.UnitType.Engeneer, Unit.UnitSpecial.EngeneerSpecial));

            // RACM
            templates.Add(new Unit(false, 120, 1, "RACM", 28, 4, 2, 60, 2,
                Unit.Core.None, Unit.UnitType.RACM, Unit.UnitSpecial.RACMSpecial));

            // Hawk
            templates.Add(new Unit(true, 200, 1, "Hawk", 50, 1, 9, 150, 3,
                Unit.Core.None, Unit.UnitType.Hawk, Unit.UnitSpecial.HawkSpecial));

            // HoverTank
            templates.Add(new Unit(true, 180, 1, "HoverTank", 40, 2, 5, 100, 2,
                Unit.Core.None, Unit.UnitType.HoverTank, Unit.UnitSpecial.HoverTankSpecial));

            // Wasp
            templates.Add(new Unit(false, 90, 1, "Wasp", 22, 3, 7, 40, 2,
                Unit.Core.None, Unit.UnitType.Wasp, Unit.UnitSpecial.WaspSpecial));

            // BOHS
            templates.Add(new Unit(false, 130, 1, "BOHS", 35, 2, 3, 70, 2,
                Unit.Core.None, Unit.UnitType.BOHS, Unit.UnitSpecial.BOHSpecial));

            // Sunlight
            templates.Add(new Unit(false, 110, 1, "Sunlight", 30, 5, 2, 80, 2,
                Unit.Core.None, Unit.UnitType.Sunlight, Unit.UnitSpecial.SunlightSpecial));

            // Breacher
            templates.Add(new Unit(true, 160, 1, "Breacher", 45, 1, 4, 120, 2,
                Unit.Core.None, Unit.UnitType.Breacher, Unit.UnitSpecial.BreacherSpecial));

            // Redoubt
            templates.Add(new Unit(true, 200, 1, "Redoubt", 40, 2, 2, 50, 1,
                Unit.Core.None, Unit.UnitType.Redoubt, Unit.UnitSpecial.RedoubtSpecial));

            // CMP
            templates.Add(new Unit(true, 250, 1, "CMP", 100, 2, 3, 100, 3,
                Unit.Core.None, Unit.UnitType.CMP, Unit.UnitSpecial.CMPSpecial));

            // HoverTransporter
            templates.Add(new Unit(false, 100, 1, "HoverTransporter", 25, 1, 6, 10, 2,
                Unit.Core.None, Unit.UnitType.HoverTransporter, Unit.UnitSpecial.HoverTransporterSpecial));

            // Set MaxHP for all templates
            foreach (Unit u in templates)
            {
                u.MaxHP = u.HP;
                u.MaxFuel = 10 + (u.Speed * 2);
                u.CurrentFuel = u.MaxFuel;
            }

            return templates;
        }

        public void BtnCancel_Click()
        {
            if (_mode != Game.InteractionMode.None)
            {
                // Cancel the current mode
                ClearMode();

                // Clear shop selection
                SelectedShopIndex = -1;

                // Clear selections
                _selectedShopUnitType = null;
                _selectedTile = null;

                // Note -> we'll keep the unit selected so player can try different actions

                SetInfo("Action cancelled. Select a new action or unit.");
                RenderGameMap();
            }
            else if (_selectedMapUnit != null)
            {
                // No active mode, but unit is selected - deselect it
                _selectedMapUnit = null;
                _selectedTile = null;

                SetInfo("Unit deselected.");
                RenderGameMap();
            }
            else
            {
                // Nothing to cancel - inform the user
                SetInfo("Nothing to cancel. Select a unit or action first.");
            }
        }

        public void BtnNextTurn_Click()
        {
            _game.EndPlayerTurn();

            ClearMode();
            SelectedShopIndex = -1;

            SyncTopBar();
            SetInfo($"{(_game.CurrentPlayer == _game.Player1 ? "Player 1" : "Player 2")}'s turn. Select a unit or deploy from shop.");
            RenderGameMap();

            // Check for game over
            if (_game.CurentGameState == Game.GameState.GameOver)
            {
                Player winner = _game.GetWinner();
                string winnerName = (winner == _game.Player1) ? "Player 1" : "Player 2";
                Message($"Game Over! {winnerName} wins!", "Victory");
            }
        }

        public void BtnSave_Click()
        {
            if (_game == null) return;
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy_MM_dd_HH_mm_ss");
            try
            {
                SaveAndLoaderHandler.SaveGame(_game, "autosave_game"  + formattedDate );

                string dir = SaveAndLoaderHandler.GetGameSavesDirectoryPath();
                SetInfo("Game saved to: " + dir);
                Message(dir);
            }
            catch (Exception ex)
            {
                Message("Save failed:\n" + ex.Message, "Error");
            }
        }
        //helpers for finding buildings 
        public List<Tile> ComputeBuildingDeployTiles(Building.BuildingType buildingType)
        {
            List<Tile> result = new List<Tile>();

            if (_game == null || _game.CurentGameMap == null)
                return result;

            // Anchors: current player's Engineers
            List<Tile> engineerAnchors = new List<Tile>();

            foreach (Unit u in _game.CurrentPlayer.DeployedUnits)
            {
                if (u == null || u.Position == null)
                    continue;

                if (u.ThisUnitType == Unit.UnitType.Engeneer)
                    engineerAnchors.Add(u.Position);
            }

            if (engineerAnchors.Count == 0)
                return result;

            Building? template = FindBuildingTemplate(buildingType);

            HashSet<Tile> unique = new HashSet<Tile>();

            foreach (Tile anchor in engineerAnchors)
            {
                // Radius 1 adjacent and includes center.
                // The occupied check will exclude the engineer tile itself
                List<Tile> area = _game.CurentGameMap.GetTilesInRadius(anchor, 1);

                foreach (Tile t in area)
                {
                    if (t == null)
                        continue;

                    if (!t.IsWalkable())
                        continue;

                    if (t.IsOccupied())
                        continue;

                    if (t.Building != null)
                        continue;

                    if (template != null && !_game.CurentGameMap.CanPlaceBuildingOnTile(template, t))
                        continue;

                    unique.Add(t);
                }
            }

            result.AddRange(unique);
            return result;
        }
        private Building? FindBuildingTemplate(Building.BuildingType type)
        {
            if (_buildingTemplates == null)
                return null;

            foreach (Building b in _buildingTemplates)
            {
                if (b != null && b.ThisBuildingType == type)
                    return b;
            }

            return null;
        }
        private Building CreateBuildingFromType(Building.BuildingType type)
        {
            // Mirrors how LoadBuilding sets up assets after constructing buildings
            Building building = new Building(type);

            try
            {
                building.BuildingSetUpAssets();
            }
            catch
            {
            }

            return building;
        }

    }
}
