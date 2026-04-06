using System;
using Serilog;
using FarmGame.Core;
using FarmGame.Models;
using FarmGame.Persistence;
using FarmGame.Persistence.Repositories;

namespace FarmGame.Bootstrap;

public class DatabaseInitResult
{
    public DatabaseManager Database { get; init; }
    public string PlayerUuid { get; init; }
    public PlayerState SavedState { get; init; }
    public string Error { get; init; }
    public bool Success => Error == null;
}

public static class DatabaseInitializer
{
    private static DatabaseInitResult _cachedResult;

    public static DatabaseInitResult GetCachedResult() => _cachedResult;

    public static DatabaseInitResult Run()
    {
        var database = new DatabaseManager(GameConstants.GameTitle);
        Log.Information("[Init] Database path: {DbPath}", database.DatabasePath);

        var initResult = database.Initialize();
        if (!initResult.Success)
        {
            Log.Error("[Init] Database initialization failed: {Error}", initResult.ErrorMessage);
            return new DatabaseInitResult { Error = initResult.ErrorMessage };
        }

        var settings = new SettingRepository(database);
        var playerStateRepo = new PlayerStateRepository(database);

        var playerUuid = settings.Get("player_uuid");
        if (string.IsNullOrEmpty(playerUuid))
        {
            playerUuid = Guid.NewGuid().ToString();
            settings.Set("player_uuid", playerUuid);
            Log.Information("[Init] Created new player UUID: {Uuid}", playerUuid);
        }
        else
        {
            Log.Information("[Init] Loaded player UUID: {Uuid}", playerUuid);
        }

        // Load saved player state
        PlayerState savedState = null;
        var loadResult = playerStateRepo.Load(playerUuid);
        if (loadResult.Success)
        {
            savedState = loadResult.Value;
            Log.Information("[Init] Loaded player save: map={Map}, pos=({X},{Y})",
                savedState.CurrentMap, savedState.PositionX, savedState.PositionY);
        }
        else
        {
            Log.Information("[Init] No saved player state found");
        }

        Log.Information("[Init] Database initialized");

        _cachedResult = new DatabaseInitResult
        {
            Database = database,
            PlayerUuid = playerUuid,
            SavedState = savedState,
        };
        return _cachedResult;
    }
}
