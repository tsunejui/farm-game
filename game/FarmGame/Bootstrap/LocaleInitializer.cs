using Serilog;
using FarmGame.Core;
using FarmGame.Persistence.Repositories;

namespace FarmGame.Bootstrap;

public static class LocaleInitializer
{
    public static void Run(string contentDir, SettingRepository settings)
    {
        var language = settings?.Get("language", GameConstants.DefaultLanguage)
            ?? GameConstants.DefaultLanguage;
        LocaleManager.Load(contentDir, language);

        Log.Information("[Init] Locale initialized: {Language}", language);
    }
}
