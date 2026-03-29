// =============================================================================
// GameSession.cs — Manages game persistence (player state, settings, reset)
// =============================================================================

using System.IO;
using FarmGame.Bootstrap;
using FarmGame.Entities;
using FarmGame.Persistence;
using FarmGame.Persistence.Models;
using FarmGame.Persistence.Repositories;
using Serilog;

namespace FarmGame.Core;

public class GameSession
{
    private PlayerStateRepository _playerStateRepo;
    private SettingRepository _settings;
    private string _playerUuid;
    private PlayerState _initialSavedState;

    public bool HasSavedState => _initialSavedState != null || LoadPlayer() != null;

    public GameSession(DatabaseInitResult dbResult)
    {
        _playerStateRepo = dbResult.PlayerStateRepo;
        _settings = dbResult.Settings;
        _playerUuid = dbResult.PlayerUuid;
        _initialSavedState = dbResult.SavedState;
    }

    public PlayerState LoadPlayer()
    {
        if (_playerStateRepo == null) return _initialSavedState;
        var result = _playerStateRepo.Load(_playerUuid);
        return result.Success ? result.Value : _initialSavedState;
    }

    public void SavePlayer(Player player, string currentMap)
    {
        if (player == null || _playerStateRepo == null) return;

        var state = new PlayerState
        {
            Uuid = _playerUuid,
            PositionX = player.GridPosition.X,
            PositionY = player.GridPosition.Y,
            FacingDirection = player.FacingDirection.ToString(),
            CurrentMap = currentMap,
        };

        var result = _playerStateRepo.Save(_playerUuid, state, GameConstants.GameTitle);
        if (result.Success)
            Log.Information("Player state saved: pos=({X},{Y}), dir={Dir}",
                state.PositionX, state.PositionY, state.FacingDirection);
        else
            Log.Error("Failed to save player state: {Error}", result.ErrorMessage);
    }

    public void SaveSetting(string key, string value)
    {
        _settings?.Set(key, value);
    }

    public string GetSetting(string key, string defaultValue = null)
    {
        return _settings?.Get(key, defaultValue) ?? defaultValue;
    }

    public void ChangeLanguage(string language, string contentDir)
    {
        LocaleManager.Load(contentDir, language);
        SaveSetting("language", language);
        Log.Information("Language changed to: {Language}", language);
    }

    public void DeleteAndReset()
    {
        var dbPath = DatabasePathResolver.GetDatabasePath(GameConstants.GameTitle);
        var currentLanguage = LocaleManager.CurrentLanguage;

        DatabaseBackup.Backup(dbPath);
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        var freshDb = DatabaseInitializer.Run();
        if (freshDb.Success)
        {
            _playerStateRepo = freshDb.PlayerStateRepo;
            _settings = freshDb.Settings;
            _playerUuid = freshDb.PlayerUuid;
            _settings.Set("language", currentLanguage);
        }
        else
        {
            _playerStateRepo = null;
            _settings = null;
        }

        _initialSavedState = null;
        Log.Information("Character deleted, database re-initialized");
    }
}
