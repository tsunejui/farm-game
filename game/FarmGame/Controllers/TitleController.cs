// =============================================================================
// TitleController.cs — Wraps the Myra-based TitleScreen as a controller
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;
using FarmGame.Screens;

namespace FarmGame.Controllers;

public class TitleLogicState
{
    public ScreenTransition PendingTransition { get; set; }
}

public class TitleRenderState
{
    public ScreenTransition PendingTransition { get; set; }
}

public class TitleController : BaseController<TitleLogicState, TitleRenderState>
{
    public override string Name => "Title";
    public override int Order => 500;

    private readonly TitleScreen _screen;

    /// <summary>Fired when a screen transition is requested.</summary>
    public Action<ScreenTransition> OnTransition { get; set; }

    public TitleController(TitleScreen screen)
    {
        _screen = screen;
    }

    public void OnEnter(GameState from) => _screen.OnEnter(from);

    public override void UpdateLogic(GameTime gameTime)
    {
        var transition = _screen.Update(gameTime);
        LogicState.PendingTransition = transition != ScreenTransition.None ? transition : null;
    }

    protected override void CopyState(TitleLogicState logic, TitleRenderState render)
    {
        render.PendingTransition = logic.PendingTransition;
        // Consume transition after copy
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
