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

using Serilog;
using FarmGame.Bootstrap;

namespace FarmGame.Core;

public class InitManager
{
    public GameSession Session { get; private set; }

    private string _contentDir;
    private ControllerManager _controllerManager;

    public InitManager WithConfig(string contentDir)
    {
        _contentDir = contentDir;
        ConfigInitializer.Run(contentDir);
        return this;
    }

    public InitManager WithDatabase()
    {
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
            Session = new GameSession(dbResult);
        return this;
    }

    public InitManager WithLocale()
    {
        var dbResult = DatabaseInitializer.GetCachedResult();
        LocaleInitializer.Run(_contentDir, dbResult?.Success == true ? dbResult.Settings : null);
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
