using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Entities.Actions.Player;

public class AttackAction : IPlayerAction
{
    private float _attackProgress;

    public bool IsActive { get; private set; }
    public float Progress => _attackProgress;

    public void Update(float deltaTime, KeyboardStateExtended keyboard)
    {
        if (!IsActive && keyboard.WasKeyPressed(Keys.Z))
        {
            IsActive = true;
            _attackProgress = 0f;
        }

        if (IsActive)
        {
            _attackProgress += deltaTime / GameConstants.PlayerAttackDuration;
            if (_attackProgress >= 1f)
            {
                Reset();
            }
        }
    }

    public void Reset()
    {
        _attackProgress = 0f;
        IsActive = false;
    }

    public void Draw(ActionDrawContext context)
    {
        if (!IsActive) return;

        float alpha = 1f - _attackProgress;
        float scale = 0.5f + 0.5f * _attackProgress;

        int range = (int)(GameConstants.PlayerAttackRange * scale);
        int width = (int)(GameConstants.PlayerAttackWidth * scale);
        int centerX = (int)context.PixelPosition.X + GameConstants.TileSize / 2;
        int centerY = (int)context.PixelPosition.Y + GameConstants.TileSize / 2 + (int)context.YOffset;
        int halfTile = GameConstants.TileSize / 2;

        Rectangle effectRect = context.FacingDirection switch
        {
            Direction.Up => new Rectangle(centerX - width / 2, centerY - halfTile - range, width, range),
            Direction.Down => new Rectangle(centerX - width / 2, centerY + halfTile, width, range),
            Direction.Left => new Rectangle(centerX - halfTile - range, centerY - width / 2, range, width),
            Direction.Right => new Rectangle(centerX + halfTile, centerY - width / 2, range, width),
            _ => Rectangle.Empty,
        };

        context.SpriteBatch.FillRectangle(effectRect, GameConstants.PlayerAttackColor * alpha);
    }
}
