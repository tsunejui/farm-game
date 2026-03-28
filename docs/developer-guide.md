# Developer Guide

This guide is for **programmers** working on the FarmGame codebase. For map and game content editing, see the [Designer Guide](designer-guide.md).

## Prerequisites

- [mise](https://mise.jdx.dev/) — tool version manager
- Git

## Environment Setup

1. Clone the repository:

```bash
git clone git@github.com:tsunejui/farm-game.git
cd farm-game
```

2. Install tools via mise (installs .NET SDK 9, just, litecli, etc.):

```bash
mise trust
mise install
```

3. Verify the installation:

```bash
dotnet --version   # 9.x.x
just --version     # x.x.x
```

## Available Commands

Run `just` to see all available commands:

| Command | Description |
|---------|-------------|
| `just start` | Run the game (generates `.env.local` with DB path) |
| `just build` | Compile the project |
| `just clean` | Remove build artifacts |
| `just env` | Generate `.env.local` without starting the game |
| `just release` | Build self-contained executables for all platforms |
| `just release-osx-arm64` | Build for macOS Apple Silicon |
| `just release-osx-x64` | Build for macOS Intel |
| `just release-win-x64` | Build for Windows x64 |
| `just release-linux-x64` | Build for Linux x64 |

## Development Workflow

### Running the Game

```bash
just start
```

This generates `.env.local` (with database path info), then compiles and launches the game window. Changes to code require restarting the game.

### Building

```bash
just build
```

Runs `dotnet build` against the solution file. Use this to check for compilation errors without launching the game.

### Inspecting the Database

```bash
mise exec -- litecli ~/Library/Application\ Support/Farm_Game/game.db
```

Uses [litecli](https://github.com/dbcli/litecli) (installed via mise) to interactively query the SQLite database. The DB path is also recorded in `.env.local` after running `just start`.

## Release Build

### Build All Platforms

```bash
just release
```

Outputs self-contained single-file executables to `dist/`:

```
dist/
├── osx-arm64/FarmGame        # macOS Apple Silicon
├── osx-x64/FarmGame           # macOS Intel
├── win-x64/FarmGame.exe       # Windows x64
└── linux-x64/FarmGame         # Linux x64
```

Each executable is fully self-contained — no .NET runtime installation is needed on the target machine.

## Game Architecture

See [Project Structure](project-structure.md) for a detailed overview of the codebase.

### Game Loop

The MonoGame framework drives a fixed game loop:

```
Initialize → LoadContent → [Update → Draw] (repeating)
```

### State Management

The game uses a `GameState` enum to control flow between screens:

```
TitleScreen ──(Start Game)──→ Playing ──(ESC)──→ Paused
                                ↑                   │
                                └──(Resume)─────────┘
                              (Exit Game)         (Exit Game)
                                  ↓                   ↓
                                Quit                Quit
                           (saves player state to DB)
```

- **TitleScreen**: Main menu with "Start Game" and "Exit Game" options. Shows DB errors if initialization failed.
- **Playing**: Active gameplay with player movement, jump, attack, map rendering, and camera tracking.
- **Paused**: Overlay menu with "Resume" and "Exit Game" options. Game world is still rendered underneath.

### Key Classes

| Class | File | Responsibility |
|-------|------|----------------|
| `Game1` | `Game1.cs` | Orchestrates game loop, state transitions, DB init, save on exit |
| `DataRegistry` | `Data/DataRegistry.cs` | Loads all terrain, item, and map definitions from YAML |
| `GameMap` | `World/GameMap.cs` | Runtime map with terrain grid, collision grid, entities |
| `MapBuilder` | `World/MapBuilder.cs` | Builds GameMap from MapDefinition + DataRegistry |
| `Player` | `Entities/Player.cs` | Coordinates actions (movement, jump, attack), renders body |
| `IPlayerAction` | `Entities/Actions/IPlayerAction.cs` | Interface for player actions |
| `DatabaseBootstrapper` | `Persistence/DatabaseBootstrapper.cs` | DB creation and table initialization |
| `PlayerStateRepository` | `Persistence/Repositories/PlayerStateRepository.cs` | Save/load player state |
| `Camera2D` | `Camera/Camera2D.cs` | Viewport transform, map boundary clamping |
| `GameConstants` | `Core/GameConstants.cs` | Shared config values (loaded from config.yaml) |

### Data System

The game uses a data-driven architecture. All content is defined in YAML files:

- **Terrain definitions** (`Content/Terrains/*.yaml`) — Ground tile types with color
- **Item definitions** (`Content/Items/*.yaml`) — Objects with visuals, physics, and logic
- **Map definitions** (`Content/Maps/*.yaml`) — Map layout, terrain placements, entity placements
- **Game config** (`Content/config.yaml`) — Screen size, tile size, player parameters

`DataRegistry.LoadAll()` scans these directories at startup and builds typed dictionaries keyed by ID.

### Action System

Player behaviors are implemented as separate `IPlayerAction` classes in `Entities/Actions/Player/`:

| Action | Key | Description |
|--------|-----|-------------|
| `MovementAction` | WASD / Arrows | Grid movement with smooth Lerp interpolation |
| `JumpAction` | Space | Parabolic vertical offset with ground shadow |
| `AttackAction` | Z | Directional slash effect with fade-out |

Each action implements `Update(deltaTime, keyboard)` and `Draw(context)`. The `Player` class iterates all actions in a loop — no switch/case needed.

### Persistence

SQLite database via sqlite-net-pcl ORM. Tables are defined as C# classes with attributes in `Persistence/Models/`:

| Table | Model | Purpose |
|-------|-------|---------|
| `schema_version` | `SchemaVersion` | Tracks DB schema version |
| `setting` | `Setting` | Key-value store (player UUID) |
| `player_state` | `PlayerStateRecord` | Player save data (JSON blob) |

Database path is platform-specific (see `DatabasePathResolver`):

| Platform | Path |
|----------|------|
| macOS | `~/Library/Application Support/Farm_Game/game.db` |
| Windows | `%LOCALAPPDATA%\Farm_Game\game.db` |
| Linux | `~/.local/share/Farm_Game/game.db` |

Player state is saved as a versioned JSON blob (`PlayerState`) with its own migration logic, independent from the DB schema version.

### Rendering

All visuals currently use a 1x1 white pixel `Texture2D` tinted with colors via `SpriteBatch.Draw`. This is a placeholder approach — replace with sprite sheets when art assets are available.

The rendering pipeline per frame:

1. `GraphicsDevice.Clear(Color.Black)`
2. `SpriteBatch.Begin` with camera transform matrix and `SamplerState.PointClamp`
3. Action effects (jump shadow, attack slash) via `IPlayerAction.Draw()`
4. `Player.DrawBody` — renders player rectangle
5. `Player.DrawDirectionIndicator` — renders facing indicator
6. `GameMap.Draw` — renders visible terrain and entity tiles
7. `SpriteBatch.End`

## Extending the Game

### Adding a New Player Action

1. Create a new class in `Entities/Actions/Player/` implementing `IPlayerAction`.
2. Add the action instance to the `_actions` array in `Player.cs`.
3. Add config parameters to `config.yaml`, `GameConfig.cs`, and `GameConstants.cs` if needed.

### Adding a New Terrain Type

1. Create a YAML file in `Content/Terrains/` (see existing files as templates).
2. Use the terrain ID in map YAML configs.

### Adding a New Item Type

1. Create a YAML file in `Content/Items/` (see existing files as templates).
2. Use the item ID in map YAML entity placements.

### Adding a New Screen

1. Create a new class in `Screens/` (follow `TitleScreen.cs` or `PauseScreen.cs` as templates).
2. Add a new value to the `GameState` enum in `Core/GameState.cs`.
3. Add `Update` and `Draw` cases for the new state in `Game1.cs`.

### Adding a DB Migration

1. Increment `MigrationManager.CurrentSchemaVersion`.
2. Add a new `case` in `ApplyMigration()` with the schema change.
3. If modifying `PlayerState` JSON fields, also update `PlayerState.CurrentVersion` and `PlayerState.Migrate()`.

## Controls

| Key | Context | Action |
|-----|---------|--------|
| WASD / Arrow Keys | Title Screen | Navigate menu |
| WASD / Arrow Keys | Playing | Move player |
| WASD / Arrow Keys | Paused | Navigate menu |
| Space | Playing | Jump |
| Z | Playing | Attack |
| Enter / Space | Title Screen | Confirm selection |
| Enter / Space | Paused | Confirm selection |
| ESC | Playing | Open pause menu |
| ESC | Paused | Resume game |
