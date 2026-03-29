using System;

namespace FarmGame.World.AI;

/// <summary>
/// Neutral state: creature wanders randomly at slow speed.
/// Transitions to Idle periodically, or to Hostile if attacked.
/// </summary>
public class NeutralState : ICreatureState
{
    private float _moveTimer;
    private float _wanderDuration;
    private int _dirX, _dirY;
    private readonly Random _rng = new();

    // Shared state references (set after construction to break circular deps)
    public IdleState IdleState { get; set; }
    public HostileState HostileState { get; set; }

    public string Name => "Neutral";

    public void Enter(WorldObject creature, GameMap map)
    {
        _moveTimer = 0f;
        _wanderDuration = 1.5f + (float)_rng.NextDouble() * 2f;
        PickRandomDirection();
    }

    public ICreatureState Update(WorldObject creature, GameMap map, float deltaTime)
    {
        // Check if behavior changed to hostile
        if (creature.State.Behavior == BehaviorState.Hostile)
            return HostileState;

        _moveTimer += deltaTime;

        // Move at creature's configured speed
        float speed = creature.Definition.Logic.MoveSpeed;
        if (speed > 0f)
        {
            float moveInterval = 1f / speed;
            if (_moveTimer >= moveInterval)
            {
                _moveTimer -= moveInterval;

                int newX = creature.TileX + _dirX;
                int newY = creature.TileY + _dirY;

                if (!map.MoveObject(creature, newX, newY))
                    PickRandomDirection();
            }
        }

        // After wandering for a while, go idle
        if (_moveTimer >= _wanderDuration || _wanderDuration <= 0)
        {
            return IdleState;
        }

        return this;
    }

    private void PickRandomDirection()
    {
        int dir = _rng.Next(4);
        _dirX = dir switch { 0 => -1, 1 => 1, _ => 0 };
        _dirY = dir switch { 2 => -1, 3 => 1, _ => 0 };
    }
}
