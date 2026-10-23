using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Data;
using static NewDawn.Models.Tile;
using Path = System.IO.Path;
namespace NewDawn.Models
{
    /// <summary>
    /// Represents the hexagonal grid map on which the game is played.
    /// Manages tiles, units, buildings, and provides pathfinding and distance calculation.
    /// Uses axial coordinate system internally with display offset rendering.
    /// </summary>
    public class Map
    {
        private Dictionary<(int column, int row), Tile> tiles; //Q and R are column and row but is just the standard notation for hex 
        private List<Unit> allUnits;
        private List<Building> allBuildings; //this is for later if we do actually implement buildings class
        private int width;
        private int height;


        /// <summary>
        /// Creates a new hexagonal map with the specified dimensions
        /// </summary>
        /// <param name="width">Width of the map in tiles</param>
        /// <param name="height">Height of the map in tiles</param>
        public Map(int width, int height)
        {
            this.width = width;
            this.height = height;
            tiles = new Dictionary<(int column, int row), Tile>();
            allUnits = new List<Unit>();
            allBuildings = new List<Building>();

            InitializeTiles();
        }

        /// <summary>
        /// Gets the width of the map in tiles
        /// </summary>
        public int Width 
        { 
            get { return width; } 
        }

        /// <summary>
        /// Gets the height of the map in tiles
        /// </summary>
        public int Height 
        {
            get { return height; } 
        }


        /// <summary>
        /// Initializes all tiles in the map with Field terrain.
        /// Creates a rectangular hex grid using axial coordinates with row offset.
        /// </summary>
        private void InitializeTiles()
        {
            // this creates our tiles in a "rectangular" hex grid
            for (int row = 0; row < height; row++)
            {
                int columnOffset = row / 2; // Offset for odd rows
                for (int column = -columnOffset; column < width - columnOffset; column++)
                {
                    Tile tile = new Tile(column, row, TerrainType.Field);
                    tiles[(column, row)] = tile;
                }
            }
        }

        /// <summary>
        /// Gets a tile at the specified axial coordinates
        /// </summary>
        /// <param name="column">Column (q) coordinate</param>
        /// <param name="row">Row (r) coordinate</param>
        /// <returns>The tile at the coordinates, or null if not found</returns>

        public Tile GetTile(int column, int row)
        {
            if (tiles.TryGetValue((column, row), out Tile tile))
                return tile;
            else
                return null;
        }

        /// <summary>
        /// Gets all tiles in the map as a list
        /// </summary>
        /// <returns>List of all tiles</returns>

        public List<Tile> GetAllTiles()
        {
            return tiles.Values.ToList();
        }
        public bool IsValidTile(int column, int row)
        {
            return tiles.ContainsKey((column, row));
        }

        /// <summary>
        /// Returns the 6 neighboring tiles of the given tile (hex grid neighbors).
        /// IMPORTANT: this method matches the way the map is DRAWN on screen.
        /// Renderer uses "display offset" columns: displayCol = Column + (Row / 2),
        /// and that creates a staggered layout where neighbor math depends on parity.
        /// To avoid parity bugs, we:
        /// 1) Convert the tile to the renderer's display offset coordinates,
        /// 2) Convert display offset -> cube coordinates,
        /// 3) Add the 6 cube direction vectors,
        /// 4) Convert each cube neighbor back -> display offset -> axial -> Tile lookup.
        /// </summary>
        public List<Tile> GetNeighbors(Tile tile)
        {
            List<Tile> neighbors = new List<Tile>();
            if (tile == null)
                return neighbors;

            // Convert internal tile coords into the same "display offset" grid used by rendering.
            // col = displayCol, row = row
            ToDisplayOffset(tile, out int col, out int row);
            // Convert display offset coords into cube coords for correct hex neighbor math.
            DisplayOffsetToCube(col, row, out int x, out int y, out int z);
            // The 6 cube-direction vectors.
            // Moving to a neighbor changes two axes by ±1 and keeps x + y + z = 0.
            int[] dx = { 1, 1, 0, -1, -1, 0 };
            int[] dy = { -1, 0, 1, 1, 0, -1 };
            int[] dz = { 0, -1, -1, 0, 1, 1 };
            // For each of the 6 directions, compute the neighbor cube coordinate,
            // then convert it back to your internal tile coordinates and fetch the Tile object.
            for (int i = 0; i < 6; i++)
            {
                // Neighbor in cube space
                int nx = x + dx[i];
                int ny = y + dy[i];
                int nz = z + dz[i];
                // Convert cube neighbor -> display offset (col,row) that matches drawing layout
                CubeToDisplayOffset(nx, ny, nz, out int ncol, out int nrow);
                // Convert display offset -> axial (q,r), because your tiles are stored by (Column,Row)  as axial-like coordinates in the map dictionary.
                DisplayOffsetToAxial(ncol, nrow, out int nq, out int nr);
                // Fetch the actual Tile object. If it doesn't exist (edge of map), skip it.
                Tile neighbor = GetTile(nq, nr);
                if (neighbor != null)
                    neighbors.Add(neighbor);
            }

            return neighbors;
        }
        //helpers for GetNeighbors
        /// <summary>
        /// Converts cube coordinates (x,y,z) back into display offset coordinates (col,row).
        /// This is the inverse of DisplayOffsetToCube for the same layout.
        /// </summary>
        private static void CubeToDisplayOffset(int x, int y, int z, out int col, out int row)
        {
            col = x;
            row = z + ((col - (col & 1)) / 2);
        }
        /// <summary>
        /// Converts display offset coordinates (col,row) back into the axial coordinates (q,r)
        /// used by your Tile storage / GetTile(q,r).
        /// This is the inverse of ToDisplayOffset:
        /// displayCol = q + (r / 2)  =>  q = displayCol - (r / 2)
        /// </summary>
        private static void DisplayOffsetToAxial(int col, int row, out int q, out int r)
        {
            r = row;
            q = col - (row / 2);
        }

        #region Unit Management

        /// <summary>
        /// Places a unit on a specific tile
        /// </summary>
        /// <param name="unit">The unit to place</param>
        /// <param name="tile">The tile to place the unit on</param>
        public void PlaceUnit(Unit unit, Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentException("Cannot place unit on null tile");
            }

            // Add to tile (assuming tile can hold multiple units based on your design)
            tile.OccupyingUnit = unit;

            // Add to master list if not already there
            if (!allUnits.Contains(unit))
            {
                allUnits.Add(unit);
            }

            // Update unit's position
            unit.Position = tile;
        }


        /// <summary>
        /// Moves a unit from one tile to another
        /// </summary>
        /// <param name="unit">The unit to move</param>
        /// <param name="fromTile">The tile the unit is currently on</param>
        /// <param name="toTile">The tile to move the unit to</param>
        public void MoveUnit(Unit unit, Tile fromTile, Tile toTile)
        {
            if (fromTile == null || toTile == null)
            {
                throw new ArgumentException("Cannot move unit with null tiles");
            }
            
            // Remove from old tile
            if (fromTile.OccupyingUnit == unit) //OccupyingUnit needs to be on implemented in tile class
            {
                fromTile.OccupyingUnit = null;
            }

            // Place on new tile
            toTile.OccupyingUnit = unit;

            unit.Position = toTile; //Position needs implementation in tile class also (could be called something else dw)
        }

        /// <summary>
        /// Removes a unit from the map completely
        /// </summary>
        /// <param name="unit">The unit to remove</param>
        public void RemoveUnit(Unit unit)
        {
            if (unit.Position != null)
            {
                unit.Position.OccupyingUnit = null;
            }

            allUnits.Remove(unit);
        }

        /// <summary>
        /// Gets the unit occupying a specific tile
        /// </summary>
        /// <param name="tile">The tile to check</param>
        /// <returns>The unit on the tile, or null if empty</returns>
        public Unit GetUnitAtTile(Tile tile)
        {
            if (tile == null)
            {
                return null;
            }

            return tile.OccupyingUnit;
        }

        #endregion

        #region Building methods

        /// <summary>
        /// Checks if a building can be placed on a specific tile based on terrain and building type
        /// </summary>
        /// <param name="building">The building to place</param>
        /// <param name="tile">The tile to check</param>
        /// <returns>True if building can be placed, false otherwise</returns>
        public bool CanPlaceBuildingOnTile(Building building, Tile tile)
        {
            if (building == null || tile == null) return false;
            if (tile.Building != null) return false;

            // Normal rule: tile terrain allowed
            if (building.TilesThisCanBePlacedOn != null &&
                building.TilesThisCanBePlacedOn.Contains(tile.TileTerrainType))
                return true;

            // Special rules for specific building types
            switch (building.ThisBuildingType)
            {
                case Building.BuildingType.AlloyQuarry:
                    return HasAdjacentTerrainType(tile, TerrainType.Mountain);

                case Building.BuildingType.FuelExtractor:
                    return HasAdjacentTerrainType(tile, TerrainType.Swamp);

                default:
                    return false;
            }
        }

      

        /// <summary>
        /// Places a building on a tile after validating placement rules
        /// </summary>
        /// <param name="newBuilding">The building to place</param>
        /// <param name="tile">The tile to place the building on</param>

        public void PlaceBuilding(Building newBuilding, Tile tile)
        {
            if (tile == null)
                throw new ArgumentException("Cannot place building on null tile");

            if (newBuilding == null)
                throw new ArgumentException("Cannot place a null building on a tile");

            if (!CanPlaceBuildingOnTile(newBuilding, tile))
                throw new InvalidOperationException("Building cannot be placed on this tile.");

            tile.Building = newBuilding;

            if (!allBuildings.Contains(newBuilding))
            {
                allBuildings.Add(newBuilding);
            }

            newBuilding.Position = tile;
        }
        #endregion

        #region Pathfinding

        /// <summary>
        /// Checks if a tile is occupied by a unit
        /// </summary>
        /// <param name="tile">The tile to check</param>
        /// <returns>True if occupied, false otherwise</returns>
       

        public List<Tile> GetMovementRange(Unit unit)
        {
            if (unit == null || unit.Position == null)
            {
                return new List<Tile>();
            }

            List<Tile> reachableTiles = new List<Tile>();
            List<(Tile tile, int movementLeft)> frontier = new List<(Tile, int)>(); // Using List instead of Queue
            HashSet<Tile> visited = new HashSet<Tile>();

            // Start at unit's current position
            frontier.Add((unit.Position, unit.Speed));
            visited.Add(unit.Position);

            while (frontier.Count > 0)
            {
                // Get first item and remove it (like Dequeue)
                var currentItem = frontier[0];
                frontier.RemoveAt(0);

                Tile currentTile = currentItem.tile;
                int movementLeft = currentItem.movementLeft;

                if (movementLeft > 0)
                {
                    // Check all neighbors
                    foreach (Tile neighbor in GetNeighbors(currentTile))
                    {
                        if (!visited.Contains(neighbor) && neighbor.IsWalkable())
                        {
                            visited.Add(neighbor);

                            // Calculate movement cost (can be modified based on terrain)
                            int movementCost = 1; // Basic cost, modify based on terrain type
                            int remainingMovement = movementLeft - movementCost;

                            if (remainingMovement >= 0)
                            {
                                reachableTiles.Add(neighbor);
                                frontier.Add((neighbor, remainingMovement));
                            }
                        }
                    }
                }
            }

            return reachableTiles;
        }

        #endregion

        #region extra utility methods

        /// <summary>
        /// //gets the directory path for saved maps
        /// </summary>
        /// <returns></returns>
        public static string GetMapsDirectory() 
        {
            return Path.Combine(Environment.CurrentDirectory, "Saves", "Maps");
        }
        /// <summary>
        /// Returns the hex-grid distance between two tiles.
        /// IMPORTANT: this distance matches the way the map is DRAWN on screen
        /// Renderer uses a “display offset” column: displayCol = Column + (Row / 2)
        /// </summary>
        public int GetDistance(Tile tile1, Tile tile2)  //fixed render of tiles with AI help
        {
            // If either tile is missing, treat distance as invalid / unreachable
            if (tile1 == null || tile2 == null)
                return int.MaxValue;
            // Convert Tile (Column,Row) into the “display offset” coordinates used by rendering.
            // c = displayCol, r = row
            ToDisplayOffset(tile1, out int c1, out int r1);
            ToDisplayOffset(tile2, out int c2, out int r2);
            // Convert display offset coords to cube coords (x,y,z) so we can use cube distance.
            // c = displayCol, r = row
            DisplayOffsetToCube(c1, r1, out int x1, out int y1, out int z1);
            DisplayOffsetToCube(c2, r2, out int x2, out int y2, out int z2);
            // Standard hex distance in cube coordinates
            return CubeDistance(x1, y1, z1, x2, y2, z2);
        }
        //helpers for GetDistance
        /// <summary>
        /// Converts a Tile’s internal coordinates (Column, Row) into the SAME column index the renderer uses.
        /// Renderer logic is: displayCol = Column + (Row / 2).
        /// That effectively shifts columns depending on the row, to create a “staggered” hex layout.
        /// We keep row unchanged because rendering uses Row directly for vertical stepping.
        /// </summary>
        public static void ToDisplayOffset(Tile t, out int col, out int row)
        {
            row = t.Row;
            col = t.Column + (row / 2);
        }
        /// <summary>
        /// Converts “display offset” coordinates (col,row) into cube coordinates (x,y,z).
        /// We assume an odd-q vertical layout for the DISPLAY grid:
        /// - Columns are “staggered” (odd columns are shifted).
        /// </summary>
        private static void DisplayOffsetToCube(int col, int row, out int x, out int y, out int z)
        {
            x = col;
            z = row - ((col - (col & 1)) / 2);
            y = -x - z;
        }
        /// <summary>
        /// Computes the distance between two cube coordinates.
        /// Hex distance in cube space is max(|dx|,|dy|,|dz|).
        /// </summary>
        private static int CubeDistance(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            int dx = Math.Abs(x1 - x2);
            int dy = Math.Abs(y1 - y2);
            int dz = Math.Abs(z1 - z2);

            // cube distance
            return Math.Max(dx, Math.Max(dy, dz));
        }

        /// <summary>
        /// Gets all tiles within a specified radius of a center tile
        /// </summary>
        /// <param name="centerTile">The center tile</param>
        /// <param name="radius">The radius in tiles</param>
        /// <returns>List of all tiles within the radius (including center)</returns>
        public List<Tile> GetTilesInRadius(Tile centerTile, int radius)
        {
            List<Tile> tilesInRadius = new List<Tile>();

            foreach (var tile in tiles.Values)
            {
                if (GetDistance(centerTile, tile) <= radius)
                {
                    tilesInRadius.Add(tile);
                }
            }

            return tilesInRadius;
        }


        /// <summary>
        /// Checks if a tile has at least one neighbor of a specific terrain type
        /// </summary>
        /// <param name="tile">The tile to check</param>
        /// <param name="terrainType">The terrain type to look for</param>
        /// <returns>True if an adjacent tile has the terrain type, false otherwise</returns>

        public bool HasAdjacentTerrainType(Tile tile, TerrainType terrainType)
        {
            List<Tile> neighbors = GetNeighbors(tile);

            foreach (Tile neighbor in neighbors)
            {
                if (neighbor.TileTerrainType == terrainType)
                {
                    return true;
                }
              
            }

            return false;
        }

        /// <summary>
        /// Gets all tiles that lie roughly on a line from start to end, within a maximum distance.
        /// Used for line-of-sight abilities like Sunlight's beam attack.
        /// </summary>
        /// <param name="start">Starting tile</param>
        /// <param name="end">Ending tile (target)</param>
        /// <param name="maxDistance">Maximum distance to check</param>
        /// <returns>List of tiles along the path (excluding start tile)</returns>
        public List<Tile> GetTilesInDirection(Tile start, Tile end, int maxDistance)
        {
            List<Tile> tilesInPath = new List<Tile>();

            // Get all tiles within range
            List<Tile> allTiles = GetTilesInRadius(start, maxDistance);

            int directDistance = GetDistance(start, end);

            foreach (Tile tile in allTiles)
            {
                // Check if tile is roughly on the path to target
                int distToStart = GetDistance(start, tile);
                int distToEnd = GetDistance(tile, end);

                // Tile is "in line" if distances add up close to direct distance
                if (distToStart + distToEnd <= directDistance + 1 && tile != start)
                {
                    tilesInPath.Add(tile);
                }
            }

            return tilesInPath;
        }

        /// <summary>
        /// Calculates the fuel cost for a unit to move from one tile to another
        /// </summary>
        /// <param name="unit">The unit moving</param>
        /// <param name="fromTile">Starting tile</param>
        /// <param name="toTile">Destination tile</param>
        /// <returns>Fuel cost (distance × unit's fuel consumption)</returns>

        public int CalculateMovementFuelCost(Unit unit, Tile fromTile, Tile toTile)
        {
            int distance = GetDistance(fromTile, toTile);
            int baseCost = distance * unit.FuelConsumption;

            // Add terrain modifiers if needed
            // if (toTile.TileTerrainType == TerrainType.Swamp)
            //     baseCost *= 2;

            return baseCost;
        }

      

        #endregion
    }
}
