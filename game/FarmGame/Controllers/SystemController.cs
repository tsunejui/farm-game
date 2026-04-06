// =============================================================================
// SystemController.cs — System state management
//
// Order: 10 (initialized first)
// Owns DatabaseManager, ConfigManager, QueueManager, LogManager.
// Creates GameSession for persistence.
// =============================================================================

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Core;
using FarmGame.Models;
using FarmGame.Persistence;
using FarmGame.Persistence.Repositories;

namespace FarmGame.Controllers;

public class SystemLogicState { }
public class SystemRenderState { }

public class SystemController : BaseController<SystemLogicState, SystemRenderState>
{
    public override string Name => "System";
    public override int Order => 10;

    /// <summary>Content directory path. Must be set before Initialize().</summary>
    public string ContentDir { get; set; }

    // ─── Managers ───────────────────────────────────────────

    public DatabaseManager Database { get; private set; }
    public ConfigManager Config { get; private set; }
    public QueueManager Queue { get; private set; }

    // ─── Session ────────────────────────────────────────────

    public GameSession Session { get; private set; }
    public PlayerState SavedState { get; private set; }

    // ─── Lifecycle ──────────────────────────────────────────

    public override void Initialize()
    {
        var configsDir = Path.Combine(Path.GetDirectoryName(ContentDir), "Configs");

        // 1. ConfigManager — load all YAML configs
        Config = new ConfigManager();
        Config.Initialize(configsDir);

        // 2. Apply game constants from config
        if (Config.GameSettings != null)
        {
            GameConstants.LoadFrom(Config.GameSettings.Data);
            LogManager.Reconfigure(Config.GameSettings.Data.LogLevel);
        }

        // 3. DatabaseManager — create, initialize, migrate, backup
        Database = new DatabaseManager(GameConstants.GameTitle);
        var dbResult = Database.Initialize();
        if (!dbResult.Success)
        {
            Log.Error("[SystemController] Database init failed: {Error}", dbResult.ErrorMessage);
            return;
        }

        // 4. Load player UUID and saved state
        var settings = new SettingRepository(Database);
        var playerStateRepo = new PlayerStateRepository(Database);

        var playerUuid = settings.Get("player_uuid");
        if (string.IsNullOrEmpty(playerUuid))
        {
            playerUuid = Guid.NewGuid().ToString();
            settings.Set("player_uuid", playerUuid);
            Log.Information("[SystemController] Created player UUID: {Uuid}", playerUuid);
        }

        PlayerState savedState = null;
        var loadResult = playerStateRepo.Load(playerUuid);
        if (loadResult.Success)
        {
            savedState = loadResult.Value;
            Log.Information("[SystemController] Loaded save: map={Map}, pos=({X},{Y})",
                savedState.CurrentMap, savedState.PositionX, savedState.PositionY);
        }
        SavedState = savedState;

        // 5. Create GameSession
        Session = new GameSession(Database, playerUuid, savedState);

        // 6. QueueManager
        Queue = new QueueManager();

        // 7. Locale
        string language = GameConstants.DefaultLanguage;
        var langSetting = settings.Get("language");
        if (!string.IsNullOrEmpty(langSetting))
            language = langSetting;
        LocaleManager.Load(ContentDir, language);

        Log.Information("[SystemController] Initialized");
    }

    public override void Load(ConfigManager config)
    {
        // QueueManager is already created; handlers will be registered externally
        // after all controllers are created (in ControllerManager.Load)
    }

    public override void Shutdown()
    {
        Queue?.Dispose();
        Database?.Backup();
        Log.Information("[SystemController] Shutdown complete");
    }

    protected override void CopyState(SystemLogicState logic, SystemRenderState render) { }
}
