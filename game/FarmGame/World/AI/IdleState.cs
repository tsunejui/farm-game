namespace FarmGame.World.AI;

/// <summary>
/// Idle state: creature stands still. Transitions to Neutral after a delay,
/// or to Hostile if behavior is set to hostile (e.g. after being attacked).
/// </summary>
public class IdleState : ICreatureState
{
    private float _idleTimer;
    private readonly float _idleDuration;
    private readonly NeutralState _neutralState;
    private readonly HostileState _hostileState;

    public string Name => "Idle";

    public IdleState(NeutralState neutralState, HostileState hostileState, float idleDuration = 2f)
    {
        _neutralState = neutralState;
        _hostileState = hostileState;
        _idleDuration = idleDuration;
    }

    public void Enter(WorldObject creature, GameMap map)
    {
        _idleTimer = 0f;
    }

    public ICreatureState Update(WorldObject creature, GameMap map, float deltaTime)
    {
        // Check if behavior changed to hostile (e.g. was attacked)
        if (creature.State.Behavior == BehaviorState.Hostile)
            return _hostileState;

        _idleTimer += deltaTime;
        if (_idleTimer >= _idleDuration)
            return _neutralState;

        return this;
    }
}
