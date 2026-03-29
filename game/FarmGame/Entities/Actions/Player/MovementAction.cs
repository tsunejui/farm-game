using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Core;
using FarmGame.World;

namespace FarmGame.Entities.Actions.Player;

public class MovementAction : IPlayerAction
{
    private readonly GameMap _tileMap;
    private Point _gridPosition;
    private Point _targetGridPosition;
    private float _moveProgress;

    public bool IsActive { get; private set; }
    public Direction FacingDirection { get; private set; }
    public Vector2 PixelPosition { get; private set; }
    public Point GridPosition => _gridPosition;

    public MovementAction(Point startPosition, GameMap tileMap, Direction facingDirection = Direction.Down)
    {
        _tileMap = tileMap;
        _gridPosition = startPosition;
        _targetGridPosition = startPosition;
        FacingDirection = facingDirection;
        IsActive = false;
        _moveProgress = 0f;
        PixelPosition = new Vector2(
            _gridPosition.X * GameConstants.TileSize,
            _gridPosition.Y * GameConstants.TileSize);
    }

    public void Update(float deltaTime, KeyboardStateExtended keyboard)
    {
        if (IsActive)
        {
            _moveProgress += GameConstants.PlayerMoveSpeed * deltaTime;
            if (_moveProgress >= 1f)
            {
                // Snap to target, carry over excess progress
                float overflow = _moveProgress - 1f;
                _gridPosition = _targetGridPosition;
                IsActive = false;
                PixelPosition = new Vector2(
                    _gridPosition.X * GameConstants.TileSize,
                    _gridPosition.Y * GameConstants.TileSize);

                // Immediately try next move so held keys chain without delay
                TryMove(keyboard);
                if (IsActive)
                    _moveProgress = overflow;
            }
            else
            {
                var from = new Vector2(
                    _gridPosition.X * GameConstants.TileSize,
                    _gridPosition.Y * GameConstants.TileSize);
                var to = new Vector2(
                    _targetGridPosition.X * GameConstants.TileSize,
                    _targetGridPosition.Y * GameConstants.TileSize);
                PixelPosition = Vector2.Lerp(from, to, _moveProgress);
            }
        }

        if (!IsActive)
        {
            TryMove(keyboard);
        }
    }

    public void Reset()
    {
        IsActive = false;
        _moveProgress = 0f;
        _targetGridPosition = _gridPosition;
        PixelPosition = new Vector2(
            _gridPosition.X * GameConstants.TileSize,
            _gridPosition.Y * GameConstants.TileSize);
    }

    public void Draw(ActionDrawContext context)
    {
        // Movement has no visual effect of its own
    }

    private void TryMove(KeyboardStateExtended keyboard)
    {
        Point direction = Point.Zero;

        if (keyboard.IsKeyDown(Keys.Up))
        {
            direction = new Point(0, -1);
            FacingDirection = Direction.Up;
        }
        else if (keyboard.IsKeyDown(Keys.Down))
        {
            direction = new Point(0, 1);
            FacingDirection = Direction.Down;
        }
        else if (keyboard.IsKeyDown(Keys.Left))
        {
            direction = new Point(-1, 0);
            FacingDirection = Direction.Left;
        }
        else if (keyboard.IsKeyDown(Keys.Right))
        {
            direction = new Point(1, 0);
            FacingDirection = Direction.Right;
        }

        if (direction != Point.Zero)
        {
            var target = _gridPosition + direction;
            if (_tileMap.IsPassable(target.X, target.Y))
            {
                _targetGridPosition = target;
                IsActive = true;
                _moveProgress = 0f;
            }
        }
    }
}
