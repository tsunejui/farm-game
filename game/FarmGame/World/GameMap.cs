using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Core.Managers;
using FarmGame.World.AI;

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
    // Key: (itemId, state) — static textures
    private readonly Dictionary<(string, string), Texture2D> _backgroundTextures = new();
    // Key: (itemId, state) — animated textures (GIF frames)
    private readonly Dictionary<(string, string), AnimatedTexture> _animatedTextures = new();
    private readonly Dictionary<string, Texture2D> _terrainBaseTextures = new();
    private readonly Dictionary<string, AnimatedTexture> _terrainAnimBaseTextures = new();
    private readonly Dictionary<(int, int), WorldObject> _objectGrid = new();

    // Terrain decorations: each tile may have one decoration index (-1 = none)
    // _decoAssignments[x,y] = index into _terrainDecoTextures for that terrain
    private int[,] _decoAssignments;
    // Key: terrain_id → list of (static texture OR animated texture)
    private readonly Dictionary<string, List<Texture2D>> _decoStaticTextures = new();
    private readonly Dictionary<string, List<AnimatedTexture>> _decoAnimTextures = new();

    public List<WorldObject> Objects { get; } = new();

    // Creature AI brains — keyed by the WorldObject they control
    private readonly Dictionary<WorldObject, CreatureBrain> _creatureBrains = new();

    // Player proxy for effects that interact with the player (not in Objects list)
    public WorldObject PlayerProxy { get; set; }

    // Pending interaction request from overlap detection (consumed by PlayingScreen)
    public Interactions.InteractionRequest PendingInteraction { get; set; }

    public GameMap(string mapId, int width, int height, Dictionary<string, Color> terrainColors)
    {
        MapId = mapId;
        Width = width;
        Height = height;
        _terrain = new string[width, height];
        _collisionGrid = new bool[width, height];
        _terrainColors = terrainColors;
    }

    public void SetTerrainBaseTexture(string terrainId, Texture2D texture)
    {
        _terrainBaseTextures[terrainId] = texture;
    }

    public void SetTerrainAnimBaseTexture(string terrainId, AnimatedTexture anim)
    {
        _terrainAnimBaseTextures[terrainId] = anim;
    }

    public void SetBackgroundTexture(string itemId, string state, Texture2D texture)
    {
        _backgroundTextures[(itemId, state)] = texture;
    }

    public void AddTerrainDecoration(string terrainId, Texture2D texture)
    {
        if (!_decoStaticTextures.ContainsKey(terrainId))
            _decoStaticTextures[terrainId] = new List<Texture2D>();
        _decoStaticTextures[terrainId].Add(texture);
    }

    public void AddTerrainAnimDecoration(string terrainId, AnimatedTexture anim)
    {
        if (!_decoAnimTextures.ContainsKey(terrainId))
            _decoAnimTextures[terrainId] = new List<AnimatedTexture>();
        _decoAnimTextures[terrainId].Add(anim);
    }

    // Assign decorations randomly to tiles based on coverage ratios
    public void AssignDecorations(Dictionary<string, List<float>> coverages)
    {
        _decoAssignments = new int[Width, Height];
        var rng = new Random(MapId.GetHashCode()); // deterministic per map

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _decoAssignments[x, y] = -1;
                var tid = _terrain[x, y];
                if (tid == null || !coverages.ContainsKey(tid)) continue;

                var rates = coverages[tid];
                for (int i = 0; i < rates.Count; i++)
                {
                    if (rng.NextDouble() < rates[i])
                    {
                        _decoAssignments[x, y] = i;
                        break; // only one decoration per tile
                    }
                }
            }
        }
    }

    public void SetAnimatedTexture(string itemId, string state, AnimatedTexture anim)
    {
        _animatedTextures[(itemId, state)] = anim;
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

    public void RegisterObject(WorldObject obj)
    {
        Objects.Add(obj);
        for (int x = obj.TileX; x < obj.TileX + obj.EffectiveWidth; x++)
            for (int y = obj.TileY; y < obj.TileY + obj.EffectiveHeight; y++)
                _objectGrid[(x, y)] = obj;

        // Auto-create AI brain for creatures with move_speed > 0
        if (obj.Category == ObjectCategory.Creature && obj.Definition.Logic.MoveSpeed > 0)
            _creatureBrains[obj] = CreatureBrainFactory.Create(obj);
    }

    public WorldObject GetObjectAt(int x, int y)
    {
        return _objectGrid.GetValueOrDefault((x, y));
    }

    /// <summary>
    /// Move an object from its current tile to a new tile.
    /// Updates the spatial index and collision grid.
    /// Returns false if the target position is blocked.
    /// </summary>
    public bool MoveObject(WorldObject obj, int newX, int newY)
    {
        // Check all target tiles are passable (and not occupied by another object)
        for (int x = newX; x < newX + obj.EffectiveWidth; x++)
            for (int y = newY; y < newY + obj.EffectiveHeight; y++)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
                var occupant = _objectGrid.GetValueOrDefault((x, y));
                if (occupant != null && occupant != obj) return false;
            }

        // Clear old grid entries and collision
        for (int x = obj.TileX; x < obj.TileX + obj.EffectiveWidth; x++)
            for (int y = obj.TileY; y < obj.TileY + obj.EffectiveHeight; y++)
            {
                _objectGrid.Remove((x, y));
                if (obj.Definition.Physics.IsCollidable)
                    _collisionGrid[x, y] = false;
            }

        // Update position
        obj.TileX = newX;
        obj.TileY = newY;

        // Set new grid entries and collision
        for (int x = newX; x < newX + obj.EffectiveWidth; x++)
            for (int y = newY; y < newY + obj.EffectiveHeight; y++)
            {
                _objectGrid[(x, y)] = obj;
                if (obj.Definition.Physics.IsCollidable)
                    _collisionGrid[x, y] = true;
            }

        return true;
    }

    // Update all object states, effects, event queues, and animated textures
    public void Update(float deltaTime)
    {
        foreach (var obj in Objects)
        {
            obj.State.Update(deltaTime);
            obj.UpdateEffects(deltaTime);
            obj.UpdateEvents(this, deltaTime);
        }

        // Tick player proxy events (player is an object but not in Objects list)
        if (PlayerProxy != null)
        {
            PlayerProxy.State.Update(deltaTime);
            PlayerProxy.UpdateEffects(deltaTime);
            PlayerProxy.UpdateEvents(this, deltaTime);
        }

        // Check interaction overlaps with player
        if (PlayerProxy != null && PendingInteraction == null)
        {
            foreach (var obj in Objects)
            {
                var request = obj.UpdateOverlap(PlayerProxy, deltaTime);
                if (request != null)
                {
                    PendingInteraction = request;
                    break;
                }
            }
        }

        // Update creature AI brains
        foreach (var (obj, brain) in _creatureBrains)
        {
            if (obj.State.IsAlive)
                brain.Update(this, deltaTime);
        }

        foreach (var anim in _animatedTextures.Values)
            anim.Update(deltaTime);

        // Tick animated terrain base textures
        foreach (var anim in _terrainAnimBaseTextures.Values)
            anim.Update(deltaTime);

        // Tick decoration animations
        foreach (var animList in _decoAnimTextures.Values)
            foreach (var anim in animList)
                anim.Update(deltaTime);
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
                if (tid != null)
                {
                    if (_terrainAnimBaseTextures.TryGetValue(tid, out var animBase))
                        spriteBatch.Draw(animBase.CurrentFrame, rect, Color.White);
                    else if (_terrainBaseTextures.TryGetValue(tid, out var baseTex))
                        spriteBatch.Draw(baseTex, rect, Color.White);
                    else if (_terrainColors.TryGetValue(tid, out var color))
                        spriteBatch.FillRectangle(rect, color);
                }

                // Draw decoration overlay if assigned
                if (_decoAssignments != null && _decoAssignments[x, y] >= 0 && tid != null)
                {
                    int decoIdx = _decoAssignments[x, y];
                    Texture2D decoTex = null;

                    // Try animated first
                    if (_decoAnimTextures.TryGetValue(tid, out var animList) && decoIdx < animList.Count)
                        decoTex = animList[decoIdx].CurrentFrame;

                    // Try static (offset by anim count)
                    if (decoTex == null && _decoStaticTextures.TryGetValue(tid, out var staticList))
                    {
                        int animCount = animList?.Count ?? 0;
                        int staticIdx = decoIdx - animCount;
                        if (staticIdx >= 0 && staticIdx < staticList.Count)
                            decoTex = staticList[staticIdx];
                    }

                    if (decoTex != null)
                        spriteBatch.Draw(decoTex, rect, Color.White);
                }
            }
        }

        // Draw entities
        foreach (var obj in Objects)
        {
            var def = obj.Definition;
            int px = obj.TileX * GameConstants.TileSize;
            int py = obj.TileY * GameConstants.TileSize;
            int pw = obj.EffectiveWidth * GameConstants.TileSize;
            int ph = obj.EffectiveHeight * GameConstants.TileSize;

            if (px + pw < visible.Left || px > visible.Right ||
                py + ph < visible.Top || py > visible.Bottom)
                continue;

            // Apply bounce offset (parabolic arc while being knocked back alive)
            int bounceY = (int)obj.State.BounceOffsetY;
            var entityArea = new Rectangle(px, py + bounceY, pw, ph);

            // Select texture by entity state: damaged → dead → alive fallback chain
            string texState;
            if (!obj.State.IsAlive)
                texState = "dead";
            else if (obj.State.IsTakingDamage)
                texState = "damaged";
            else
                texState = "alive";
            var bg = def.Visuals.Background;

            if (bg.Enabled)
            {
                // Try animated texture first, then static, then alive fallback
                Texture2D bgTex = null;
                if (_animatedTextures.TryGetValue((obj.ItemId, texState), out var anim))
                    bgTex = anim.CurrentFrame;
                else if (_animatedTextures.TryGetValue((obj.ItemId, "alive"), out var animFallback) && texState == "alive")
                    bgTex = animFallback.CurrentFrame;

                if (bgTex == null && !_backgroundTextures.TryGetValue((obj.ItemId, texState), out bgTex))
                    _backgroundTextures.TryGetValue((obj.ItemId, "alive"), out bgTex);

                if (bgTex != null)
                {
                    int ox = bg.OffsetX;
                    int oy = bg.OffsetY;

                    int drawY = py + bounceY;
                    switch (bg.DisplayMode)
                    {
                        case "stretch":
                            spriteBatch.Draw(bgTex,
                                new Rectangle(px + ox, drawY + oy, pw, ph),
                                Color.White);
                            break;

                        case "tile":
                            for (int ty2 = drawY + oy; ty2 < drawY + ph; ty2 += bgTex.Height)
                                for (int tx = px + ox; tx < px + pw; tx += bgTex.Width)
                                {
                                    int dw = Math.Min(bgTex.Width, px + pw - tx);
                                    int dh = Math.Min(bgTex.Height, drawY + ph - ty2);
                                    spriteBatch.Draw(bgTex,
                                        new Rectangle(tx, ty2, dw, dh),
                                        new Rectangle(0, 0, dw, dh),
                                        Color.White);
                                }
                            break;

                        case "center":
                            int cx = px + (pw - bgTex.Width) / 2 + ox;
                            int cy = drawY + (ph - bgTex.Height) / 2 + oy;
                            spriteBatch.Draw(bgTex,
                                new Vector2(cx, cy),
                                Color.White);
                            break;
                    }
                }
            }

            // Draw foreground color on top (only for alive entities)
            if (obj.State.IsAlive && !string.IsNullOrEmpty(def.Visuals.Color))
            {
                var entityColor = Core.ColorHelper.FromHex(def.Visuals.Color);
                spriteBatch.FillRectangle(entityArea, entityColor);
            }

            // Dead entity without a dead texture: gray overlay fallback
            if (!obj.State.IsAlive && !_backgroundTextures.ContainsKey((obj.ItemId, "dead")))
            {
                spriteBatch.FillRectangle(entityArea, Color.DarkGray * 0.5f);
                continue;
            }

            if (!obj.State.IsAlive) continue;
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
    public void DrawObjectInfo(SpriteBatch spriteBatch, Point playerGridPos)
    {
        int proximity = GameConstants.ObjectInfoProximityTiles;
        int ts = GameConstants.TileSize;
        int playerHeight = ts; // player occupies 1 tile
        int maxHalfDisplay = playerHeight * 3;

        var font = FontManager.GetFont(GameConstants.ObjectInfoFontSize);
        if (font == null) return;

        foreach (var obj in Objects)
        {
            // Skip objects with no HP unless they are interactable
            if (obj.Definition.Logic.MaxHealth <= 0 && !obj.Definition.Logic.IsInteractable) continue;

            // Proximity check (Chebyshev distance from player to nearest entity tile)
            int nearestX = Math.Clamp(playerGridPos.X, obj.TileX, obj.TileX + obj.EffectiveWidth - 1);
            int nearestY = Math.Clamp(playerGridPos.Y, obj.TileY, obj.TileY + obj.EffectiveHeight - 1);
            int dist = Math.Max(Math.Abs(playerGridPos.X - nearestX), Math.Abs(playerGridPos.Y - nearestY));
            if (dist > proximity) continue;

            // Entity pixel bounds
            int px = obj.TileX * ts;
            int py = obj.TileY * ts;
            int pw = obj.EffectiveWidth * ts;
            int ph = obj.EffectiveHeight * ts;
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
            string name = LocaleManager.Get("items", obj.ItemId, obj.Definition.Metadata.DisplayName);
            var textSize = font.MeasureString(name);
            int nameOffsetY = GameConstants.ObjectInfoNameOffsetY;
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
            if (obj.State.ShowDamageNumber)
            {
                float progress = obj.State.DamageNumberProgress;
                float alpha = 1f - progress;
                float floatUp = progress * 16f;

                bool isCrit = obj.State.LastDamageWasCrit;
                string dmgText = isCrit
                    ? $"{obj.State.LastDamageAmount}!"
                    : obj.State.LastDamageAmount.ToString();

                // Crit: larger font + light red; Normal: same font + red
                var dmgFont = isCrit
                    ? FontManager.GetFont(GameConstants.ObjectInfoFontSize + 8)
                    : font;
                Color dmgColor = isCrit
                    ? new Color(255, 120, 120)   // light red
                    : Color.Red;

                if (dmgFont != null)
                {
                    var dmgSize = dmgFont.MeasureString(dmgText);
                    float dmgX = entityCenterX - dmgSize.X / 2f;
                    float dmgY = textY - dmgSize.Y - 2 - floatUp;

                    dmgFont.DrawText(spriteBatch, dmgText,
                        new Vector2(dmgX + 1, dmgY + 1),
                        Color.Black * (alpha * 0.6f));
                    dmgFont.DrawText(spriteBatch, dmgText,
                        new Vector2(dmgX, dmgY),
                        dmgColor * alpha);
                }
            }

            // --- Draw HP bar below entity (skip for objects with no HP) ---
            if (obj.Definition.Logic.MaxHealth <= 0) continue;
            int barW = GameConstants.ObjectInfoHpBarWidth;
            int barH = GameConstants.ObjectInfoHpBarHeight;
            int barOffsetY = GameConstants.ObjectInfoHpBarOffsetY;
            int barX = entityCenterX - barW / 2;
            int barY2 = hpBarY + barOffsetY;

            // Background (dark)
            spriteBatch.FillRectangle(new Rectangle(barX, barY2, barW, barH), Color.Black * 0.6f);

            // Foreground (green → red based on HP ratio)
            float hpRatio = (float)obj.State.CurrentHp / obj.State.MaxHp;
            hpRatio = Math.Clamp(hpRatio, 0f, 1f);
            int fillW = (int)(barW * hpRatio);
            Color barColor = hpRatio > 0.5f
                ? Color.Lerp(Color.Yellow, Color.LimeGreen, (hpRatio - 0.5f) * 2f)
                : Color.Lerp(Color.Red, Color.Yellow, hpRatio * 2f);

            if (fillW > 0)
                spriteBatch.FillRectangle(new Rectangle(barX, barY2, fillW, barH), barColor);

            // --- Draw HP text below the bar: "current / max" ---
            var hpFont = FontManager.GetFont(GameConstants.ObjectInfoHpFontSize);
            if (hpFont != null)
            {
                string hpText = $"{obj.State.CurrentHp} / {obj.State.MaxHp}";
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
