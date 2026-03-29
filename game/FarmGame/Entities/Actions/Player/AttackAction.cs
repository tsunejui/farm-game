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
using FarmGame.Core;
using FarmGame.World;

namespace FarmGame.Entities.Actions.Player;

public class AttackAction : IPlayerAction
{
    private static readonly Random Rng = new();

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

        // Only damage non-friendly, alive entities with HP > 0
        if (entity.State.Faction == Faction.Friendly) return;
        if (!entity.State.IsAlive) return;
        if (entity.Definition.Logic.MaxHealth <= 0) return;

        int damage = Rng.Next(GameConstants.DefaultMinDamage, GameConstants.DefaultMaxDamage + 1);
        entity.State.TakeDamage(damage);
    }
}
