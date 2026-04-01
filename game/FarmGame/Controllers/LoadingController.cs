// =============================================================================
// LoadingController.cs — Wraps LoadingScreen as a controller
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;
using FarmGame.Screens;
using FarmGame.Screens.HUD;

namespace FarmGame.Controllers;

public class LoadingLogicState
{
    public ScreenTransition PendingTransition { get; set; }
}

public class LoadingRenderState
{
    public ScreenTransition PendingTransition { get; set; }
}

public class LoadingController : BaseController<LoadingLogicState, LoadingRenderState>
{
    public override string Name => "Loading";
    public override int Order => 500;

    private readonly LoadingScreen _screen;

    public Action<ScreenTransition> OnTransition { get; set; }

    public LoadingController(LoadingScreen screen)
    {
        _screen = screen;
    }

    public void Configure(Action workAction, GameState targetState) =>
        _screen.Configure(workAction, targetState);

    public void OnEnter(GameState from) => _screen.OnEnter(from);

    public override void UpdateLogic(GameTime gameTime)
    {
        var transition = _screen.Update(gameTime);
        LogicState.PendingTransition = transition != ScreenTransition.None ? transition : null;
    }

    protected override void CopyState(LoadingLogicState logic, LoadingRenderState render)
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
