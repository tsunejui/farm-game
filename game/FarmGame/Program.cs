using System;
using Serilog;
using FarmGame.Core;
using FarmGame.Persistence;

// Load .env.local before anything else
EnvLoader.Load();

var logDir = Environment.GetEnvironmentVariable("LOG_DIR");
if (string.IsNullOrEmpty(logDir))
    logDir = System.IO.Path.Combine(
        DatabaseManager.ResolveDatabaseDirectory("Farm Game"), "logs");
LogManager.Initialize(logDir);

try
{
    Log.Information("FarmGame starting");
    using var game = new FarmGame.Game1();
    game.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FarmGame terminated unexpectedly");
}
finally
{
    Log.Information("FarmGame exiting");
    LogManager.Shutdown();
}
