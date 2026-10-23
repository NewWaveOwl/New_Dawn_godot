# Project: New Dawn

**Names:** Matthew Buzzetti (2363090) ; Artem Sova (2272422)

---

## Project Description

The goal of the app is to create a sci-fi hex-tiled, turn-based strategy game with fast units and a small 25×17 diamond-shaped map. The game will feature **14 different unit types**, **5 buildings**, and **2 game modes**:

- **Elimination** – defeat the enemy commander.
- **Contest** – the first player to reach 100 contest points wins.

Units are divided into **Armored** and **Unarmored** categories. Unarmored units are cheaper and consume less fuel or none at all. To produce units, players must build **alloy quarries**, which generate alloy used to spawn additional units near the commander unit or hub building.

The game will include various terrain types that provide different buffs and debuffs to units. A core feature of the game is fast-paced combat with mechs and the ability to place units of different classes on the same tile.

Mech units have special abilities and customizable upgrades:

- **Special Moves** – each mech can choose between multiple abilities with a 3-turn cooldown.
- **Cores** – a special upgrade selected when the mech is created, providing a significant one-time bonus.

All mech units consume fuel to move, so players must build **fuel extractors** to maintain mobility.

The game will include saving and loading during matches, as well as access to previous saves from the main menu. The main menu will also feature a **map editor**, allowing players to create new maps and modify existing ones, and a **unit editor**, where the user can adjust unit stats.

---

## Formulating the Problem

Details for units, tiles, and buildings are in `ListOfUnits.xlsx`.  
The game will need **12 classes** and **7 WPF windows**. The coding of big classes will be split between both of us; smaller classes will be written individually.

The game reads **mouse clicks** as the main way of input. The `Game` stores units and maps, a game state for game saving/loading, and `GlobalGameState` for the application to understand where to read files in different `.csv` files.

The map will be split into hexes, the location of each hex will be stored, and interactions between hexes and the units on them will pass through pointers to the hex locations.

---

## Steps

### Designing

- The unit design for units would be outsourced to Artem’s friends; they want to make art for the game.
- Windows’ design would be split between team members.

### Implementing

#### Classes

- Create `Tile` class (Artem)
- Create `Map` class (Matthew)
- Create `MapObject` class (Artem and Matthew)
- Create `Unit : MapObject` class (Artem and Matthew)
- Create `Building : MapObject` class
- Create `Player` class (Matthew)
- Create `AI : Player` class (Artem)
- Create `Game` class (Artem and Matthew)
- Create `StingsClass` (Artem)
- Create `SaveLoader` class (Matthew)
- Create `MapCreator` class (Artem)
- Create `UnitEditor` class (Matthew)

#### UI

- `MainMenu` window (Artem)
- `SelectGameMode` window (Matthew)
- `Settings` window (Artem)
- `LoadSave` window (Matthew)
- `Game` window (Artem and Matthew)
- `MapEditor` (Artem)
- `UnitEditor` (Matthew)

### Testing

- Give the game to at least two people to play against each other 10 times.
- Give the game to at least two people to play against AI 10 times.
- Modify save files by hand and try to run the game.

### Fixing

- TBD

---

## Developing the Application

Our project will have **12 classes**:

### 1) Game Class

**What it does:**  
Central controller of the entire application. Loads game state, manages menus, turns, map objects, and win conditions.

**Why we need it:**  
It acts as the main entry point and orchestrator for all other classes, keeping the game logic consistent and synchronized.

---

### 2) Map Class

**What it does:**  
Stores tiles, their positions, and loads map layouts from files.

**Why we need it:**  
Provides structured access to the game world and allows the game to reference tiles through the `Game` class.

---

### 3) Tile Class

**What it does:**  
Represents a single tile using axial coordinates and contains tile-based effects (slow, debuffs, etc.).

**Why we need it:**  
Units interact with tiles constantly; tiles define terrain effects and rendering positions.

---

### 4) MapObject Class

**What it does:**  
Base class for units and buildings. Handles HP, armor, damage calculations, effects, and resource-generation capabilities.

**Why we need it:**  
Provides shared functionality for all interactive objects on the map (units & buildings).

---

### 5) Unit Class (inherits MapObject)

**What it does:**  
Represents combat units with damage arrays, speed, range, fuel, mech systems, and special moves. Tracks movement, deals damage, applies buffs/debuffs. Includes enums for Cores, Effects, and Specials.

**Why we need it:**  
Implements unit-specific combat mechanics, movement logic, and unique abilities that drive gameplay.

---

### 6) Building Class (inherits MapObject)

**What it does:**  
Represents buildings that can produce or multiply resources. Includes production-enhancing methods like `DoubleProduction()`.

**Why we need it:**  
Controls economy-related mechanics and provides strategic map control.

---

### 7) Player Class

**What it does:**  
Manages player resources, unit rosters, income per turn, deployment, upgrades, purchases, and combat actions.

**Why we need it:**  
Tracks each player’s economy and available units, enabling fair and structured gameplay.

---

### 8) AI Class (inherits Player)

**What it does:**  
Adds logic for autonomous decision-making: moving units, attacking, deploying units/buildings.

**Why we need it:**  
Allows single-player gameplay by providing an opponent with automated strategy.

---

### 9) Game Settings Class

**What it does:**  
Stores and handles game mode (Contest or Kill Commander), player preferences, mode descriptions, and names.

**Why we need it:**  
Keeps game rules consistent and allows different modes to be chosen and configured.

---

### 10) Save Loader Class

**What it does:**  
Validates, loads, and saves game states, maps, and unit templates. Handles serialization and file integrity checks.

**Why we need it:**  
Allows persistence of game progress, map creation, and unit definitions across sessions.

---

### 11) Map Creator Class (WPF)

**What it does:**  
Provides an editor interface to create, edit, save, and load maps. Supports placing terrain, buildings, resizing maps.

**Why we need it:**  
Allows players or developers to build custom maps and modify existing ones visually.

---

### 12) Unit Editor Class (WPF)

**What it does:**  
Provides an interface for creating, editing, and saving unit templates. Allows editing stats and managing unit lists.

**Why we need it:**  
Makes unit customization easy and lets players or developers design balanced unit sets.

---

## Project Windows (7 WPF Windows)

_(They would look like this design: <https://www.figma.com/design/kvMlTds0la1U1TEycerYF1/Untitled?node-id=0-1&t=Gw9EaZPs6m41ywBb-1>)_

### 1) Main Menu Window

Provides the entry interface of the application. Lets the player:

- Start a new game
- Continue a previous game
- Open editors
- Access settings

**Navigation:**

- **New Game** → Game VS AI or PvP → opens new window with Game VS AI or Player
- **Continue** → Choose a Save or Load Last game → loads last saved game
- **Unit edit** → opens Unit Edit window
- **Map editor** → opens Map Edit window

---

### 2) Select Game Mode Window

Allows the player to choose the game mode (e.g., Contest, Kill Commander) before starting a new game.

---

### 3) Settings Window

Contains adjustable game preferences such as audio, display settings, controls, and general gameplay options.

---

### 4) Load Save Window

Displays available saved games and allows selecting, previewing, and loading a previously saved game state.

---

### 5) Game Window

The main gameplay screen. Shows:

- The map
- Units
- UI panels
- Turn controls
- Interactive elements used to play the game

---

### 6) Map Editor

A tool window for creating and editing maps. Lets users:

- Place tiles
- Place buildings
- Resize maps
- Save or load map files

---

### 7) Unit Editor

Allows editing or creating unit templates. Players can:

- Modify stats
- Modify abilities
- Save custom unit configurations

