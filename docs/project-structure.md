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
│   └── project-structure.md    # This file
└── FarmGame/                   # Main game project
    ├── FarmGame.csproj         # Project file (MonoGame 3.8, .NET 9)
    ├── Program.cs              # Entry point
    ├── Game1.cs                # Main game loop (orchestrator)
    ├── Core/
    │   └── GameConstants.cs    # Shared constants (tile size, map dimensions, etc.)
    ├── World/
    │   ├── TileType.cs         # Tile type enum (Grass, Dirt, Water, Path)
    │   ├── TileMap.cs          # 2D tile grid data structure and rendering
    │   └── MapGenerator.cs     # Procedural map generation
    ├── Entities/
    │   ├── Direction.cs        # Facing direction enum
    │   └── Player.cs           # Player state, movement, and rendering
    ├── Camera/
    │   └── Camera2D.cs         # Viewport camera that follows the player
    └── Content/
        └── Content.mgcb        # MonoGame content pipeline config
```

## Module Descriptions

### Core

Contains shared constants used across the project. `GameConstants` defines tile size (32px), map dimensions (40x30), player movement speed, and screen resolution.

### World

Handles the game world representation.

- **TileType**: Enum defining terrain types, each mapped to a placeholder color.
- **TileMap**: Stores the map as a `TileType[,]` 2D array. Provides tile queries (`GetTile`, `IsPassable`) and renders only visible tiles using viewport culling.
- **MapGenerator**: Generates a default map layout with grass, dirt farm plots, water ponds, and path strips.

### Entities

Contains game entities.

- **Direction**: Four-directional facing enum.
- **Player**: Manages grid-based position with smooth pixel interpolation (Lerp). Handles keyboard input (WASD / arrow keys), collision detection against the tile map, and renders as a colored rectangle with a directional indicator.

### Camera

- **Camera2D**: Computes a transformation matrix for `SpriteBatch` to scroll the world relative to the player. Clamps to map boundaries to prevent showing empty space beyond edges. Exposes a `VisibleArea` rectangle for tile rendering optimization.

### Game1 (Orchestrator)

The main game class wires all systems together:

1. **Initialize** — Sets screen resolution (800x600).
2. **LoadContent** — Creates a 1x1 white pixel texture (used for all colored rectangle rendering), generates the map, spawns the player, and initializes the camera.
3. **Update** — Updates player movement, then camera position.
4. **Draw** — Clears to black, draws the tile map, then the player, using the camera transform.

## Rendering Approach

All graphics currently use a single 1x1 white pixel `Texture2D`, tinted with different colors via `SpriteBatch.Draw`. This placeholder approach allows focusing on game logic before introducing sprite assets.

| Tile Type | Color               |
|-----------|---------------------|
| Grass     | Forest Green        |
| Dirt      | Tan Brown           |
| Water     | Dodger Blue         |
| Path      | Light Tan           |
| Player    | Orange Red (28x28)  |
