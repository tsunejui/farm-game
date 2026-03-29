// =============================================================================
// AttackAction.cs — Player attack action with entity damage
//
// Triggered by Z key. Plays a directional visual effect and checks for
// entities in the attack area. Damages non-friendly entities with a random
// amount distributed over DamageTickDurationMs.
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
    private float _attackProgress;
    private bool _hasDealtDamage;

    public bool IsActive { get; private set; }
    public float Progress => _attackProgress;

    public AttackAction(GameMap map, Func<Point> getGridPosition, Func<Direction> getFacingDirection)
    {
        _map = map;
        _getGridPosition = getGridPosition;
        _getFacingDirection = getFacingDirection;
    }

    public void Update(float deltaTime, KeyboardStateExtended keyboard)
    {
        if (!IsActive && keyboard.WasKeyPressed(Keys.Z))
        {
            IsActive = true;
            _attackProgress = 0f;
            _hasDealtDamage = false;
        }

        if (IsActive)
        {
            _attackProgress += deltaTime / GameConstants.PlayerAttackDuration;

            // Deal damage once at ~30% into the animation (impact frame)
            if (!_hasDealtDamage && _attackProgress >= 0.3f)
            {
                _hasDealtDamage = true;
                DealDamageToEntitiesInRange();
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

    private void DealDamageToEntitiesInRange()
    {
        var pos = _getGridPosition();
        var dir = _getFacingDirection();

        // Check the tile in front of the player
        Point target = dir switch
        {
            Direction.Up => new Point(pos.X, pos.Y - 1),
            Direction.Down => new Point(pos.X, pos.Y + 1),
            Direction.Left => new Point(pos.X - 1, pos.Y),
            Direction.Right => new Point(pos.X + 1, pos.Y),
            _ => pos,
        };

        var entity = _map.GetEntityAt(target.X, target.Y);
        if (entity == null) return;
        if (entity.State.Faction == Faction.Friendly) return;

        // Deal damage to alive entities with HP
        if (entity.State.IsAlive && entity.Definition.Logic.MaxHealth > 0)
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
                TargetDefense = entity.Definition.Logic.Defense,
            };

            int damage = DamagePipeline.CalculateDamage(ctx);
            entity.State.TakeDamage(damage, ctx.IsCritical);

            Log.Debug("Attack: {ItemId} took {Damage} damage{Crit}, hp={Hp}/{MaxHp}",
                entity.ItemId, damage, ctx.IsCritical ? " (CRIT!)" : "",
                entity.State.CurrentHp, entity.State.MaxHp);
        }

        // Knockback: push entity regardless of alive/dead
        if (entity.Definition.Physics.IsKnockbackable)
        {
            int kb = GameConstants.KnockbackTiles;
            Point knockDir = dir switch
            {
                Direction.Up => new Point(0, -kb),
                Direction.Down => new Point(0, kb),
                Direction.Left => new Point(-kb, 0),
                Direction.Right => new Point(kb, 0),
                _ => Point.Zero,
            };

            int newX = entity.TileX + knockDir.X;
            int newY = entity.TileY + knockDir.Y;
            if (_map.MoveEntity(entity, newX, newY))
                Log.Debug("Knockback: {ItemId} pushed to ({X},{Y})", entity.ItemId, newX, newY);
        }
    }
}
