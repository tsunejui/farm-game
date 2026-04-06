using Serilog;
using FarmGame.Core;
using FarmGame.Persistence;

namespace FarmGame.Bootstrap;

public static class LocaleInitializer
{
    public static void Run(string contentDir, DatabaseManager db)
    {
        var language = db?.GetSetting("language", GameConstants.DefaultLanguage)
            ?? GameConstants.DefaultLanguage;
        LocaleManager.Load(contentDir, language);

        Log.Information("[Init] Locale initialized: {Language}", language);
    }
}
