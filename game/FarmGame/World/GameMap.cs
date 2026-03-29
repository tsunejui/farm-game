using System;
using System.Collections.Generic;
using FontStashSharp;
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
    // Key: (itemId, state) where state is "alive" or "dead"
    private readonly Dictionary<(string, string), Texture2D> _backgroundTextures = new();
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

    public void SetBackgroundTexture(string itemId, string state, Texture2D texture)
    {
        _backgroundTextures[(itemId, state)] = texture;
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

            // Select texture by entity state: damaged → dead → alive fallback chain
            string texState;
            if (!entity.State.IsAlive)
                texState = "dead";
            else if (entity.State.IsTakingDamage)
                texState = "damaged";
            else
                texState = "alive";
            var bg = def.Visuals.Background;

            if (bg.Enabled)
            {
                // Try state-specific texture first, fall back to alive texture
                if (!_backgroundTextures.TryGetValue((entity.ItemId, texState), out var bgTex))
                    _backgroundTextures.TryGetValue((entity.ItemId, "alive"), out bgTex);

                if (bgTex != null)
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
            }

            // Draw foreground color on top (only for alive entities)
            if (entity.State.IsAlive && !string.IsNullOrEmpty(def.Visuals.Color))
            {
                var entityColor = Core.ColorHelper.FromHex(def.Visuals.Color);
                spriteBatch.FillRectangle(entityArea, entityColor);
            }

            // Dead entity without a dead texture: gray overlay fallback
            if (!entity.State.IsAlive && !_backgroundTextures.ContainsKey((entity.ItemId, "dead")))
            {
                spriteBatch.FillRectangle(entityArea, Color.DarkGray * 0.5f);
                continue;
            }

            if (!entity.State.IsAlive) continue;
        }
    }

    /// <summary>
    /// Draw entity name and HP bar for entities near the player.
    /// Called in world-space (inside camera transform).
    ///
    /// Positioning rules:
    ///   - Name: above entity top, centered horizontally.
    ///   - HP bar: below entity bottom, centered horizontally.
    ///   - Height cap: if entity half-height > player height * 3,
    ///     both name and HP bar are clamped to player_height * 3
    ///     above/below the entity center.
    /// </summary>
    public void DrawEntityInfo(SpriteBatch spriteBatch, Point playerGridPos)
    {
        int proximity = GameConstants.EntityInfoProximityTiles;
        int ts = GameConstants.TileSize;
        int playerHeight = ts; // player occupies 1 tile
        int maxHalfDisplay = playerHeight * 3;

        var font = FontManager.GetFont(GameConstants.EntityInfoFontSize);
        if (font == null) return;

        foreach (var entity in Entities)
        {
            // Skip entities with no HP (indestructible / terrain features)
            if (entity.Definition.Logic.MaxHealth <= 0) continue;

            // Proximity check (Chebyshev distance from player to nearest entity tile)
            int nearestX = Math.Clamp(playerGridPos.X, entity.TileX, entity.TileX + entity.EffectiveWidth - 1);
            int nearestY = Math.Clamp(playerGridPos.Y, entity.TileY, entity.TileY + entity.EffectiveHeight - 1);
            int dist = Math.Max(Math.Abs(playerGridPos.X - nearestX), Math.Abs(playerGridPos.Y - nearestY));
            if (dist > proximity) continue;

            // Entity pixel bounds
            int px = entity.TileX * ts;
            int py = entity.TileY * ts;
            int pw = entity.EffectiveWidth * ts;
            int ph = entity.EffectiveHeight * ts;
            int entityCenterX = px + pw / 2;
            int entityCenterY = py + ph / 2;
            int entityHalfH = ph / 2;

            // Determine whether to cap display positions
            bool capped = entityHalfH > maxHalfDisplay;
            int nameY;   // bottom edge of name text
            int hpBarY;  // top edge of HP bar

            if (capped)
            {
                nameY = entityCenterY - maxHalfDisplay;
                hpBarY = entityCenterY + maxHalfDisplay;
            }
            else
            {
                nameY = py; // entity top
                hpBarY = py + ph; // entity bottom
            }

            // --- Draw name above entity ---
            string name = LocaleManager.Get("items", entity.ItemId, entity.Definition.Metadata.DisplayName);
            var textSize = font.MeasureString(name);
            int nameOffsetY = GameConstants.EntityInfoNameOffsetY;
            float textX = entityCenterX - textSize.X / 2f;
            float textY = nameY - textSize.Y - nameOffsetY;

            // Text shadow
            font.DrawText(spriteBatch, name,
                new Vector2(textX + 1, textY + 1),
                Color.Black * 0.6f);
            font.DrawText(spriteBatch, name,
                new Vector2(textX, textY),
                Color.White);

            // --- Draw floating damage number above the name ---
            if (entity.State.ShowDamageNumber)
            {
                float progress = entity.State.DamageNumberProgress;
                float alpha = 1f - progress;           // fade out
                float floatUp = progress * 16f;        // drift 16px upward

                string dmgText = entity.State.LastDamageWasCrit
                    ? $"{entity.State.LastDamageAmount}!"
                    : entity.State.LastDamageAmount.ToString();
                Color dmgColor = entity.State.LastDamageWasCrit ? Color.Orange : Color.Red;

                var dmgSize = font.MeasureString(dmgText);
                float dmgX = entityCenterX - dmgSize.X / 2f;
                float dmgY = textY - dmgSize.Y - 2 - floatUp;

                font.DrawText(spriteBatch, dmgText,
                    new Vector2(dmgX + 1, dmgY + 1),
                    Color.Black * (alpha * 0.6f));
                font.DrawText(spriteBatch, dmgText,
                    new Vector2(dmgX, dmgY),
                    dmgColor * alpha);
            }

            // --- Draw HP bar below entity ---
            int barW = GameConstants.EntityInfoHpBarWidth;
            int barH = GameConstants.EntityInfoHpBarHeight;
            int barOffsetY = GameConstants.EntityInfoHpBarOffsetY;
            int barX = entityCenterX - barW / 2;
            int barY2 = hpBarY + barOffsetY;

            // Background (dark)
            spriteBatch.FillRectangle(new Rectangle(barX, barY2, barW, barH), Color.Black * 0.6f);

            // Foreground (green → red based on HP ratio)
            float hpRatio = (float)entity.State.CurrentHp / entity.State.MaxHp;
            hpRatio = Math.Clamp(hpRatio, 0f, 1f);
            int fillW = (int)(barW * hpRatio);
            Color barColor = hpRatio > 0.5f
                ? Color.Lerp(Color.Yellow, Color.LimeGreen, (hpRatio - 0.5f) * 2f)
                : Color.Lerp(Color.Red, Color.Yellow, hpRatio * 2f);

            if (fillW > 0)
                spriteBatch.FillRectangle(new Rectangle(barX, barY2, fillW, barH), barColor);

            // --- Draw HP text below the bar: "current / max" ---
            var hpFont = FontManager.GetFont(GameConstants.EntityInfoHpFontSize);
            if (hpFont != null)
            {
                string hpText = $"{entity.State.CurrentHp} / {entity.State.MaxHp}";
                var hpTextSize = hpFont.MeasureString(hpText);
                float hpTextX = entityCenterX - hpTextSize.X / 2f;
                float hpTextY = barY2 + barH + 1;

                hpFont.DrawText(spriteBatch, hpText,
                    new Vector2(hpTextX + 1, hpTextY + 1),
                    Color.Black * 0.5f);
                hpFont.DrawText(spriteBatch, hpText,
                    new Vector2(hpTextX, hpTextY),
                    Color.White * 0.9f);
            }
        }
    }
}
