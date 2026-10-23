using NewDawn;
using NewDawn.Models;
using System.Reflection;

string root = Path.GetFullPath(args[0]);
string working = Path.Combine(root, ".godot", "gameplay-checks", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(working);
Environment.CurrentDirectory = working;
int checks = 0;
void Check(bool condition, string name) {
    if (!condition) throw new Exception("FAILED: " + name);
    checks++;
}

Game NewGame() {
    var game = new Game(new Player(30, 6), new Player(45, 6), new Map(12, 8));
    game.UnitTemplateFileName = "DefaultUnits";
    game.InitializeGame(game.CurentGameMap, game.Player1, game.Player2);
    return game;
}
GameplayController Controller(Game game) {
    var c = new GameplayController(game);
    c.Initialize();
    return c;
}
Unit Add(Game game, Player player, Unit.UnitType type, Tile tile, Unit.UnitSpecial special = Unit.UnitSpecial.None) {
    var unit = new Unit(false, 100, 1, type.ToString(), 10, 3, 4, 40, 1, Unit.Core.None, type, special);
    unit.UnitSetUp();
    game.CurentGameMap.PlaceUnit(unit, tile);
    player.DeployedUnits.Add(unit);
    return unit;
}

// Setup reads once, sorts newest first, and preserves the original selection quirks.
var empty = new NewGameSetupData();
string? notice = null;
empty.Notice += (text, _) => notice = text;
empty.Initialize();
Check(empty.BuildGame() == null && notice == "No maps found. Save a map first.", "No-map message");
SaveAndLoaderHandler.SaveMap(new Map(12, 8), "older");
SaveAndLoaderHandler.SaveMap(new Map(12, 8), "newer");
File.SetLastWriteTimeUtc(Path.Combine("Saves", "Maps", "older.map"), new DateTime(2025, 1, 1));
File.SetLastWriteTimeUtc(Path.Combine("Saves", "Maps", "newer.map"), new DateTime(2025, 1, 2));
SaveAndLoaderHandler.SaveUnitTemplates(new List<Unit> { new Unit(false, 70, 1, "Custom Rebel", 17, 1, 4, 10, 1,
    Unit.Core.None, Unit.UnitType.Rebel, Unit.UnitSpecial.None) }, "custom");
var setup = new NewGameSetupData();
setup.Initialize();
Check(setup.Maps[0].FileName == "newer.map", "Newest map selected");
Check(setup.SelectedMode == NewGameSetupData.SetupGameMode.Contest, "Original unconditional mode assignment");
setup.UnitsModeChecked(true);
Check(setup.SelectedTemplateName == "DefaultUnits", "Switching to custom retains original preselected-template quirk");
setup.SelectedTemplateIndex = -1;
setup.SelectedTemplateIndex = 0;
Game started = setup.BuildGame()!;
Check(started.UnitTemplateFileName == "custom", "Explicit custom-template selection");
Check(started.Player1.DeployedUnits.Count == 1 && started.Player2.DeployedUnits.Count == 1, "Starting commanders");
Check(started.Player1.AlloyResources == 45 && started.Player1.FuelResources == 7 && started.Player2.AlloyResources == 45,
    "Original start income and resource asymmetry");
setup.UnitsModeChecked(false);
Check(setup.BuildGame()!.UnitTemplateFileName == "DefaultUnits", "Default template mode");
int initialMapCount = setup.Maps.Count;
SaveAndLoaderHandler.SaveMap(new Map(12, 8), "added-after-open");
Check(setup.Maps.Count == initialMapCount, "Setup file lists do not refresh after opening");
File.WriteAllText(Path.Combine("Saves", "Maps", "unsupported.map"), "VER|4\nSIZE|12|8\n");
var badSetup = new NewGameSetupData();
badSetup.Initialize();
badSetup.SelectMap(badSetup.Maps.ToList().FindIndex(x => x.FileName == "unsupported.map"));
badSetup.Notice += (text, _) => notice = text;
Check(badSetup.BuildGame() == null && notice!.Contains("Unsupported save version 4"), "Setup map-load failure");

File.WriteAllText(Path.Combine("Saves", "Units", "empty.csv"), "header\n");
Game emptyTemplatesGame = NewGame();
emptyTemplatesGame.UnitTemplateFileName = "empty";
var emptyTemplatesController = new GameplayController(emptyTemplatesGame);
int renderRequests = 0;
emptyTemplatesController.MapRenderRequested += () => renderRequests++;
emptyTemplatesController.Initialize();
Check(emptyTemplatesController.Info == "No unit templates loaded." && renderRequests == 0,
    "Original empty-template startup exits before rendering");

Game game = NewGame();
GameplayController controller = Controller(game);
Check(controller.ShopEntries.Count(x => x.Tag is Unit.UnitType) == 13, "Thirteen purchasable unit types");
Check(!controller.ShopEntries.Any(x => x.Tag is Unit.UnitType t && t == Unit.UnitType.CMP), "No commander in shop");
Check(controller.ShopEntries.Count(x => x.Tag is Building.BuildingType) == 5, "Five economic/strategic buildings");
Check(controller.P1FuelText == "", "Legacy initial top-bar values remain blank until sync");
controller.BtnMove_Click();
Check(controller.Info == "No unit selected. Click a unit first.", "Move validation");
controller.SelectShopItem(controller.ShopEntries.FindIndex(x => x.Tag is Unit.UnitType t && t == Unit.UnitType.Rebel));
Tile deploy = controller.DeployTiles.First();
int alloy = game.CurrentPlayer.AlloyResources;
controller.GameHex_LeftClick(deploy);
Unit rebel = deploy.OccupyingUnit;
Check(rebel.ThisUnitType == Unit.UnitType.Rebel && game.CurrentPlayer.AlloyResources == alloy - 10, "Deploy and price");
controller.GameHex_LeftClick(deploy);
controller.BtnMove_Click();
Tile destination = controller.MoveTiles.First();
int fuel = game.CurrentPlayer.FuelResources;
controller.GameHex_LeftClick(destination);
Check(rebel.Position == destination && rebel.HasPerformedAction && game.CurrentPlayer.FuelResources == fuel, "Free infantry move and action flag");
controller.BtnMove_Click();
Check(controller.Info.Contains("already acted"), "Repeated action blocked");
controller.BtnNextTurn_Click();
Check(game.CurrentPlayer == game.Player2 && game.PlayerTurnCounter == 1, "Switch to Player 2");
controller.BtnNextTurn_Click();
Check(game.CurrentPlayer == game.Player1 && game.PlayerTurnCounter == 2 && !rebel.HasPerformedAction, "Full turn refresh");

// A normal attack must use the existing Game model's action and damage rules.
game = NewGame();
controller = Controller(game);
var attacker = Add(game, game.Player1, Unit.UnitType.Rebel, game.CurentGameMap.GetTile(3, 3));
var victim = Add(game, game.Player2, Unit.UnitType.Rebel, game.CurentGameMap.GetNeighbors(attacker.Position).First(t => t.IsWalkable()));
controller.GameHex_LeftClick(attacker.Position);
controller.BtnAttack_Click();
Check(controller.AttackTiles.Contains(victim.Position), "Enemy target highlighting");
int hp = victim.HP;
controller.GameHex_LeftClick(victim.Position);
Check(victim.HP < hp && attacker.HasPerformedAction, "Attack damage and action flag");
controller.GameHex_LeftClick(victim.Position);
controller.BtnAttack_Click();
Check(controller.Info == "You can only attack with your own units!", "Opponent ownership guard");

// Building deployment intentionally keeps the original double deduction.
game = NewGame();
controller = Controller(game);
game.Player1.AlloyResources = 1000;
Add(game, game.Player1, Unit.UnitType.Engeneer, game.CurentGameMap.GetTile(3, 3));
int hubIndex = controller.ShopEntries.FindIndex(x => x.Tag is Building.BuildingType t && t == Building.BuildingType.Hub);
controller.SelectShopItem(hubIndex);
Tile buildingTile = controller.DeployTiles.First();
controller.GameHex_LeftClick(buildingTile);
Check(buildingTile.Building?.ThisBuildingType == Building.BuildingType.Hub, "Engineer building deployment");
Check(game.Player1.AlloyResources == 640, "Original double building cost retained");
Check(game.Player1.OwnedBuildings.Contains(buildingTile.Building!), "Building ownership recorded");

// Target selection and direct Unit.UseSpecial execution follow the original click path.
game = NewGame();
controller = Controller(game);
var racm = Add(game, game.Player1, Unit.UnitType.RACM, game.CurentGameMap.GetTile(3, 3), Unit.UnitSpecial.RACMSpecial);
controller.GameHex_LeftClick(racm.Position);
controller.BtnSpecail_Click();
Check(controller.SpecialTiles.Count == 1 && controller.SpecialTiles[0] == racm.Position, "Fortify self target");
controller.GameHex_LeftClick(racm.Position);
Check(racm.IsFortified && racm.SpecialCooldown > 0, "Special effect and cooldown");
controller.BtnSpecail_Click();
Check(controller.Info.Contains("cooldown"), "Special cooldown guard");
controller.BtnCancel_Click();
Check(controller.SelectedUnit == null, "Cancel deselects in idle mode");

SaveAndLoaderHandler.SaveGame(game, "roundtrip");
Game loaded = SaveAndLoaderHandler.GameLoad("roundtrip");
Check(loaded.Player1.DeployedUnits.Count == game.Player1.DeployedUnits.Count, "Saved unit ownership");
Check(loaded.Player1.DeployedUnits.Any(u => u.ThisUnitType == Unit.UnitType.RACM && u.SpecialCooldown == 0), "Original save format omits cooldown state");
Check(loaded.Player1.AlloyResources == game.Player1.AlloyResources && loaded.CurrentPlayer == loaded.Player1, "Saved resources and turn");
controller.BtnSave_Click();
Check(Directory.GetFiles(Path.Combine("Saves", "GameSaves"), "autosave_game*.sagm").Length == 1, "Timestamped game save");
bool home = false;
controller.HomeRequested += () => home = true;
controller.BtnBack_Click();
Check(home && File.Exists(Path.Combine("Saves", "GameSaves", "autosave_current.sagm")), "Home autosaves before navigation");

// Every purchasable fallback template must deploy through the same handler.
foreach (Unit.UnitType type in Enum.GetValues<Unit.UnitType>()) {
    if (type is Unit.UnitType.None or Unit.UnitType.CMP) continue;
    game = NewGame(); controller = Controller(game);
    game.Player1.AlloyResources = 9999;
    int index = controller.ShopEntries.FindIndex(x => x.Tag is Unit.UnitType t && t == type);
    controller.SelectShopItem(index);
    Tile target = controller.DeployTiles.First();
    controller.GameHex_LeftClick(target);
    Check(target.OccupyingUnit?.ThisUnitType == type, $"Fallback shop template {type}");
}

// All special handlers keep their existing target and execution behavior,
// including the mismatches between window targeting and Unit.UseSpecial.
var abilities = new[] {
    (Unit.UnitType.Protectors, Unit.UnitSpecial.ProtectorsSpecial, true),
    (Unit.UnitType.Engeneer, Unit.UnitSpecial.EngeneerSpecial, true),
    (Unit.UnitType.RACM, Unit.UnitSpecial.RACMSpecial, true),
    (Unit.UnitType.HoverTank, Unit.UnitSpecial.HoverTankSpecial, true),
    (Unit.UnitType.Hawk, Unit.UnitSpecial.HawkSpecial, true),
    (Unit.UnitType.Wasp, Unit.UnitSpecial.WaspSpecial, false),
    (Unit.UnitType.BOHS, Unit.UnitSpecial.BOHSpecial, true),
    (Unit.UnitType.Sunlight, Unit.UnitSpecial.SunlightSpecial, true),
    (Unit.UnitType.Breacher, Unit.UnitSpecial.BreacherSpecial, false),
    (Unit.UnitType.Redoubt, Unit.UnitSpecial.RedoubtSpecial, true),
    (Unit.UnitType.CMP, Unit.UnitSpecial.CMPSpecial, false),
    (Unit.UnitType.HoverTransporter, Unit.UnitSpecial.HoverTransporterSpecial, false)
};
foreach (var (type, special, succeeds) in abilities) {
    game = NewGame(); controller = Controller(game);
    Unit actor = Add(game, game.Player1, type, game.CurentGameMap.GetTile(3, 3), special);
    if (type == Unit.UnitType.Hawk) actor.HP = 1;
    Tile adjacent = game.CurentGameMap.GetNeighbors(actor.Position).First(t => t.IsWalkable());
    Unit enemy = Add(game, game.Player2, Unit.UnitType.Rebel, adjacent);
    controller.GameHex_LeftClick(actor.Position);
    controller.BtnSpecail_Click();
    Check(controller.SpecialTiles.Count > 0, $"{type} target list");
    Tile target = type is Unit.UnitType.Sunlight or Unit.UnitType.Breacher
        ? enemy.Position : controller.SpecialTiles.First();
    controller.GameHex_LeftClick(target);
    Check(controller.Info.Contains(succeeds ? " used " : "Failed to use special ability."), $"{type} original special result");
    if (type == Unit.UnitType.Protectors)
        Check(enemy.DamageReduction == .5, "Original shield also affects an adjacent opponent");
    if (type == Unit.UnitType.Redoubt)
        Check(actor.CurrentFuel == actor.MaxFuel, "Original jump does not consume fuel");
}

// The original death handler marks game over before removing a dead commander.
game = NewGame(); controller = Controller(game);
Unit commander = game.Player2.DeployedUnits.Single();
Unit killer = Add(game, game.Player1, Unit.UnitType.Rebel,
    game.CurentGameMap.GetNeighbors(commander.Position).First(t => t.IsWalkable()));
commander.HP = 1;
controller.GameHex_LeftClick(killer.Position);
controller.BtnAttack_Click();
controller.GameHex_LeftClick(commander.Position);
Check(game.CurentGameState == Game.GameState.GameOver && commander.HP == 0, "Commander destruction marks game over");
Check(game.GetWinner() == null && game.Player2.DeployedUnits.Contains(commander), "Original dead-commander ownership/winner quirk");
string? victory = null;
controller.Notice += (text, _) => victory = text;
controller.BtnNextTurn_Click();
Check(victory == "Game Over! Player 2 wins!", "Original next-turn victory fallback");
Console.WriteLine($"PASS: {checks} setup/gameplay checks; fixtures isolated under {working}");
