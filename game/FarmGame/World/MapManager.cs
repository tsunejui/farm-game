// =============================================================================
// MapManager.cs — Map loading and management
//
// Handles loading maps from definitions, building GameMap instances,
// and managing map transitions (teleport).
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Core.Managers;
using FarmGame.Data;
using FarmGame.Entities;
using FarmGame.Models;

namespace FarmGame.World;

public class MapLoadResult
{
    public GameMap Map { get; init; }
    public Player Player { get; init; }
    public Camera2D Camera { get; init; }
    public string MapName { get; init; }
}

public class MapManager
{
    private DataRegistry _registry;
    private Func<string, Texture2D> _loadTexture;
    private GraphicsDevice _graphicsDevice;
    private string _contentDir;

    public GameMap CurrentMap { get; private set; }
    public string CurrentMapId => CurrentMap?.MapId;

    public void Configure(DataRegistry registry, Func<string, Texture2D> loadTexture,
        GraphicsDevice graphicsDevice, string contentDir)
    {
        _registry = registry;
        _loadTexture = loadTexture;
        _graphicsDevice = graphicsDevice;
        _contentDir = contentDir;
    }

    /// <summary>
    /// Load a map and create player + camera from saved state or defaults.
    /// </summary>
    public MapLoadResult LoadMap(PlayerState savedState)
    {
        var mapId = savedState?.CurrentMap ?? GameConstants.StartMap;
        var mapDef = _registry.Maps[mapId];
        var map = MapBuilder.Build(mapDef, _registry, _loadTexture, _graphicsDevice, _contentDir);

        var config = mapDef.Config;
        Point playerStart;
        Direction facingDirection;

        if (savedState != null)
        {
            playerStart = new Point(savedState.PositionX, savedState.PositionY);
            Enum.TryParse(savedState.FacingDirection, out facingDirection);
        }
        else
        {
            playerStart = new Point(config.PlayerStart[0], config.PlayerStart[1]);
            facingDirection = Direction.Down;
        }

        var player = new Player(playerStart, map, facingDirection);
        var camera = new Camera2D(_graphicsDevice);
        var mapName = LocaleManager.Get("maps", mapId, mapDef.Metadata.DisplayName ?? mapId);

        CurrentMap = map;

        return new MapLoadResult
        {
            Map = map,
            Player = player,
            Camera = camera,
            MapName = mapName,
        };
    }
}
