using System;
using System.IO;
using Serilog;
using FarmGame.Core;
using FarmGame.Data;

namespace FarmGame.Bootstrap;

public static class ConfigInitializer
{
    public static string Run(string contentDir)
    {
        var configsDir = Path.Combine(Path.GetDirectoryName(contentDir), "Configs");
        var configPath = Path.Combine(configsDir, "config.yaml");
        var config = GameConfig.Load(configPath);
        GameConstants.LoadFrom(config);

        // Reconfigure logger with the level from config.yaml
        LogManager.Reconfigure(config.LogLevel);

        Log.Information("[Init] Config loaded: {Width}x{Height}, tile size {TileSize}",
            config.Screen.Width, config.Screen.Height, config.Tile.Size);

        return contentDir;
    }
}
