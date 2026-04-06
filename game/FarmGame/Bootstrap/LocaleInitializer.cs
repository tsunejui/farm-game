using Serilog;
using FarmGame.Core;
using FarmGame.Persistence;
using FarmGame.Persistence.Repositories;

namespace FarmGame.Bootstrap;

public static class LocaleInitializer
{
    public static void Run(string contentDir, DatabaseManager dbManager)
    {
        string language = GameConstants.DefaultLanguage;
        if (dbManager != null)
        {
            var settings = new SettingRepository(dbManager);
            language = settings.Get("language", GameConstants.DefaultLanguage);
        }

        LocaleManager.Load(contentDir, language);
        Log.Information("[Init] Locale initialized: {Language}", language);
    }
}
