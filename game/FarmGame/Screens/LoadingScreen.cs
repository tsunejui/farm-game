// =============================================================================
// LoadingScreen.cs — Reusable loading screen with configurable work action
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using FarmGame.Core;

namespace FarmGame.Screens;

public class LoadingScreen : IScreen
{
    private Desktop _desktop;
    private int _frameCount;
    private bool _workDone;
    private Action _workAction;
    private GameState _targetState;

    public void Configure(Action workAction, GameState targetState)
    {
        _workAction = workAction;
        _targetState = targetState;
    }

    public void Initialize() { BuildUI(); }
    public void Rebuild() { BuildUI(); }

    public void OnEnter(GameState fromState)
    {
        _frameCount = 0;
        _workDone = false;
    }

    private void BuildUI()
    {
        var label = UIHelper.CreateLabel(LocaleManager.Get("ui", "loading"), 28);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextColor = new Color(200, 220, 200);

        _desktop = new Desktop { Root = label };
    }

    public ScreenTransition Update(GameTime gameTime)
    {
        _frameCount++;

        if (_frameCount <= 1)
            return ScreenTransition.None;

        if (!_workDone)
        {
            _workAction?.Invoke();
            _workDone = true;
            return ScreenTransition.None;
        }

        return ScreenTransition.To(_targetState);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
