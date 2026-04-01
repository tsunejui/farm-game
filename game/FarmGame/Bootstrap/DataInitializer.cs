using Serilog;
using FarmGame.Data;

namespace FarmGame.Bootstrap;

public static class DataInitializer
{
    private static DataRegistry _cachedRegistry;

    public static DataRegistry Run(string contentDir)
    {
        var registry = DataRegistry.LoadAll(contentDir);
        _cachedRegistry = registry;

        Log.Information("[Init] Data registry loaded");

        return registry;
    }

    public static DataRegistry GetCachedRegistry() => _cachedRegistry;
}
