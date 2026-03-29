// =============================================================================
// AttackAction.cs — Player attack action with object damage
//
// Triggered by Z key. Checks the tile in front of the player:
//   - Interactable objects: skip attack, trigger interaction callback
//   - Invincible objects: skip damage entirely
//   - Normal objects: run damage pipeline, apply knockback
// =============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using Serilog;
using FarmGame.Combat;
using FarmGame.Core;
using FarmGame.World;

namespace FarmGame.Entities.Actions.Player;

public class AttackAction : IPlayerAction
{
    private readonly GameMap _map;
    private readonly Func<Point> _getGridPosition;
    private readonly Func<Direction> _getFacingDirection;
    private readonly Action<WorldObject> _onInteract;
    private float _attackProgress;
    private bool _hasDealtDamage;

    public bool IsActive { get; private set; }
    public float Progress => _attackProgress;

    public AttackAction(GameMap map, Func<Point> getGridPosition, Func<Direction> getFacingDirection,
        Action<WorldObject> onInteract = null)
    {
        _map = map;
        _getGridPosition = getGridPosition;
        _getFacingDirection = getFacingDirection;
        _onInteract = onInteract;
    }

    public void Update(float deltaTime, KeyboardStateExtended keyboard)
    {
        if (!IsActive && keyboard.WasKeyPressed(Keys.Z))
        {
            // Check if the facing object is interactable — if so, interact instead of attack
            var facingObj = GetFacingObject();
            if (facingObj != null && facingObj.Definition.Logic.IsInteractable)
            {
                _onInteract?.Invoke(facingObj);
                return;
            }

            IsActive = true;
            _attackProgress = 0f;
            _hasDealtDamage = false;
        }

        if (IsActive)
        {
            _attackProgress += deltaTime / GameConstants.PlayerAttackDuration;

            if (!_hasDealtDamage && _attackProgress >= 0.3f)
            {
                _hasDealtDamage = true;
                DealDamageToObjectsInRange();
            }

            if (_attackProgress >= 1f)
            {
                Reset();
            }
        }
    }

    public void Reset()
    {
        _attackProgress = 0f;
        IsActive = false;
        _hasDealtDamage = false;
    }

    public void Draw(ActionDrawContext context)
    {
        if (!IsActive) return;

        float alpha = 1f - _attackProgress;
        float scale = 0.5f + 0.5f * _attackProgress;

        int range = (int)(GameConstants.PlayerAttackRange * scale);
        int width = (int)(GameConstants.PlayerAttackWidth * scale);
        int centerX = (int)context.PixelPosition.X + GameConstants.TileSize / 2;
        int centerY = (int)context.PixelPosition.Y + GameConstants.TileSize / 2 + (int)context.YOffset;
        int halfTile = GameConstants.TileSize / 2;

        Rectangle effectRect = context.FacingDirection switch
        {
            Direction.Up => new Rectangle(centerX - width / 2, centerY - halfTile - range, width, range),
            Direction.Down => new Rectangle(centerX - width / 2, centerY + halfTile, width, range),
            Direction.Left => new Rectangle(centerX - halfTile - range, centerY - width / 2, range, width),
            Direction.Right => new Rectangle(centerX + halfTile, centerY - width / 2, range, width),
            _ => Rectangle.Empty,
        };

        context.SpriteBatch.FillRectangle(effectRect, GameConstants.PlayerAttackColor * alpha);
    }

    private WorldObject GetFacingObject()
    {
        var pos = _getGridPosition();
        var dir = _getFacingDirection();
        Point target = dir switch
        {
            Direction.Up => new Point(pos.X, pos.Y - 1),
            Direction.Down => new Point(pos.X, pos.Y + 1),
            Direction.Left => new Point(pos.X - 1, pos.Y),
            Direction.Right => new Point(pos.X + 1, pos.Y),
            _ => pos,
        };
        return _map.GetObjectAt(target.X, target.Y);
    }

    private void DealDamageToObjectsInRange()
    {
        var obj = GetFacingObject();
        if (obj == null) return;
        if (obj.State.Faction == Faction.Friendly) return;

        // Invincible objects take no damage
        if (obj.Definition.Logic.IsInvincible) return;

        // Deal damage to alive objects with HP
        if (obj.State.IsAlive && obj.Definition.Logic.MaxHealth > 0)
        {
            var ctx = new DamageContext
            {
                Strength = GameConstants.PlayerStrength,
                Dexterity = GameConstants.PlayerDexterity,
                WeaponAtk = GameConstants.PlayerWeaponAtk,
                BuffPercent = GameConstants.PlayerBuffPercent,
                SkillPowerPercent = 1f,
                CritRate = GameConstants.PlayerCritRate,
                CritDamageMultiplier = GameConstants.PlayerCritDamage,
                TargetDefense = obj.Definition.Logic.Defense,
            };

            int damage = DamagePipeline.CalculateDamage(ctx);
            obj.State.TakeDamage(damage, ctx.IsCritical);

            Log.Debug("Attack: {ItemId} took {Damage} damage{Crit}, hp={Hp}/{MaxHp}",
                obj.ItemId, damage, ctx.IsCritical ? " (CRIT!)" : "",
                obj.State.CurrentHp, obj.State.MaxHp);
        }

        // Knockback
        if (obj.Definition.Physics.IsKnockbackable)
        {
            var dir = _getFacingDirection();
            int kb = GameConstants.KnockbackTiles;
            Point knockDir = dir switch
            {
                Direction.Up => new Point(0, -kb),
                Direction.Down => new Point(0, kb),
                Direction.Left => new Point(-kb, 0),
                Direction.Right => new Point(kb, 0),
                _ => Point.Zero,
            };

            int newX = obj.TileX + knockDir.X;
            int newY = obj.TileY + knockDir.Y;
            if (_map.MoveObject(obj, newX, newY))
            {
                if (obj.State.IsAlive)
                    obj.State.TriggerBounce();
                Log.Debug("Knockback: {ItemId} pushed to ({X},{Y})", obj.ItemId, newX, newY);
            }
        }
    }
}
