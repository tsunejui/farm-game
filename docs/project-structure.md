# Project Structure

## Overview

```
farm-game/
├── .mise.toml                  # Tool version management (dotnet, just)
├── justfile                    # Task runner commands
├── FarmGame.sln                # .NET solution file
├── SETUP.md                    # Project setup guide
├── README.md                   # Project overview
├── docs/
│   ├── project-structure.md    # This file
│   ├── developer-guide.md     # Developer guide (for programmers)
│   └── designer-guide.md      # Designer guide (for game designers)
└── FarmGame/                   # Main game project
    ├── FarmGame.csproj         # Project file (MonoGame 3.8, YamlDotNet, .NET 9)
    ├── Program.cs              # Entry point
    ├── Game1.cs                # Main game loop (orchestrator)
    ├── Core/
    │   ├── GameConstants.cs    # Shared constants (tile size, speed, screen size)
    │   └── GameState.cs        # Game state enum
    ├── World/
    │   ├── TerrainType.cs      # Terrain type enum (Grass, Dirt, Path, Sand)
    │   ├── ObjectType.cs       # Object type enum (Water, Rock, Fence, Tree)
    │   ├── TileMap.cs          # Two-layer tile map (terrain + objects)
    │   └── MapLoader.cs        # Loads map from YAML config
    ├── Entities/
    │   ├── Direction.cs        # Facing direction enum
    │   └── Player.cs           # Player state, movement, and rendering
    ├── Camera/
    │   └── Camera2D.cs         # Viewport camera that follows the player
    ├── Screens/
    │   ├── TitleScreen.cs      # Title menu screen
    │   └── PauseScreen.cs      # Pause overlay screen
    └── Content/
        ├── Content.mgcb        # MonoGame content pipeline config
        ├── DefaultFont.spritefont  # Default font asset
        └── Maps/               # YAML map config directory
            └── farm.yaml       # Default farm map definition
```

## Module Descriptions

### Core

Contains shared constants and state definitions.

- **GameConstants**: Defines tile size (32px), player movement speed, and screen resolution (800x600).
- **GameState**: Enum controlling game flow — `TitleScreen`, `Playing`, `Paused`.

### World

Handles the game world representation using a two-layer system.

- **TerrainType**: Enum for walkable ground tiles — Grass, Dirt, Path, Sand. Terrain is always passable.
- **ObjectType**: Enum for impassable objects placed on top of terrain — Water, Rock, Fence, Tree.
- **TileMap**: Stores the map as two 2D arrays: `TerrainType[,]` for the ground layer and `ObjectType?[,]` for the object layer. Provides tile queries (`GetTerrain`, `GetObject`, `IsPassable`) and renders both layers with viewport culling. Colors are defined per-map in YAML configs.
- **MapLoader**: Reads YAML map config files and constructs a `TileMap` instance. Returns the map and player start position.

### Entities

Contains game entities.

- **Direction**: Four-directional facing enum.
- **Player**: Manages grid-based position with smooth pixel interpolation (Lerp). Handles keyboard input (WASD / arrow keys), collision detection against the tile map, and renders as a colored rectangle with a directional indicator.

### Camera

- **Camera2D**: Computes a transformation matrix for `SpriteBatch` to scroll the world relative to the player. Clamps to map boundaries to prevent showing empty space beyond edges. Exposes a `VisibleArea` rectangle for tile rendering optimization.

### Screens

- **TitleScreen**: Main menu with "Start Game" and "Exit Game" options. Supports keyboard navigation.
- **PauseScreen**: Semi-transparent overlay with "Resume" and "Exit Game" options. Game world renders underneath.

### Content/Maps

YAML config files that define map parameters. Each file specifies:
- Map dimensions and player spawn position
- Terrain and object color palettes
- Terrain region placements (walkable ground)
- Object region placements (impassable blocks)

See the [Developer Guide](developer-guide.md#working-with-map-files) for the full YAML format reference.

### Game1 (Orchestrator)

The main game class wires all systems together:

1. **Initialize** — Sets screen resolution (800x600).
2. **LoadContent** — Creates a 1x1 white pixel texture (used for all colored rectangle rendering), initializes screen UIs.
3. **StartGame** — Loads map from YAML via `MapLoader`, spawns the player, and initializes the camera.
4. **Update** — Routes input and updates based on current `GameState`.
5. **Draw** — Clears to black, draws the tile map (terrain then objects), then the player, using the camera transform.

## Rendering Approach

All graphics currently use a single 1x1 white pixel `Texture2D`, tinted with different colors via `SpriteBatch.Draw`. Colors are defined in each map's YAML config. This placeholder approach allows focusing on game logic before introducing sprite assets.

| Layer | Examples | Passable |
|-------|----------|----------|
| Terrain | Grass, Dirt, Path, Sand | Always |
| Object | Water, Rock, Fence, Tree | Never |
| Player | Orange Red (28x28) | — |
