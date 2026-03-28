using System.IO;
using Serilog;
using Serilog.Events;

namespace FarmGame.Core;

// =============================================================================
// LogManager.cs — Serilog logger initialization
//
// Functions:
//   - Initialize(logDirectory) : Configure Serilog with console and daily rolling file sinks.
//   - Shutdown()               : Flush and close the logger.
// =============================================================================
public static class LogManager
{
    public static void Initialize(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "game-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }
}
