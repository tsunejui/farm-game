using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Camera;
using FarmGame.Core;

namespace FarmGame.World;

public class TileMap
{
    private readonly TileType[,] _tiles;

    public int Width { get; }
    public int Height { get; }

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new TileType[width, height];
    }

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return TileType.Water;
        return _tiles[x, y];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _tiles[x, y] = type;
    }

    public bool IsPassable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        return _tiles[x, y] != TileType.Water;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Camera2D camera)
    {
        var visible = camera.VisibleArea;
        int startX = Math.Max(0, visible.Left / GameConstants.TileSize);
        int startY = Math.Max(0, visible.Top / GameConstants.TileSize);
        int endX = Math.Min(Width, visible.Right / GameConstants.TileSize + 1);
        int endY = Math.Min(Height, visible.Bottom / GameConstants.TileSize + 1);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                var rect = new Rectangle(
                    x * GameConstants.TileSize,
                    y * GameConstants.TileSize,
                    GameConstants.TileSize,
                    GameConstants.TileSize);
                spriteBatch.Draw(pixel, rect, GetTileColor(_tiles[x, y]));
            }
        }
    }

    private static Color GetTileColor(TileType type) => type switch
    {
        TileType.Grass => new Color(34, 139, 34),
        TileType.Dirt => new Color(139, 119, 101),
        TileType.Water => new Color(30, 144, 255),
        TileType.Path => new Color(210, 180, 140),
        _ => Color.Magenta,
    };
}
