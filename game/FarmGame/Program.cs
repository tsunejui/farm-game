using Serilog;
using FarmGame.Core;
using FarmGame.Persistence;

var logDir = System.IO.Path.Combine(
    DatabaseManager.ResolveDatabaseDirectory("Farm Game"), "logs");
LogManager.Initialize(logDir);

try
{
    Log.Information("FarmGame starting");
    using var game = new FarmGame.Game1();
    game.Run();
}
catch (System.Exception ex)
{
    Log.Fatal(ex, "FarmGame terminated unexpectedly");
}
finally
{
    Log.Information("FarmGame exiting");
    LogManager.Shutdown();
}
