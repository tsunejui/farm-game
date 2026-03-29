using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using FarmGame.Core;
using FarmGame.Entities.Actions;
using FarmGame.Entities.Actions.Player;
using FarmGame.Persistence.Models;
using FarmGame.World;

namespace FarmGame.Entities;

// =============================================================================
// Player.cs — Player entity coordinator
//
// Orchestrates all player actions and renders the player body.
// Each action (movement, jump, attack) is a separate IPlayerAction
// that handles its own update logic and visual effects.
//
// Functions:
//   - Player()                : Constructor. Creates all actions and registers them in the update loop.
//   - Update()                : Per-frame update. Delegates to each action's Update via the IPlayerAction interface.
//   - Draw()                  : Per-frame render. Draws action effects first, then body and direction indicator on top.
//   - DrawBody()              : Renders the player body as a colored rectangle inset from the tile edge by body_padding.
//   - DrawDirectionIndicator(): Renders a small white square on the edge of the body indicating the facing direction.
// =============================================================================
public class Player
{
    private readonly MovementAction _movement;
    private readonly JumpAction _jump;
    private readonly AttackAction _attack;
    private readonly IPlayerAction[] _actions;

    public Vector2 PixelPosition => _movement.PixelPosition;
    public Point GridPosition => _movement.GridPosition;
    public Direction FacingDirection => _movement.FacingDirection;

    // Primary attributes
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public bool IsAlive => CurrentHp > 0;
    public float Strength { get; set; }
    public float Dexterity { get; set; }
    public float WeaponAtk { get; set; }
    public float BuffPercent { get; set; }
    public float CritRate { get; set; }
    public float CritDamage { get; set; }

    // Callback when interacting with an interactable object
    public Action<WorldObject> OnInteract { get; set; }

    public Player(Point startPosition, GameMap tileMap, Direction facingDirection = Direction.Down)
    {
        _movement = new MovementAction(startPosition, tileMap, facingDirection);
        _jump = new JumpAction();
        _attack = new AttackAction(tileMap, () => _movement.GridPosition, () => _movement.FacingDirection,
            () => this, obj => OnInteract?.Invoke(obj));
        _actions = new IPlayerAction[] { _movement, _jump, _attack };

        // Initialize from config defaults
        MaxHp = GameConstants.PlayerMaxHp;
        CurrentHp = MaxHp;
        Strength = GameConstants.PlayerStrength;
        Dexterity = GameConstants.PlayerDexterity;
        WeaponAtk = GameConstants.PlayerWeaponAtk;
        BuffPercent = GameConstants.PlayerBuffPercent;
        CritRate = GameConstants.PlayerCritRate;
        CritDamage = GameConstants.PlayerCritDamage;
    }

    // Restore attributes from saved state
    public void RestoreAttributes(PlayerState savedState)
    {
        if (savedState == null) return;
        MaxHp = savedState.MaxHp > 0 ? savedState.MaxHp : GameConstants.PlayerMaxHp;
        CurrentHp = savedState.CurrentHp > 0 ? savedState.CurrentHp : MaxHp;
        Strength = savedState.Strength;
        Dexterity = savedState.Dexterity;
        WeaponAtk = savedState.WeaponAtk;
        BuffPercent = savedState.BuffPercent;
        CritRate = savedState.CritRate;
        CritDamage = savedState.CritDamage;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboard = KeyboardExtended.GetState();

        foreach (var action in _actions)
            action.Update(deltaTime, keyboard);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var context = new ActionDrawContext
        {
            SpriteBatch = spriteBatch,
            PixelPosition = _movement.PixelPosition,
            FacingDirection = _movement.FacingDirection,
            YOffset = _jump.Offset,
        };

        // Action effects drawn before body (e.g. shadow under player)
        foreach (var action in _actions)
            action.Draw(context);

        // Body and direction indicator on top
        DrawBody(spriteBatch);
        DrawDirectionIndicator(spriteBatch);
        DrawHpBar(spriteBatch);
    }

    private void DrawBody(SpriteBatch spriteBatch)
    {
        int pad = GameConstants.PlayerBodyPadding;
        int bodyW = GameConstants.TileSize - pad * 2;
        int bodyH = GameConstants.TileSize - pad * 2;
        int baseX = (int)_movement.PixelPosition.X + pad;
        int baseY = (int)_movement.PixelPosition.Y + pad;

        var bodyRect = new Rectangle(baseX, baseY + (int)_jump.Offset, bodyW, bodyH);
        spriteBatch.FillRectangle(bodyRect, GameConstants.PlayerColor);
    }

    private void DrawDirectionIndicator(SpriteBatch spriteBatch)
    {
        int sz = GameConstants.PlayerIndicatorSize;
        int pad = GameConstants.PlayerBodyPadding;
        int half = sz / 2;
        int yOffset = (int)_jump.Offset;
        int cx = (int)_movement.PixelPosition.X + GameConstants.TileSize / 2 - half;
        int cy = (int)_movement.PixelPosition.Y + GameConstants.TileSize / 2 - half + yOffset;

        Rectangle rect = _movement.FacingDirection switch
        {
            Direction.Up => new Rectangle(cx, (int)_movement.PixelPosition.Y + pad + yOffset, sz, sz),
            Direction.Down => new Rectangle(cx, (int)_movement.PixelPosition.Y + GameConstants.TileSize - pad - sz + yOffset, sz, sz),
            Direction.Left => new Rectangle((int)_movement.PixelPosition.X + pad, cy, sz, sz),
            Direction.Right => new Rectangle((int)_movement.PixelPosition.X + GameConstants.TileSize - pad - sz, cy, sz, sz),
            _ => new Rectangle(cx, cy, sz, sz),
        };

        spriteBatch.FillRectangle(rect, Color.White);
    }

    private void DrawHpBar(SpriteBatch spriteBatch)
    {
        int ts = GameConstants.TileSize;
        int barW = GameConstants.ObjectInfoHpBarWidth;
        int barH = GameConstants.ObjectInfoHpBarHeight;
        int yOffset = (int)_jump.Offset;

        // Center below the player tile
        int centerX = (int)_movement.PixelPosition.X + ts / 2;
        int bottomY = (int)_movement.PixelPosition.Y + ts + yOffset + 2;
        int barX = centerX - barW / 2;

        // Background
        spriteBatch.FillRectangle(new Rectangle(barX, bottomY, barW, barH), Color.Black * 0.6f);

        // Fill
        float ratio = Math.Clamp((float)CurrentHp / MaxHp, 0f, 1f);
        int fillW = (int)(barW * ratio);
        Color barColor = ratio > 0.5f
            ? Color.Lerp(Color.Yellow, Color.LimeGreen, (ratio - 0.5f) * 2f)
            : Color.Lerp(Color.Red, Color.Yellow, ratio * 2f);
        if (fillW > 0)
            spriteBatch.FillRectangle(new Rectangle(barX, bottomY, fillW, barH), barColor);

        // HP text
        var hpFont = FontManager.GetFont(GameConstants.ObjectInfoHpFontSize);
        if (hpFont != null)
        {
            string hpText = $"{CurrentHp} / {MaxHp}";
            var textSize = hpFont.MeasureString(hpText);
            float textX = centerX - textSize.X / 2f;
            float textY = bottomY + barH + 1;
            hpFont.DrawText(spriteBatch, hpText,
                new Vector2(textX + 1, textY + 1), Color.Black * 0.5f);
            hpFont.DrawText(spriteBatch, hpText,
                new Vector2(textX, textY), Color.White * 0.9f);
        }
    }
}
