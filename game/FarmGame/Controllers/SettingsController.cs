// =============================================================================
// SettingsController.cs — Wraps the Myra-based SettingsScreen as a controller
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;
using FarmGame.Screens;

namespace FarmGame.Controllers;

public class SettingsLogicState
{
    public ScreenTransition PendingTransition { get; set; }
}

public class SettingsRenderState
{
    public ScreenTransition PendingTransition { get; set; }
}

public class SettingsController : BaseController<SettingsLogicState, SettingsRenderState>
{
    public override string Name => "Settings";
    public override int Order => 500;

    private readonly SettingsScreen _screen;

    public Action<ScreenTransition> OnTransition { get; set; }

    public SettingsController(SettingsScreen screen)
    {
        _screen = screen;
    }

    public void OnEnter(GameState from) => _screen.OnEnter(from);

    public override void UpdateLogic(GameTime gameTime)
    {
        var transition = _screen.Update(gameTime);
        LogicState.PendingTransition = transition != ScreenTransition.None ? transition : null;
    }

    protected override void CopyState(SettingsLogicState logic, SettingsRenderState render)
    {
        render.PendingTransition = logic.PendingTransition;
        if (logic.PendingTransition != null)
        {
            OnTransition?.Invoke(logic.PendingTransition);
            logic.PendingTransition = null;
        }
    }

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        _screen.Draw(spriteBatch);
    }
}
