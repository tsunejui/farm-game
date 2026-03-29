// =============================================================================
// ScreenManager.cs — Manages screen registration, lookup, and lifecycle
// =============================================================================

using System.Collections.Generic;
using FarmGame.Persistence.Repositories;
using FarmGame.Screens;
using Serilog;

namespace FarmGame.Core;

public class ScreenManager
{
    private readonly Dictionary<GameState, IScreen> _screens = new();
    private readonly string _contentDir;
    private readonly SettingRepository _settings;

    public ScreenManager(string contentDir, SettingRepository settings)
    {
        _contentDir = contentDir;
        _settings = settings;
    }

    public void Register(GameState state, IScreen screen)
    {
        _screens[state] = screen;
    }

    public bool TryGet(GameState state, out IScreen screen)
    {
        return _screens.TryGetValue(state, out screen);
    }

    public void ChangeLanguage(string language)
    {
        LocaleManager.Load(_contentDir, language);
        _settings?.Set("language", language);
        Log.Information("Language changed to: {Language}", language);
        RebuildAll();
    }

    public void RebuildAll()
    {
        foreach (var screen in _screens.Values)
            screen.Rebuild();
    }
}
