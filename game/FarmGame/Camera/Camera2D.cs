using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using FarmGame.Core;
using FarmGame.Entities;
using FarmGame.World;

namespace FarmGame.Camera;

public class Camera2D
{
    private readonly OrthographicCamera _camera;

    public Matrix TransformMatrix => _camera.GetViewMatrix();
    public Rectangle VisibleArea => _camera.BoundingRectangle.ToRectangle();

    public Camera2D(GraphicsDevice graphicsDevice)
    {
        _camera = new OrthographicCamera(graphicsDevice);
        _camera.Zoom = GameConstants.CameraZoom;
    }

    // Call once when the map is loaded or changed
    public void SetWorldBounds(GameMap map)
    {
        int mapPixelWidth = map.Width * GameConstants.TileSize;
        int mapPixelHeight = map.Height * GameConstants.TileSize;
        _camera.EnableWorldBounds(new Rectangle(0, 0, mapPixelWidth, mapPixelHeight));
    }

    public void Update(Player player)
    {
        var target = player.PixelPosition + new Vector2(GameConstants.TileSize / 2f);
        _camera.LookAt(target);
    }

    // Convert screen pixel coordinates to world coordinates
    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return Vector2.Transform(screenPos, Matrix.Invert(TransformMatrix));
    }
}
