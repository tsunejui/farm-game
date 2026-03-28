using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Camera;
using FarmGame.Core;

namespace FarmGame.World;

public class TileMap
{
    private readonly TerrainType[,] _terrain;
    private readonly ObjectType?[,] _objects;
    private readonly Dictionary<TerrainType, Color> _terrainColors;
    private readonly Dictionary<ObjectType, Color> _objectColors;
    private readonly Dictionary<(int, int), Dictionary<string, object>> _tileProperties = new();

    public int Width { get; }
    public int Height { get; }

    public TileMap(
        int width,
        int height,
        Dictionary<TerrainType, Color> terrainColors,
        Dictionary<ObjectType, Color> objectColors)
    {
        Width = width;
        Height = height;
        _terrain = new TerrainType[width, height];
        _objects = new ObjectType?[width, height];
        _terrainColors = terrainColors;
        _objectColors = objectColors;
    }

    public void SetTerrain(int x, int y, TerrainType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _terrain[x, y] = type;
    }

    public void SetObject(int x, int y, ObjectType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _objects[x, y] = type;
    }

    public TerrainType GetTerrain(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return TerrainType.Grass;
        return _terrain[x, y];
    }

    public ObjectType? GetObject(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        return _objects[x, y];
    }

    public void SetTileProperty(int x, int y, string name, object value)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        var key = (x, y);
        if (!_tileProperties.ContainsKey(key))
            _tileProperties[key] = new Dictionary<string, object>();
        _tileProperties[key][name] = value;
    }

    public bool HasProperty(int x, int y, string name)
    {
        return _tileProperties.TryGetValue((x, y), out var props) && props.ContainsKey(name);
    }

    public object GetProperty(int x, int y, string name)
    {
        if (_tileProperties.TryGetValue((x, y), out var props) && props.TryGetValue(name, out var val))
            return val;
        return null;
    }

    public bool IsPassable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        return _objects[x, y] == null;
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

                // Draw terrain layer
                if (_terrainColors.TryGetValue(_terrain[x, y], out var terrainColor))
                    spriteBatch.Draw(pixel, rect, terrainColor);

                // Draw object layer on top
                if (_objects[x, y] is ObjectType obj && _objectColors.TryGetValue(obj, out var objColor))
                    spriteBatch.Draw(pixel, rect, objColor);
            }
        }
    }
}
