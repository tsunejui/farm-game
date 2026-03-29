# Project Structure

## Overview

```
farm-game/
├── .mise.toml                  # Tool version management (dotnet, just, python, gum, litecli)
├── justfile                    # Task runner commands
├── FarmGame.sln                # .NET solution file
├── CLAUDE.md                   # Project context for Claude Code
├── scripts/
│   └── interactive.sh          # Interactive menu selector (gum)
├── tools/
│   ├── yaml_to_tmx.py         # YAML → TMX conversion script
│   └── requirements.txt        # Python dependencies
├── docs/
│   ├── project-structure.md    # This file
│   ├── developer-guide.md     # Developer guide (for programmers)
│   └── designer-guide.md      # Designer guide (for game designers)
└── game/
    └── FarmGame/               # Main game project
        ├── FarmGame.csproj     # Project file (MonoGame 3.8, YamlDotNet, sqlite-net-pcl, .NET 9)
        ├── Program.cs          # Entry point
        ├── Game1.cs            # Main game loop (orchestrator)
        ├── Core/
        │   ├── GameConstants.cs    # Shared constants (loaded from config.yaml)
        │   ├── GameState.cs        # Game state enum
        │   └── ColorHelper.cs      # Hex color code parser
        ├── Data/
        │   ├── DataRegistry.cs     # Loads all terrain, item, and map definitions
        │   ├── GameConfig.cs       # YAML config deserialization (screen, tile, player, game)
        │   ├── TerrainDefinition.cs # Terrain type definition (id, name, color)
        │   ├── ItemDefinition.cs    # Item definition (visuals, physics, logic)
        │   └── MapDefinition.cs     # Map definition (terrain placements, entity placements)
        ├── World/
        │   ├── GameMap.cs          # Runtime map (terrain grid, collision grid, entities)
        │   ├── MapBuilder.cs       # Builds GameMap from MapDefinition + DataRegistry
        │   └── EntityInstance.cs    # Runtime entity with position and properties
        ├── Entities/
        │   ├── Direction.cs        # Facing direction enum
        │   ├── Player.cs           # Player coordinator (actions + body rendering)
        │   └── Actions/
        │       ├── IPlayerAction.cs      # Common interface (IsActive, Update, Draw, Reset)
        │       ├── ActionDrawContext.cs   # Shared draw context (position, direction, offset)
        │       └── Player/
        │           ├── MovementAction.cs  # Grid movement with Lerp interpolation
        │           ├── JumpAction.cs      # Parabolic jump with ground shadow
        │           └── AttackAction.cs    # Directional slash effect
        ├── Persistence/
        │   ├── DatabaseBootstrapper.cs   # DB creation, table init, disk space check
        │   ├── DatabasePathResolver.cs   # Cross-platform path resolution (Win/macOS/Linux)
        │   ├── DatabaseResult.cs         # Result pattern for error handling
        │   ├── MigrationManager.cs       # Schema versioning and upgrades
        │   ├── Models/
        │   │   ├── PlayerState.cs        # Player state (JSON serialized, versioned)
        │   │   ├── PlayerStateRecord.cs  # ORM model for player_state table
        │   │   ├── Setting.cs            # ORM model for setting table (key-value)
        │   │   └── SchemaVersion.cs      # ORM model for schema_version table
        │   └── Repositories/
        │       ├── PlayerStateRepository.cs  # CRUD for player state
        │       └── SettingRepository.cs      # Get/Set for settings
        ├── Camera/
        │   └── Camera2D.cs         # Viewport camera that follows the player
        ├── Screens/
        │   ├── TitleScreen.cs      # Title menu screen (with DB error display)
        │   ├── PauseScreen.cs      # Pause overlay screen
        │   ├── SettingsScreen.cs   # Language and game settings
        │   └── HUD/
        │       ├── MapTransitionOverlay.cs  # Map name fade-in/out on map load
        │       └── ToastAlert.cs            # Event notification toast (bottom-left)
        └── Content/
            ├── config.yaml         # Game-wide configuration
            ├── Content.mgcb        # MonoGame content pipeline config
            ├── DefaultFont.spritefont  # Default font asset
            ├── Fonts/              # TTF/OTF fonts (NotoSansCJK for Chinese)
            ├── Locales/            # i18n JSON files (en/, zh-TW/)
            ├── Images/             # Generated pixel art PNGs
            ├── Terrains/           # Terrain type definitions (*.yaml)
            ├── Items/              # Item type definitions (*.yaml)
            └── Maps/               # Map definitions (*.yaml)
```

## Module Descriptions

### Core

Contains shared constants and state definitions.

- **GameConstants**: Static properties loaded from `config.yaml` at startup — tile size, screen resolution, player speed, jump/attack parameters, colors.
- **GameState**: Enum controlling game flow — `TitleScreen`, `Settings`, `Playing`, `Paused`.
- **ColorHelper**: Parses hex color codes (e.g., `#FF4500`) to MonoGame `Color`.
- **LocaleManager**: Loads JSON language packs from `Content/Locales/<lang>/`, provides string lookup with fallback to English.
- **FontManager**: Loads TTF/OTF fonts via FontStashSharp for Unicode (Chinese) text rendering. Also configures Myra's default font.

### Data

Data-driven content loading system using YAML definitions.

- **DataRegistry**: Scans `Content/Terrains/`, `Content/Items/`, and `Content/Maps/` directories, deserializes all YAML files into typed definitions, keyed by ID.
- **GameConfig**: Deserializes `Content/config.yaml` into strongly-typed config classes (ScreenConfig, TileConfig, PlayerConfig, GameStartConfig).
- **TerrainDefinition**: Terrain type with metadata (id, name, category) and visuals (hex color).
- **ItemDefinition**: Item with metadata, visuals (color, background), physics (size, collidable), and logic (action handler, drops).
- **MapDefinition**: Map with metadata, config (dimensions, player start), terrain placements (regions), and entity placements.

### World

Runtime game world representation.

- **GameMap**: Stores terrain as a string ID grid, collision as a boolean grid, and entities as a list of `EntityInstance`. Provides `IsPassable()` queries and renders visible tiles with viewport culling.
- **MapBuilder**: Constructs a `GameMap` from a `MapDefinition` and `DataRegistry`. Fills default terrain, applies region placements, places entities, and sets up the collision grid.
- **EntityInstance**: Runtime reference to an `ItemDefinition` with tile position and per-instance property overrides.

### Entities

Game entities and the action system.

- **Direction**: Four-directional facing enum (Up, Down, Left, Right).
- **Player**: Coordinator that owns all `IPlayerAction` instances and renders the player body (colored rectangle with directional indicator). Exposes `GridPosition`, `FacingDirection`, and `PixelPosition`.

#### Actions

- **IPlayerAction**: Interface that all player actions implement — `IsActive`, `Update(deltaTime, keyboard)`, `Draw(context)`, `Reset()`.
- **ActionDrawContext**: Readonly struct passed to each action's Draw, providing SpriteBatch, pixel texture, position, facing direction, and Y offset.
- **MovementAction**: Grid-based movement with smooth Lerp interpolation. Reads WASD/Arrow keys, checks `GameMap.IsPassable()` for collision.
- **JumpAction**: Spacebar-triggered parabolic vertical offset. Draws a ground shadow that scales with jump height.
- **AttackAction**: Z key-triggered directional slash effect. Renders a rectangle in the facing direction with scale-up and fade-out animation.

### Persistence

SQLite database layer using sqlite-net-pcl ORM.

- **DatabasePathResolver**: Determines the writable database path per platform — `%LOCALAPPDATA%` (Windows), `~/Library/Application Support/` (macOS), `$XDG_DATA_HOME` (Linux). Sanitizes game name for directory safety.
- **DatabaseBootstrapper**: Creates the database file, initializes tables via `CreateTable<T>()`, and checks disk space. Provides `CreateConnection()` for repositories.
- **DatabaseResult**: Result pattern (Success/Fail with ErrorKind and message) to avoid exceptions in the game loop.
- **MigrationManager**: Tracks schema version in `schema_version` table. Runs incremental migrations in transactions.

#### Models

- **PlayerState**: JSON-serializable player state with its own `version` field for forward migration. Contains position, facing direction, current map, play time, and UUID.
- **PlayerStateRecord**: SQLite-net ORM model for the `player_state` table.
- **Setting**: ORM model for the `setting` key-value table (stores player UUID, etc.).
- **SchemaVersion**: ORM model for the `schema_version` table.

#### Repositories

- **PlayerStateRepository**: Save/Load/Delete/Exists operations for player state, keyed by player UUID.
- **SettingRepository**: Get/Set operations for the setting key-value store.

### Camera

- **Camera2D**: Computes a transformation matrix for `SpriteBatch` to scroll the world relative to the player. Clamps to map boundaries to prevent showing empty space. Exposes `VisibleArea` for rendering optimization.

### Screens

- **TitleScreen**: Main menu with "Start Game", "Settings", and "Exit Game" options. Displays database error messages. All text localized via `LocaleManager`.
- **PauseScreen**: Semi-transparent overlay with "Resume" and "Exit Game" options. Game world renders underneath.
- **SettingsScreen**: Game settings page with language selection (English / 中文). Stores preference in SQLite `setting` table.

#### HUD

In-game overlay elements rendered on top of the gameplay scene in screen-space.

- **MapTransitionOverlay**: Shows map name with fade-in/hold/fade-out animation when entering a map. Uses FontStashSharp for Chinese text rendering.
- **ToastAlert**: Event notification toast at bottom-left. Supports stacking up to 5 messages with fade animations. Used for map entry, save events, etc.

### Game1 (Orchestrator)

The main game class wires all systems together:

1. **Initialize** — Loads `config.yaml`, sets screen resolution, initializes SQLite database, loads language from settings, initializes `LocaleManager`.
2. **LoadContent** — Initializes Myra UI, FontManager, all screens (Title, Pause, Settings), HUD (MapTransition, Toast), and loads all data via `DataRegistry`.
3. **StartGame** — Builds map via `MapBuilder`, spawns player, initializes camera, shows localized map transition and toast.
4. **Update** — Routes input and updates based on current `GameState` (TitleScreen, Settings, Playing, Paused).
5. **Draw** — Renders map, player, HUD overlays, and Myra UI screens.
6. **OnExiting** — Saves player state to database on game exit.
7. **ChangeLanguage** — Reloads locale, saves to DB, rebuilds all screen UIs.

## Rendering Approach

Graphics use MonoGame.Extended `FillRectangle` for colored rectangles and pixel art PNG textures for entities. Colors are defined in YAML definition files (terrains, items) and `config.yaml` (player). Myra handles all menu UI rendering. FontStashSharp renders Unicode text (Chinese) in HUD overlays.

## Database

SQLite database stored at platform-specific path (e.g., `~/Library/Application Support/Farm_Game/game.db` on macOS). Tables managed by sqlite-net-pcl ORM:

| Table | Purpose |
|-------|---------|
| `schema_version` | Tracks database schema version for migrations |
| `setting` | Key-value store (player UUID, etc.) |
| `player_state` | Player save data as JSON blob with UUID |
