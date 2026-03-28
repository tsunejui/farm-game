using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;
using FarmGame.Entities;
using FarmGame.World;

namespace FarmGame.Camera;

public class Camera2D
{
    private Vector2 _position;
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;

    public Rectangle VisibleArea { get; private set; }

    public Matrix TransformMatrix =>
        Matrix.CreateTranslation(
            -_position.X + _viewportWidth / 2f,
            -_position.Y + _viewportHeight / 2f,
            0);

    public Camera2D(Viewport viewport)
    {
        _viewportWidth = viewport.Width;
        _viewportHeight = viewport.Height;
    }

    public void Update(Player player, GameMap map)
    {
        _position = player.PixelPosition + new Vector2(GameConstants.TileSize / 2f);

        int mapPixelWidth = map.Width * GameConstants.TileSize;
        int mapPixelHeight = map.Height * GameConstants.TileSize;

        float halfW = _viewportWidth / 2f;
        float halfH = _viewportHeight / 2f;

        if (mapPixelWidth > _viewportWidth)
            _position.X = MathHelper.Clamp(_position.X, halfW, mapPixelWidth - halfW);
        else
            _position.X = mapPixelWidth / 2f;

        if (mapPixelHeight > _viewportHeight)
            _position.Y = MathHelper.Clamp(_position.Y, halfH, mapPixelHeight - halfH);
        else
            _position.Y = mapPixelHeight / 2f;

        VisibleArea = new Rectangle(
            (int)(_position.X - halfW),
            (int)(_position.Y - halfH),
            _viewportWidth,
            _viewportHeight);
    }
}
