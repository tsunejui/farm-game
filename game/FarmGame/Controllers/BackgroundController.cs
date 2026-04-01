using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using FarmGame.Core;

namespace FarmGame.Controllers;

public class BackgroundLogicState
{
    public float ScrollOffset { get; set; }
    public Color TopColor { get; set; } = new Color(20, 40, 20);
    public Color BottomColor { get; set; } = new Color(10, 25, 10);
}

public class BackgroundRenderState
{
    public float ScrollOffset { get; set; }
    public Color TopColor { get; set; } = new Color(20, 40, 20);
    public Color BottomColor { get; set; } = new Color(10, 25, 10);
}

/// <summary>
/// Draws a full-screen gradient background beneath all other layers.
/// Always active, even when the game is paused.
/// </summary>
public class BackgroundController : BaseController<BackgroundLogicState, BackgroundRenderState>
{
    private const float ScrollSpeed = 8f; // pixels per second

    public override string Name => "Background";
    public override int Order => 0;

    public override void UpdateLogic(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        LogicState.ScrollOffset += ScrollSpeed * dt;

        // Wrap to avoid float overflow over long sessions
        if (LogicState.ScrollOffset > 10000f)
            LogicState.ScrollOffset -= 10000f;
    }

    public override void DrawRender(SpriteBatch spriteBatch)
    {
        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        const int bandCount = 16;
        int bandHeight = screenH / bandCount + 1;

        for (int i = 0; i < bandCount; i++)
        {
            float t = (float)i / (bandCount - 1);
            var color = Color.Lerp(RenderState.TopColor, RenderState.BottomColor, t);
            int y = i * (screenH / bandCount);
            spriteBatch.FillRectangle(
                new Rectangle(0, y, screenW, bandHeight),
                color);
        }

        spriteBatch.End();
    }

    protected override void CopyState(BackgroundLogicState logic, BackgroundRenderState render)
    {
        render.ScrollOffset = logic.ScrollOffset;
        render.TopColor = logic.TopColor;
        render.BottomColor = logic.BottomColor;
    }
}
