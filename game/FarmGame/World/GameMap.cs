using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
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
    private readonly Dictionary<(int, int), EntityInstance> _entityGrid = new();

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

    public void RegisterEntity(EntityInstance entity)
    {
        Entities.Add(entity);
        for (int x = entity.TileX; x < entity.TileX + entity.EffectiveWidth; x++)
            for (int y = entity.TileY; y < entity.TileY + entity.EffectiveHeight; y++)
                _entityGrid[(x, y)] = entity;
    }

    public EntityInstance GetEntityAt(int x, int y)
    {
        return _entityGrid.GetValueOrDefault((x, y));
    }

    // Update all entity states (damage-over-time ticks)
    public void Update(float deltaTime)
    {
        foreach (var entity in Entities)
            entity.State.Update(deltaTime);
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera)
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
                    spriteBatch.FillRectangle(rect, color);
            }
        }

        // Draw entities
        foreach (var entity in Entities)
        {
            var def = entity.Definition;
            int px = entity.TileX * GameConstants.TileSize;
            int py = entity.TileY * GameConstants.TileSize;
            int pw = entity.EffectiveWidth * GameConstants.TileSize;
            int ph = entity.EffectiveHeight * GameConstants.TileSize;

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
                var entityColor = Core.ColorHelper.FromHex(def.Visuals.Color);
                spriteBatch.FillRectangle(entityArea, entityColor);
            }

            // Death state: gray overlay at 50% opacity
            if (!entity.State.IsAlive)
            {
                spriteBatch.FillRectangle(entityArea, Color.DarkGray * 0.5f);
                continue;
            }

            // Damage flash: semi-transparent dark gray overlay while taking damage
            if (entity.State.IsTakingDamage)
            {
                spriteBatch.FillRectangle(entityArea,
                    Color.DarkGray * GameConstants.DamageFlashOpacity);
            }
        }
    }
}
