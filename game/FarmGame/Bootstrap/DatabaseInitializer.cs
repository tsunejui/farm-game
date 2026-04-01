using System;
using Serilog;
using FarmGame.Core;
using FarmGame.Persistence;
using FarmGame.Models;
using FarmGame.Persistence.Entities;
using FarmGame.Persistence.Repositories;

namespace FarmGame.Bootstrap;

public class DatabaseInitResult
{
    public DatabaseBootstrapper Database { get; init; }
    public SettingRepository Settings { get; init; }
    public PlayerStateRepository PlayerStateRepo { get; init; }
    public MapStateRepository MapStateRepo { get; init; }
    public string PlayerUuid { get; init; }
    public PlayerState SavedState { get; init; }
    public string Error { get; init; }
    public bool Success => Error == null;
}

public static class DatabaseInitializer
{
    public static DatabaseInitResult Run()
    {
        var dbDir = DatabasePathResolver.GetDatabaseDirectory(GameConstants.GameTitle);
        var dirResult = DatabasePathResolver.EnsureDirectoryExists(dbDir);
        if (!dirResult.Success)
            return new DatabaseInitResult { Error = dirResult.ErrorMessage };

        var permResult = DatabasePathResolver.CheckWritePermission(dbDir);
        if (!permResult.Success)
            return new DatabaseInitResult { Error = permResult.ErrorMessage };

        var dbPath = DatabasePathResolver.GetDatabasePath(GameConstants.GameTitle);
        Log.Information("[Init] Database path: {DbPath}", dbPath);

        var database = new DatabaseBootstrapper(dbPath);
        var initResult = database.Initialize();
        if (!initResult.Success)
        {
            Log.Error("[Init] Database initialization failed: {Error}", initResult.ErrorMessage);
            return new DatabaseInitResult { Error = initResult.ErrorMessage };
        }

        // Backup database
        DatabaseBackup.Backup(dbPath);
        Log.Information("[Init] Database backup completed");

        var settings = new SettingRepository(database);
        var playerStateRepo = new PlayerStateRepository(database);
        var mapStateRepo = new MapStateRepository(database);

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

        return new DatabaseInitResult
        {
            Database = database,
            Settings = settings,
            PlayerStateRepo = playerStateRepo,
            MapStateRepo = mapStateRepo,
            PlayerUuid = playerUuid,
            SavedState = savedState,
        };
    }
}
