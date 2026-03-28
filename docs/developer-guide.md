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

2. Install tools via mise (installs .NET SDK 9 and just):

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
| `just start` | Run the game in development mode |
| `just build` | Compile the project |
| `just clean` | Remove build artifacts |
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

This compiles and launches the game window. Changes to code require restarting the game.

### Building

```bash
just build
```

Runs `dotnet build` against the solution file. Use this to check for compilation errors without launching the game.

### Cleaning

```bash
just clean
```

Removes `bin/` and `obj/` build artifacts.

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

### Build a Single Platform

```bash
just release-osx-arm64    # macOS Apple Silicon only
just release-win-x64      # Windows only
```

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
```

- **TitleScreen**: Main menu with "Start Game" and "Exit Game" options.
- **Playing**: Active gameplay with player movement, map rendering, and camera tracking.
- **Paused**: Overlay menu with "Resume" and "Exit Game" options. Game world is still rendered underneath.

### Key Classes

| Class | File | Responsibility |
|-------|------|----------------|
| `Game1` | `Game1.cs` | Orchestrates the game loop, manages state transitions |
| `TitleScreen` | `Screens/TitleScreen.cs` | Title menu UI and input |
| `PauseScreen` | `Screens/PauseScreen.cs` | Pause overlay UI and input |
| `Player` | `Entities/Player.cs` | Movement, collision, rendering |
| `TileMap` | `World/TileMap.cs` | Two-layer map (terrain + objects), tile queries, rendering |
| `MapLoader` | `World/MapLoader.cs` | Loads map data from YAML config files |
| `Camera2D` | `Camera/Camera2D.cs` | Viewport transform, map boundary clamping |
| `GameConstants` | `Core/GameConstants.cs` | Shared configuration values |

### Map System

The game uses a two-layer tile map system:

- **Terrain layer** — Ground tiles that are always walkable (Grass, Dirt, Path, Sand).
- **Object layer** — Tiles placed on top of terrain that block player movement (Water, Rock, Fence, Tree).

Map definitions are stored as YAML files in `game/FarmGame/Content/Maps/`. The `MapLoader` reads these files at runtime to construct the `TileMap`. See the [Designer Guide](designer-guide.md) for the YAML format reference.

### Rendering

All visuals currently use a 1x1 white pixel `Texture2D` tinted with colors via `SpriteBatch.Draw`. This is a placeholder approach — replace with sprite sheets when art assets are available.

The rendering pipeline per frame:

1. `GraphicsDevice.Clear(Color.Black)`
2. `SpriteBatch.Begin` with camera transform matrix and `SamplerState.PointClamp`
3. `TileMap.Draw` — renders terrain layer first, then object layer on top (only visible tiles)
4. `Player.Draw` — renders player rectangle with direction indicator
5. `SpriteBatch.End`

For the pause screen, a second `SpriteBatch.Begin/End` pass is used without the camera transform (screen-space) to draw the overlay.

## Extending the Game

### Adding a New Terrain Type

1. Add the value to the `TerrainType` enum in `World/TerrainType.cs`.
2. The designer can then use it in YAML map configs (color and placement are defined there).

### Adding a New Object Type

1. Add the value to the `ObjectType` enum in `World/ObjectType.cs`.
2. The designer can then use it in YAML map configs (color and placement are defined there).

### Switching the Active Map

In `Game1.cs`, change the map path in `StartGame()`:

```csharp
var (map, playerStart) = MapLoader.Load(
    Path.Combine(Content.RootDirectory, "Maps", "village.yaml"));
```

### Adding a New Screen

1. Create a new class in `Screens/` (follow `TitleScreen.cs` or `PauseScreen.cs` as templates).
2. Add a new value to the `GameState` enum in `Core/GameState.cs`.
3. Add `Update` and `Draw` cases for the new state in `Game1.cs`.

### Adding Content Assets

1. Place the asset file (sprite, font, sound) in `game/FarmGame/Content/`.
2. Register it in `game/FarmGame/Content/Content.mgcb` with the appropriate importer/processor.
3. Load it in `Game1.LoadContent` using `Content.Load<T>("AssetName")`.

## Controls

| Key | Context | Action |
|-----|---------|--------|
| WASD / Arrow Keys | Title Screen | Navigate menu |
| WASD / Arrow Keys | Playing | Move player |
| WASD / Arrow Keys | Paused | Navigate menu |
| Enter / Space | Title Screen | Confirm selection |
| Enter / Space | Paused | Confirm selection |
| ESC | Playing | Open pause menu |
| ESC | Paused | Resume game |
