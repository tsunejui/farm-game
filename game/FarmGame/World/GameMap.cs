using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Camera;
using FarmGame.Core;

namespace FarmGame.World;

public class GameMap
{
    public string MapId { get; }
    public int Width { get; }
    public int Height { get; }

    private readonly string[,] _terrain;
    private readonly bool[,] _collisionGrid;
    private readonly Dictionary<string, Color> _terrainColors;
    private readonly Dictionary<(int, int), Dictionary<string, object>> _tileProperties = new();
    private readonly Dictionary<string, Texture2D> _backgroundTextures = new();

    public List<EntityInstance> Entities { get; } = new();

    public GameMap(string mapId, int width, int height, Dictionary<string, Color> terrainColors)
    {
        MapId = mapId;
        Width = width;
        Height = height;
        _terrain = new string[width, height];
        _collisionGrid = new bool[width, height];
        _terrainColors = terrainColors;
    }

    public void SetBackgroundTexture(string itemId, Texture2D texture)
    {
        _backgroundTextures[itemId] = texture;
    }

    public void SetTerrain(int x, int y, string terrainId)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _terrain[x, y] = terrainId;
    }

    public string GetTerrain(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return "";
        return _terrain[x, y] ?? "";
    }

    public void SetCollision(int x, int y, bool blocked)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _collisionGrid[x, y] = blocked;
    }

    public bool IsPassable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        return !_collisionGrid[x, y];
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

    public EntityInstance GetEntityAt(int x, int y)
    {
        return Entities.FirstOrDefault(e =>
        {
            int ew = e.Definition.Physics.OccupyWidth;
            int eh = e.Definition.Physics.OccupyHeight;
            if (e.Properties.TryGetValue("fill_width", out var fw))
                ew = Convert.ToInt32(fw);
            if (e.Properties.TryGetValue("fill_height", out var fh))
                eh = Convert.ToInt32(fh);
            return x >= e.TileX && x < e.TileX + ew
                && y >= e.TileY && y < e.TileY + eh;
        });
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Camera2D camera)
    {
        var visible = camera.VisibleArea;
        int startX = Math.Max(0, visible.Left / GameConstants.TileSize);
        int startY = Math.Max(0, visible.Top / GameConstants.TileSize);
        int endX = Math.Min(Width, visible.Right / GameConstants.TileSize + 1);
        int endY = Math.Min(Height, visible.Bottom / GameConstants.TileSize + 1);

        // Draw terrain layer
        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                var rect = new Rectangle(
                    x * GameConstants.TileSize,
                    y * GameConstants.TileSize,
                    GameConstants.TileSize,
                    GameConstants.TileSize);

                var tid = _terrain[x, y];
                if (tid != null && _terrainColors.TryGetValue(tid, out var color))
                    spriteBatch.Draw(pixel, rect, color);
            }
        }

        // Draw entities
        foreach (var entity in Entities)
        {
            var def = entity.Definition;
            int ew = def.Physics.OccupyWidth;
            int eh = def.Physics.OccupyHeight;
            if (entity.Properties.TryGetValue("fill_width", out var fw))
                ew = Convert.ToInt32(fw);
            if (entity.Properties.TryGetValue("fill_height", out var fh))
                eh = Convert.ToInt32(fh);

            int px = entity.TileX * GameConstants.TileSize;
            int py = entity.TileY * GameConstants.TileSize;
            int pw = ew * GameConstants.TileSize;
            int ph = eh * GameConstants.TileSize;

            if (px + pw < visible.Left || px > visible.Right ||
                py + ph < visible.Top || py > visible.Bottom)
                continue;

            var entityArea = new Rectangle(px, py, pw, ph);

            // Draw background image if enabled
            var bg = def.Visuals.Background;
            if (bg.Enabled && _backgroundTextures.TryGetValue(entity.ItemId, out var bgTex))
            {
                int ox = bg.OffsetX;
                int oy = bg.OffsetY;

                switch (bg.DisplayMode)
                {
                    case "stretch":
                        spriteBatch.Draw(bgTex,
                            new Rectangle(px + ox, py + oy, pw, ph),
                            Color.White);
                        break;

                    case "tile":
                        for (int ty = py + oy; ty < py + ph; ty += bgTex.Height)
                            for (int tx = px + ox; tx < px + pw; tx += bgTex.Width)
                            {
                                int dw = Math.Min(bgTex.Width, px + pw - tx);
                                int dh = Math.Min(bgTex.Height, py + ph - ty);
                                spriteBatch.Draw(bgTex,
                                    new Rectangle(tx, ty, dw, dh),
                                    new Rectangle(0, 0, dw, dh),
                                    Color.White);
                            }
                        break;

                    case "center":
                        int cx = px + (pw - bgTex.Width) / 2 + ox;
                        int cy = py + (ph - bgTex.Height) / 2 + oy;
                        spriteBatch.Draw(bgTex,
                            new Vector2(cx, cy),
                            Color.White);
                        break;
                }
            }

            // Draw foreground color on top
            if (!string.IsNullOrEmpty(def.Visuals.Color))
            {
                var entityColor = ColorHelper.FromHex(def.Visuals.Color);
                spriteBatch.Draw(pixel, entityArea, entityColor);
            }
        }
    }
}
