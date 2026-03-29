using System;
using FarmGame.World.Events;
using Serilog;

namespace FarmGame.World.AI;

/// <summary>
/// Hostile state: creature actively chases and attacks the player.
/// Moves toward the player at increased speed. Attacks when adjacent.
/// </summary>
public class HostileState : ICreatureState
{
    private float _moveTimer;
    private float _attackCooldownTimer;

    public string Name => "Hostile";

    public void Enter(WorldObject creature, GameMap map)
    {
        _moveTimer = 0f;
        _attackCooldownTimer = 0f;
    }

    public ICreatureState Update(WorldObject creature, GameMap map, float deltaTime)
    {
        var player = map.PlayerProxy;
        if (player == null) return this;

        float speed = creature.Definition.Logic.MoveSpeed
            * creature.Definition.Logic.HostileSpeedMultiplier;
        int attackRange = creature.Definition.Logic.AttackRange;

        // Calculate distance to player (Chebyshev)
        int dx = player.TileX - creature.TileX;
        int dy = player.TileY - creature.TileY;
        int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));

        // Attack if within range and cooldown is ready
        _attackCooldownTimer -= deltaTime;
        if (dist <= attackRange && _attackCooldownTimer <= 0f)
        {
            AttackPlayer(creature, map);
            _attackCooldownTimer = creature.Definition.Logic.AttackCooldown;
            return this;
        }

        // Chase: move toward player
        if (speed > 0f && dist > attackRange)
        {
            float moveInterval = 1f / speed;
            _moveTimer += deltaTime;

            if (_moveTimer >= moveInterval)
            {
                _moveTimer -= moveInterval;

                // Pick the axis with the larger distance to close
                int stepX = 0, stepY = 0;
                if (Math.Abs(dx) >= Math.Abs(dy))
                    stepX = Math.Sign(dx);
                else
                    stepY = Math.Sign(dy);

                int newX = creature.TileX + stepX;
                int newY = creature.TileY + stepY;

                // Try primary direction, then the other axis
                if (!map.MoveObject(creature, newX, newY))
                {
                    // Try alternate axis
                    if (stepX != 0 && dy != 0)
                        map.MoveObject(creature, creature.TileX, creature.TileY + Math.Sign(dy));
                    else if (stepY != 0 && dx != 0)
                        map.MoveObject(creature, creature.TileX + Math.Sign(dx), creature.TileY);
                }
            }
        }

        return this;
    }

    private void AttackPlayer(WorldObject creature, GameMap map)
    {
        var player = map.PlayerProxy;
        if (player == null || !player.State.IsAlive) return;

        int damage = creature.Definition.Logic.AttackDamage;
        if (damage <= 0) return;

        // Apply damage to player proxy
        player.State.TakeDamage(damage);

        Log.Debug("Creature {Id} attacks player for {Damage} damage",
            creature.ItemId, damage);
    }
}
