// =============================================================================
// InitManager.cs — Fluent game initialization pipeline
//
// Usage:
//   _init
//     .WithConfig(contentDir)
//     .WithDatabase()
//     .WithLocale()
//     .WithControllers(controllerManager)
//     .Bootstrap();
// =============================================================================

using System.IO;
using Serilog;
using FarmGame.Bootstrap;

namespace FarmGame.Core;

public class InitManager
{
    public GameSession Session { get; private set; }
    public ConfigManager Config { get; private set; }

    private string _contentDir;
    private string _configsDir;
    private ControllerManager _controllerManager;

    public InitManager WithConfig(string contentDir)
    {
        _contentDir = contentDir;
        _configsDir = Path.Combine(Path.GetDirectoryName(contentDir), "Configs");

        // Initialize ConfigManager — loads all YAML configs
        Config = new ConfigManager();
        Config.Initialize(_configsDir);

        // Populate GameConstants from loaded game settings
        if (Config.GameSettings != null)
        {
            GameConstants.LoadFrom(Config.GameSettings.Data);
            LogManager.Reconfigure(Config.GameSettings.Data.LogLevel);
        }

        Log.Information("[Init] Config loaded via ConfigManager");
        return this;
    }

    public InitManager WithDatabase()
    {
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
            Session = new GameSession(dbResult.Database, dbResult.PlayerUuid, dbResult.SavedState);
        return this;
    }

    public InitManager WithLocale()
    {
        var dbResult = DatabaseInitializer.GetCachedResult();
        LocaleInitializer.Run(_contentDir, dbResult?.Success == true ? dbResult.Database : null);
        return this;
    }

    public InitManager WithControllers(ControllerManager controllerManager)
    {
        _controllerManager = controllerManager;
        return this;
    }

    public InitManager Bootstrap()
    {
        Log.Information("[Init] Bootstrap complete");
        return this;
    }
}
