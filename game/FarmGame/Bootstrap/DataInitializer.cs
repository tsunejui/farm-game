using Serilog;
using FarmGame.Data;

namespace FarmGame.Bootstrap;

public static class DataInitializer
{
    public static DataRegistry Run(string contentDir)
    {
        var registry = DataRegistry.LoadAll(contentDir);

        Log.Information("[Init] Data registry loaded");

        return registry;
    }
}
