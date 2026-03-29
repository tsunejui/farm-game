// =============================================================================
// GameplayInitializer.cs — Initializes gameplay state (map, player, camera)
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Data;
using FarmGame.Entities;
using FarmGame.Persistence.Models;
using FarmGame.World;

namespace FarmGame.Bootstrap;

public class GameplayInitResult
{
    public GameMap Map { get; init; }
    public Player Player { get; init; }
    public Camera2D Camera { get; init; }
    public string MapName { get; init; }
}

public static class GameplayInitializer
{
    public static GameplayInitResult Run(
        PlayerState savedState,
        DataRegistry registry,
        Func<string, Texture2D> loadTexture,
        GraphicsDevice graphicsDevice)
    {
        var mapId = savedState?.CurrentMap ?? GameConstants.StartMap;
        var mapDef = registry.Maps[mapId];
        var map = MapBuilder.Build(mapDef, registry, loadTexture);

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
        var camera = new Camera2D(graphicsDevice);
        var mapName = LocaleManager.Get("maps", mapId, mapDef.Metadata.DisplayName ?? mapId);

        return new GameplayInitResult
        {
            Map = map,
            Player = player,
            Camera = camera,
            MapName = mapName,
        };
    }
}
