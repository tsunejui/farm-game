// =============================================================================
// UIController.cs — HUD, toast alerts, map transition, dialogue overlays
//
// Order: 300 (drawn above particles, below network overlay)
// Manages all screen-space UI elements.
// =============================================================================

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
    public bool IsMapTransitionActive { get; set; }
    public string MapTransitionName { get; set; } = "";
    public float MapTransitionTimer { get; set; }
    public bool IsPaused { get; set; }
}

public class UIRenderState
{
    public bool IsMapTransitionActive { get; set; }
    public string MapTransitionName { get; set; } = "";
    public float MapTransitionTimer { get; set; }
    public bool IsPaused { get; set; }
}

// ─── Controller ─────────────────────────────────────────────

public class UIController : BaseController<UILogicState, UIRenderState>,
    INotificationHandler<MapLoadedEvent>,
    INotificationHandler<DamageDealtEvent>
{
    public override string Name => "UI";
    public override int Order => 300;

    private readonly MapTransitionOverlay _mapTransition = new();
    private readonly ToastAlert _toast = new();

    public override void Subscribe(QueueManager queue) { }
    public override void LoadResource(GraphicsDevice graphicsDevice, string contentDir) { }

    public override void UpdateLogic(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mapTransition.Update(dt);
        _toast.Update(dt);
    }

    protected override void CopyState(UILogicState logic, UIRenderState render)
    {
        render.IsMapTransitionActive = _mapTransition.IsActive;
        render.IsPaused = logic.IsPaused;
    }

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _toast.Draw(spriteBatch);
        if (_mapTransition.IsActive)
            _mapTransition.Draw(spriteBatch);
        spriteBatch.End();
    }

    /// <summary>Start map transition overlay (called by WorldController after map load).</summary>
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
        // Damage numbers are handled by ParticleController
        return Task.CompletedTask;
    }

}
