// =============================================================================
// ScreenManager.cs — Manages screen registration, lookup, and lifecycle
// =============================================================================

using System.Collections.Generic;
using FarmGame.Screens;

namespace FarmGame.Core;

public class ScreenManager
{
    private readonly Dictionary<GameState, IScreen> _screens = new();

    public void Register(GameState state, IScreen screen)
    {
        _screens[state] = screen;
    }

    public bool TryGet(GameState state, out IScreen screen)
    {
        return _screens.TryGetValue(state, out screen);
    }

    public void RebuildAll()
    {
        foreach (var screen in _screens.Values)
            screen.Rebuild();
    }
}
