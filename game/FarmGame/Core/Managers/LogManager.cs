using System.IO;
using Serilog;
using Serilog.Events;

namespace FarmGame.Core.Managers;

// =============================================================================
// LogManager.cs — Serilog logger initialization
//
// Functions:
//   - Initialize(logDirectory) : Configure Serilog with console and daily rolling file sinks.
//   - Shutdown()               : Flush and close the logger.
// =============================================================================
public static class LogManager
{
    private static string _logPath;

    public static void Initialize(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        _logPath = Path.Combine(logDirectory, "game-.log");

        // Start with Debug level; Reconfigure() narrows it after config loads
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                _logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Rebuild the logger with the given minimum level.
    /// Called after config.yaml is parsed so the log_level setting takes effect.
    /// </summary>
    public static void Reconfigure(string level)
    {
        var minLevel = ParseLevel(level);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                _logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Log level set to {Level}", minLevel);
    }

    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }

    private static LogEventLevel ParseLevel(string level)
    {
        return level?.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };
    }
}
