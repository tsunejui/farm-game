// =============================================================================
// UIController.cs — HUD, toast alerts, map transition, in-game menu
//
// Order: 300 (drawn above particles, below network overlay)
// Manages all screen-space UI elements including the ESC game menu.
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Core;
using FarmGame.Queues;
using FarmGame.Queues.Events;
using FarmGame.Screens.Panels;

namespace FarmGame.Controllers;

// ─── State Definitions ──────────────────────────────────────

public class UILogicState
{
    public bool IsMenuOpen { get; set; }
}

public class UIRenderState
{
    public bool IsMenuOpen { get; set; }
}

// ─── Controller ─────────────────────────────────────────────

public class UIController : BaseController<UILogicState, UIRenderState>,
    INotificationHandler<MapLoadedEvent>,
    INotificationHandler<DamageDealtEvent>,
    INotificationHandler<TogglePauseEvent>
{
    public override string Name => "UI";
    public override int Order => 300;

    private readonly MapTransitionOverlay _mapTransition = new();
    private readonly ToastAlert _toast = new();
    private readonly GameMenuPanel _gameMenu = new();

    /// <summary>Fired when player selects "Leave Game" from the menu.</summary>
    public Action OnLeaveGame { get; set; }

    /// <summary>Fired when player selects "Settings" from the menu.</summary>
    public Action OnSettings { get; set; }

    public override void Subscribe(QueueManager queue) { }

    public override void LoadResource(GraphicsDevice graphicsDevice, string contentDir)
    {
        _gameMenu.OnLeaveGame = () => OnLeaveGame?.Invoke();
        _gameMenu.OnSettings = () => OnSettings?.Invoke();
    }

    public override void UpdateLogic(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mapTransition.Update(dt);
        _toast.Update(dt);
        _gameMenu.Update();

        LogicState.IsMenuOpen = _gameMenu.IsOpen;
    }

    protected override void CopyState(UILogicState logic, UIRenderState render)
    {
        render.IsMenuOpen = logic.IsMenuOpen;
    }

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _toast.Draw(spriteBatch);
        if (_mapTransition.IsActive)
            _mapTransition.Draw(spriteBatch);
        _gameMenu.Draw(spriteBatch);
        spriteBatch.End();
    }

    public void ShowMapTransition(string mapName)
    {
        _mapTransition.Start(mapName);
    }

    // ─── Event Handlers ─────────────────────────────────────

    public Task Handle(MapLoadedEvent notification, CancellationToken ct)
    {
        _mapTransition.Start(notification.MapName);
        return Task.CompletedTask;
    }

    public Task Handle(DamageDealtEvent notification, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task Handle(TogglePauseEvent notification, CancellationToken ct)
    {
        _gameMenu.Toggle();
        return Task.CompletedTask;
    }
}
