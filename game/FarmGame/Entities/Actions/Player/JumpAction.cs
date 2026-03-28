using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FarmGame.Core;

namespace FarmGame.Entities.Actions.Player;

public class JumpAction : IPlayerAction
{
    private float _jumpProgress;
    private KeyboardState _previousKeyboard;

    public bool IsActive { get; private set; }
    public float Offset { get; private set; }

    public void Update(float deltaTime, KeyboardState keyboard)
    {
        if (!IsActive && keyboard.IsKeyDown(Keys.Space) && _previousKeyboard.IsKeyUp(Keys.Space))
        {
            IsActive = true;
            _jumpProgress = 0f;
        }

        if (IsActive)
        {
            _jumpProgress += deltaTime / GameConstants.PlayerJumpDuration;
            if (_jumpProgress >= 1f)
            {
                Reset();
            }
            else
            {
                float p = _jumpProgress;
                Offset = -GameConstants.PlayerJumpHeight * 4f * p * (1f - p);
            }
        }

        _previousKeyboard = keyboard;
    }

    public void Reset()
    {
        _jumpProgress = 0f;
        Offset = 0f;
        IsActive = false;
    }

    public void Draw(ActionDrawContext context)
    {
        if (!IsActive) return;

        int pad = GameConstants.PlayerBodyPadding;
        int bodyW = GameConstants.TileSize - pad * 2;
        int bodyH = GameConstants.TileSize - pad * 2;
        int baseX = (int)context.PixelPosition.X + pad;
        int baseY = (int)context.PixelPosition.Y + pad;

        float shadowScale = 1f + Offset / GameConstants.PlayerJumpHeight * 0.3f;
        int shadowW = (int)(bodyW * shadowScale);
        int shadowH = (int)(bodyH * 0.3f * shadowScale);
        int shadowX = baseX + (bodyW - shadowW) / 2;
        int shadowY = baseY + bodyH - shadowH;
        context.SpriteBatch.Draw(context.Pixel, new Rectangle(shadowX, shadowY, shadowW, shadowH), Color.Black * 0.3f);
    }
}
