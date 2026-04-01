using System.Threading;
using System.Threading.Tasks;
using FontStashSharp;
using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using FarmGame.Core;
using FarmGame.Queues;

namespace FarmGame.Controllers;

public class NetworkLogicState
{
    public bool IsDisconnected { get; set; }
    public string DisconnectReason { get; set; } = string.Empty;
    public float ReconnectTimer { get; set; }
}

public class NetworkRenderState
{
    public bool IsDisconnected { get; set; }
    public string DisconnectReason { get; set; } = string.Empty;
    public float ReconnectTimer { get; set; }
}

/// <summary>
/// Monitors database connectivity and draws a reconnecting overlay when disconnected.
/// Subscribes to DatabaseDisconnectedEvent and DatabaseReconnectedEvent via MediatR.
/// </summary>
public class NetworkSystemController : BaseController<NetworkLogicState, NetworkRenderState>,
    INotificationHandler<DatabaseDisconnectedEvent>,
    INotificationHandler<DatabaseReconnectedEvent>
{
    private const int OverlayFontSize = 28;

    public override string Name => "NetworkSystem";
    public override int Order => 900;

    public Task Handle(DatabaseDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        LogicState.IsDisconnected = true;
        LogicState.DisconnectReason = notification.Reason;
        LogicState.ReconnectTimer = 0f;
        return Task.CompletedTask;
    }

    public Task Handle(DatabaseReconnectedEvent notification, CancellationToken cancellationToken)
    {
        LogicState.IsDisconnected = false;
        LogicState.DisconnectReason = string.Empty;
        LogicState.ReconnectTimer = 0f;
        return Task.CompletedTask;
    }

    public override void UpdateLogic(GameTime gameTime)
    {
        if (LogicState.IsDisconnected)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            LogicState.ReconnectTimer += dt;
        }
    }

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        if (!RenderState.IsDisconnected)
            return;

        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Semi-transparent black overlay
        spriteBatch.FillRectangle(
            new Rectangle(0, 0, screenW, screenH),
            Color.Black * 0.7f);

        var font = FontManager.GetFont(OverlayFontSize);
        if (font == null) return;

        // Animate dots: cycle through ".", "..", "..."
        int dotCount = ((int)RenderState.ReconnectTimer % 3) + 1;
        string dots = new string('.', dotCount);
        string text = $"Reconnecting{dots}";

        var textSize = font.MeasureString(text);
        var textPos = new Vector2(
            (screenW - textSize.X) / 2f,
            (screenH - textSize.Y) / 2f);

        font.DrawText(spriteBatch, text, textPos, Color.White);

        // Show reason below if available
        if (!string.IsNullOrEmpty(RenderState.DisconnectReason))
        {
            var smallFont = FontManager.GetFont(16);
            if (smallFont != null)
            {
                var reasonSize = smallFont.MeasureString(RenderState.DisconnectReason);
                var reasonPos = new Vector2(
                    (screenW - reasonSize.X) / 2f,
                    textPos.Y + textSize.Y + 12f);
                smallFont.DrawText(spriteBatch, RenderState.DisconnectReason, reasonPos, Color.Gray);
            }
        }

        spriteBatch.End();
    }

    protected override void CopyState(NetworkLogicState logic, NetworkRenderState render)
    {
        render.IsDisconnected = logic.IsDisconnected;
        render.DisconnectReason = logic.DisconnectReason;
        render.ReconnectTimer = logic.ReconnectTimer;
    }
}
