#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace NewDawn.Models
{

    public class Game
    {
        //enums
        public enum GameState { MainMenu = 0, FirstPlayerTurn = 1, SecondPlayerTurn = 2, GameOver = 3};
        public enum InteractionMode { None, Deploy, Move, Attack, Special };
        public enum Resourese { Fuel = 0, Alloy = 1};

        //fields
        private string _unitTemplateFileName;
        private bool _skipTurn = false;
        private int _playerTurnCounter;
        private Map _curentGameMap;
        private Player _player1;
        private Player _player2;
        private Player _currentPlayer;
        private Player _currentOpponent;
        //private AI _bot1;
        private GameState _currentGameState;

        //properties

        /// <summary>
        /// Gets or sets the file name of the unit template to be used.
        /// </summary>
        public string UnitTemplateFileName
        {
            get { return _unitTemplateFileName; } set { _unitTemplateFileName = value; }
        }

        /// <summary>
        /// Gets or sets the current state of the game.
        /// </summary>
        public GameState CurentGameState
        {
            get { return _currentGameState; }
            set { _currentGameState = value; }
        }

        /// <summary>
        /// Gets or sets the number of turns taken by the player.
        /// </summary>
        public int PlayerTurnCounter
        {
            get { return _playerTurnCounter; }
            set 
            {
                if(value >= 0)
                    _playerTurnCounter = value;
                else
                    _playerTurnCounter = 0;
            }
        }

        /// <summary>
        /// Gets or sets the current game map.
        /// </summary>
        public Map CurentGameMap
        {
            get { return _curentGameMap; }
            set 
            {
                if(value != null)
                    _curentGameMap = value;
                else
                    throw new ArgumentNullException("CurentGameMap cannot be null");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current player's turn should be skipped.
        /// </summary>
        public bool SkipTurn
        {
            get { return _skipTurn; }
            set { _skipTurn = value; }
        }

        /// <summary>
        /// Gets or sets the first player in the game.
        /// </summary>
        public Player Player1
        {
            get { return _player1; }
            set 
            {
                if(value != null)
                    _player1 = value;
                else
                    throw new ArgumentNullException("Player1 cannot be null");
            }
        }

        /// <summary>
        /// Gets or sets the second player in the game.
        /// </summary>
        public Player Player2
        {
            get { return _player2; }
            set 
            {
                if(value != null)
                    _player2 = value;
                else
                    throw new ArgumentNullException("Player2 cannot be null");
            }
        }

        /// <summary>
        /// Gets or sets the Current player in the game, validating it accordingly.
        /// </summary>
        public Player CurrentPlayer
        {
            get { return _currentPlayer; }
            set 
            {
                if(value != null)
                    _currentPlayer = value;
                else
                    throw new ArgumentNullException("CurrentPlayer cannot be null");
            }
        }

        /// <summary>
        /// Gets or sets the opponent of the current player. Cannot be null.
        /// </summary>
        public Player CurrentOpponent
        {
            get { return _currentOpponent; }
            set 
            {
                if(value != null)
                    _currentOpponent = value;
                else
                    throw new ArgumentNullException("CurrentOpponent cannot be null");
            }
        }
        //constructor 

        /// <summary>
        /// Creates a new game with two players and a map
        /// </summary>
        /// <param name="player1">First player</param>
        /// <param name="player2">Second player</param>
        /// <param name="gameMap">The map to play on</param>
        public Game(Player player1, Player player2, Map gameMap)
        {
            Player1 = player1;
            Player2 = player2;
            CurentGameMap = gameMap;
            CurrentPlayer = Player1;
            CurrentOpponent = Player2;
            CurentGameState = GameState.FirstPlayerTurn;
            PlayerTurnCounter = 1;
            SkipTurn = false;
            GetAllTargetForCurrentPlayer();
        }


        /// <summary>
        /// Checks if it is the specified player's turn
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>True if it's the player's turn, false otherwise</returns>
        public bool IsPlayerTurn(Player player)
        {
            return CurrentPlayer == player;
        }

        /// <summary>
        /// Creates a default game with two players and either loads an autosave or creates a new 20x14 map
        /// </summary>
        /// <returns>A new Game instance</returns>
        public Game CreateDefaultGame() // creates a default game with 2 players and a new map or loads autosave
        {
            Player p1 = new Player(startingAlloy: 15, startingFuel: 0);
            Player p2 = new Player(startingAlloy: 30, startingFuel: 0);//  starts with 30 as compensation for being second

            Map map;
            try // try to load autosave
            {
                map = SaveAndLoaderHandler.LoadMap("autosave_current");
            }
            catch
            { // if fails create new map mb should move user to main menu (?)
                map = new Map(20, 14);
            }
            return new Game(p1, p2, map);
            //! should allow user to choose map from saved folder(SaveMaps), for now just load autosave or create new !
        }

        /// <summary>
        /// Initializes the game with a map and two players, and places starting Commander units (CMPs)
        /// </summary>
        /// <param name="map">The map to play on</param>
        /// <param name="player1">First player</param>
        /// <param name="player2">Second player</param>
        /// <param name="player1StartsFirst">If true, player 1 goes first. If false, player 2 goes first.</param>

        public void InitializeGame(Map map, Player player1, Player player2, bool player1StartsFirst = true)
        {
            CurentGameMap = map;
            Player1 = player1;
            Player2 = player2;

            
            PlaceStartingCMPs(); //this is my  method for commander auto-placemnt


            if (player1StartsFirst)
            {
                CurrentPlayer = Player1;
                CurrentOpponent = Player2;
                CurentGameState = GameState.FirstPlayerTurn;
            }
            else
            {
                CurrentPlayer = Player2;
                CurrentOpponent = Player1;
                CurentGameState = GameState.SecondPlayerTurn;
            }

            PlayerTurnCounter = 1;
            StartPlayerTurn();
        }

        /// <summary>
        /// Places Commander Mech Platforms (CMPs) for both players at opposite corners of the map
        /// </summary>
        private void PlaceStartingCMPs()
        {
            // this methdo is for specific spawn placement on map
            Tile p1SpawnTile = GetPlayerSpawnTile(1); // Bottom-left corner
            Tile p2SpawnTile = GetPlayerSpawnTile(2); // Top-right corner

            Unit p1CMP = CreateCMP();
            Unit p2CMP = CreateCMP();

            CurentGameMap.PlaceUnit(p1CMP, p1SpawnTile);
            CurentGameMap.PlaceUnit(p2CMP, p2SpawnTile);

            Player1.DeployedUnits.Add(p1CMP);
            Player2.DeployedUnits.Add(p2CMP);
        }

        /// <summary>
        /// Gets the spawn tile for a player based on their number
        /// </summary>
        /// <param name="playerNumber">1 for Player 1 (bottom-left), 2 for Player 2 (top-right)</param>
        /// <returns>The tile where the player should spawn their CMP</returns>
        private Tile GetPlayerSpawnTile(int playerNumber)
        {
            if (playerNumber == 1)
            {
                // Player 1 spawns bottom-left
                return CurentGameMap.GetTile(0, CurentGameMap.Height - 1);
            }
            else
            {
                // Player 2 spawns top-right
                return CurentGameMap.GetTile(CurentGameMap.Width - 1, 0);
            }
        }

        /// <summary>
        /// Creates a Commander Mech Platform (CMP) unit with predefined stats
        /// </summary>
        /// <returns>A new CMP unit</returns>
        private Unit CreateCMP()
        {
            Unit u = new Unit(
                newArmored: true,
                newHP: 250,
                placementPerTile: 1,
                mapObjectName: "CMP",
                newPrice: 100,           // ← Goes to MapObject constructor
                newRange: 2,             
                newSpeed: 3,
                newDamge: 100,
                newFuelConsumption: 3,
                newCore: Unit.Core.None,
                newUnitType: Unit.UnitType.CMP,
                newUnitSpecial: Unit.UnitSpecial.CMPSpecial
            );
            u.UnitSetUp();
            return u;
        }

        /// <summary>
        /// Checks if either player has lost their Commander (CMP).
        /// If a CMP is destroyed, the game ends.
        /// </summary>

        public void CheckWinCondition()
        {
            if (!Player1.HasDeployedUnitOfType(Unit.UnitType.CMP))
            {
                CurentGameState = GameState.GameOver;
                //Player2 wins
            }
            else if (!Player2.HasDeployedUnitOfType(Unit.UnitType.CMP))
            {
                CurentGameState = GameState.GameOver;
                //Player1 wins
            }
        }

        /// <summary>
        /// Updates the list of available attack targets for the current player.
        /// Finds all enemy units that are within attack range of any friendly unit.
        /// </summary>
        public void GetAllTargetForCurrentPlayer()
        {
            if (CurrentPlayer == null || _curentGameMap == null)
                return;
            List<Unit> targets = new List<Unit>();
            if (CurrentOpponent == null)
            {
                CurrentPlayer.AvailableTargets = new List<Unit>();
                return;
            }
            foreach (Unit attacker in CurrentPlayer.DeployedUnits) //loop through all current player's units
            {
                if (attacker == null || attacker.Position == null)
                    continue;

                int range = attacker.Range;
                if (range <= 0)
                    continue;

                foreach (Unit enemy in CurrentOpponent.DeployedUnits) //loop through all enemy units
                {
                    if (enemy == null || enemy.Position == null)
                        continue;

                    int dist = CurentGameMap.GetDistance(attacker.Position, enemy.Position);

                    if (dist <= range)
                    {
                        if (!targets.Contains(enemy))
                            targets.Add(enemy);
                    }
                }
            }
            CurrentPlayer.AvailableTargets = targets;
        }


        /// <summary>
        /// Ends the current player's turn and switches to the oppsonent
        /// </summary>
        public void EndPlayerTurn()
        {
            if (CurrentPlayer == null || CurentGameMap == null)
                return;
            // Any end of turn effects can be handled here
            EndTurnCleanup(CurrentPlayer);
            // Switch to the other player
            CurrentPlayer = (CurrentPlayer == Player1) ? Player2 : Player1;
            CurrentOpponent = (CurrentPlayer == Player1) ? Player2 : Player1;
            // Increment turn counter if Player1's turn starts
            if (CurrentPlayer == Player1)
            {
                PlayerTurnCounter++;
            }
            StartPlayerTurn();
        }

        /// <summary>
        /// Cleans up temporary effects on a player's units at the end of their turn
        /// </summary>
        /// <param name="player">The player whose units should be cleaned up</param>
        public void EndTurnCleanup(Player player)
        {
            if (player == null)
                return;
            foreach (Unit unit in player.DeployedUnits)
            {
                if (unit != null)
                {
                    unit.EndTurnCleanup();
                }
            }
        }

        /// <summary>
        /// Initializes the current player's turn by generating income, refilling fuel,
        /// refreshing units, and updating available targets
        /// </summary>
        public void StartPlayerTurn()
        {
            if (CurrentPlayer == null || CurentGameMap == null)
                return;

            // I just made this method so that players c=can collect on their turn start
            CurrentPlayer.GenerateIncome();
            CurrentPlayer.RefillAllUnitsFuel();
            CurrentPlayer.RefreshUnits();
            ReduceAllUnitCooldowns(CurrentPlayer); // this method is commented out below
            GetAllTargetForCurrentPlayer();
            CheckWinCondition();
        }


        /// <summary>
        /// Reduces the special ability cooldown for all of a player's units by 1
        /// </summary>
        /// <param name="player">The player whose units should have cooldowns reduced</param>

        public void ReduceAllUnitCooldowns(Player player)
        {
            if (player == null)
                return;
            foreach (Unit unit in player.DeployedUnits)
            {
                if (unit == null)
                    continue;
                if (unit.SpecialCooldown > 0)
                {
                    unit.ReduceSpecialCooldown();
                }
            }
        }

        /// <summary>
        /// Executes an attack from one unit to another, dealing damage and checking for death
        /// </summary>
        /// <param name="attacker">The unit performing the attack</param>
        /// <param name="target">The unit being attacked</param>
        /// <returns>True if attack was successful, false if invalid</returns>

        public bool ExecuteAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null)
                return false;

            if (attacker.Position == null || target.Position == null)
                return false;

            // Check if attacker belongs to current player
            if (!CurrentPlayer.DeployedUnits.Contains(attacker))
                return false;

            // Check if target belongs to opponent
            if (!CurrentOpponent.DeployedUnits.Contains(target))
                return false;

            //in range?
            int distance = CurentGameMap.GetDistance(attacker.Position, target.Position);
            if (distance > attacker.Range)
                return false;

            // Check if attacker can attack (hasn't acted yet)
            if (attacker.HasPerformedAction)
                return false;

            // Calculate and deal damage
            int damage = attacker.DoDamgeMainGun(target, distance);
            target.ReceiveDamge(damage);

            // Mark attacker as having acted
            attacker.HasPerformedAction = true;

            // if unit died
            if (target.HP <= 0)
            {
                HandleUnitDeath(target);
            }

            // Update available targets
            GetAllTargetForCurrentPlayer();

            return true;
        }


        /// <summary>
        /// Handles the death of a unit, removing it from the map and checking for CMP death (game over)
        /// </summary>
        /// <param name="deadUnit">The unit that has died</param>

        private void HandleUnitDeath(Unit deadUnit)
        {
            if (deadUnit == null)
                return;

            // Check if it's CMP (Commander) - game over if CMP dies
            if (deadUnit.ThisUnitType == Unit.UnitType.CMP)
            {
                // Find which player owned this unit
                Player owner = null;
                if (Player1.DeployedUnits.Contains(deadUnit))
                    owner = Player1;
                else if (Player2.DeployedUnits.Contains(deadUnit))
                    owner = Player2;

                if (owner != null)
                {
                    // Owner loses the game
                    EndGameWithWinner(owner == Player1 ? Player2 : Player1);
                    return;
                }
            }


            // Remove unit from map and owner
            CurentGameMap.RemoveUnit(deadUnit);

            if (Player1.DeployedUnits.Contains(deadUnit))
                Player1.RemoveUnit(deadUnit);
            else if (Player2.DeployedUnits.Contains(deadUnit))
                Player2.RemoveUnit(deadUnit);
        }




        /// <summary>
        /// Moves a unit from its current position to a destination tile, consuming fuel
        /// </summary>
        /// <param name="unit">The unit to move</param>
        /// <param name="destinationTile">The tile to move to</param>
        /// <returns>True if move was successful, false if invalid</returns>

        public bool MoveUnit(Unit unit, Tile destinationTile)
        {
            if (unit == null || destinationTile == null || CurentGameMap == null) return false;
            if (unit.Position == null) return false;
            if (!CurrentPlayer.DeployedUnits.Contains(unit)) return false;
            if (unit.HasPerformedAction) return false;

            List<Tile> reachableTiles = CurentGameMap.GetMovementRange(unit);
            if (!reachableTiles.Contains(destinationTile)) return false;

            int fuelCost = IsFreeMoveUnit(unit)
                ? 0
                : CurentGameMap.CalculateMovementFuelCost(unit, unit.Position, destinationTile);

            if (fuelCost > 0)
            {
                if (!CurrentPlayer.SpendFuelResources(fuelCost))
                    return false;
            }

            Tile fromTile = unit.Position;
            CurentGameMap.MoveUnit(unit, fromTile, destinationTile);

            if (unit.IsFortified) unit.RemoveFortify();

            unit.HasPerformedAction = true;
            return true;
        }
        private static bool IsFreeMoveUnit(Unit unit) =>
        unit.ThisUnitType == Unit.UnitType.Rebel
        || unit.ThisUnitType == Unit.UnitType.Railgunner
        || unit.ThisUnitType == Unit.UnitType.Protectors
        || unit.ThisUnitType == Unit.UnitType.Engeneer;




        /// <summary>
        /// Deploys a unit from the player's available units onto a tile
        /// </summary>
        /// <param name="unit">The unit to deploy</param>
        /// <param name="tile">The tile to deploy to</param>
        /// <returns>True if deployment was successful, false if invalid</returns>
        public bool DeployUnit(Unit unit, Tile tile)
        {
            if (unit == null || tile == null || CurentGameMap == null || CurrentPlayer == null)
                return false;

            if (!CurrentPlayer.AvailableUnitsToPlace.Contains(unit))
                return false;

            //Makes deployment rules consistent even if UI highlight logic changes
            if (!tile.IsWalkable() || tile.IsOccupied() || tile.Building != null)
                return false;

            // Physically place the unit so it renders and can move
            CurentGameMap.PlaceUnit(unit, tile); // sets tile.OccupyingUnit and unit.Position:contentReference

            // Keep player lists consistent 
            CurrentPlayer.AvailableUnitsToPlace.Remove(unit);

            if (!CurrentPlayer.DeployedUnits.Contains(unit))
                CurrentPlayer.DeployedUnits.Add(unit);

            GetAllTargetForCurrentPlayer();
            return true;

        }

        /// <summary>
        /// Ends the game with the specified winner
        /// </summary>
        /// <param name="winner">The player who won the game</param>
        private void EndGameWithWinner(Player winner)
        {
            CurentGameState = GameState.GameOver;
        }


        /// <summary>
        /// Gets the winning player if the game is over
        /// </summary>
        /// <returns>The winning player, or null if game is not over or no winner determined</returns>
        public Player GetWinner()
        {
            if (CurentGameState != GameState.GameOver)
                return null;

            if (!Player1.HasDeployedUnitOfType(Unit.UnitType.CMP))
                return Player2;
            else if (!Player2.HasDeployedUnitOfType(Unit.UnitType.CMP))
                return Player1;

            return null;
        }

        /// <summary>
        /// Gets a formatted string showing the current turn number and whose turn it is
        /// </summary>
        /// <returns>String in format "Turn X - Player Y's Turn"</returns>
        public string GetTurnInfo()
        {
            string playerName = (CurrentPlayer == Player1) ? "Player 1" : "Player 2";
            return $"Turn {PlayerTurnCounter} - {playerName}'s Turn";
        }

        /// <summary>
        /// Reset game to initial state
        /// </summary>
        public void ResetGame()
        {
            _playerTurnCounter = 0;
            _currentGameState = GameState.MainMenu;
            _currentPlayer = null;
            _currentOpponent = null;
        }

        /// <summary>
        /// Saves the current game map to a file
        /// </summary>
        /// <param name="filename">The filename to save to (default: "autosave_current")</param>
        public void SaveGame(string filename = "autosave_current")
        {
            try
            {
                SaveAndLoaderHandler.SaveMap(CurentGameMap, filename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save game: {ex.Message}"); // this is how i think we log the exception message without writing to screen
            }
        }
    }


}
