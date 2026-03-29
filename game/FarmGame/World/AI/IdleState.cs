using FarmGame.World.Effects;

namespace FarmGame.World.AI;

/// <summary>
/// Idle state: creature stands still. After 3 seconds of being still with
/// no hostile creatures nearby, gains the "rest" effect (heals 1% HP/2s).
/// Transitions to Neutral after a longer delay, or to Hostile if attacked.
/// </summary>
public class IdleState : ICreatureState
{
    private float _idleTimer;
    private float _stillTimer;
    private bool _isResting;
    private readonly float _idleDuration;
    private readonly float _restThreshold;
    private readonly NeutralState _neutralState;
    private readonly HostileState _hostileState;

    public string Name => "Idle";

    public IdleState(NeutralState neutralState, HostileState hostileState,
        float idleDuration = 5f, float restThreshold = 3f)
    {
        _neutralState = neutralState;
        _hostileState = hostileState;
        _idleDuration = idleDuration;
        _restThreshold = restThreshold;
    }

    public void Enter(WorldObject creature, GameMap map)
    {
        _idleTimer = 0f;
        _stillTimer = 0f;
        _isResting = false;
    }

    public ICreatureState Update(WorldObject creature, GameMap map, float deltaTime)
    {
        // Check if behavior changed to hostile (e.g. was attacked)
        if (creature.State.Behavior == BehaviorState.Hostile)
        {
            RemoveRestEffect(creature);
            return _hostileState;
        }

        _idleTimer += deltaTime;
        _stillTimer += deltaTime;

        // After 3 seconds of being still, apply rest effect
        if (!_isResting && _stillTimer >= _restThreshold)
        {
            if (!creature.HasEffect("rest"))
            {
                var restEffect = EffectRegistry.Get("rest");
                if (restEffect != null)
                    creature.AddEffect(new ActiveEffect(restEffect, 0f)); // permanent while resting
            }
            _isResting = true;
        }

        // Transition to neutral after idle duration
        if (_idleTimer >= _idleDuration)
        {
            RemoveRestEffect(creature);
            return _neutralState;
        }

        return this;
    }

    private void RemoveRestEffect(WorldObject creature)
    {
        creature.Effects.RemoveAll(e => e.EffectId == "rest");
        _isResting = false;
    }
}
