using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Core;
using FarmGame.World;

namespace FarmGame.Entities;

public class Player
{
    private readonly GameMap _tileMap;
    private Point _gridPosition;
    private Point _targetGridPosition;
    private float _moveProgress;
    private bool _isMoving;
    private Direction _facingDirection;

    public Vector2 PixelPosition { get; private set; }

    public Player(Point startPosition, GameMap tileMap)
    {
        _tileMap = tileMap;
        _gridPosition = startPosition;
        _targetGridPosition = startPosition;
        _facingDirection = Direction.Down;
        _isMoving = false;
        _moveProgress = 0f;
        PixelPosition = new Vector2(
            _gridPosition.X * GameConstants.TileSize,
            _gridPosition.Y * GameConstants.TileSize);
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isMoving)
        {
            _moveProgress += GameConstants.PlayerMoveSpeed * deltaTime;
            if (_moveProgress >= 1f)
            {
                _moveProgress = 0f;
                _gridPosition = _targetGridPosition;
                _isMoving = false;
                PixelPosition = new Vector2(
                    _gridPosition.X * GameConstants.TileSize,
                    _gridPosition.Y * GameConstants.TileSize);
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

        if (!_isMoving)
        {
            TryMove(Keyboard.GetState());
        }
    }

    private void TryMove(KeyboardState keyboard)
    {
        Point direction = Point.Zero;

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
        {
            direction = new Point(0, -1);
            _facingDirection = Direction.Up;
        }
        else if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
        {
            direction = new Point(0, 1);
            _facingDirection = Direction.Down;
        }
        else if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
        {
            direction = new Point(-1, 0);
            _facingDirection = Direction.Left;
        }
        else if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
        {
            direction = new Point(1, 0);
            _facingDirection = Direction.Right;
        }

        if (direction != Point.Zero)
        {
            var target = _gridPosition + direction;
            if (_tileMap.IsPassable(target.X, target.Y))
            {
                _targetGridPosition = target;
                _isMoving = true;
                _moveProgress = 0f;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int pad = GameConstants.PlayerBodyPadding;
        var bodyRect = new Rectangle(
            (int)PixelPosition.X + pad,
            (int)PixelPosition.Y + pad,
            GameConstants.TileSize - pad * 2,
            GameConstants.TileSize - pad * 2);
        spriteBatch.Draw(pixel, bodyRect, GameConstants.PlayerColor);

        var indicatorRect = GetDirectionIndicatorRect();
        spriteBatch.Draw(pixel, indicatorRect, Color.White);
    }

    private Rectangle GetDirectionIndicatorRect()
    {
        int sz = GameConstants.PlayerIndicatorSize;
        int pad = GameConstants.PlayerBodyPadding;
        int half = sz / 2;
        int cx = (int)PixelPosition.X + GameConstants.TileSize / 2 - half;
        int cy = (int)PixelPosition.Y + GameConstants.TileSize / 2 - half;

        return _facingDirection switch
        {
            Direction.Up => new Rectangle(cx, (int)PixelPosition.Y + pad, sz, sz),
            Direction.Down => new Rectangle(cx, (int)PixelPosition.Y + GameConstants.TileSize - pad - sz, sz, sz),
            Direction.Left => new Rectangle((int)PixelPosition.X + pad, cy, sz, sz),
            Direction.Right => new Rectangle((int)PixelPosition.X + GameConstants.TileSize - pad - sz, cy, sz, sz),
            _ => new Rectangle(cx, cy, sz, sz),
        };
    }
}
